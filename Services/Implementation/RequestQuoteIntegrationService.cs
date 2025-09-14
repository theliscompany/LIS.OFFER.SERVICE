using QuoteOffer.Services.Interfaces;
using System.Text.Json;

namespace QuoteOffer.Services.Implementation;

/// <summary>
/// Service d'intégration avec le service LIS.QUOTES pour récupérer les données des requêtes
/// </summary>
public class RequestQuoteIntegrationService : IRequestQuoteIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RequestQuoteIntegrationService> _logger;
    private readonly string _quotesServiceBaseUrl;

    public RequestQuoteIntegrationService(
        HttpClient httpClient,
        ILogger<RequestQuoteIntegrationService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _quotesServiceBaseUrl = configuration.GetValue<string>("Services:LisQuotes:BaseUrl") 
            ?? "http://localhost:5116"; // URL par défaut du service LIS.QUOTES
    }

    public async Task<RequestQuoteData?> GetRequestQuoteDataAsync(string requestId)
    {
        try
        {
            _logger.LogInformation("Retrieving request quote data for {RequestId} from LIS.QUOTES service", requestId);
            
            var response = await _httpClient.GetAsync($"{_quotesServiceBaseUrl}/api/Request/{requestId}");
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Request quote {RequestId} not found in LIS.QUOTES service", requestId);
                    return null;
                }
                
                _logger.LogError("Failed to retrieve request quote {RequestId}: {StatusCode}", requestId, response.StatusCode);
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var requestQuoteResponse = JsonSerializer.Deserialize<RequestQuoteResponseFromService>(jsonContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (requestQuoteResponse == null)
            {
                _logger.LogError("Failed to deserialize request quote response for {RequestId}", requestId);
                return null;
            }

            // Mapper la réponse du service vers notre modèle interne
            return MapToRequestQuoteData(requestQuoteResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving request quote data for {RequestId}", requestId);
            return null;
        }
    }

    private RequestQuoteData MapToRequestQuoteData(RequestQuoteResponseFromService response)
    {
        return new RequestQuoteData
        {
            RequestQuoteId = response.RequestQuoteId,
            CustomerId = response.CustomerId,
            CustomerCompany = response.CompanyName ?? "",
            CustomerEmail = response.Email ?? "",
            CustomerPhone = response.Phone,
            ContactName = response.ContactFullName,
            
            PickupLocation = new RequestLocation
            {
                City = response.PickupLocation?.City ?? "",
                Country = response.PickupLocation?.Country ?? "",
                AddressLine = response.PickupLocation?.AddressLine,
                PostalCode = response.PickupLocation?.PostalCode
            },
            
            DeliveryLocation = new RequestLocation
            {
                City = response.DeliveryLocation?.City ?? "",
                Country = response.DeliveryLocation?.Country ?? "",
                AddressLine = response.DeliveryLocation?.AddressLine,
                PostalCode = response.DeliveryLocation?.PostalCode
            },
            
            CargoType = response.CargoType?.ToString(),
            Quantity = response.Quantity,
            GoodsDescription = response.GoodsDescription,
            NumberOfUnits = response.NumberOfUnits,
            TotalWeightKg = response.TotalWeightKg,
            TotalDimensions = response.TotalDimensions,
            IsDangerousGoods = response.IsDangerousGoods,
            RequiresTemperatureControl = response.RequiresTemperatureControl,
            IsFragileOrHighValue = response.IsFragileOrHighValue,
            RequiresSpecialHandling = response.RequiresSpecialHandling,
            SpecialInstructions = response.SpecialInstructions,
            
            ProductId = response.ProductId,
            ProductName = response.ProductName,
            Incoterm = response.Incoterm,
            PreferredTransportMode = response.PreferredTransportMode,
            
            PickupDate = response.PickupDate,
            DeliveryDate = response.DeliveryDate,
            
            PackingType = response.PackingType,
            Tags = response.Tags,
            AdditionalComments = response.AdditionalComments,
            CreatedAt = response.CreatedAt
        };
    }
}

/// <summary>
/// DTO pour la réponse du service LIS.QUOTES (basé sur RequestQuoteResponseViewModel)
/// </summary>
public class RequestQuoteResponseFromService
{
    public string RequestQuoteId { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string? CompanyName { get; set; }
    public string? ContactFullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    
    public LocationFromService? PickupLocation { get; set; }
    public LocationFromService? DeliveryLocation { get; set; }
    
    public int? CargoType { get; set; }
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
    
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Incoterm { get; set; }
    public int? PreferredTransportMode { get; set; }
    
    public DateTime? PickupDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    
    public string? PackingType { get; set; }
    public string? Tags { get; set; }
    public string? AdditionalComments { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LocationFromService
{
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? AddressLine { get; set; }
    public string? PostalCode { get; set; }
}
