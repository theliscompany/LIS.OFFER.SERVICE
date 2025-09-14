using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace QuoteOffer.Models;

/// <summary>
/// Entité principale représentant une offre de devis ou un brouillon
/// </summary>
[BsonCollection("quote_offers")]
public class QuoteOffer
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;
    
    [BsonElement("request_quote_id")]
    public string RequestQuoteId { get; set; } = string.Empty;
    
    [BsonElement("client_number")]
    public string ClientNumber { get; set; } = string.Empty;
    
    [BsonElement("email_user")]
    public string EmailUser { get; set; } = string.Empty;
    
    [BsonElement("comment")]
    public string? Comment { get; set; }
    
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public QuoteOfferStatus Status { get; set; }
    
    [BsonElement("quote_offer_number")]
    public int QuoteOfferNumber { get; set; }
    
    [BsonElement("selected_option")]
    public int SelectedOption { get; set; }
    
    [BsonElement("created_date")]
    public DateTime CreatedDate { get; set; }
    
    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [BsonElement("expiration_date")]
    public DateTime? ExpirationDate { get; set; }
    
    [BsonElement("client_approval")]
    public string? ClientApproval { get; set; }
    
    // Structure des données optimisées pour le wizard
    [BsonElement("optimized_draft_data")]
    public OptimizedDraftData? OptimizedDraftData { get; set; }
    
    // Options sauvegardées (pour les devis finalisés)
    [BsonElement("options")]
    public List<QuoteOption> Options { get; set; } = new();
    
    // Fichiers attachés
    [BsonElement("files")]
    public List<AttachedFile> Files { get; set; } = new();
}

/// <summary>
/// Statuts possibles d'une offre
/// </summary>
public enum QuoteOfferStatus
{
    DRAFT,
    SENT_TO_CLIENT,
    PENDING_APPROVAL,
    ACCEPTED,
    REJECTED,
    EXPIRED
}

/// <summary>
/// Option d'un devis avec totaux calculés
/// </summary>
public class QuoteOption
{
    [BsonElement("option_id")]
    public string OptionId { get; set; } = string.Empty;
    
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;
    
    [BsonElement("totals")]
    public OptionTotals? Totals { get; set; }
}

/// <summary>
/// Totaux d'une option
/// </summary>
public class OptionTotals
{
    [BsonElement("haulage_total")]
    public decimal HaulageTotal { get; set; }
    
    [BsonElement("seafreight_total")]
    public decimal SeafreightTotal { get; set; }
    
    [BsonElement("miscellaneous_total")]
    public decimal MiscellaneousTotal { get; set; }
    
    [BsonElement("grand_total")]
    public decimal GrandTotal { get; set; }
    
    [BsonElement("currency")]
    public string Currency { get; set; } = "EUR";
}

/// <summary>
/// Fichier attaché
/// </summary>
public class AttachedFile
{
    [BsonElement("file_name")]
    public string FileName { get; set; } = string.Empty;
    
    [BsonElement("file_path")]
    public string FilePath { get; set; } = string.Empty;
    
    [BsonElement("file_url")]
    public string FileUrl { get; set; } = string.Empty;
    
    [BsonElement("file_size")]
    public long FileSize { get; set; }
    
    [BsonElement("content_type")]
    public string ContentType { get; set; } = string.Empty;
    
    [BsonElement("uploaded_at")]
    public DateTime UploadedAt { get; set; }
    
    [BsonElement("uploaded_by")]
    public string UploadedBy { get; set; } = string.Empty;
}

/// <summary>
/// Structure optimisée pour les données du wizard de création de devis
/// </summary>
public class OptimizedDraftData
{
    public WizardMetadata Wizard { get; set; } = new();
    public WizardSteps Steps { get; set; } = new();
    public List<DraftOption> Options { get; set; } = new();
    public string? PreferredOptionId { get; set; }
    
    // Nouvelles données enrichies pour le schéma DraftQuotes
    public EnrichedWizardData? EnrichedData { get; set; }
}

/// <summary>
/// Données enrichies pour le nouveau schéma DraftQuotes
/// </summary>
public class EnrichedWizardData
{
    public GeneralRequestInfo GeneralRequestInformation { get; set; } = new();
    public RoutingAndCargo RoutingAndCargo { get; set; } = new();
    public List<SeafreightData> Seafreights { get; set; } = new();
    public List<HaulageData> Haulages { get; set; } = new();
    public List<ServiceData> Services { get; set; } = new();
}

/// <summary>
/// Informations générales de la demande (persisté)
/// </summary>
public class GeneralRequestInfo
{
    public string Channel { get; set; } = string.Empty;
    public string Priority { get; set; } = "normal";
    public string? Notes { get; set; }
}

/// <summary>
/// Données de routing et cargo (persisté)
/// </summary>
public class RoutingAndCargo
{
    public string PortOfLoading { get; set; } = string.Empty;
    public string PortOfDestination { get; set; } = string.Empty;
    public CargoData Cargo { get; set; } = new();
}

/// <summary>
/// Données de cargo (persisté)
/// </summary>
public class CargoData
{
    public List<CargoItem> Items { get; set; } = new();
    public bool Hazmat { get; set; } = false;
    public string GoodsDescription { get; set; } = string.Empty;
}

/// <summary>
/// Item de cargo (persisté)
/// </summary>
public class CargoItem
{
    public string ContainerType { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal GrossWeightKg { get; set; }
    public decimal VolumeM3 { get; set; }
}

/// <summary>
/// Données de fret maritime (persisté)
/// </summary>
public class SeafreightData
{
    public string Id { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public DateTime Etd { get; set; }
    public DateTime Eta { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime ValidUntil { get; set; }
    public List<ContainerRate> Rates { get; set; } = new();
    public List<SurchargeData> Surcharges { get; set; } = new();
    public FreeTimeData? FreeTime { get; set; }
}

/// <summary>
/// Tarif par conteneur (persisté)
/// </summary>
public class ContainerRate
{
    public string ContainerType { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
}

/// <summary>
/// Surcharge (persisté)
/// </summary>
public class SurchargeData
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Calc { get; set; } = string.Empty;
    public string? Base { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool Taxable { get; set; }
    public List<string> AppliesTo { get; set; } = new();
}

/// <summary>
/// Temps de franchise (persisté)
/// </summary>
public class FreeTimeData
{
    public FreeTimePeriod Origin { get; set; } = new();
    public FreeTimePeriod Destination { get; set; } = new();
}

/// <summary>
/// Période de temps de franchise (persisté)
/// </summary>
public class FreeTimePeriod
{
    public int Days { get; set; }
}

/// <summary>
/// Données de transport routier (persisté)
/// </summary>
public class HaulageData
{
    public string Id { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Currency { get; set; } = "EUR";
    public List<HaulagePricing> Pricing { get; set; } = new();
    public string? Notes { get; set; }
}

/// <summary>
/// Tarification transport routier (persisté)
/// </summary>
public class HaulagePricing
{
    public string ContainerType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int IncludedWaitingHours { get; set; } = 1;
    public decimal ExtraHourPrice { get; set; }
}

/// <summary>
/// Données de service (persisté)
/// </summary>
public class ServiceData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool Taxable { get; set; }
    public decimal? TaxRate { get; set; }
}

/// <summary>
/// Métadonnées du wizard
/// </summary>
public class WizardMetadata
{
    public int CurrentStep { get; set; } = 1;
    public string Status { get; set; } = "not_started";
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Étapes du wizard
/// </summary>
public class WizardSteps
{
    public OptimizedStep1? Step1 { get; set; }
    public OptimizedStep2? Step2 { get; set; }
    public OptimizedStep3? Step3 { get; set; }
    public OptimizedStep4? Step4 { get; set; }
    public OptimizedStep5? Step5 { get; set; }
    public OptimizedStep6? Step6 { get; set; }
    public OptimizedStep7? Step7 { get; set; }
}

// Classes pour chaque étape du wizard
public class OptimizedStep1
{
    public RouteData? Route { get; set; }
    public CustomerData? Customer { get; set; }
}

public class OptimizedStep2
{
    public List<string> SelectedServices { get; set; } = new();
}

public class OptimizedStep3
{
    public List<ContainerData> Containers { get; set; } = new();
    public ContainerSummary? Summary { get; set; }
}

public class OptimizedStep4
{
    public HaulageSelection? Selection { get; set; }
    public HaulageCalculation? Calculation { get; set; }
}

public class OptimizedStep5
{
    public List<SeafreightPricingSelection> Selections { get; set; } = new();
    public SeafreightSummary? Summary { get; set; }
}

public class OptimizedStep6
{
    public List<MiscSelection> Selections { get; set; } = new();
    public MiscSummary? Summary { get; set; }
}

public class OptimizedStep7
{
    public FinalizationData? Finalization { get; set; }
}

// Classes de support
public class RouteData
{
    public PortLocation? Origin { get; set; }
    public PortLocation? Destination { get; set; }
}

public class PortLocation
{
    public PortData? Port { get; set; }
}

public class PortData
{
    public string PortName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PortCode { get; set; } = string.Empty;
}

public class CustomerData
{
    public string ContactName { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ContainerData
{
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TEU { get; set; }
}

public class ContainerSummary
{
    public int TotalContainers { get; set; }
    public decimal TotalTEU { get; set; }
}

public class HaulageSelection
{
    public int HaulierId { get; set; }
    public string HaulierName { get; set; } = string.Empty;
    public string? OfferId { get; set; }
    public decimal OvertimePrice { get; set; }
    public int OvertimeQuantity { get; set; }
}

public class HaulageCalculation
{
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public decimal OvertimeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "EUR";
}

public class SeafreightPricingSelection
{
    public string Id { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public CarrierInfo Carrier { get; set; } = new();
    public RouteInfo Route { get; set; } = new();
    public ContainerPricing Container { get; set; } = new();
    public ChargeDetails Charges { get; set; } = new();
    public decimal GrandTotal { get; set; }
}

public class CarrierInfo
{
    public string CarrierName { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
}

public class RouteInfo
{
    public int TransitDays { get; set; }
}

public class ContainerPricing
{
    public decimal Subtotal { get; set; }
}

public class ChargeDetails
{
    public decimal TotalPrice { get; set; }
}

public class SeafreightSummary
{
    public decimal TotalAmount { get; set; }
}

public class MiscSelection
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public MiscPricing Pricing { get; set; } = new();
}

public class MiscPricing
{
    public decimal Subtotal { get; set; }
}

public class MiscSummary
{
    public decimal TotalAmount { get; set; }
}

public class FinalizationData
{
    public string OptionName { get; set; } = string.Empty;
    public string? OptionDescription { get; set; }
}

/// <summary>
/// Option sauvegardée dans un brouillon
/// </summary>
public class DraftOption
{
    public string OptionId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MarginType { get; set; } = "percentage";
    public decimal MarginValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    
    // IDs des sélections pour reconstituer l'option
    public string? HaulageSelectionId { get; set; }
    public List<string> SeafreightSelectionIds { get; set; } = new();
    public List<string> MiscSelectionIds { get; set; } = new();
    
    public DraftOptionTotals? Totals { get; set; }
}

/// <summary>
/// Totaux calculés d'une option de brouillon
/// </summary>
public class DraftOptionTotals
{
    public decimal HaulageTotal { get; set; }
    public decimal SeafreightTotal { get; set; }
    public decimal MiscellaneousTotal { get; set; }
    public decimal SubTotal { get; set; }
    public decimal MarginAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "EUR";
}
