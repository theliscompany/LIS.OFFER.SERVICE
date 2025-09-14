namespace QuoteOffer.DTOs;

#region Draft Creation DTOs

/// <summary>
/// DTO pour créer un brouillon à partir d'une demande de devis
/// </summary>
public class CreateDraftFromRequestDto
{
    public string RequestId { get; set; } = string.Empty;
    public string ClientNumber { get; set; } = string.Empty;
    public string EmailUser { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public OptimizedDraftDataDto? InitialWizardData { get; set; }
}

/// <summary>
/// Réponse lors de la création d'un brouillon
/// </summary>
public class CreateDraftResponse
{
    public string DraftId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ResumeToken { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

#endregion

#region Draft Detail DTOs

/// <summary>
/// Réponse détaillée d'un brouillon (format similaire à votre payload exemple)
/// </summary>
public class DraftDetailResponse
{
    public string DraftId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public string ResumeToken { get; set; } = string.Empty;
    
    public DraftHeaderDto Header { get; set; } = new();
    public OptimizedDraftDataDto? WizardData { get; set; }
    public List<DraftOptionDto> Options { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
}

/// <summary>
/// Header du brouillon (informations principales)
/// </summary>
public class DraftHeaderDto
{
    public DraftClientDto Client { get; set; } = new();
    public DraftShipmentDto Shipment { get; set; } = new();
    public DraftCommercialTermsDto CommercialTerms { get; set; } = new();
}

/// <summary>
/// Informations client dans le header
/// </summary>
public class DraftClientDto
{
    public string Company { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

/// <summary>
/// Informations d'expédition dans le header
/// </summary>
public class DraftShipmentDto
{
    public bool FromRequest { get; set; }
    public List<string> Readonly { get; set; } = new();
    public string CargoType { get; set; } = string.Empty;
    public string GoodsDescription { get; set; } = string.Empty;
    public LocationDto Origin { get; set; } = new();
    public LocationDto Destination { get; set; } = new();
    public DateTime RequestedDeparture { get; set; }
}

/// <summary>
/// Localisation (origine/destination)
/// </summary>
public class LocationDto
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

/// <summary>
/// Termes commerciaux dans le header
/// </summary>
public class DraftCommercialTermsDto
{
    public string Currency { get; set; } = "EUR";
    public string Incoterm { get; set; } = string.Empty;
    public int ValidityDays { get; set; } = 15;
    public bool CgvAccepted { get; set; }
}

#endregion

#region Draft Validation DTOs

/// <summary>
/// Réponse de validation d'un brouillon
/// </summary>
public class DraftValidationResponse
{
    public bool IsValid { get; set; }
    public bool CanFinalize { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public ValidationChecks? Checks { get; set; }
}

/// <summary>
/// Détail des vérifications de validation
/// </summary>
public class ValidationChecks
{
    public bool DatesConsistency { get; set; }
    public bool SurchargesValidity { get; set; }
    public bool CgvPresent { get; set; }
    public bool RatesPerContainer { get; set; }
    public string? Message { get; set; }
    
    // Propriétés manquantes pour DraftsController
    public bool HasRequiredData { get; set; }
    public bool HasValidRoute { get; set; }
    public bool HasContainers { get; set; }
    public bool HasPricing { get; set; }
    public bool ReadyForFinalization { get; set; }
}

#endregion

#region Extended Wizard Data DTOs

/// <summary>
/// Données étendues du wizard pour correspondre à votre payload
/// </summary>
public class ExtendedWizardDataDto
{
    public GeneralRequestInformationDto? GeneralRequestInformation { get; set; }
    public RoutingAndCargoDto? RoutingAndCargo { get; set; }
    public List<SeafreightDto> Seafreights { get; set; } = new();
    public List<HaulageDto> Haulages { get; set; } = new();
    public List<ServiceDto> Services { get; set; } = new();
    
    // Propriétés manquantes pour DraftsController
    public string? CurrentStep { get; set; }
    public string? Status { get; set; }
    public DateTime? LastModified { get; set; }
    public QuoteOffer.Models.RouteData? Route { get; set; }
    public QuoteOffer.Models.CustomerData? Customer { get; set; }
    public List<QuoteOffer.Models.ContainerData>? Containers { get; set; }
    public QuoteOffer.Models.OptimizedStep4? Haulage { get; set; }
    public List<QuoteOffer.Models.SeafreightPricingSelection>? SeafreightSelections { get; set; }
    public List<QuoteOffer.Models.MiscSelection>? MiscServices { get; set; }
}

/// <summary>
/// Informations générales de la demande
/// </summary>
public class GeneralRequestInformationDto
{
    public string Channel { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// Données de routage et cargo
/// </summary>
public class RoutingAndCargoDto
{
    public string PortOfLoading { get; set; } = string.Empty;
    public string PortOfDestination { get; set; } = string.Empty;
    public CargoDto Cargo { get; set; } = new();
}

/// <summary>
/// Informations cargo
/// </summary>
public class CargoDto
{
    public List<CargoItemDto> Items { get; set; } = new();
    public bool Hazmat { get; set; }
    public string GoodsDescription { get; set; } = string.Empty;
}

/// <summary>
/// Item de cargo (conteneur)
/// </summary>
public class CargoItemDto
{
    public string ContainerType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int GrossWeightKg { get; set; }
    public decimal VolumeM3 { get; set; }
}

/// <summary>
/// Données seafreight
/// </summary>
public class SeafreightDto
{
    public string Id { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public DateTime Etd { get; set; }
    public DateTime Eta { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime ValidUntil { get; set; }
    public List<RateDto> Rates { get; set; } = new();
    public List<SurchargeDto> Surcharges { get; set; } = new();
    public FreeTimeDto? FreeTime { get; set; }
    
    // Propriétés manquantes pour DraftsController
    public string? CarrierName { get; set; }
    public string? AgentName { get; set; }
    public int TransitDays { get; set; }
    public decimal TotalPrice { get; set; }
}

/// <summary>
/// Tarif par type de conteneur
/// </summary>
public class RateDto
{
    public string ContainerType { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
}

/// <summary>
/// Surcharge
/// </summary>
public class SurchargeDto
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Calc { get; set; } = string.Empty; // "percent" ou "flat"
    public string? Base { get; set; } // "seafreight" pour les pourcentages
    public string Unit { get; set; } = string.Empty; // "per_container"
    public decimal Value { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool Taxable { get; set; }
    public List<string> AppliesTo { get; set; } = new();
}

/// <summary>
/// Temps de franchise
/// </summary>
public class FreeTimeDto
{
    public FreeTimePeriodDto Origin { get; set; } = new();
    public FreeTimePeriodDto Destination { get; set; } = new();
}

/// <summary>
/// Période de temps de franchise
/// </summary>
public class FreeTimePeriodDto
{
    public int Days { get; set; }
}

/// <summary>
/// Données de transport routier
/// </summary>
public class HaulageDto
{
    public string Id { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty; // "pre-carriage", "on-carriage"
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Currency { get; set; } = "EUR";
    public List<HaulagePricingDto> Pricing { get; set; } = new();
    public string? Notes { get; set; }
    
    // Propriétés manquantes pour DraftsController
    public string? HaulierId { get; set; }
    public string? HaulierName { get; set; }
    public string? OfferId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Tarification transport routier
/// </summary>
public class HaulagePricingDto
{
    public string ContainerType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty; // "per_container"
    public decimal Price { get; set; }
    public int IncludedWaitingHours { get; set; }
    public decimal ExtraHourPrice { get; set; }
}

/// <summary>
/// Service additionnel
/// </summary>
public class ServiceDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty; // "per_shipment"
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public bool Taxable { get; set; }
    public decimal? TaxRate { get; set; }
    
    // Propriété manquante pour DraftsController
    public string? ServiceName { get; set; }
}

#endregion

#region Extended Draft Option DTOs

/// <summary>
/// Option de brouillon étendue (format de votre payload)
/// </summary>
public class ExtendedDraftOptionDto
{
    public string OptionId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<OptionContainerDto> Containers { get; set; } = new();
    public string SeafreightRef { get; set; } = string.Empty;
    public List<string> HaulageRefs { get; set; } = new();
    public List<string> ServiceRefs { get; set; } = new();
    public OptionSuppliersDto Suppliers { get; set; } = new();
    public List<ServiceOfferedDto> ServicesOffered { get; set; } = new();
    public ScheduleDto Schedule { get; set; } = new();
    public MilestonesDto Milestones { get; set; } = new();
    public PricingPreviewDto PricingPreview { get; set; } = new();
}

/// <summary>
/// Conteneur dans une option
/// </summary>
public class OptionContainerDto
{
    public string ContainerType { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

/// <summary>
/// Fournisseurs d'une option
/// </summary>
public class OptionSuppliersDto
{
    public string Carrier { get; set; } = string.Empty;
    public List<string> Haulage { get; set; } = new();
    public List<ServiceProviderDto> Services { get; set; } = new();
}

/// <summary>
/// Fournisseur de service
/// </summary>
public class ServiceProviderDto
{
    public string ServiceId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}

/// <summary>
/// Service offert dans une option
/// </summary>
public class ServiceOfferedDto
{
    public string Type { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// Planning d'une option
/// </summary>
public class ScheduleDto
{
    public DateTime Etd { get; set; }
    public DateTime Eta { get; set; }
}

/// <summary>
/// Jalons importants d'une option
/// </summary>
public class MilestonesDto
{
    public DateTime Pickup { get; set; }
    public DateTime Vgm { get; set; }
    public DateTime Si { get; set; }
    public DateTime Cutoff { get; set; }
}

/// <summary>
/// Aperçu tarifaire d'une option
/// </summary>
public class PricingPreviewDto
{
    public string Currency { get; set; } = "EUR";
    public List<PricingLineDto> Lines { get; set; } = new();
    public PricingSubtotalsDto Subtotals { get; set; } = new();
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
}

/// <summary>
/// Ligne de tarification
/// </summary>
public class PricingLineDto
{
    public string Kind { get; set; } = string.Empty; // "seafreight", "haulage", "service"
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Qty { get; set; }
    public bool Taxable { get; set; }
    public decimal? TaxRate { get; set; }
}

/// <summary>
/// Sous-totaux de tarification
/// </summary>
public class PricingSubtotalsDto
{
    public decimal TaxableBase { get; set; }
    public decimal NontaxableBase { get; set; }
}

#endregion

#region Additional DTOs for DraftsController

/// <summary>
/// DTO pour créer un nouveau brouillon
/// </summary>
public class CreateDraftRequest
{
    public string RequestQuoteId { get; set; } = string.Empty;
    public string ClientNumber { get; set; } = string.Empty;
    public string EmailUser { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

/// <summary>
/// Réponse complète d'un brouillon
/// </summary>
public class DraftResponse
{
    public string Id { get; set; } = string.Empty;
    public string RequestQuoteId { get; set; } = string.Empty;
    public string ClientNumber { get; set; } = string.Empty;
    public string EmailUser { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime LastModified { get; set; }
    
    public ExtendedWizardDataDto WizardData { get; set; } = new();
    public List<DraftOptionSummary> Options { get; set; } = new();
    public ValidationResult ValidationStatus { get; set; } = new();
}

/// <summary>
/// Résumé d'une option de brouillon
/// </summary>
public class DraftOptionSummary
{
    public string OptionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Résultat de validation d'un brouillon
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public ValidationChecks Checks { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Demande d'ajout d'option à un brouillon
/// </summary>
public class AddDraftOptionRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MarginType { get; set; } = "percentage";
    public decimal MarginValue { get; set; }
    public string? HaulageSelectionId { get; set; }
    public List<string> SeafreightSelectionIds { get; set; } = new();
    public List<string> MiscSelectionIds { get; set; } = new();
}

/// <summary>
/// Demande de recherche de brouillons
/// </summary>
public class DraftSearchRequest
{
    public string? ClientNumber { get; set; }
    public string? EmailUser { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string? Query { get; set; }
    
    // Pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Résumé d'un brouillon pour les listes
/// </summary>
public class DraftSummary
{
    public string Id { get; set; } = string.Empty;
    public string RequestQuoteId { get; set; } = string.Empty;
    public string ClientNumber { get; set; } = string.Empty;
    public string EmailUser { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime LastModified { get; set; }
    public int WizardStep { get; set; }
    public int OptionsCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour les données de route
/// </summary>
public class RouteDto
{
    public PortDto Origin { get; set; } = new();
    public PortDto Destination { get; set; } = new();
}

/// <summary>
/// DTO pour les données de port
/// </summary>
public class PortDto
{
    public string PortName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PortCode { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour les données client
/// </summary>
public class CustomerDto
{
    public string ContactName { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour les données de conteneur
/// </summary>
public class ContainerDto
{
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TEU { get; set; }
}

#endregion
