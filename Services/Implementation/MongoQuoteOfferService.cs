using MongoDB.Driver;
using QuoteOffer.Models;
using QuoteOffer.DTOs;
using QuoteOffer.Services.Infrastructure;
using QuoteOfferModel = QuoteOffer.Models.QuoteOffer;

namespace QuoteOffer.Services.Implementation;

/// <summary>
/// Service MongoDB pour la gestion des offres de devis
/// </summary>
public class MongoQuoteOfferService : MongoDbServiceBase<QuoteOfferModel>, IQuoteOfferService
{
    private static int _nextQuoteNumber = 1;
    private static readonly object _lock = new();

    public MongoQuoteOfferService(
        IMongoDatabase database, 
        ILogger<MongoQuoteOfferService> logger) 
        : base(database, logger)
    {
        // Initialiser le compteur de numéros de devis
        InitializeQuoteNumber();
    }

    public async Task<string> CreateQuoteOfferAsync(CreateQuoteOfferDto createDto)
    {
        try
        {
            var quoteOffer = new QuoteOfferModel
            {
                Id = Guid.NewGuid().ToString(),
                RequestQuoteId = createDto.RequestQuoteId,
                ClientNumber = createDto.ClientNumber,
                EmailUser = createDto.EmailUser,
                Comment = createDto.Comment,
                Status = QuoteOfferStatus.DRAFT,
                QuoteOfferNumber = GetNextQuoteNumber(),
                SelectedOption = 0,
                CreatedDate = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ExpirationDate = createDto.ExpirationDate,
                OptimizedDraftData = new OptimizedDraftData
                {
                    Wizard = new WizardMetadata
                    {
                        CurrentStep = 1,
                        Status = "not_started",
                        LastModified = DateTime.UtcNow
                    },
                    Steps = new WizardSteps()
                }
            };

            return await CreateAsync(quoteOffer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quote offer for request {RequestId}", createDto.RequestQuoteId);
            throw;
        }
    }

    public async Task<QuoteOfferDetailDto?> GetQuoteOfferByIdAsync(string id)
    {
        try
        {
            var quoteOffer = await GetByIdAsync(id);
            return quoteOffer != null ? MapToDetailDto(quoteOffer) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quote offer {Id}", id);
            throw;
        }
    }

    public async Task<bool> UpdateQuoteOfferAsync(string id, UpdateQuoteOfferDto updateDto)
    {
        try
        {
            var existing = await GetByIdAsync(id);
            if (existing == null)
                return false;

            // Mettre à jour les propriétés modifiables
            existing.Comment = updateDto.Comment ?? existing.Comment;
            existing.UpdatedAt = DateTime.UtcNow;

            return await UpdateAsync(id, existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quote offer {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteQuoteOfferAsync(string id)
    {
        try
        {
            return await DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting quote offer {Id}", id);
            throw;
        }
    }

    public async Task<QuoteOfferSearchResultDto> SearchQuoteOffersAsync(QuoteOfferSearchDto searchDto)
    {
        try
        {
            var filterBuilder = Builders<QuoteOfferModel>.Filter;
            var filters = new List<FilterDefinition<QuoteOfferModel>>();

            // Filtres par propriétés
            if (!string.IsNullOrEmpty(searchDto.ClientNumber))
                filters.Add(filterBuilder.Eq(x => x.ClientNumber, searchDto.ClientNumber));

            if (!string.IsNullOrEmpty(searchDto.RequestQuoteId))
                filters.Add(filterBuilder.Eq(x => x.RequestQuoteId, searchDto.RequestQuoteId));

            if (!string.IsNullOrEmpty(searchDto.Status))
            {
                if (Enum.TryParse<QuoteOfferStatus>(searchDto.Status, true, out var status))
                    filters.Add(filterBuilder.Eq(x => x.Status, status));
            }

            // Filtre par dates
            if (searchDto.CreatedFrom.HasValue)
                filters.Add(filterBuilder.Gte(x => x.CreatedDate, searchDto.CreatedFrom.Value));

            if (searchDto.CreatedTo.HasValue)
                filters.Add(filterBuilder.Lte(x => x.CreatedDate, searchDto.CreatedTo.Value));

            // Filtre de recherche textuelle
            if (!string.IsNullOrEmpty(searchDto.SearchTerm))
            {
                var textFilters = new List<FilterDefinition<QuoteOfferModel>>
                {
                    filterBuilder.Regex(x => x.ClientNumber, new MongoDB.Bson.BsonRegularExpression(searchDto.SearchTerm, "i")),
                    filterBuilder.Regex(x => x.EmailUser, new MongoDB.Bson.BsonRegularExpression(searchDto.SearchTerm, "i")),
                    filterBuilder.Regex(x => x.Comment, new MongoDB.Bson.BsonRegularExpression(searchDto.SearchTerm, "i"))
                };
                filters.Add(filterBuilder.Or(textFilters));
            }

            var finalFilter = filters.Any() 
                ? filterBuilder.And(filters) 
                : FilterDefinition<QuoteOfferModel>.Empty;

            // Tri
            var sort = searchDto.SortBy?.ToLower() switch
            {
                "createdat" => searchDto.SortDirection?.ToLower() == "desc" 
                    ? Builders<QuoteOfferModel>.Sort.Descending(x => x.CreatedDate)
                    : Builders<QuoteOfferModel>.Sort.Ascending(x => x.CreatedDate),
                "updatedat" => searchDto.SortDirection?.ToLower() == "desc"
                    ? Builders<QuoteOfferModel>.Sort.Descending(x => x.UpdatedAt)
                    : Builders<QuoteOfferModel>.Sort.Ascending(x => x.UpdatedAt),
                "client" => searchDto.SortDirection?.ToLower() == "desc"
                    ? Builders<QuoteOfferModel>.Sort.Descending(x => x.ClientNumber)
                    : Builders<QuoteOfferModel>.Sort.Ascending(x => x.ClientNumber),
                _ => Builders<QuoteOfferModel>.Sort.Descending(x => x.CreatedDate)
            };

            var (items, totalCount) = await SearchAsync(finalFilter, searchDto.Page, searchDto.PageSize, sort);

            return new QuoteOfferSearchResultDto
            {
                Items = items.Select(MapToSummaryDto).ToList(),
                TotalCount = (int)totalCount,
                Page = searchDto.Page,
                PageSize = searchDto.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / searchDto.PageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching quote offers");
            throw;
        }
    }

    public async Task<List<QuoteOfferSummaryDto>> GetQuoteOffersByClientAsync(string clientNumber)
    {
        try
        {
            var filter = Builders<QuoteOfferModel>.Filter.Eq(x => x.ClientNumber, clientNumber);
            var sort = Builders<QuoteOfferModel>.Sort.Descending(x => x.CreatedDate);
            
            var (items, _) = await SearchAsync(filter, 1, 100, sort);
            return items.Select(MapToSummaryDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quote offers for client {ClientNumber}", clientNumber);
            throw;
        }
    }

    public async Task<List<QuoteOfferSummaryDto>> GetQuoteOffersByRequestQuoteIdAsync(string requestQuoteId)
    {
        try
        {
            var filter = Builders<QuoteOfferModel>.Filter.Eq(x => x.RequestQuoteId, requestQuoteId);
            var sort = Builders<QuoteOfferModel>.Sort.Descending(x => x.CreatedDate);
            
            var (items, _) = await SearchAsync(filter, 1, 100, sort);
            return items.Select(MapToSummaryDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quote offers for request {RequestQuoteId}", requestQuoteId);
            throw;
        }
    }

    public async Task<bool> UpdateWizardDataAsync(string id, UpdateWizardDataDto wizardData)
    {
        try
        {
            var existing = await GetByIdAsync(id);
            if (existing == null)
                return false;

            // Mettre à jour les données du wizard
            if (existing.OptimizedDraftData == null)
                existing.OptimizedDraftData = new OptimizedDraftData();

            // TODO: Mapper les données du wizard selon les besoins
            existing.UpdatedAt = DateTime.UtcNow;

            return await UpdateAsync(id, existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating wizard data for {Id}", id);
            throw;
        }
    }

    public async Task<OptimizedDraftDataDto?> GetWizardDataAsync(string id)
    {
        try
        {
            var quoteOffer = await GetByIdAsync(id);
            return quoteOffer?.OptimizedDraftData != null ? MapToOptimizedDraftDataDto(quoteOffer.OptimizedDraftData) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wizard data for {Id}", id);
            throw;
        }
    }

    // Implémentations simplifiées pour les autres méthodes
    public Task<bool> AddDraftOptionAsync(string id, DraftOptionDto option) => throw new NotImplementedException();
    public Task<bool> UpdateDraftOptionAsync(string id, string optionId, DraftOptionDto option) => throw new NotImplementedException();
    public Task<bool> DeleteDraftOptionAsync(string id, string optionId) => throw new NotImplementedException();
    public Task<List<DraftOptionDto>> GetDraftOptionsAsync(string id) => throw new NotImplementedException();
    public Task<bool> FinalizeQuoteAsync(string id, FinalizeQuoteDto finalizeDto) => throw new NotImplementedException();
    public Task<bool> SendToClientAsync(string id) => throw new NotImplementedException();
    public Task<bool> ApproveQuoteAsync(string id, string approval) => throw new NotImplementedException();
    public Task<bool> UpdateStatusAsync(string id, QuoteOfferStatus status) => throw new NotImplementedException();
    public Task<QuoteOfferStatsDto> GetStatsAsync(string? clientNumber = null) => throw new NotImplementedException();
    public Task<bool> AddFileAsync(string id, AttachedFileDto file) => throw new NotImplementedException();
    public Task<bool> RemoveFileAsync(string id, string fileName) => throw new NotImplementedException();
    public Task<List<AttachedFileDto>> GetFilesAsync(string id) => throw new NotImplementedException();

    #region Private Methods

    private void InitializeQuoteNumber()
    {
        try
        {
            // Récupérer le dernier numéro de devis utilisé
            var lastQuote = _collection
                .Find(FilterDefinition<QuoteOfferModel>.Empty)
                .Sort(Builders<QuoteOfferModel>.Sort.Descending(x => x.QuoteOfferNumber))
                .FirstOrDefault();

            if (lastQuote != null)
            {
                _nextQuoteNumber = lastQuote.QuoteOfferNumber + 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize quote number, using default");
            _nextQuoteNumber = 1;
        }
    }

    private int GetNextQuoteNumber()
    {
        lock (_lock)
        {
            return _nextQuoteNumber++;
        }
    }

    private QuoteOfferDetailDto MapToDetailDto(QuoteOfferModel model)
    {
        return new QuoteOfferDetailDto
        {
            Id = model.Id,
            RequestQuoteId = model.RequestQuoteId,
            ClientNumber = model.ClientNumber,
            EmailUser = model.EmailUser,
            Comment = model.Comment,
            Status = model.Status.ToString(),
            QuoteOfferNumber = model.QuoteOfferNumber,
            SelectedOption = model.SelectedOption,
            CreatedDate = model.CreatedDate,
            UpdatedAt = model.UpdatedAt,
            ExpirationDate = model.ExpirationDate,
            ClientApproval = model.ClientApproval,
            OptimizedDraftData = model.OptimizedDraftData != null ? MapToOptimizedDraftDataDto(model.OptimizedDraftData) : null,
            Options = model.Options?.Select(MapToOptionDto).ToList() ?? new List<QuoteOptionDto>(),
            Files = model.Files?.Select(MapToFileDto).ToList() ?? new List<AttachedFileDto>()
        };
    }

    private QuoteOfferSummaryDto MapToSummaryDto(QuoteOfferModel model)
    {
        return new QuoteOfferSummaryDto
        {
            Id = model.Id,
            RequestQuoteId = model.RequestQuoteId,
            ClientNumber = model.ClientNumber,
            EmailUser = model.EmailUser,
            Status = model.Status.ToString(),
            QuoteOfferNumber = model.QuoteOfferNumber,
            CreatedDate = model.CreatedDate,
            UpdatedAt = model.UpdatedAt,
            ExpirationDate = model.ExpirationDate
        };
    }

    private OptimizedDraftDataDto MapToOptimizedDraftDataDto(OptimizedDraftData model)
    {
        // TODO: Implémenter le mapping complet
        return new OptimizedDraftDataDto();
    }

    private QuoteOptionDto MapToOptionDto(Models.QuoteOption model)
    {
        return new QuoteOptionDto
        {
            OptionId = model.OptionId,
            Description = model.Description,
            Totals = model.Totals != null ? new OptionTotalsDto
            {
                HaulageTotal = model.Totals.HaulageTotal,
                SeafreightTotal = model.Totals.SeafreightTotal,
                MiscellaneousTotal = model.Totals.MiscellaneousTotal,
                GrandTotal = model.Totals.GrandTotal,
                Currency = model.Totals.Currency
            } : null
        };
    }

    private AttachedFileDto MapToFileDto(AttachedFile model)
    {
        return new AttachedFileDto
        {
            FileName = model.FileName,
            FilePath = model.FilePath,
            FileSize = model.FileSize,
            ContentType = model.ContentType,
            UploadedAt = model.UploadedAt,
            UploadedBy = model.UploadedBy
        };
    }

    #endregion
}
