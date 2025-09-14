using Microsoft.AspNetCore.Mvc;
using QuoteOffer.Models;
using QuoteOffer.Services;
using QuoteOffer.DTOs;
using QuoteOfferModel = QuoteOffer.Models.QuoteOffer;

namespace QuoteOffer.Controllers
{
    /// <summary>
    /// Contrôleur pour la gestion des devis finalisés
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Tags("📋 Quotes Management")]
    public class QuotesController : ControllerBase
    {
        private readonly IQuoteOfferService _quoteOfferService;
        private readonly ILogger<QuotesController> _logger;

        public QuotesController(IQuoteOfferService quoteOfferService, ILogger<QuotesController> logger)
        {
            _quoteOfferService = quoteOfferService;
            _logger = logger;
        }

        /// <summary>
        /// Recherche les devis selon les critères spécifiés
        /// </summary>
        [HttpPost("search")]
        [ProducesResponseType(typeof(CommonApiResponse<List<QuoteResponse>>), 200)]
        public async Task<ActionResult<CommonApiResponse<List<QuoteResponse>>>> SearchQuotes([FromBody] QuoteSearchRequest request)
        {
            try
            {
                // Utiliser la méthode de recherche existante du service
                var searchDto = new QuoteOfferSearchDto
                {
                    ClientNumber = request.ClientNumber,
                    EmailUser = request.EmailUser,
                    Page = request.PageNumber,
                    PageSize = request.PageSize
                };

                var searchResult = await _quoteOfferService.SearchQuoteOffersAsync(searchDto);
                
                // Filtrer pour ne garder que les devis finalisés
                var finalizedQuotes = searchResult.Items
                    .Where(q => q.Status != QuoteOfferStatus.DRAFT.ToString())
                    .Select(MapSummaryToQuoteResponse)
                    .ToList();

                var meta = new
                {
                    totalCount = finalizedQuotes.Count,
                    pageNumber = request.PageNumber,
                    pageSize = request.PageSize,
                    totalPages = (int)Math.Ceiling((double)finalizedQuotes.Count / request.PageSize)
                };

                var response = CommonApiResponse<List<QuoteResponse>>.Success(finalizedQuotes, "Quotes retrieved successfully");
                response.Meta = meta;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching quotes");
                return StatusCode(500, CommonApiResponse<List<QuoteResponse>>.Error(500, "Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Récupère un devis par son ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CommonApiResponse<QuoteResponse>), 200)]
        [ProducesResponseType(typeof(CommonApiResponse<QuoteResponse>), 404)]
        public async Task<ActionResult<CommonApiResponse<QuoteResponse>>> GetQuoteById(string id)
        {
            try
            {
                var quote = await _quoteOfferService.GetQuoteOfferByIdAsync(id);
                if (quote == null)
                    return NotFound(CommonApiResponse<QuoteResponse>.NotFound("Quote not found"));

                if (quote.Status == QuoteOfferStatus.DRAFT.ToString())
                    return BadRequest(CommonApiResponse<QuoteResponse>.ValidationFailed("Cannot access draft through quotes endpoint"));

                var response = MapDetailToQuoteResponse(quote);
                return Ok(CommonApiResponse<QuoteResponse>.Success(response, "Quote retrieved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quote {QuoteId}", id);
                return StatusCode(500, CommonApiResponse<QuoteResponse>.Error(500, "Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Finalise un brouillon en devis (méthode simplifiée)
        /// </summary>
        [HttpPost("finalize/{draftId}")]
        [ProducesResponseType(typeof(CommonApiResponse<QuoteResponse>), 201)]
        [ProducesResponseType(typeof(CommonApiResponse<QuoteResponse>), 404)]
        [ProducesResponseType(typeof(CommonApiResponse<QuoteResponse>), 400)]
        public async Task<ActionResult<CommonApiResponse<QuoteResponse>>> FinalizeDraftToQuote(string draftId, [FromBody] FinalizeDraftRequest request)
        {
            try
            {
                var draft = await _quoteOfferService.GetQuoteOfferByIdAsync(draftId);
                if (draft == null)
                    return NotFound(CommonApiResponse<QuoteResponse>.NotFound("Draft not found"));

                if (draft.Status != QuoteOfferStatus.DRAFT.ToString())
                    return BadRequest(CommonApiResponse<QuoteResponse>.ValidationFailed("Only drafts can be finalized"));

                // Validation des options
                if (request.Options.Count < 1 || request.Options.Count > 3)
                    return BadRequest(CommonApiResponse<QuoteResponse>.ValidationFailed("A quote must have between 1 and 3 options"));

                if (!request.Options.Any(o => o.OptionId == request.PreferredOptionId))
                    return BadRequest(CommonApiResponse<QuoteResponse>.ValidationFailed("Preferred option must exist in options list"));

                // Mettre à jour le statut vers devis finalisé
                var updateDto = new UpdateQuoteOfferDto
                {
                    Comment = request.QuoteComments ?? draft.Comment,
                    SelectedOption = request.PreferredOptionId,
                    ExpirationDate = request.ExpirationDate ?? DateTime.UtcNow.AddDays(30)
                };

                var success = await _quoteOfferService.UpdateQuoteOfferAsync(draftId, updateDto);
                if (!success)
                    return StatusCode(500, CommonApiResponse<QuoteResponse>.Error(500, "Failed to finalize draft"));

                // Récupérer le devis finalisé
                var finalizedQuote = await _quoteOfferService.GetQuoteOfferByIdAsync(draftId);
                var response = MapDetailToQuoteResponse(finalizedQuote!);

                _logger.LogInformation("Draft {DraftId} finalized to quote successfully", draftId);
                return CreatedAtAction(nameof(GetQuoteById), new { id = draftId }, 
                    CommonApiResponse<QuoteResponse>.Created(response, "Draft finalized to quote successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finalizing draft {DraftId}", draftId);
                return StatusCode(500, CommonApiResponse<QuoteResponse>.Error(500, "Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Change le statut d'un devis
        /// </summary>
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(CommonApiResponse<QuoteResponse>), 200)]
        public async Task<ActionResult<CommonApiResponse<QuoteResponse>>> ChangeQuoteStatus(string id, [FromBody] ChangeQuoteStatusRequest request)
        {
            try
            {
                var quote = await _quoteOfferService.GetQuoteOfferByIdAsync(id);
                if (quote == null)
                    return NotFound(CommonApiResponse<QuoteResponse>.NotFound("Quote not found"));

                if (quote.Status == QuoteOfferStatus.DRAFT.ToString())
                    return BadRequest(CommonApiResponse<QuoteResponse>.ValidationFailed("Cannot modify draft status through quotes endpoint"));

                // Valider le nouveau statut
                if (!Enum.TryParse<QuoteOfferStatus>(request.NewStatus, out var newStatus))
                    return BadRequest(CommonApiResponse<QuoteResponse>.ValidationFailed("Invalid status"));

                if (newStatus == QuoteOfferStatus.DRAFT)
                    return BadRequest(CommonApiResponse<QuoteResponse>.ValidationFailed("Cannot change quote back to draft"));

                var updateDto = new UpdateQuoteOfferDto
                {
                    Comment = quote.Comment,
                    SelectedOption = quote.SelectedOption
                };

                var success = await _quoteOfferService.UpdateQuoteOfferAsync(id, updateDto);
                if (!success)
                    return StatusCode(500, CommonApiResponse<QuoteResponse>.Error(500, "Failed to update quote status"));

                var updatedQuote = await _quoteOfferService.GetQuoteOfferByIdAsync(id);
                var response = MapDetailToQuoteResponse(updatedQuote!);

                return Ok(CommonApiResponse<QuoteResponse>.Success(response, "Quote status updated successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for quote {QuoteId}", id);
                return StatusCode(500, CommonApiResponse<QuoteResponse>.Error(500, "Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Traite l'approbation du client
        /// </summary>
        [HttpPost("{id}/client-approval")]
        [ProducesResponseType(typeof(CommonApiResponse<QuoteResponse>), 200)]
        public async Task<ActionResult<CommonApiResponse<QuoteResponse>>> ProcessClientApproval(string id, [FromBody] ClientApprovalRequest request)
        {
            try
            {
                var quote = await _quoteOfferService.GetQuoteOfferByIdAsync(id);
                if (quote == null)
                    return NotFound(CommonApiResponse<QuoteResponse>.NotFound("Quote not found"));

                if (quote.Status == QuoteOfferStatus.DRAFT.ToString())
                    return BadRequest(CommonApiResponse<QuoteResponse>.ValidationFailed("Cannot approve draft"));

                // Déterminer le nouveau statut
                QuoteOfferStatus newStatus;
                if (request.Approval.ToLower() == "accepted")
                {
                    newStatus = QuoteOfferStatus.ACCEPTED;
                }
                else if (request.Approval.ToLower() == "rejected")
                {
                    newStatus = QuoteOfferStatus.REJECTED;
                }
                else
                {
                    return BadRequest(CommonApiResponse<QuoteResponse>.ValidationFailed("Invalid approval value"));
                }

                var updateDto = new UpdateQuoteOfferDto
                {
                    Comment = (quote.Comment ?? "") + "\n[Client] " + (request.Comments ?? ""),
                    SelectedOption = request.SelectedOptionId ?? quote.SelectedOption,
                    ClientApproval = request.Approval
                };

                var success = await _quoteOfferService.UpdateQuoteOfferAsync(id, updateDto);
                if (!success)
                    return StatusCode(500, CommonApiResponse<QuoteResponse>.Error(500, "Failed to process client approval"));

                var updatedQuote = await _quoteOfferService.GetQuoteOfferByIdAsync(id);
                var response = MapDetailToQuoteResponse(updatedQuote!);

                return Ok(CommonApiResponse<QuoteResponse>.Success(response, "Client approval processed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing client approval for quote {QuoteId}", id);
                return StatusCode(500, CommonApiResponse<QuoteResponse>.Error(500, "Internal server error", ex.Message));
            }
        }

        /// <summary>
        /// Supprime un devis
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(CommonApiResponse<object>), 200)]
        public async Task<ActionResult<CommonApiResponse<object>>> DeleteQuote(string id)
        {
            try
            {
                var quote = await _quoteOfferService.GetQuoteOfferByIdAsync(id);
                if (quote == null)
                    return NotFound(CommonApiResponse<object>.NotFound("Quote not found"));

                if (quote.Status == QuoteOfferStatus.DRAFT.ToString())
                    return BadRequest(CommonApiResponse<object>.ValidationFailed("Cannot delete draft through quotes endpoint"));

                var deleted = await _quoteOfferService.DeleteQuoteOfferAsync(id);
                if (!deleted)
                    return StatusCode(500, CommonApiResponse<object>.Error(500, "Failed to delete quote"));

                return Ok(CommonApiResponse<object>.Success(null, "Quote deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quote {QuoteId}", id);
                return StatusCode(500, CommonApiResponse<object>.Error(500, "Internal server error", ex.Message));
            }
        }

        #region Private Methods

        private QuoteResponse MapDetailToQuoteResponse(QuoteOfferDetailDto quote)
        {
            return new QuoteResponse
            {
                Id = quote.Id,
                QuoteNumber = quote.QuoteOfferNumber,
                RequestQuoteId = quote.RequestQuoteId,
                ClientNumber = quote.ClientNumber,
                EmailUser = quote.EmailUser,
                Comments = quote.Comment,
                Status = quote.Status,
                ClientApproval = quote.ClientApproval,
                ExpirationDate = quote.ExpirationDate ?? DateTime.UtcNow.AddDays(30),
                CreatedDate = quote.CreatedDate,
                LastModified = quote.UpdatedAt,
                Files = quote.Files?.Select(f => f.FileName).ToList() ?? new List<string>(),
                Options = quote.Options?.Select(o => new QuoteOptionResponse
                {
                    OptionId = int.TryParse(o.OptionId, out int optId) ? optId : 1,
                    Description = o.Description,
                    IsPreferred = int.TryParse(o.OptionId, out int parseId) && parseId == quote.SelectedOption,
                    Totals = new QuoteOptionTotalsDto
                    {
                        HaulageTotal = o.Totals?.HaulageTotal ?? 0,
                        SeaFreightTotal = o.Totals?.SeafreightTotal ?? 0,
                        MiscellaneousTotal = o.Totals?.MiscellaneousTotal ?? 0,
                        GrandTotal = o.Totals?.GrandTotal ?? 0,
                        Currency = o.Totals?.Currency ?? "EUR"
                    },
                    Details = new QuoteOptionDetailsDto
                    {
                        HaulageProvider = "",
                        SeaFreightCarrier = "",
                        Route = "",
                        Etd = null,
                        Eta = null,
                        ServicesIncluded = new List<string>()
                    }
                }).ToList() ?? new List<QuoteOptionResponse>(),
                PreferredOptionId = quote.SelectedOption,
                Summary = new QuoteSummaryDto
                {
                    Route = $"Route pour devis {quote.QuoteOfferNumber}",
                    TotalOptions = quote.Options?.Count ?? 0,
                    BestPrice = quote.Options?.Min(o => o.Totals?.GrandTotal ?? 0) ?? 0,
                    HighestPrice = quote.Options?.Max(o => o.Totals?.GrandTotal ?? 0) ?? 0,
                    Currency = "EUR",
                    PreferredOptionDescription = quote.Options?.FirstOrDefault(o => int.TryParse(o.OptionId, out int id) && id == quote.SelectedOption)?.Description ?? ""
                }
            };
        }

        private QuoteResponse MapSummaryToQuoteResponse(QuoteOfferSummaryDto quote)
        {
            return new QuoteResponse
            {
                Id = quote.Id,
                QuoteNumber = quote.QuoteOfferNumber,
                RequestQuoteId = quote.RequestQuoteId,
                ClientNumber = quote.ClientNumber,
                EmailUser = quote.EmailUser,
                Comments = "",
                Status = quote.Status,
                ClientApproval = null,
                ExpirationDate = quote.ExpirationDate ?? DateTime.UtcNow.AddDays(30),
                CreatedDate = quote.CreatedDate,
                LastModified = quote.CreatedDate, // QuoteOfferSummaryDto n'a pas UpdatedAt
                Files = new List<string>(),
                Options = new List<QuoteOptionResponse>(),
                PreferredOptionId = 0,
                Summary = new QuoteSummaryDto
                {
                    Route = $"Route pour devis {quote.QuoteOfferNumber}",
                    TotalOptions = 0,
                    BestPrice = 0,
                    HighestPrice = 0,
                    Currency = "EUR",
                    PreferredOptionDescription = ""
                }
            };
        }

        #endregion
    }
}
