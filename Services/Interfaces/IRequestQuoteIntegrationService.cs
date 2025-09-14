using QuoteOffer.Models;

namespace QuoteOffer.Services.Interfaces;

/// <summary>
/// Interface pour récupérer les données des requêtes de devis depuis le service LIS.QUOTES
/// </summary>
public interface IRequestQuoteIntegrationService
{
    /// <summary>
    /// Récupère les détails d'une requête de devis depuis le service LIS.QUOTES
    /// </summary>
    /// <param name="requestId">ID de la requête</param>
    /// <returns>Données enrichies de la requête ou null si non trouvée</returns>
    Task<RequestQuoteData?> GetRequestQuoteDataAsync(string requestId);
}

/// <summary>
/// Données d'une requête de devis provenant du service LIS.QUOTES
/// </summary>
public class RequestQuoteData
{
    public string RequestQuoteId { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerCompany { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? ContactName { get; set; }
    
    // Locations
    public RequestLocation PickupLocation { get; set; } = new();
    public RequestLocation DeliveryLocation { get; set; } = new();
    
    // Cargo details
    public string? CargoType { get; set; }
    public int? Quantity { get; set; }
    public string? GoodsDescription { get; set; }
    public int? NumberOfUnits { get; set; }
    public double? TotalWeightKg { get; set; }
    public string? TotalDimensions { get; set; }
    public bool IsDangerousGoods { get; set; }
    public bool RequiresTemperatureControl { get; set; }
    public bool IsFragileOrHighValue { get; set; }
    public bool RequiresSpecialHandling { get; set; }
    public string? SpecialInstructions { get; set; }
    
    // Product & Transport
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Incoterm { get; set; }
    public int? PreferredTransportMode { get; set; }
    
    // Dates
    public DateTime? PickupDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    
    // Additional
    public string? PackingType { get; set; }
    public string? Tags { get; set; }
    public string? AdditionalComments { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Localisation dans une requête
/// </summary>
public class RequestLocation
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public string? PostalCode { get; set; }
}
