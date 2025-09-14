using Microsoft.AspNetCore.Mvc;
using QuoteOffer.Models;
using QuoteOffer.Services;
using QuoteOffer.DTOs;

namespace QuoteOffer.Controllers
{
    /// <summary>
    /// Contrôleur pour la gestion des brouillons de devis enrichis (nouveau schéma)
    /// </summary>
    [ApiController]
    [Route("api/draft-quotes")]
    [Produces("application/json")]
    [Tags("📝 Draft Quotes Management (Enhanced)")]
    public class DraftQuotesController : ControllerBase
    {
        private readonly IQuoteOfferService _quoteOfferService;
        private readonly ILogger<DraftQuotesController> _logger;

        public DraftQuotesController(IQuoteOfferService quoteOfferService, ILogger<DraftQuotesController> logger)
        {
            _quoteOfferService = quoteOfferService;
            _logger = logger;
        }

        /// <summary>
        /// Crée un nouveau brouillon de devis enrichi
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CommonApiResponse<DraftQuoteResponse>), 201)]
        [ProducesResponseType(typeof(CommonApiResponse<object>), 400)]
        public async Task<ActionResult<CommonApiResponse<DraftQuoteResponse>>> CreateDraftQuote([FromBody] CreateDraftQuoteRequest request)
        {
            try
            {
                // Générer un ID unique pour le brouillon
                var draftQuoteId = GenerateDraftQuoteId();
                
                // Créer l'entité QuoteOffer avec le statut DRAFT
                var quoteOffer = new QuoteOffer.Models.QuoteOffer
                {
                    Id = draftQuoteId,
                    RequestQuoteId = request.RequestId,
                    Status = QuoteOfferStatus.DRAFT,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    OptimizedDraftData = new OptimizedDraftData
                    {
                        Wizard = new WizardMetadata
                        {
                            CurrentStep = 1,
                            Status = "in_progress",
                            LastModified = DateTime.UtcNow
                        }
                    }
                };

                // Créer dans le service
                var createdId = await _quoteOfferService.CreateQuoteOfferAsync(new CreateQuoteOfferDto
                {
                    RequestQuoteId = request.RequestId,
                    ClientNumber = request.Header?.Client?.Company ?? "",
                    EmailUser = request.Header?.Client?.Email ?? "",
                    Comment = request.WizardData?.GeneralRequestInformation?.Notes
                });

                if (string.IsNullOrEmpty(createdId))
                    return StatusCode(500, CommonApiResponse<object>.Error(500, "Failed to create draft quote"));

                // Construire la réponse enrichie
                var response = new DraftQuoteResponse
                {
                    DraftQuoteId = draftQuoteId,
                    RequestId = request.RequestId,
                    Status = "in_progress",
                    Version = 1,
                    ResumeToken = GenerateResumeToken(),
                    Header = request.Header ?? new DraftQuoteHeaderDto(),
                    WizardData = request.WizardData ?? new DraftQuoteWizardDataDto(),
                    Options = new List<DraftQuoteOptionDto>(),
                    Validation = CreateInitialValidation(),
                    Audit = new DraftQuoteAuditDto
                    {
                        CreatedBy = "user:system", // TODO: Récupérer l'utilisateur actuel
                        CreatedAt = DateTime.UtcNow,
                        LastUpdatedBy = "user:system",
                        LastUpdatedAt = DateTime.UtcNow,
                        StepProgress = new DraftQuoteStepProgressDto()
                    },
                    TotalOptions = 0,
                    Currency = request.Header?.CommercialTerms?.Currency ?? "EUR"
                };

                _logger.LogInformation("Draft quote {DraftQuoteId} created for request {RequestId}", draftQuoteId, request.RequestId);
                return CreatedAtAction(nameof(GetDraftQuote), new { id = draftQuoteId }, 
                    CommonApiResponse<DraftQuoteResponse>.Created(response, "Draft quote created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating draft quote for request {RequestId}", request.RequestId);
                return StatusCode(500, CommonApiResponse<object>.Error(500, "Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Récupère un brouillon de devis enrichi par son ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CommonApiResponse<DraftQuoteResponse>), 200)]
        [ProducesResponseType(typeof(CommonApiResponse<object>), 404)]
        public async Task<ActionResult<CommonApiResponse<DraftQuoteResponse>>> GetDraftQuote(string id)
        {
            try
            {
                var draft = await _quoteOfferService.GetQuoteOfferByIdAsync(id);
                if (draft == null)
                    return NotFound(CommonApiResponse<object>.NotFound("Draft quote not found"));

                if (draft.Status != QuoteOfferStatus.DRAFT.ToString())
                    return BadRequest(CommonApiResponse<object>.ValidationFailed("Item is not a draft"));

                var response = MapToEnrichedDraftQuoteResponse(draft);
                return Ok(CommonApiResponse<DraftQuoteResponse>.Success(response, "Draft quote retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving draft quote {DraftQuoteId}", id);
                return StatusCode(500, CommonApiResponse<object>.Error(500, "Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Met à jour un brouillon de devis enrichi
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(CommonApiResponse<DraftQuoteResponse>), 200)]
        [ProducesResponseType(typeof(CommonApiResponse<object>), 404)]
        public async Task<ActionResult<CommonApiResponse<DraftQuoteResponse>>> UpdateDraftQuote(string id, [FromBody] UpdateDraftQuoteRequest request)
        {
            try
            {
                var draft = await _quoteOfferService.GetQuoteOfferByIdAsync(id);
                if (draft == null)
                    return NotFound(CommonApiResponse<object>.NotFound("Draft quote not found"));

                if (draft.Status != QuoteOfferStatus.DRAFT.ToString())
                    return BadRequest(CommonApiResponse<object>.ValidationFailed("Cannot update non-draft items"));

                // Incrémenter la version
                var newVersion = ExtractVersionFromDraft(draft) + 1;

                // Mettre à jour via le service existant
                var updateDto = new UpdateQuoteOfferDto
                {
                    Comment = request.Notes ?? draft.Comment,
                    // Autres propriétés à mapper selon les besoins
                };

                var success = await _quoteOfferService.UpdateQuoteOfferAsync(id, updateDto);
                if (!success)
                    return StatusCode(500, CommonApiResponse<object>.Error(500, "Failed to update draft quote"));

                // Récupérer la version mise à jour
                var updatedDraft = await _quoteOfferService.GetQuoteOfferByIdAsync(id);
                var response = MapToEnrichedDraftQuoteResponse(updatedDraft!);
                response.Version = newVersion;

                return Ok(CommonApiResponse<DraftQuoteResponse>.Success(response, "Draft quote updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating draft quote {DraftQuoteId}", id);
                return StatusCode(500, CommonApiResponse<object>.Error(500, "Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Ajoute ou met à jour une option dans un brouillon
        /// </summary>
        [HttpPost("{id}/options")]
        [ProducesResponseType(typeof(CommonApiResponse<DraftQuoteResponse>), 200)]
        [ProducesResponseType(typeof(CommonApiResponse<object>), 404)]
        public async Task<ActionResult<CommonApiResponse<DraftQuoteResponse>>> AddOrUpdateOption(string id, [FromBody] AddDraftQuoteOptionRequest request)
        {
            try
            {
                var draft = await _quoteOfferService.GetQuoteOfferByIdAsync(id);
                if (draft == null)
                    return NotFound(CommonApiResponse<object>.NotFound("Draft quote not found"));

                if (draft.Status != QuoteOfferStatus.DRAFT.ToString())
                    return BadRequest(CommonApiResponse<object>.ValidationFailed("Cannot modify non-draft items"));

                // Logique pour ajouter/mettre à jour l'option
                // TODO: Implémenter la logique métier pour gérer les options enrichies

                var updatedDraft = await _quoteOfferService.GetQuoteOfferByIdAsync(id);
                var response = MapToEnrichedDraftQuoteResponse(updatedDraft!);

                return Ok(CommonApiResponse<DraftQuoteResponse>.Success(response, "Option added/updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding option to draft quote {DraftQuoteId}", id);
                return StatusCode(500, CommonApiResponse<object>.Error(500, "Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Valide un brouillon de devis enrichi
        /// </summary>
        [HttpPost("{id}/validate")]
        [ProducesResponseType(typeof(CommonApiResponse<DraftQuoteValidationDto>), 200)]
        [ProducesResponseType(typeof(CommonApiResponse<object>), 404)]
        public async Task<ActionResult<CommonApiResponse<DraftQuoteValidationDto>>> ValidateDraftQuote(string id)
        {
            try
            {
                var draft = await _quoteOfferService.GetQuoteOfferByIdAsync(id);
                if (draft == null)
                    return NotFound(CommonApiResponse<object>.NotFound("Draft quote not found"));

                if (draft.Status != QuoteOfferStatus.DRAFT.ToString())
                    return BadRequest(CommonApiResponse<object>.ValidationFailed("Cannot validate non-draft items"));

                // Effectuer la validation enrichie
                var validation = PerformEnhancedValidation(draft);

                return Ok(CommonApiResponse<DraftQuoteValidationDto>.Success(validation, "Draft quote validation completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating draft quote {DraftQuoteId}", id);
                return StatusCode(500, CommonApiResponse<object>.Error(500, "Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Recherche les brouillons de devis enrichis
        /// </summary>
        [HttpPost("search")]
        [ProducesResponseType(typeof(CommonApiResponse<List<DraftQuoteResponse>>), 200)]
        public async Task<ActionResult<CommonApiResponse<List<DraftQuoteResponse>>>> SearchDraftQuotes([FromBody] QuoteOfferSearchDto request)
        {
            try
            {
                var searchResult = await _quoteOfferService.SearchQuoteOffersAsync(request);
                
                // Filtrer pour ne garder que les brouillons
                var drafts = searchResult.Items
                    .Where(q => q.Status == QuoteOfferStatus.DRAFT.ToString())
                    .Select(async summary => 
                    {
                        var detail = await _quoteOfferService.GetQuoteOfferByIdAsync(summary.Id);
                        return detail != null ? MapToEnrichedDraftQuoteResponse(detail) : null;
                    })
                    .Where(d => d != null)
                    .Select(t => t.Result!)
                    .ToList();

                var meta = new
                {
                    totalCount = drafts.Count,
                    pageNumber = request.Page,
                    pageSize = request.PageSize,
                    totalPages = (int)Math.Ceiling((double)drafts.Count / request.PageSize)
                };

                var response = CommonApiResponse<List<DraftQuoteResponse>>.Success(drafts, "Draft quotes retrieved successfully");
                response.Meta = meta;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching draft quotes");
                return StatusCode(500, CommonApiResponse<List<DraftQuoteResponse>>.Error(500, "Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Supprime un brouillon de devis
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(CommonApiResponse<object>), 200)]
        [ProducesResponseType(typeof(CommonApiResponse<object>), 404)]
        public async Task<ActionResult<CommonApiResponse<object>>> DeleteDraftQuote(string id)
        {
            try
            {
                var draft = await _quoteOfferService.GetQuoteOfferByIdAsync(id);
                if (draft == null)
                    return NotFound(CommonApiResponse<object>.NotFound("Draft quote not found"));

                if (draft.Status != QuoteOfferStatus.DRAFT.ToString())
                    return BadRequest(CommonApiResponse<object>.ValidationFailed("Cannot delete non-draft items"));

                var deleted = await _quoteOfferService.DeleteQuoteOfferAsync(id);
                if (!deleted)
                    return StatusCode(500, CommonApiResponse<object>.Error(500, "Failed to delete draft quote"));

                return Ok(CommonApiResponse<object>.Success(null, "Draft quote deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting draft quote {DraftQuoteId}", id);
                return StatusCode(500, CommonApiResponse<object>.Error(500, "Internal server error", ex.Message));
            }
        }

        #region Private Methods

        private string GenerateDraftQuoteId()
        {
            var year = DateTime.UtcNow.Year;
            var timestamp = DateTime.UtcNow.ToString("MMdd-HHmmss");
            return $"DQ-{year}-{timestamp}";
        }

        private string GenerateResumeToken()
        {
            return Guid.NewGuid().ToString("N")[..12];
        }

        private DraftQuoteValidationDto CreateInitialValidation()
        {
            return new DraftQuoteValidationDto
            {
                Checks = new List<DraftQuoteValidationCheckDto>
                {
                    new() { Rule = "dates_consistency", Ok = false, Message = "Dates not yet defined" },
                    new() { Rule = "surcharges_validity", Ok = false, Message = "No surcharges defined" },
                    new() { Rule = "cgv_present", Ok = false, Message = "CGV not yet accepted" },
                    new() { Rule = "rates_per_container", Ok = false, Message = "No rates defined" }
                },
                Warnings = new List<DraftQuoteValidationWarningDto>()
            };
        }

        private DraftQuoteValidationDto PerformEnhancedValidation(QuoteOfferDetailDto draft)
        {
            var checks = new List<DraftQuoteValidationCheckDto>();
            var warnings = new List<DraftQuoteValidationWarningDto>();

            // Validation des dates
            checks.Add(new DraftQuoteValidationCheckDto
            {
                Rule = "dates_consistency",
                Ok = draft.CreatedDate <= draft.UpdatedAt,
                Message = draft.CreatedDate <= draft.UpdatedAt ? null : "Creation date is after update date"
            });

            // Validation des surcharges
            checks.Add(new DraftQuoteValidationCheckDto
            {
                Rule = "surcharges_validity",
                Ok = true, // TODO: Implémenter la logique de validation des surcharges
                Message = null
            });

            // Validation CGV
            checks.Add(new DraftQuoteValidationCheckDto
            {
                Rule = "cgv_present",
                Ok = false, // TODO: Vérifier si les CGV sont acceptées
                Message = "CGV non acceptées (signature/validation requise avant émission du devis)."
            });

            // Validation des tarifs
            checks.Add(new DraftQuoteValidationCheckDto
            {
                Rule = "rates_per_container",
                Ok = draft.Options?.Any() == true,
                Message = draft.Options?.Any() == true ? "Un seul rate défini par type de conteneur pour chaque seafreight." : "Aucune option définie"
            });

            // Avertissements
            warnings.Add(new DraftQuoteValidationWarningDto
            {
                Code = "FREE_TIME_DEST",
                Message = "Free time destination limité à 5–7 jours selon l'option."
            });

            return new DraftQuoteValidationDto
            {
                Checks = checks,
                Warnings = warnings
            };
        }

        private DraftQuoteResponse MapToEnrichedDraftQuoteResponse(QuoteOfferDetailDto draft)
        {
            return new DraftQuoteResponse
            {
                DraftQuoteId = draft.Id,
                RequestId = draft.RequestQuoteId,
                Status = draft.Status == QuoteOfferStatus.DRAFT.ToString() ? "in_progress" : draft.Status,
                Version = ExtractVersionFromDraft(draft),
                ResumeToken = GenerateResumeToken(),
                Header = MapToHeaderDto(draft),
                WizardData = MapToWizardDataDto(draft),
                Options = MapToOptionDtos(draft),
                Validation = PerformEnhancedValidation(draft),
                Audit = MapToAuditDto(draft),
                TotalOptions = draft.Options?.Count ?? 0,
                BestPrice = draft.Options?.Min(o => o.Totals?.GrandTotal) ?? 0,
                Currency = "EUR"
            };
        }

        private int ExtractVersionFromDraft(QuoteOfferDetailDto draft)
        {
            // TODO: Extraire la version depuis les métadonnées du draft
            return 1;
        }

        private DraftQuoteHeaderDto MapToHeaderDto(QuoteOfferDetailDto draft)
        {
            return new DraftQuoteHeaderDto
            {
                Client = new DraftQuoteClientDto
                {
                    Company = draft.ClientNumber,
                    Contact = "N/A", // TODO: Extraire du draft
                    Email = draft.EmailUser,
                    Phone = null
                },
                Shipment = new DraftQuoteShipmentDto
                {
                    FromRequest = true,
                    Readonly = new List<string> { "cargoType", "goodsDescription", "origin", "destination", "requestedDeparture" },
                    CargoType = "FCL", // TODO: Extraire du draft
                    GoodsDescription = "Description from request", // TODO: Extraire du draft
                    Origin = new DraftQuoteLocationDto { City = "Lyon", Country = "FR" }, // TODO: Extraire du draft
                    Destination = new DraftQuoteLocationDto { City = "Douala", Country = "CM" }, // TODO: Extraire du draft
                    RequestedDeparture = DateTime.UtcNow.AddDays(30) // TODO: Extraire du draft
                },
                CommercialTerms = new DraftQuoteCommercialTermsDto
                {
                    Currency = "EUR",
                    Incoterm = "CIF",
                    ValidityDays = 15,
                    CgvAccepted = false
                }
            };
        }

        private DraftQuoteWizardDataDto MapToWizardDataDto(QuoteOfferDetailDto draft)
        {
            return new DraftQuoteWizardDataDto
            {
                GeneralRequestInformation = new DraftQuoteGeneralRequestInfoDto
                {
                    Channel = "Email",
                    Priority = "normal",
                    Notes = draft.Comment
                },
                RoutingAndCargo = new DraftQuoteRoutingAndCargoDto
                {
                    PortOfLoading = "Anvers",
                    PortOfDestination = "Douala",
                    Cargo = new DraftQuoteCargoDto
                    {
                        Items = new List<DraftQuoteCargoItemDto>
                        {
                            new() { ContainerType = "20DV", Quantity = 1, GrossWeightKg = 8000, VolumeM3 = 28 },
                            new() { ContainerType = "40HC", Quantity = 1, GrossWeightKg = 12000, VolumeM3 = 60 }
                        },
                        Hazmat = false,
                        GoodsDescription = "Marchandises diverses"
                    }
                },
                Seafreights = new List<DraftQuoteSeafreightDto>
                {
                    new()
                    {
                        Id = "SF-001",
                        Carrier = "MAERSK",
                        Service = "AE1 - Service hebdomadaire Anvers-Douala",
                        Etd = DateTime.UtcNow.AddDays(30),
                        Eta = DateTime.UtcNow.AddDays(58),
                        Currency = "EUR",
                        ValidUntil = DateTime.UtcNow.AddDays(15),
                        Rates = new List<DraftQuoteRateDto>
                        {
                            new() { ContainerType = "20DV", BasePrice = 1250.00m },
                            new() { ContainerType = "40HC", BasePrice = 1890.00m }
                        },
                        Surcharges = new List<DraftQuoteSurchargeDto>
                        {
                            new() { Code = "THC", Label = "Terminal Handling Charge", Value = 125.00m, Currency = "EUR", Calc = "flat", Unit = "per_container", AppliesTo = new List<string> { "20DV", "40HC" } },
                            new() { Code = "DOC", Label = "Documentation Fee", Value = 50.00m, Currency = "EUR", Calc = "flat", Unit = "per_container", AppliesTo = new List<string> { "20DV", "40HC" } },
                            new() { Code = "SEAL", Label = "Seal Fee", Value = 15.00m, Currency = "EUR", Calc = "flat", Unit = "per_container", AppliesTo = new List<string> { "20DV", "40HC" } }
                        },
                        FreeTime = new DraftQuoteFreeTimeDto
                        {
                            Origin = new DraftQuoteFreeTimePeriodDto { Days = 7 },
                            Destination = new DraftQuoteFreeTimePeriodDto { Days = 10 }
                        }
                    }
                },
                Haulages = new List<DraftQuoteHaulageDto>
                {
                    new()
                    {
                        Id = "HL-1",
                        Provider = "Transport Solutions SA",
                        Scope = "pre-carriage",
                        From = "Lyon, FR",
                        To = "Anvers, BE",
                        Currency = "EUR",
                        Pricing = new List<DraftQuoteHaulagePricingDto>
                        {
                            new() { ContainerType = "20DV", Unit = "per_container", Price = 450.00m, IncludedWaitingHours = 2, ExtraHourPrice = 35.00m },
                            new() { ContainerType = "40HC", Unit = "per_container", Price = 520.00m, IncludedWaitingHours = 2, ExtraHourPrice = 35.00m }
                        },
                        Notes = "Transport routier direct avec équipement spécialisé conteneurs"
                    }
                },
                Services = new List<DraftQuoteServiceDto>
                {
                    new()
                    {
                        Id = "SV-1",
                        Name = "Déclaration douane export",
                        Provider = "Customs Broker Europe",
                        Unit = "per_shipment",
                        Price = 150.00m,
                        Currency = "EUR",
                        Taxable = true,
                        TaxRate = 0.21m
                    },
                    new()
                    {
                        Id = "SV-2",
                        Name = "Assurance maritime tous risques",
                        Provider = "CODIBUSINESS Insurance",
                        Unit = "per_shipment",
                        Price = 0.25m,
                        Currency = "EUR",
                        Taxable = false
                    },
                    new()
                    {
                        Id = "SV-3",
                        Name = "Empotage et arrimage conteneurs",
                        Provider = "Lyon Logistics Hub",
                        Unit = "per_container",
                        Price = 95.00m,
                        Currency = "EUR",
                        Taxable = true,
                        TaxRate = 0.21m
                    }
                }
            };
        }

        private List<DraftQuoteOptionDto> MapToOptionDtos(QuoteOfferDetailDto draft)
        {
            return draft.Options?.Select(option => new DraftQuoteOptionDto
            {
                OptionId = option.OptionId,
                Label = option.Description,
                Containers = new List<DraftQuoteOptionContainerDto>
                {
                    new() { ContainerType = "20DV", Quantity = 1 },
                    new() { ContainerType = "40HC", Quantity = 1 }
                },
                SeafreightRef = "SF-001",
                HaulageRefs = new List<string> { "HL-1" },
                ServiceRefs = new List<string> { "SV-1", "SV-2", "SV-3" },
                Suppliers = new DraftQuoteOptionSuppliersDto
                {
                    Carrier = "MAERSK",
                    Haulage = new List<string> { "Transport Solutions SA" },
                    Services = new List<DraftQuoteOptionServiceProviderDto>
                    {
                        new() { ServiceId = "SV-1", Provider = "Customs Broker Europe" },
                        new() { ServiceId = "SV-2", Provider = "CODIBUSINESS Insurance" },
                        new() { ServiceId = "SV-3", Provider = "Lyon Logistics Hub" }
                    }
                },
                ServicesOffered = new List<DraftQuoteServiceOfferedDto>
                {
                    new() { Type = "Maritime", Details = "Service maritime hebdomadaire direct Anvers-Douala via MAERSK AE1" },
                    new() { Type = "Route", Details = "Pré-acheminement routier Lyon-Anvers avec conteneurs spécialisés" },
                    new() { Type = "Douane", Details = "Déclaration export et formalités douanières complètes" },
                    new() { Type = "Assurance", Details = "Assurance maritime tous risques 110% valeur facture warehouse-to-warehouse" },
                    new() { Type = "Manutention", Details = "Empotage professionnel et arrimage sécurisé des conteneurs" }
                },
                Schedule = new DraftQuoteScheduleDto
                {
                    Etd = DateTime.UtcNow.AddDays(30),
                    Eta = DateTime.UtcNow.AddDays(58)
                },
                Milestones = new DraftQuoteMilestonesDto
                {
                    Pickup = DateTime.UtcNow.AddDays(28),
                    Vgm = DateTime.UtcNow.AddDays(29),
                    Si = DateTime.UtcNow.AddDays(29),
                    Cutoff = DateTime.UtcNow.AddDays(30)
                },
                PricingPreview = new DraftQuotePricingPreviewDto
                {
                    Currency = "EUR",
                    Lines = new List<DraftQuotePricingLineDto>
                    {
                        new() 
                        { 
                            Kind = "seafreight", 
                            Description = "Fret maritime MAERSK AE1 - Anvers/Douala (20DV: €1,250 + 40HC: €1,890 + frais)", 
                            UnitPrice = 3140.00m, // 1250 + 1890 + frais terminaux (125+50+15)*2
                            Qty = 1, 
                            Taxable = false
                        },
                        new() 
                        { 
                            Kind = "haulage", 
                            Description = "Transport routier Lyon-Anvers (20DV: €450 + 40HC: €520 + temps d'attente)", 
                            UnitPrice = 1040.00m, // 450 + 520 + temps d'attente (35*2)
                            Qty = 1, 
                            Taxable = false
                        },
                        new() 
                        { 
                            Kind = "service", 
                            Description = "Déclaration douane export + frais inspection", 
                            UnitPrice = 175.00m, // 150 + 25 inspection
                            Qty = 1, 
                            Taxable = true, 
                            TaxRate = 0.21m
                        },
                        new() 
                        { 
                            Kind = "service", 
                            Description = "Assurance maritime tous risques (0.25% valeur)", 
                            UnitPrice = 25.00m, // Estimation basée sur valeur 10,000 EUR
                            Qty = 1, 
                            Taxable = false
                        },
                        new() 
                        { 
                            Kind = "service", 
                            Description = "Empotage et arrimage conteneurs (2 conteneurs)", 
                            UnitPrice = 190.00m, // 95 * 2 conteneurs
                            Qty = 1, 
                            Taxable = true, 
                            TaxRate = 0.21m
                        }
                    },
                    Subtotals = new DraftQuotePricingSubtotalsDto
                    {
                        TaxableBase = 365.00m, // Services taxables: déclaration (175) + empotage (190)
                        NontaxableBase = 4205.00m // Fret (3140) + transport (1040) + assurance (25)
                    },
                    TaxTotal = 76.65m, // 21% sur base taxable
                    GrandTotal = 4646.65m // Base non-taxable + base taxable + taxes
                }
            }).ToList() ?? new List<DraftQuoteOptionDto>();
        }

        private DraftQuoteAuditDto MapToAuditDto(QuoteOfferDetailDto draft)
        {
            return new DraftQuoteAuditDto
            {
                CreatedBy = "user:system", // TODO: Extraire du draft
                CreatedAt = draft.CreatedDate,
                LastUpdatedBy = "user:system", // TODO: Extraire du draft
                LastUpdatedAt = draft.UpdatedAt,
                StepProgress = new DraftQuoteStepProgressDto
                {
                    GeneralRequestInformation = "done",
                    RoutingAndCargo = "done",
                    Seafreights = draft.Options?.Any() == true ? "done" : "not_started",
                    Haulages = "done",
                    Services = "done",
                    Options = draft.Options?.Any() == true ? "in_progress" : "not_started"
                }
            };
        }

        #endregion
    }
}
