namespace QuoteOffer.DTOs;

/// <summary>
/// DTO pour créer une nouvelle offre de devis
/// </summary>
public class CreateQuoteOfferDto
{
    public string RequestQuoteId { get; set; } = string.Empty;
    public string ClientNumber { get; set; } = string.Empty;
    public string EmailUser { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

/// <summary>
/// DTO pour mettre à jour une offre existante
/// </summary>
public class UpdateQuoteOfferDto
{
    public string? Comment { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? ClientApproval { get; set; }
    public int? SelectedOption { get; set; }
}

/// <summary>
/// DTO pour la recherche d'offres avec filtres
/// </summary>
public class QuoteOfferSearchDto
{
    public string? ClientNumber { get; set; }
    public string? EmailUser { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public int? QuoteOfferNumber { get; set; }
    public string? RequestQuoteId { get; set; }
    
    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    // Tri
    public string? SortBy { get; set; } = "CreatedDate";
    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// DTO pour les résultats de recherche paginés
/// </summary>
public class QuoteOfferSearchResultDto
{
    public List<QuoteOfferSummaryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// DTO résumé d'une offre pour les listes
/// </summary>
public class QuoteOfferSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string RequestQuoteId { get; set; } = string.Empty;
    public string ClientNumber { get; set; } = string.Empty;
    public string EmailUser { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int QuoteOfferNumber { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal? GrandTotal { get; set; }
    public string Currency { get; set; } = "EUR";
    public int OptionsCount { get; set; }
}

/// <summary>
/// DTO complet d'une offre avec tous les détails
/// </summary>
public class QuoteOfferDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string RequestQuoteId { get; set; } = string.Empty;
    public string ClientNumber { get; set; } = string.Empty;
    public string EmailUser { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string Status { get; set; } = string.Empty;
    public int QuoteOfferNumber { get; set; }
    public int SelectedOption { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? ClientApproval { get; set; }
    
    // Données optimisées du wizard
    public OptimizedDraftDataDto? OptimizedDraftData { get; set; }
    
    // Options finalisées
    public List<QuoteOptionDto> Options { get; set; } = new();
    
    // Fichiers attachés
    public List<AttachedFileDto> Files { get; set; } = new();
}

/// <summary>
/// DTO pour une option de devis
/// </summary>
public class QuoteOptionDto
{
    public string OptionId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public OptionTotalsDto? Totals { get; set; }
}

/// <summary>
/// DTO pour les totaux d'une option
/// </summary>
public class OptionTotalsDto
{
    public decimal HaulageTotal { get; set; }
    public decimal SeafreightTotal { get; set; }
    public decimal MiscellaneousTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "EUR";
}

/// <summary>
/// DTO pour un fichier attaché
/// </summary>
public class AttachedFileDto
{
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// DTO pour les données optimisées du wizard
/// </summary>
public class OptimizedDraftDataDto
{
    public WizardMetadataDto Wizard { get; set; } = new();
    public WizardStepsDto Steps { get; set; } = new();
    public List<DraftOptionDto> Options { get; set; } = new();
    public string? PreferredOptionId { get; set; }
}

/// <summary>
/// DTO pour les métadonnées du wizard
/// </summary>
public class WizardMetadataDto
{
    public int CurrentStep { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
}

/// <summary>
/// DTO pour les étapes du wizard
/// </summary>
public class WizardStepsDto
{
    public OptimizedStep1Dto? Step1 { get; set; }
    public OptimizedStep2Dto? Step2 { get; set; }
    public OptimizedStep3Dto? Step3 { get; set; }
    public OptimizedStep4Dto? Step4 { get; set; }
    public OptimizedStep5Dto? Step5 { get; set; }
    public OptimizedStep6Dto? Step6 { get; set; }
    public OptimizedStep7Dto? Step7 { get; set; }
}

// DTOs pour chaque étape du wizard
public class OptimizedStep1Dto
{
    public RouteDataDto? Route { get; set; }
    public CustomerDataDto? Customer { get; set; }
}

public class OptimizedStep2Dto
{
    public List<string> SelectedServices { get; set; } = new();
}

public class OptimizedStep3Dto
{
    public List<ContainerDataDto> Containers { get; set; } = new();
    public ContainerSummaryDto? Summary { get; set; }
}

public class OptimizedStep4Dto
{
    public HaulageSelectionDto? Selection { get; set; }
    public HaulageCalculationDto? Calculation { get; set; }
}

public class OptimizedStep5Dto
{
    public List<SeafreightPricingSelectionDto> Selections { get; set; } = new();
    public SeafreightSummaryDto? Summary { get; set; }
}

public class OptimizedStep6Dto
{
    public List<MiscSelectionDto> Selections { get; set; } = new();
    public MiscSummaryDto? Summary { get; set; }
}

public class OptimizedStep7Dto
{
    public FinalizationDataDto? Finalization { get; set; }
}

// DTOs de support
public class RouteDataDto
{
    public PortLocationDto? Origin { get; set; }
    public PortLocationDto? Destination { get; set; }
}

public class PortLocationDto
{
    public PortDataDto? Port { get; set; }
}

public class PortDataDto
{
    public string PortName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PortCode { get; set; } = string.Empty;
}

public class CustomerDataDto
{
    public string ContactName { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ContainerDataDto
{
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TEU { get; set; }
}

public class ContainerSummaryDto
{
    public int TotalContainers { get; set; }
    public decimal TotalTEU { get; set; }
}

public class HaulageSelectionDto
{
    public int HaulierId { get; set; }
    public string HaulierName { get; set; } = string.Empty;
    public string? OfferId { get; set; }
    public decimal OvertimePrice { get; set; }
    public int OvertimeQuantity { get; set; }
}

public class HaulageCalculationDto
{
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public decimal OvertimeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "EUR";
}

public class SeafreightPricingSelectionDto
{
    public string Id { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public CarrierInfoDto Carrier { get; set; } = new();
    public RouteInfoDto Route { get; set; } = new();
    public ContainerPricingDto Container { get; set; } = new();
    public ChargeDetailsDto Charges { get; set; } = new();
    public decimal GrandTotal { get; set; }
}

public class CarrierInfoDto
{
    public string CarrierName { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
}

public class RouteInfoDto
{
    public int TransitDays { get; set; }
}

public class ContainerPricingDto
{
    public decimal Subtotal { get; set; }
}

public class ChargeDetailsDto
{
    public decimal TotalPrice { get; set; }
}

public class SeafreightSummaryDto
{
    public decimal TotalAmount { get; set; }
}

public class MiscSelectionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public MiscPricingDto Pricing { get; set; } = new();
}

public class MiscPricingDto
{
    public decimal Subtotal { get; set; }
}

public class MiscSummaryDto
{
    public decimal TotalAmount { get; set; }
}

public class FinalizationDataDto
{
    public string OptionName { get; set; } = string.Empty;
    public string? OptionDescription { get; set; }
}

/// <summary>
/// DTO pour une option de brouillon
/// </summary>
public class DraftOptionDto
{
    public string OptionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MarginType { get; set; } = string.Empty;
    public decimal MarginValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    
    public string? HaulageSelectionId { get; set; }
    public List<string> SeafreightSelectionIds { get; set; } = new();
    public List<string> MiscSelectionIds { get; set; } = new();
    
    public DraftOptionTotalsDto? Totals { get; set; }
}

/// <summary>
/// DTO pour les totaux d'une option de brouillon
/// </summary>
public class DraftOptionTotalsDto
{
    public decimal HaulageTotal { get; set; }
    public decimal SeafreightTotal { get; set; }
    public decimal MiscellaneousTotal { get; set; }
    public decimal SubTotal { get; set; }
    public decimal MarginAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "EUR";
}

/// <summary>
/// DTO pour la mise à jour des données de wizard
/// </summary>
public class UpdateWizardDataDto
{
    public OptimizedDraftDataDto OptimizedDraftData { get; set; } = new();
}

/// <summary>
/// DTO pour la finalisation d'un devis
/// </summary>
public class FinalizeQuoteDto
{
    public List<QuoteOptionDto> Options { get; set; } = new();
    public int SelectedOption { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

/// <summary>
/// DTO pour les statistiques des offres
/// </summary>
public class QuoteOfferStatsDto
{
    public int TotalOffers { get; set; }
    public int DraftOffers { get; set; }
    public int SentOffers { get; set; }
    public int AcceptedOffers { get; set; }
    public int RejectedOffers { get; set; }
    public int ExpiredOffers { get; set; }
    public decimal ConversionRate { get; set; }
}
