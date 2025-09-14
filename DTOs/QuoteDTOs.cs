namespace QuoteOffer.DTOs;

#region Quote Finalization DTOs

/// <summary>
/// DTO pour finaliser un brouillon en devis
/// </summary>
public class FinalizeDraftRequest
{
    public List<QuoteOptionRequest> Options { get; set; } = new();
    public int PreferredOptionId { get; set; }
    public string? QuoteComments { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public bool SendToClient { get; set; } = true;
}

/// <summary>
/// Option dans une demande de finalisation
/// </summary>
public class QuoteOptionRequest
{
    public int OptionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public QuoteOptionPricingRequest Pricing { get; set; } = new();
    public HaulageOptionRequest? Haulage { get; set; }
    public SeaFreightOptionRequest? SeaFreight { get; set; }
    public List<MiscellaneousOptionRequest>? Miscellaneous { get; set; }
}

/// <summary>
/// Tarification d'une option de devis
/// </summary>
public class QuoteOptionPricingRequest
{
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "EUR";
    public List<PricingBreakdownRequest>? Breakdown { get; set; }
}

/// <summary>
/// Détail tarifaire
/// </summary>
public class PricingBreakdownRequest
{
    public string Category { get; set; } = string.Empty; // "seafreight", "haulage", "services"
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>
/// Transport routier dans une option
/// </summary>
public class HaulageOptionRequest
{
    public string Provider { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public QuoteOptionPricingRequest Pricing { get; set; } = new();
}

/// <summary>
/// Transport maritime dans une option
/// </summary>
public class SeaFreightOptionRequest
{
    public string Carrier { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public DateTime Etd { get; set; }
    public DateTime Eta { get; set; }
    public QuoteOptionPricingRequest Pricing { get; set; } = new();
}

/// <summary>
/// Service divers dans une option
/// </summary>
public class MiscellaneousOptionRequest
{
    public string ServiceName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public QuoteOptionPricingRequest Pricing { get; set; } = new();
}

#endregion

#region Quote Response DTOs

/// <summary>
/// Réponse complète d'un devis
/// </summary>
public class QuoteResponse
{
    public string Id { get; set; } = string.Empty;
    public int QuoteNumber { get; set; }
    public string RequestQuoteId { get; set; } = string.Empty;
    public string ClientNumber { get; set; } = string.Empty;
    public string EmailUser { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ClientApproval { get; set; }
    
    public List<QuoteOptionResponse> Options { get; set; } = new();
    public int PreferredOptionId { get; set; }
    public QuoteSummaryDto Summary { get; set; } = new();
    
    public DateTime ExpirationDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModified { get; set; }
    public List<string>? Files { get; set; }
}

/// <summary>
/// Option dans la réponse d'un devis
/// </summary>
public class QuoteOptionResponse
{
    public int OptionId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public QuoteOptionTotalsDto Totals { get; set; } = new();
    public QuoteOptionDetailsDto? Details { get; set; }
}

/// <summary>
/// Totaux d'une option de devis
/// </summary>
public class QuoteOptionTotalsDto
{
    public decimal HaulageTotal { get; set; }
    public decimal SeaFreightTotal { get; set; }
    public decimal MiscellaneousTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "EUR";
}

/// <summary>
/// Détails d'une option de devis
/// </summary>
public class QuoteOptionDetailsDto
{
    public string? HaulageProvider { get; set; }
    public string? SeaFreightCarrier { get; set; }
    public string? Route { get; set; }
    public DateTime? Etd { get; set; }
    public DateTime? Eta { get; set; }
    public List<string> ServicesIncluded { get; set; } = new();
}

/// <summary>
/// Résumé d'un devis
/// </summary>
public class QuoteSummaryDto
{
    public string Route { get; set; } = string.Empty;
    public int TotalOptions { get; set; }
    public decimal BestPrice { get; set; }
    public decimal HighestPrice { get; set; }
    public string Currency { get; set; } = "EUR";
    public string PreferredOptionDescription { get; set; } = string.Empty;
}

#endregion

#region Quote Search DTOs

/// <summary>
/// Critères de recherche pour les devis
/// </summary>
public class QuoteSearchRequest
{
    public string? ClientNumber { get; set; }
    public string? EmailUser { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public DateTime? ExpirationFrom { get; set; }
    public DateTime? ExpirationTo { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? Query { get; set; } // Recherche textuelle libre
    
    // Pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

#endregion

#region Quote Actions DTOs

/// <summary>
/// Demande de sélection d'option préférée
/// </summary>
public class SelectPreferredOptionRequest
{
    public int OptionId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Demande de changement de statut
/// </summary>
public class ChangeQuoteStatusRequest
{
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public bool NotifyClient { get; set; } = false;
}

/// <summary>
/// Demande d'approbation client
/// </summary>
public class ClientApprovalRequest
{
    public string Approval { get; set; } = string.Empty; // "accepted" ou "rejected"
    public string? Comments { get; set; }
    public int? SelectedOptionId { get; set; } // Si accepté
}

#endregion

#region Common API Response DTOs

/// <summary>
/// Réponse API commune
/// </summary>
public class CommonApiResponse<T>
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public object? Meta { get; set; }

    public static CommonApiResponse<T> Success(T data, string message = "Success")
    {
        return new CommonApiResponse<T>
        {
            Code = 200,
            Message = message,
            Data = data
        };
    }

    public static CommonApiResponse<T> Created(T data, string message = "Created")
    {
        return new CommonApiResponse<T>
        {
            Code = 201,
            Message = message,
            Data = data
        };
    }

    public static CommonApiResponse<T> NotFound(string message = "Not found")
    {
        return new CommonApiResponse<T>
        {
            Code = 404,
            Message = message
        };
    }

    public static CommonApiResponse<T> ValidationFailed(string message, IEnumerable<string>? errors = null)
    {
        return new CommonApiResponse<T>
        {
            Code = 400,
            Message = message,
            Errors = errors?.ToList()
        };
    }

    public static CommonApiResponse<T> Error(int code, string message, string? detail = null)
    {
        var response = new CommonApiResponse<T>
        {
            Code = code,
            Message = message
        };

        if (!string.IsNullOrEmpty(detail))
        {
            response.Errors = new List<string> { detail };
        }

        return response;
    }
}

#endregion
