using QuoteOffer.Models;
using QuoteOffer.DTOs;
using QuoteOfferModel = QuoteOffer.Models.QuoteOffer;

namespace QuoteOffer.Services;

/// <summary>
/// Interface pour le service de gestion des offres de devis
/// </summary>
public interface IQuoteOfferService
{
    // CRUD de base
    Task<string> CreateQuoteOfferAsync(CreateQuoteOfferDto createDto);
    Task<QuoteOfferDetailDto?> GetQuoteOfferByIdAsync(string id);
    Task<bool> UpdateQuoteOfferAsync(string id, UpdateQuoteOfferDto updateDto);
    Task<bool> DeleteQuoteOfferAsync(string id);
    
    // Recherche et pagination
    Task<QuoteOfferSearchResultDto> SearchQuoteOffersAsync(QuoteOfferSearchDto searchDto);
    Task<List<QuoteOfferSummaryDto>> GetQuoteOffersByClientAsync(string clientNumber);
    Task<List<QuoteOfferSummaryDto>> GetQuoteOffersByRequestQuoteIdAsync(string requestQuoteId);
    
    // Gestion du wizard
    Task<bool> UpdateWizardDataAsync(string id, UpdateWizardDataDto wizardData);
    Task<OptimizedDraftDataDto?> GetWizardDataAsync(string id);
    
    // Gestion des options
    Task<bool> AddDraftOptionAsync(string id, DraftOptionDto option);
    Task<bool> UpdateDraftOptionAsync(string id, string optionId, DraftOptionDto option);
    Task<bool> DeleteDraftOptionAsync(string id, string optionId);
    Task<List<DraftOptionDto>> GetDraftOptionsAsync(string id);
    
    // Finalisation
    Task<bool> FinalizeQuoteAsync(string id, FinalizeQuoteDto finalizeDto);
    Task<bool> SendToClientAsync(string id);
    Task<bool> ApproveQuoteAsync(string id, string approval);
    
    // Gestion des statuts
    Task<bool> UpdateStatusAsync(string id, QuoteOfferStatus status);
    
    // Statistiques
    Task<QuoteOfferStatsDto> GetStatsAsync(string? clientNumber = null);
    
    // Gestion des fichiers
    Task<bool> AddFileAsync(string id, AttachedFileDto file);
    Task<bool> RemoveFileAsync(string id, string fileName);
    Task<List<AttachedFileDto>> GetFilesAsync(string id);
}

/// <summary>
/// Service de gestion des offres de devis
/// </summary>
public class QuoteOfferService : IQuoteOfferService
{
    // Pour cette démo, utilisation d'un stockage en mémoire
    // En production, remplacer par une base de données
    private static readonly List<QuoteOfferModel> _quoteOffers = new();
    private static int _nextQuoteNumber = 1;
    private static readonly object _lock = new();

    public async Task<string> CreateQuoteOfferAsync(CreateQuoteOfferDto createDto)
    {
        await Task.Delay(1); // Simulation async
        
        lock (_lock)
        {
            var quoteOffer = new QuoteOfferModel
            {
                Id = Guid.NewGuid().ToString(),
                RequestQuoteId = createDto.RequestQuoteId,
                ClientNumber = createDto.ClientNumber,
                EmailUser = createDto.EmailUser,
                Comment = createDto.Comment,
                Status = QuoteOfferStatus.DRAFT,
                QuoteOfferNumber = _nextQuoteNumber++,
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

            _quoteOffers.Add(quoteOffer);
            return quoteOffer.Id;
        }
    }

    public async Task<QuoteOfferDetailDto?> GetQuoteOfferByIdAsync(string id)
    {
        await Task.Delay(1); // Simulation async
        
        var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
        if (quoteOffer == null) return null;

        return MapToDetailDto(quoteOffer);
    }

    public async Task<bool> UpdateQuoteOfferAsync(string id, UpdateQuoteOfferDto updateDto)
    {
        await Task.Delay(1); // Simulation async
        
        var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
        if (quoteOffer == null) return false;

        if (updateDto.Comment != null)
            quoteOffer.Comment = updateDto.Comment;
        
        if (updateDto.ExpirationDate.HasValue)
            quoteOffer.ExpirationDate = updateDto.ExpirationDate;
        
        if (updateDto.ClientApproval != null)
            quoteOffer.ClientApproval = updateDto.ClientApproval;
        
        if (updateDto.SelectedOption.HasValue)
            quoteOffer.SelectedOption = updateDto.SelectedOption.Value;

        quoteOffer.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public async Task<bool> DeleteQuoteOfferAsync(string id)
    {
        await Task.Delay(1); // Simulation async
        
        lock (_lock)
        {
            var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
            if (quoteOffer == null) return false;

            _quoteOffers.Remove(quoteOffer);
            return true;
        }
    }

    public async Task<QuoteOfferSearchResultDto> SearchQuoteOffersAsync(QuoteOfferSearchDto searchDto)
    {
        await Task.Delay(1); // Simulation async
        
        var query = _quoteOffers.AsQueryable();

        // Filtres
        if (!string.IsNullOrEmpty(searchDto.ClientNumber))
            query = query.Where(q => q.ClientNumber.Contains(searchDto.ClientNumber));

        if (!string.IsNullOrEmpty(searchDto.EmailUser))
            query = query.Where(q => q.EmailUser.Contains(searchDto.EmailUser));

        if (!string.IsNullOrEmpty(searchDto.Status))
            query = query.Where(q => q.Status.ToString() == searchDto.Status);

        if (searchDto.CreatedFrom.HasValue)
            query = query.Where(q => q.CreatedDate >= searchDto.CreatedFrom.Value);

        if (searchDto.CreatedTo.HasValue)
            query = query.Where(q => q.CreatedDate <= searchDto.CreatedTo.Value);

        if (searchDto.QuoteOfferNumber.HasValue)
            query = query.Where(q => q.QuoteOfferNumber == searchDto.QuoteOfferNumber.Value);

        if (!string.IsNullOrEmpty(searchDto.RequestQuoteId))
            query = query.Where(q => q.RequestQuoteId == searchDto.RequestQuoteId);

        // Tri
        if (!string.IsNullOrEmpty(searchDto.SortBy))
        {
            switch (searchDto.SortBy.ToLower())
            {
                case "createddate":
                    query = searchDto.SortOrder?.ToLower() == "asc" 
                        ? query.OrderBy(q => q.CreatedDate)
                        : query.OrderByDescending(q => q.CreatedDate);
                    break;
                case "quotenumber":
                    query = searchDto.SortOrder?.ToLower() == "asc"
                        ? query.OrderBy(q => q.QuoteOfferNumber)
                        : query.OrderByDescending(q => q.QuoteOfferNumber);
                    break;
                case "status":
                    query = searchDto.SortOrder?.ToLower() == "asc"
                        ? query.OrderBy(q => q.Status)
                        : query.OrderByDescending(q => q.Status);
                    break;
                default:
                    query = query.OrderByDescending(q => q.CreatedDate);
                    break;
            }
        }

        var totalCount = query.Count();

        // Pagination
        var items = query
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .Select(MapToSummaryDto)
            .ToList();

        return new QuoteOfferSearchResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / searchDto.PageSize)
        };
    }

    public async Task<List<QuoteOfferSummaryDto>> GetQuoteOffersByClientAsync(string clientNumber)
    {
        await Task.Delay(1); // Simulation async
        
        return _quoteOffers
            .Where(q => q.ClientNumber == clientNumber)
            .OrderByDescending(q => q.CreatedDate)
            .Select(MapToSummaryDto)
            .ToList();
    }

    public async Task<List<QuoteOfferSummaryDto>> GetQuoteOffersByRequestQuoteIdAsync(string requestQuoteId)
    {
        await Task.Delay(1); // Simulation async
        
        return _quoteOffers
            .Where(q => q.RequestQuoteId == requestQuoteId)
            .OrderByDescending(q => q.CreatedDate)
            .Select(MapToSummaryDto)
            .ToList();
    }

    public async Task<bool> UpdateWizardDataAsync(string id, UpdateWizardDataDto wizardData)
    {
        await Task.Delay(1); // Simulation async
        
        var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
        if (quoteOffer == null) return false;

        quoteOffer.OptimizedDraftData = MapFromDraftDataDto(wizardData.OptimizedDraftData);
        quoteOffer.UpdatedAt = DateTime.UtcNow;
        
        return true;
    }

    public async Task<OptimizedDraftDataDto?> GetWizardDataAsync(string id)
    {
        await Task.Delay(1); // Simulation async
        
        var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
        if (quoteOffer?.OptimizedDraftData == null) return null;

        return MapToDraftDataDto(quoteOffer.OptimizedDraftData);
    }

    public async Task<bool> AddDraftOptionAsync(string id, DraftOptionDto option)
    {
        await Task.Delay(1); // Simulation async
        
        var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
        if (quoteOffer?.OptimizedDraftData == null) return false;

        var draftOption = MapFromDraftOptionDto(option);
        quoteOffer.OptimizedDraftData.Options.Add(draftOption);
        quoteOffer.UpdatedAt = DateTime.UtcNow;
        
        return true;
    }

    public async Task<bool> UpdateDraftOptionAsync(string id, string optionId, DraftOptionDto option)
    {
        await Task.Delay(1); // Simulation async
        
        var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
        if (quoteOffer?.OptimizedDraftData == null) return false;

        var existingOption = quoteOffer.OptimizedDraftData.Options.FirstOrDefault(o => o.OptionId == optionId);
        if (existingOption == null) return false;

        var updatedOption = MapFromDraftOptionDto(option);
        updatedOption.OptionId = optionId; // Préserver l'ID
        updatedOption.CreatedAt = existingOption.CreatedAt; // Préserver la date de création
        
        var index = quoteOffer.OptimizedDraftData.Options.IndexOf(existingOption);
        quoteOffer.OptimizedDraftData.Options[index] = updatedOption;
        quoteOffer.UpdatedAt = DateTime.UtcNow;
        
        return true;
    }

    public async Task<bool> DeleteDraftOptionAsync(string id, string optionId)
    {
        await Task.Delay(1); // Simulation async
        
        var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
        if (quoteOffer?.OptimizedDraftData == null) return false;

        var option = quoteOffer.OptimizedDraftData.Options.FirstOrDefault(o => o.OptionId == optionId);
        if (option == null) return false;

        quoteOffer.OptimizedDraftData.Options.Remove(option);
        quoteOffer.UpdatedAt = DateTime.UtcNow;
        
        return true;
    }

    public async Task<List<DraftOptionDto>> GetDraftOptionsAsync(string id)
    {
        await Task.Delay(1); // Simulation async
        
        var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
        if (quoteOffer?.OptimizedDraftData == null) return new List<DraftOptionDto>();

        return quoteOffer.OptimizedDraftData.Options.Select(MapToDraftOptionDto).ToList();
    }

    public async Task<bool> FinalizeQuoteAsync(string id, FinalizeQuoteDto finalizeDto)
    {
        await Task.Delay(1); // Simulation async
        
        var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
        if (quoteOffer == null) return false;

        quoteOffer.Options = finalizeDto.Options.Select(o => new QuoteOption
        {
            OptionId = o.OptionId,
            Description = o.Description,
            Totals = o.Totals != null ? new OptionTotals
            {
                HaulageTotal = o.Totals.HaulageTotal,
                SeafreightTotal = o.Totals.SeafreightTotal,
                MiscellaneousTotal = o.Totals.MiscellaneousTotal,
                GrandTotal = o.Totals.GrandTotal,
                Currency = o.Totals.Currency
            } : null
        }).ToList();

        quoteOffer.SelectedOption = finalizeDto.SelectedOption;
        if (finalizeDto.ExpirationDate.HasValue)
            quoteOffer.ExpirationDate = finalizeDto.ExpirationDate;
        
        quoteOffer.Status = QuoteOfferStatus.SENT_TO_CLIENT;
        quoteOffer.UpdatedAt = DateTime.UtcNow;
        
        return true;
    }

    public async Task<bool> SendToClientAsync(string id)
    {
        await Task.Delay(1); // Simulation async
        
        return await UpdateStatusAsync(id, QuoteOfferStatus.SENT_TO_CLIENT);
    }

    public async Task<bool> ApproveQuoteAsync(string id, string approval)
    {
        await Task.Delay(1); // Simulation async
        
        var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
        if (quoteOffer == null) return false;

        quoteOffer.ClientApproval = approval;
        quoteOffer.Status = approval.ToLower() == "accepted" 
            ? QuoteOfferStatus.ACCEPTED 
            : QuoteOfferStatus.REJECTED;
        quoteOffer.UpdatedAt = DateTime.UtcNow;
        
        return true;
    }

    public async Task<bool> UpdateStatusAsync(string id, QuoteOfferStatus status)
    {
        await Task.Delay(1); // Simulation async
        
        var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
        if (quoteOffer == null) return false;

        quoteOffer.Status = status;
        quoteOffer.UpdatedAt = DateTime.UtcNow;
        
        return true;
    }

    public async Task<QuoteOfferStatsDto> GetStatsAsync(string? clientNumber = null)
    {
        await Task.Delay(1); // Simulation async
        
        var query = _quoteOffers.AsQueryable();
        
        if (!string.IsNullOrEmpty(clientNumber))
            query = query.Where(q => q.ClientNumber == clientNumber);

        var totalOffers = query.Count();
        var draftOffers = query.Count(q => q.Status == QuoteOfferStatus.DRAFT);
        var sentOffers = query.Count(q => q.Status == QuoteOfferStatus.SENT_TO_CLIENT);
        var acceptedOffers = query.Count(q => q.Status == QuoteOfferStatus.ACCEPTED);
        var rejectedOffers = query.Count(q => q.Status == QuoteOfferStatus.REJECTED);
        var expiredOffers = query.Count(q => q.Status == QuoteOfferStatus.EXPIRED);
        
        var conversionRate = sentOffers > 0 ? (decimal)acceptedOffers / sentOffers * 100 : 0;

        return new QuoteOfferStatsDto
        {
            TotalOffers = totalOffers,
            DraftOffers = draftOffers,
            SentOffers = sentOffers,
            AcceptedOffers = acceptedOffers,
            RejectedOffers = rejectedOffers,
            ExpiredOffers = expiredOffers,
            ConversionRate = Math.Round(conversionRate, 2)
        };
    }

    public async Task<bool> AddFileAsync(string id, AttachedFileDto file)
    {
        await Task.Delay(1); // Simulation async
        
        var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
        if (quoteOffer == null) return false;

        quoteOffer.Files.Add(new AttachedFile
        {
            FileName = file.FileName,
            FileUrl = file.FileUrl,
            FileSize = file.FileSize,
            ContentType = file.ContentType
        });
        
        quoteOffer.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public async Task<bool> RemoveFileAsync(string id, string fileName)
    {
        await Task.Delay(1); // Simulation async
        
        var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
        if (quoteOffer == null) return false;

        var file = quoteOffer.Files.FirstOrDefault(f => f.FileName == fileName);
        if (file == null) return false;

        quoteOffer.Files.Remove(file);
        quoteOffer.UpdatedAt = DateTime.UtcNow;
        
        return true;
    }

    public async Task<List<AttachedFileDto>> GetFilesAsync(string id)
    {
        await Task.Delay(1); // Simulation async
        
        var quoteOffer = _quoteOffers.FirstOrDefault(q => q.Id == id);
        if (quoteOffer == null) return new List<AttachedFileDto>();

        return quoteOffer.Files.Select(f => new AttachedFileDto
        {
            FileName = f.FileName,
            FileUrl = f.FileUrl,
            FileSize = f.FileSize,
            ContentType = f.ContentType
        }).ToList();
    }

    // Méthodes de mapping privées
    private static QuoteOfferSummaryDto MapToSummaryDto(QuoteOfferModel model)
    {
        var grandTotal = model.Options.FirstOrDefault(o => o.OptionId == model.SelectedOption.ToString())?.Totals?.GrandTotal;
        
        return new QuoteOfferSummaryDto
        {
            Id = model.Id,
            RequestQuoteId = model.RequestQuoteId,
            ClientNumber = model.ClientNumber,
            EmailUser = model.EmailUser,
            Status = model.Status.ToString(),
            QuoteOfferNumber = model.QuoteOfferNumber,
            CreatedDate = model.CreatedDate,
            ExpirationDate = model.ExpirationDate,
            GrandTotal = grandTotal,
            Currency = "EUR",
            OptionsCount = model.Options.Count
        };
    }

    private static QuoteOfferDetailDto MapToDetailDto(QuoteOfferModel model)
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
            OptimizedDraftData = model.OptimizedDraftData != null ? MapToDraftDataDto(model.OptimizedDraftData) : null,
            Options = model.Options.Select(o => new QuoteOptionDto
            {
                OptionId = o.OptionId,
                Description = o.Description,
                Totals = o.Totals != null ? new OptionTotalsDto
                {
                    HaulageTotal = o.Totals.HaulageTotal,
                    SeafreightTotal = o.Totals.SeafreightTotal,
                    MiscellaneousTotal = o.Totals.MiscellaneousTotal,
                    GrandTotal = o.Totals.GrandTotal,
                    Currency = o.Totals.Currency
                } : null
            }).ToList(),
            Files = model.Files.Select(f => new AttachedFileDto
            {
                FileName = f.FileName,
                FileUrl = f.FileUrl,
                FileSize = f.FileSize,
                ContentType = f.ContentType
            }).ToList()
        };
    }

    private static OptimizedDraftDataDto MapToDraftDataDto(OptimizedDraftData model)
    {
        return new OptimizedDraftDataDto
        {
            Wizard = new WizardMetadataDto
            {
                CurrentStep = model.Wizard.CurrentStep,
                Status = model.Wizard.Status,
                LastModified = model.Wizard.LastModified
            },
            Steps = new WizardStepsDto(), // Mapping détaillé selon les besoins
            Options = model.Options.Select(MapToDraftOptionDto).ToList(),
            PreferredOptionId = model.PreferredOptionId
        };
    }

    private static OptimizedDraftData MapFromDraftDataDto(OptimizedDraftDataDto dto)
    {
        return new OptimizedDraftData
        {
            Wizard = new WizardMetadata
            {
                CurrentStep = dto.Wizard.CurrentStep,
                Status = dto.Wizard.Status,
                LastModified = dto.Wizard.LastModified
            },
            Steps = new WizardSteps(), // Mapping détaillé selon les besoins
            Options = dto.Options.Select(MapFromDraftOptionDto).ToList(),
            PreferredOptionId = dto.PreferredOptionId
        };
    }

    private static DraftOptionDto MapToDraftOptionDto(DraftOption model)
    {
        return new DraftOptionDto
        {
            OptionId = model.OptionId,
            Name = model.Name,
            Description = model.Description,
            MarginType = model.MarginType,
            MarginValue = model.MarginValue,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            HaulageSelectionId = model.HaulageSelectionId,
            SeafreightSelectionIds = model.SeafreightSelectionIds,
            MiscSelectionIds = model.MiscSelectionIds,
            Totals = model.Totals != null ? new DraftOptionTotalsDto
            {
                HaulageTotal = model.Totals.HaulageTotal,
                SeafreightTotal = model.Totals.SeafreightTotal,
                MiscellaneousTotal = model.Totals.MiscellaneousTotal,
                SubTotal = model.Totals.SubTotal,
                MarginAmount = model.Totals.MarginAmount,
                GrandTotal = model.Totals.GrandTotal,
                Currency = model.Totals.Currency
            } : null
        };
    }

    private static DraftOption MapFromDraftOptionDto(DraftOptionDto dto)
    {
        return new DraftOption
        {
            OptionId = string.IsNullOrEmpty(dto.OptionId) ? Guid.NewGuid().ToString() : dto.OptionId,
            Name = dto.Name,
            Description = dto.Description,
            MarginType = dto.MarginType,
            MarginValue = dto.MarginValue,
            CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = dto.CreatedBy,
            HaulageSelectionId = dto.HaulageSelectionId,
            SeafreightSelectionIds = dto.SeafreightSelectionIds,
            MiscSelectionIds = dto.MiscSelectionIds,
            Totals = dto.Totals != null ? new DraftOptionTotals
            {
                HaulageTotal = dto.Totals.HaulageTotal,
                SeafreightTotal = dto.Totals.SeafreightTotal,
                MiscellaneousTotal = dto.Totals.MiscellaneousTotal,
                SubTotal = dto.Totals.SubTotal,
                MarginAmount = dto.Totals.MarginAmount,
                GrandTotal = dto.Totals.GrandTotal,
                Currency = dto.Totals.Currency
            } : null
        };
    }
}
