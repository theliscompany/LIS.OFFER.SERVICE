using Microsoft.AspNetCore.Mvc;
using QuoteOffer.Models;
using QuoteOffer.Services;
using QuoteOffer.DTOs;
using QuoteOffer.Services.Interfaces;

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
        private readonly IRequestQuoteIntegrationService _requestQuoteIntegration;

        public DraftQuotesController(
            IQuoteOfferService quoteOfferService, 
            ILogger<DraftQuotesController> logger,
            IRequestQuoteIntegrationService requestQuoteIntegration)
        {
            _quoteOfferService = quoteOfferService;
            _logger = logger;
            _requestQuoteIntegration = requestQuoteIntegration;
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
                // Récupérer les données de la requête depuis le service LIS.QUOTES
                var requestQuoteData = await _requestQuoteIntegration.GetRequestQuoteDataAsync(request.RequestId);
                if (requestQuoteData == null)
                {
                    return BadRequest(CommonApiResponse<object>.ValidationFailed($"Request quote {request.RequestId} not found"));
                }

                // Générer un ID unique pour le brouillon
                var draftQuoteId = GenerateDraftQuoteId();
                
                // Créer les données enrichies à partir de la requête
                var enrichedWizardData = CreateEnrichedWizardDataFromRequest(requestQuoteData);
                
                // Créer l'entité QuoteOffer avec le statut DRAFT et les données enrichies
                var quoteOffer = new QuoteOffer.Models.QuoteOffer
                {
                    Id = draftQuoteId,
                    RequestQuoteId = request.RequestId,
                    Status = QuoteOfferStatus.DRAFT,
                    ClientNumber = requestQuoteData.CustomerCompany,
                    EmailUser = requestQuoteData.CustomerEmail,
                    Comment = requestQuoteData.AdditionalComments,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    OptimizedDraftData = new OptimizedDraftData
                    {
                        Wizard = new WizardMetadata
                        {
                            CurrentStep = 1,
                            Status = "in_progress",
                            LastModified = DateTime.UtcNow
                        },
                        EnrichedData = enrichedWizardData
                    }
                };

                // Créer dans le service
                var createdId = await _quoteOfferService.CreateQuoteOfferAsync(new CreateQuoteOfferDto
                {
                    RequestQuoteId = request.RequestId,
                    ClientNumber = requestQuoteData.CustomerCompany,
                    EmailUser = requestQuoteData.CustomerEmail,
                    Comment = requestQuoteData.AdditionalComments
                });

                if (string.IsNullOrEmpty(createdId))
                    return StatusCode(500, CommonApiResponse<object>.Error(500, "Failed to create draft quote"));

                // Récupérer le brouillon créé pour construire la réponse enrichie
                var createdDraft = await _quoteOfferService.GetQuoteOfferByIdAsync(createdId);
                if (createdDraft == null)
                    return StatusCode(500, CommonApiResponse<object>.Error(500, "Failed to retrieve created draft quote"));

                // Construire la réponse enrichie
                var response = MapToEnrichedDraftQuoteResponse(createdDraft);

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

                return Ok(CommonApiResponse<object>.Success(new { deleted = true }, "Draft quote deleted successfully"));
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
            // Extraire les données client depuis les étapes du wizard
            var customerData = draft.OptimizedDraftData?.Steps?.Step1?.Customer;
            var routeData = draft.OptimizedDraftData?.Steps?.Step1?.Route;
            
            return new DraftQuoteHeaderDto
            {
                Client = new DraftQuoteClientDto
                {
                    Company = customerData?.Company ?? draft.ClientNumber,
                    Contact = customerData?.ContactName ?? ExtractContactFromEmail(draft.EmailUser),
                    Email = customerData?.Email ?? draft.EmailUser,
                    Phone = null // TODO: Ajouter support du téléphone dans les modèles
                },
                Shipment = new DraftQuoteShipmentDto
                {
                    FromRequest = true,
                    Readonly = new List<string> { "cargoType", "goodsDescription", "origin", "destination", "requestedDeparture" },
                    CargoType = "FCL", // TODO: Extraire du draft ou rendre configurable
                    GoodsDescription = ExtractGoodsDescription(draft),
                    Origin = new DraftQuoteLocationDto 
                    { 
                        City = ExtractCityFromPort(routeData?.Origin?.Port?.PortName ?? "Le Havre"), 
                        Country = routeData?.Origin?.Port?.Country ?? "FR" 
                    },
                    Destination = new DraftQuoteLocationDto 
                    { 
                        City = ExtractCityFromPort(routeData?.Destination?.Port?.PortName ?? "Douala"), 
                        Country = routeData?.Destination?.Port?.Country ?? "CM" 
                    },
                    RequestedDeparture = ExtractDepartureDate(draft)
                },
                CommercialTerms = new DraftQuoteCommercialTermsDto
                {
                    Currency = "EUR", // TODO: Extraire de la configuration ou du draft
                    Incoterm = "CIF", // TODO: Extraire de la configuration ou du draft
                    ValidityDays = 15, // TODO: Extraire de la configuration
                    CgvAccepted = false // TODO: Vérifier le statut CGV
                }
            };
        }

        private DraftQuoteWizardDataDto MapToWizardDataDto(QuoteOfferDetailDto draft)
        {
            // Utiliser les données enrichies persistées ou des valeurs par défaut
            var enrichedData = draft.OptimizedDraftData?.EnrichedData;
            
            return new DraftQuoteWizardDataDto
            {
                GeneralRequestInformation = new DraftQuoteGeneralRequestInfoDto
                {
                    Channel = enrichedData?.GeneralRequestInformation?.Channel ?? "Email",
                    Priority = enrichedData?.GeneralRequestInformation?.Priority ?? "normal",
                    Notes = enrichedData?.GeneralRequestInformation?.Notes ?? draft.Comment
                },
                RoutingAndCargo = new DraftQuoteRoutingAndCargoDto
                {
                    PortOfLoading = enrichedData?.RoutingAndCargo?.PortOfLoading ?? ExtractPortFromDraft(draft, "origin"),
                    PortOfDestination = enrichedData?.RoutingAndCargo?.PortOfDestination ?? ExtractPortFromDraft(draft, "destination"),
                    Cargo = new DraftQuoteCargoDto
                    {
                        Items = enrichedData?.RoutingAndCargo?.Cargo?.Items?.Select(item => new DraftQuoteCargoItemDto
                        {
                            ContainerType = item.ContainerType,
                            Quantity = item.Quantity,
                            GrossWeightKg = item.GrossWeightKg,
                            VolumeM3 = item.VolumeM3
                        }).ToList() ?? GetDefaultCargoItems(draft),
                        Hazmat = enrichedData?.RoutingAndCargo?.Cargo?.Hazmat ?? false,
                        GoodsDescription = enrichedData?.RoutingAndCargo?.Cargo?.GoodsDescription ?? ExtractGoodsDescription(draft)
                    }
                },
                Seafreights = enrichedData?.Seafreights?.Select(sf => new DraftQuoteSeafreightDto
                {
                    Id = sf.Id,
                    Carrier = sf.Carrier,
                    Service = sf.Service,
                    Etd = sf.Etd,
                    Eta = sf.Eta,
                    Currency = sf.Currency,
                    ValidUntil = sf.ValidUntil,
                    Rates = sf.Rates.Select(rate => new DraftQuoteRateDto
                    {
                        ContainerType = rate.ContainerType,
                        BasePrice = rate.BasePrice
                    }).ToList(),
                    Surcharges = sf.Surcharges.Select(surcharge => new DraftQuoteSurchargeDto
                    {
                        Code = surcharge.Code,
                        Label = surcharge.Label,
                        Value = surcharge.Value,
                        Currency = surcharge.Currency,
                        Calc = surcharge.Calc,
                        Unit = surcharge.Unit,
                        Taxable = surcharge.Taxable,
                        AppliesTo = surcharge.AppliesTo,
                        Base = surcharge.Base
                    }).ToList(),
                    FreeTime = sf.FreeTime != null ? new DraftQuoteFreeTimeDto
                    {
                        Origin = new DraftQuoteFreeTimePeriodDto { Days = sf.FreeTime.Origin.Days },
                        Destination = new DraftQuoteFreeTimePeriodDto { Days = sf.FreeTime.Destination.Days }
                    } : null
                }).ToList() ?? new List<DraftQuoteSeafreightDto>(),
                Haulages = enrichedData?.Haulages?.Select(haulage => new DraftQuoteHaulageDto
                {
                    Id = haulage.Id,
                    Provider = haulage.Provider,
                    Scope = haulage.Scope,
                    From = haulage.From,
                    To = haulage.To,
                    Currency = haulage.Currency,
                    Pricing = haulage.Pricing.Select(pricing => new DraftQuoteHaulagePricingDto
                    {
                        ContainerType = pricing.ContainerType,
                        Unit = pricing.Unit,
                        Price = pricing.Price,
                        IncludedWaitingHours = pricing.IncludedWaitingHours,
                        ExtraHourPrice = pricing.ExtraHourPrice
                    }).ToList(),
                    Notes = haulage.Notes
                }).ToList() ?? new List<DraftQuoteHaulageDto>(),
                Services = enrichedData?.Services?.Select(service => new DraftQuoteServiceDto
                {
                    Id = service.Id,
                    Name = service.Name,
                    Provider = service.Provider,
                    Unit = service.Unit,
                    Price = service.Price,
                    Currency = service.Currency,
                    Taxable = service.Taxable,
                    TaxRate = service.TaxRate
                }).ToList() ?? new List<DraftQuoteServiceDto>()
            };
        }

        private List<DraftQuoteOptionDto> MapToOptionDtos(QuoteOfferDetailDto draft)
        {
            if (draft.Options?.Any() != true)
                return new List<DraftQuoteOptionDto>();

            // Extraire les données enrichies depuis les modèles persistés
            var enrichedData = GetEnrichedDataFromDraft(draft);
            
            return draft.Options.Select(optionDto => 
            {
                // Convertir l'option DTO vers le modèle
                var option = new QuoteOption
                {
                    OptionId = optionDto.OptionId,
                    Description = optionDto.Description
                    // Ajouter d'autres propriétés si nécessaire
                };

                return new DraftQuoteOptionDto
                {
                    OptionId = optionDto.OptionId,
                    Label = optionDto.Description,
                    Containers = GetContainersFromDraft(draft),
                    SeafreightRef = GetSeafreightReference(enrichedData, option),
                    HaulageRefs = GetHaulageReferences(enrichedData, option),
                    ServiceRefs = GetServiceReferences(enrichedData, option),
                    Suppliers = MapToSuppliers(enrichedData, option),
                    ServicesOffered = MapToServicesOffered(enrichedData, option),
                    Schedule = MapToSchedule(enrichedData, option),
                    Milestones = MapToMilestones(enrichedData, option),
                    PricingPreview = MapToPricingPreview(option, enrichedData)
                };
            }).ToList();
        }

        private List<DraftQuoteOptionContainerDto> GetContainersFromDraft(QuoteOfferDetailDto draft)
        {
            var containers = draft.OptimizedDraftData?.Steps?.Step3?.Containers;
            if (containers?.Any() == true)
            {
                return containers.Select(c => new DraftQuoteOptionContainerDto
                {
                    ContainerType = c.Type,
                    Quantity = c.Quantity
                }).ToList();
            }

            // Valeurs par défaut
            return new List<DraftQuoteOptionContainerDto>
            {
                new() { ContainerType = "20DV", Quantity = 1 },
                new() { ContainerType = "40HC", Quantity = 1 }
            };
        }

        private string? GetSeafreightReference(EnrichedWizardData? enrichedData, Models.QuoteOption option)
        {
            // Retourner la première référence de seafreight disponible
            return enrichedData?.Seafreights?.FirstOrDefault()?.Id;
        }

        private List<string> GetHaulageReferences(EnrichedWizardData? enrichedData, Models.QuoteOption option)
        {
            return enrichedData?.Haulages?.Select(h => h.Id).ToList() ?? new List<string>();
        }

        private List<string> GetServiceReferences(EnrichedWizardData? enrichedData, Models.QuoteOption option)
        {
            return enrichedData?.Services?.Select(s => s.Id).ToList() ?? new List<string>();
        }

        private DraftQuoteOptionSuppliersDto MapToSuppliers(EnrichedWizardData? enrichedData, Models.QuoteOption option)
        {
            return new DraftQuoteOptionSuppliersDto
            {
                Carrier = enrichedData?.Seafreights?.FirstOrDefault()?.Carrier ?? "Compagnie Maritime",
                Haulage = enrichedData?.Haulages?.Select(h => h.Provider).ToList() ?? new List<string> { "Transporteur" },
                Services = enrichedData?.Services?.Select(s => new DraftQuoteOptionServiceProviderDto
                {
                    ServiceId = s.Id,
                    Provider = s.Provider
                }).ToList() ?? new List<DraftQuoteOptionServiceProviderDto>()
            };
        }

        private List<DraftQuoteServiceOfferedDto> MapToServicesOffered(EnrichedWizardData? enrichedData, Models.QuoteOption option)
        {
            var services = new List<DraftQuoteServiceOfferedDto>();
            
            // Ajouter les services basés sur les données enrichies
            var seafreight = enrichedData?.Seafreights?.FirstOrDefault();
            if (seafreight != null)
            {
                services.Add(new DraftQuoteServiceOfferedDto
                {
                    Type = "Maritime",
                    Details = $"Service {seafreight.Carrier} - {seafreight.Service}"
                });
            }

            foreach (var haulage in enrichedData?.Haulages ?? new List<Models.HaulageData>())
            {
                services.Add(new DraftQuoteServiceOfferedDto
                {
                    Type = "Route",
                    Details = $"{haulage.Scope} {haulage.From} → {haulage.To} par {haulage.Provider}"
                });
            }

            foreach (var service in enrichedData?.Services ?? new List<Models.ServiceData>())
            {
                services.Add(new DraftQuoteServiceOfferedDto
                {
                    Type = GetServiceCategory(service.Name),
                    Details = $"{service.Name} par {service.Provider}"
                });
            }

            return services;
        }

        private DraftQuoteScheduleDto? MapToSchedule(EnrichedWizardData? enrichedData, Models.QuoteOption option)
        {
            var seafreight = enrichedData?.Seafreights?.FirstOrDefault();
            if (seafreight != null)
            {
                return new DraftQuoteScheduleDto
                {
                    Etd = seafreight.Etd,
                    Eta = seafreight.Eta
                };
            }

            return null;
        }

        private DraftQuoteMilestonesDto? MapToMilestones(EnrichedWizardData? enrichedData, Models.QuoteOption option)
        {
            var seafreight = enrichedData?.Seafreights?.FirstOrDefault();
            if (seafreight != null)
            {
                return new DraftQuoteMilestonesDto
                {
                    Pickup = seafreight.Etd.AddDays(-2),
                    Vgm = seafreight.Etd.AddDays(-1),
                    Si = seafreight.Etd.AddDays(-1),
                    Cutoff = seafreight.Etd
                };
            }

            return null;
        }

        private DraftQuotePricingPreviewDto? MapToPricingPreview(Models.QuoteOption option, EnrichedWizardData? enrichedData)
        {
            var lines = new List<DraftQuotePricingLineDto>();
            decimal totalTaxable = 0;
            decimal totalNonTaxable = 0;

            // Ajouter les lignes basées sur les données enrichies
            foreach (var seafreight in enrichedData?.Seafreights ?? new List<Models.SeafreightData>())
            {
                var seafreightTotal = CalculateSeafreightTotal(seafreight);
                lines.Add(new DraftQuotePricingLineDto
                {
                    Kind = "seafreight",
                    Description = $"Fret maritime {seafreight.Carrier} - {seafreight.Service}",
                    UnitPrice = seafreightTotal,
                    Qty = 1,
                    Taxable = false
                });
                totalNonTaxable += seafreightTotal;
            }

            foreach (var haulage in enrichedData?.Haulages ?? new List<Models.HaulageData>())
            {
                var haulageTotal = CalculateHaulageTotal(haulage);
                lines.Add(new DraftQuotePricingLineDto
                {
                    Kind = "haulage",
                    Description = $"Transport {haulage.Scope} {haulage.From} → {haulage.To}",
                    UnitPrice = haulageTotal,
                    Qty = 1,
                    Taxable = false
                });
                totalNonTaxable += haulageTotal;
            }

            foreach (var service in enrichedData?.Services ?? new List<Models.ServiceData>())
            {
                lines.Add(new DraftQuotePricingLineDto
                {
                    Kind = "service",
                    Description = service.Name,
                    UnitPrice = service.Price,
                    Qty = 1,
                    Taxable = service.Taxable,
                    TaxRate = service.TaxRate
                });

                if (service.Taxable)
                    totalTaxable += service.Price;
                else
                    totalNonTaxable += service.Price;
            }

            var taxTotal = totalTaxable * 0.21m; // Taux de TVA standard
            var grandTotal = totalNonTaxable + totalTaxable + taxTotal;

            return new DraftQuotePricingPreviewDto
            {
                Currency = "EUR",
                Lines = lines,
                Subtotals = new DraftQuotePricingSubtotalsDto
                {
                    TaxableBase = totalTaxable,
                    NontaxableBase = totalNonTaxable
                },
                TaxTotal = taxTotal,
                GrandTotal = grandTotal
            };
        }

        private string GetServiceCategory(string serviceName)
        {
            return serviceName.ToLower() switch
            {
                var name when name.Contains("douane") || name.Contains("customs") => "Douane",
                var name when name.Contains("assurance") || name.Contains("insurance") => "Assurance",
                var name when name.Contains("empotage") || name.Contains("stuffing") => "Manutention",
                _ => "Service"
            };
        }

        private decimal CalculateSeafreightTotal(Models.SeafreightData seafreight)
        {
            var baseTotal = seafreight.Rates.Sum(r => r.BasePrice);
            var surchargeTotal = seafreight.Surcharges.Sum(s => s.Value);
            return baseTotal + surchargeTotal;
        }

        private decimal CalculateHaulageTotal(Models.HaulageData haulage)
        {
            return haulage.Pricing.Sum(p => p.Price);
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

        #region Private Helper Methods

        /// <summary>
        /// Crée les données enrichies du wizard à partir d'une requête de devis
        /// </summary>
        private EnrichedWizardData CreateEnrichedWizardDataFromRequest(RequestQuoteData requestData)
        {
            return new EnrichedWizardData
            {
                GeneralRequestInformation = new GeneralRequestInfo
                {
                    Channel = "Email", // TODO: Déterminer le canal depuis la requête
                    Priority = requestData.Tags?.Contains("urgent") == true ? "high" : "normal",
                    Notes = requestData.AdditionalComments
                },
                RoutingAndCargo = new RoutingAndCargo
                {
                    PortOfLoading = MapCityToPort(requestData.PickupLocation.City, requestData.PickupLocation.Country),
                    PortOfDestination = MapCityToPort(requestData.DeliveryLocation.City, requestData.DeliveryLocation.Country),
                    Cargo = new CargoData
                    {
                        Items = CreateCargoItemsFromRequest(requestData),
                        Hazmat = requestData.IsDangerousGoods,
                        GoodsDescription = requestData.GoodsDescription ?? ""
                    }
                },
                Seafreights = new List<SeafreightData>(), // TODO: Enrichir avec des données de fret maritime par défaut
                Haulages = new List<HaulageData>(), // TODO: Enrichir avec des données de transport routier par défaut
                Services = new List<ServiceData>() // TODO: Enrichir avec des services par défaut
            };
        }

        /// <summary>
        /// Crée les items de cargo à partir des données de la requête
        /// </summary>
        private List<CargoItem> CreateCargoItemsFromRequest(RequestQuoteData requestData)
        {
            var items = new List<CargoItem>();

            // Déterminer le type de conteneur basé sur le type de cargo
            var containerType = requestData.CargoType switch
            {
                "0" => "20DV", // Container
                "1" => "CONVENTIONAL", // Conventional
                "2" => "RORO", // Roll On Roll Off
                _ => "20DV" // Par défaut
            };

            items.Add(new CargoItem
            {
                ContainerType = containerType,
                Quantity = requestData.Quantity ?? 1,
                GrossWeightKg = (decimal)(requestData.TotalWeightKg ?? 0),
                VolumeM3 = CalculateVolumeFromDimensions(requestData.TotalDimensions)
            });

            return items;
        }

        /// <summary>
        /// Mappe une ville/pays vers un port approprié
        /// </summary>
        private string MapCityToPort(string city, string country)
        {
            // TODO: Implémenter un mapping plus sophistiqué ville -> port
            // Pour l'instant, utiliser des mappings simples
            return country.ToUpper() switch
            {
                "FR" => city.ToLower() switch
                {
                    "lyon" => "Le Havre",
                    "marseille" => "Marseille",
                    "paris" => "Le Havre",
                    _ => "Le Havre"
                },
                "CM" => "Douala",
                "BE" => "Antwerp",
                "NL" => "Rotterdam",
                "DE" => "Hamburg",
                _ => city
            };
        }

        /// <summary>
        /// Calcule le volume à partir des dimensions textuelles
        /// </summary>
        private decimal CalculateVolumeFromDimensions(string? dimensions)
        {
            if (string.IsNullOrWhiteSpace(dimensions))
                return 0;

            try
            {
                // Exemple: "2.5m x 1.5m x 1.2m" -> 4.5 m³
                var parts = dimensions.ToLower()
                    .Replace("m", "")
                    .Split('x')
                    .Select(p => p.Trim())
                    .Where(p => decimal.TryParse(p, out _))
                    .Select(p => decimal.Parse(p))
                    .ToList();

                if (parts.Count >= 3)
                    return parts[0] * parts[1] * parts[2];
                
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Extrait et convertit les données enrichies depuis le draft vers les modèles
        /// </summary>
        private EnrichedWizardData? GetEnrichedDataFromDraft(QuoteOfferDetailDto draft)
        {
            var enrichedDto = draft.OptimizedDraftData?.EnrichedData;
            if (enrichedDto == null)
                return null;

            return new EnrichedWizardData
            {
                GeneralRequestInformation = new GeneralRequestInfo
                {
                    Channel = enrichedDto.GeneralRequestInformation?.Channel ?? "",
                    Priority = enrichedDto.GeneralRequestInformation?.Priority ?? "normal",
                    Notes = enrichedDto.GeneralRequestInformation?.Notes
                },
                RoutingAndCargo = new RoutingAndCargo
                {
                    PortOfLoading = enrichedDto.RoutingAndCargo?.PortOfLoading ?? "",
                    PortOfDestination = enrichedDto.RoutingAndCargo?.PortOfDestination ?? "",
                    Cargo = new CargoData
                    {
                        Items = enrichedDto.RoutingAndCargo?.Cargo?.Items?.Select(item => new CargoItem
                        {
                            ContainerType = item.ContainerType,
                            Quantity = item.Quantity,
                            GrossWeightKg = item.GrossWeightKg,
                            VolumeM3 = item.VolumeM3
                        }).ToList() ?? new List<CargoItem>(),
                        Hazmat = enrichedDto.RoutingAndCargo?.Cargo?.Hazmat ?? false,
                        GoodsDescription = enrichedDto.RoutingAndCargo?.Cargo?.GoodsDescription ?? ""
                    }
                },
                Seafreights = enrichedDto.Seafreights?.Select(sf => new SeafreightData
                {
                    Id = sf.Id,
                    Carrier = sf.Carrier,
                    Service = sf.Service,
                    Etd = sf.Etd,
                    Eta = sf.Eta,
                    Currency = sf.Currency,
                    ValidUntil = sf.ValidUntil,
                    Rates = sf.Rates?.Select(rate => new ContainerRate
                    {
                        ContainerType = rate.ContainerType,
                        BasePrice = rate.BasePrice
                    }).ToList() ?? new List<ContainerRate>(),
                    Surcharges = sf.Surcharges?.Select(surcharge => new SurchargeData
                    {
                        Code = surcharge.Code,
                        Label = surcharge.Label,
                        Calc = surcharge.Calc,
                        Base = surcharge.Base,
                        Unit = surcharge.Unit,
                        Value = surcharge.Value,
                        Currency = surcharge.Currency,
                        Taxable = surcharge.Taxable,
                        AppliesTo = surcharge.AppliesTo
                    }).ToList() ?? new List<SurchargeData>(),
                    FreeTime = sf.FreeTime != null ? new FreeTimeData
                    {
                        Origin = new FreeTimePeriod { Days = sf.FreeTime.Origin?.Days ?? 0 },
                        Destination = new FreeTimePeriod { Days = sf.FreeTime.Destination?.Days ?? 0 }
                    } : null
                }).ToList() ?? new List<SeafreightData>(),
                Haulages = enrichedDto.Haulages?.Select(h => new HaulageData
                {
                    Id = h.Id,
                    Provider = h.Provider,
                    Scope = h.Scope,
                    From = h.From,
                    To = h.To,
                    Currency = h.Currency,
                    Pricing = h.Pricing?.Select(p => new HaulagePricing
                    {
                        ContainerType = p.ContainerType,
                        Unit = p.Unit,
                        Price = p.Price,
                        IncludedWaitingHours = p.IncludedWaitingHours,
                        ExtraHourPrice = p.ExtraHourPrice
                    }).ToList() ?? new List<HaulagePricing>(),
                    Notes = h.Notes
                }).ToList() ?? new List<HaulageData>(),
                Services = enrichedDto.Services?.Select(s => new ServiceData
                {
                    Id = s.Id,
                    Name = s.Name,
                    Provider = s.Provider,
                    Unit = s.Unit,
                    Price = s.Price,
                    Currency = s.Currency,
                    Taxable = s.Taxable,
                    TaxRate = s.TaxRate
                }).ToList() ?? new List<ServiceData>()
            };
        }

        /// <summary>
        /// Extrait le port d'origine ou de destination du draft existant
        /// </summary>
        private string ExtractPortFromDraft(QuoteOfferDetailDto draft, string type)
        {
            // Essayer d'extraire depuis les étapes du wizard
            var routeData = draft.OptimizedDraftData?.Steps?.Step1?.Route;
            if (type == "origin" && routeData?.Origin?.Port != null)
                return routeData.Origin.Port.PortName;
            if (type == "destination" && routeData?.Destination?.Port != null)
                return routeData.Destination.Port.PortName;
            
            // Valeurs par défaut basées sur les patterns communs
            return type == "origin" ? "Le Havre" : "Douala";
        }

        /// <summary>
        /// Extrait la description des marchandises du draft existant
        /// </summary>
        private string ExtractGoodsDescription(QuoteOfferDetailDto draft)
        {
            // Pourrait être stocké dans le comment ou ailleurs
            if (!string.IsNullOrEmpty(draft.Comment))
                return draft.Comment;
            
            return "Marchandises diverses";
        }

        /// <summary>
        /// Génère les items de cargo par défaut basés sur les données du draft
        /// </summary>
        private List<DraftQuoteCargoItemDto> GetDefaultCargoItems(QuoteOfferDetailDto draft)
        {
            // Essayer d'extraire depuis les conteneurs du wizard
            var containers = draft.OptimizedDraftData?.Steps?.Step3?.Containers;
            if (containers?.Any() == true)
            {
                return containers.Select(container => new DraftQuoteCargoItemDto
                {
                    ContainerType = container.Type,
                    Quantity = container.Quantity,
                    GrossWeightKg = GetEstimatedWeight(container.Type),
                    VolumeM3 = GetEstimatedVolume(container.Type)
                }).ToList();
            }

            // Valeurs par défaut
            return new List<DraftQuoteCargoItemDto>
            {
                new() { ContainerType = "20DV", Quantity = 1, GrossWeightKg = 8000, VolumeM3 = 28 },
                new() { ContainerType = "40HC", Quantity = 1, GrossWeightKg = 12000, VolumeM3 = 60 }
            };
        }

        /// <summary>
        /// Estime le poids d'un conteneur selon son type
        /// </summary>
        private decimal GetEstimatedWeight(string containerType)
        {
            return containerType switch
            {
                "20DV" => 8000,
                "40DV" => 12000,
                "40HC" => 12000,
                "45HC" => 15000,
                _ => 10000
            };
        }

        /// <summary>
        /// Estime le volume d'un conteneur selon son type
        /// </summary>
        private decimal GetEstimatedVolume(string containerType)
        {
            return containerType switch
            {
                "20DV" => 28,
                "40DV" => 58,
                "40HC" => 68,
                "45HC" => 76,
                _ => 30
            };
        }

        /// <summary>
        /// Extrait le nom du contact depuis l'email
        /// </summary>
        private string ExtractContactFromEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return "N/A";
            
            var localPart = email.Split('@')[0];
            // Convertir john.doe en John Doe
            var parts = localPart.Split('.');
            if (parts.Length > 1)
            {
                return string.Join(" ", parts.Select(p => 
                    char.ToUpper(p[0]) + p.Substring(1).ToLower()));
            }
            
            return char.ToUpper(localPart[0]) + localPart.Substring(1).ToLower();
        }

        /// <summary>
        /// Extrait la ville depuis le nom du port
        /// </summary>
        private string ExtractCityFromPort(string portName)
        {
            // Les ports principaux peuvent avoir leurs propres mappings
            return portName switch
            {
                "Le Havre" => "Le Havre",
                "Anvers" => "Anvers", 
                "Douala" => "Douala",
                "Rotterdam" => "Rotterdam",
                _ => portName
            };
        }

        /// <summary>
        /// Extrait la date de départ demandée
        /// </summary>
        private DateTime? ExtractDepartureDate(QuoteOfferDetailDto draft)
        {
            // TODO: Ajouter support de la date de départ dans les modèles
            // Pour l'instant, utiliser une date par défaut
            return DateTime.UtcNow.AddDays(30);
        }

        #endregion
    }
}
