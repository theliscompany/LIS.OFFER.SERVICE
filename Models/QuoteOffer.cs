namespace QuoteOffer.Models;

/// <summary>
/// Entité principale représentant une offre de devis ou un brouillon
/// </summary>
public class QuoteOffer
{
    public string Id { get; set; } = string.Empty;
    public string RequestQuoteId { get; set; } = string.Empty;
    public string ClientNumber { get; set; } = string.Empty;
    public string EmailUser { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public QuoteOfferStatus Status { get; set; }
    public int QuoteOfferNumber { get; set; }
    public int SelectedOption { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? ClientApproval { get; set; }
    
    // Structure des données optimisées pour le wizard
    public OptimizedDraftData? OptimizedDraftData { get; set; }
    
    // Options sauvegardées (pour les devis finalisés)
    public List<QuoteOption> Options { get; set; } = new();
    
    // Fichiers attachés
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
    public string OptionId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public OptionTotals? Totals { get; set; }
}

/// <summary>
/// Totaux d'une option
/// </summary>
public class OptionTotals
{
    public decimal HaulageTotal { get; set; }
    public decimal SeafreightTotal { get; set; }
    public decimal MiscellaneousTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "EUR";
}

/// <summary>
/// Fichier attaché
/// </summary>
public class AttachedFile
{
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
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
