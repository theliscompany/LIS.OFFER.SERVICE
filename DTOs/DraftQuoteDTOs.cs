using System.ComponentModel.DataAnnotations;

namespace QuoteOffer.DTOs
{
    #region Root Draft Quote DTO

    /// <summary>
    /// DTO principal pour un brouillon de devis selon le nouveau schéma
    /// </summary>
    public class DraftQuoteDto
    {
        public string DraftQuoteId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public string Status { get; set; } = "draft";
        public int Version { get; set; } = 1;
        public string? ResumeToken { get; set; }
        public DraftQuoteHeaderDto Header { get; set; } = new();
        public DraftQuoteWizardDataDto WizardData { get; set; } = new();
        public List<DraftQuoteOptionDto> Options { get; set; } = new();
        public DraftQuoteValidationDto Validation { get; set; } = new();
        public DraftQuoteAuditDto Audit { get; set; } = new();
    }

    #endregion

    #region Header DTOs

    /// <summary>
    /// Header du brouillon de devis
    /// </summary>
    public class DraftQuoteHeaderDto
    {
        public DraftQuoteClientDto Client { get; set; } = new();
        public DraftQuoteShipmentDto Shipment { get; set; } = new();
        public DraftQuoteCommercialTermsDto CommercialTerms { get; set; } = new();
    }

    /// <summary>
    /// Informations client
    /// </summary>
    public class DraftQuoteClientDto
    {
        public string Company { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    /// <summary>
    /// Informations envoi
    /// </summary>
    public class DraftQuoteShipmentDto
    {
        public bool FromRequest { get; set; } = true;
        public List<string> Readonly { get; set; } = new();
        public string CargoType { get; set; } = string.Empty;
        public string GoodsDescription { get; set; } = string.Empty;
        public DraftQuoteLocationDto Origin { get; set; } = new();
        public DraftQuoteLocationDto Destination { get; set; } = new();
        public DateTime? RequestedDeparture { get; set; }
    }

    /// <summary>
    /// Localisation (origine/destination)
    /// </summary>
    public class DraftQuoteLocationDto
    {
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }

    /// <summary>
    /// Conditions commerciales
    /// </summary>
    public class DraftQuoteCommercialTermsDto
    {
        public string Currency { get; set; } = "EUR";
        public string Incoterm { get; set; } = "CIF";
        public int ValidityDays { get; set; } = 15;
        public bool CgvAccepted { get; set; } = false;
    }

    #endregion

    #region Wizard Data DTOs

    /// <summary>
    /// Données du wizard enrichies
    /// </summary>
    public class DraftQuoteWizardDataDto
    {
        public DraftQuoteGeneralRequestInfoDto GeneralRequestInformation { get; set; } = new();
        public DraftQuoteRoutingAndCargoDto RoutingAndCargo { get; set; } = new();
        public List<DraftQuoteSeafreightDto> Seafreights { get; set; } = new();
        public List<DraftQuoteHaulageDto> Haulages { get; set; } = new();
        public List<DraftQuoteServiceDto> Services { get; set; } = new();
    }

    /// <summary>
    /// Informations générales de la demande
    /// </summary>
    public class DraftQuoteGeneralRequestInfoDto
    {
        public string Channel { get; set; } = string.Empty;
        public string Priority { get; set; } = "normal";
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Routing et cargo
    /// </summary>
    public class DraftQuoteRoutingAndCargoDto
    {
        public string PortOfLoading { get; set; } = string.Empty;
        public string PortOfDestination { get; set; } = string.Empty;
        public DraftQuoteCargoDto Cargo { get; set; } = new();
    }

    /// <summary>
    /// Informations cargo
    /// </summary>
    public class DraftQuoteCargoDto
    {
        public List<DraftQuoteCargoItemDto> Items { get; set; } = new();
        public bool Hazmat { get; set; } = false;
        public string GoodsDescription { get; set; } = string.Empty;
    }

    /// <summary>
    /// Item de cargo (conteneur)
    /// </summary>
    public class DraftQuoteCargoItemDto
    {
        public string ContainerType { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal GrossWeightKg { get; set; }
        public decimal VolumeM3 { get; set; }
    }

    /// <summary>
    /// Fret maritime enrichi
    /// </summary>
    public class DraftQuoteSeafreightDto
    {
        public string Id { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string Service { get; set; } = string.Empty;
        public DateTime Etd { get; set; }
        public DateTime Eta { get; set; }
        public string Currency { get; set; } = "EUR";
        public DateTime ValidUntil { get; set; }
        public List<DraftQuoteRateDto> Rates { get; set; } = new();
        public List<DraftQuoteSurchargeDto> Surcharges { get; set; } = new();
        public DraftQuoteFreeTimeDto? FreeTime { get; set; }
    }

    /// <summary>
    /// Tarif par type de conteneur
    /// </summary>
    public class DraftQuoteRateDto
    {
        public string ContainerType { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
    }

    /// <summary>
    /// Surcharge enrichie
    /// </summary>
    public class DraftQuoteSurchargeDto
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
    public class DraftQuoteFreeTimeDto
    {
        public DraftQuoteFreeTimePeriodDto Origin { get; set; } = new();
        public DraftQuoteFreeTimePeriodDto Destination { get; set; } = new();
    }

    /// <summary>
    /// Période de temps de franchise
    /// </summary>
    public class DraftQuoteFreeTimePeriodDto
    {
        public int Days { get; set; }
    }

    /// <summary>
    /// Transport routier enrichi
    /// </summary>
    public class DraftQuoteHaulageDto
    {
        public string Id { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty; // "pre-carriage", "on-carriage"
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string Currency { get; set; } = "EUR";
        public List<DraftQuoteHaulagePricingDto> Pricing { get; set; } = new();
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Tarification transport routier enrichie
    /// </summary>
    public class DraftQuoteHaulagePricingDto
    {
        public string ContainerType { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty; // "per_container"
        public decimal Price { get; set; }
        public int IncludedWaitingHours { get; set; } = 1;
        public decimal ExtraHourPrice { get; set; }
    }

    /// <summary>
    /// Service enrichi
    /// </summary>
    public class DraftQuoteServiceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty; // "per_shipment"
        public decimal Price { get; set; }
        public string Currency { get; set; } = "EUR";
        public bool Taxable { get; set; }
        public decimal? TaxRate { get; set; }
    }

    #endregion

    #region Options DTOs

    /// <summary>
    /// Option de devis enrichie
    /// </summary>
    public class DraftQuoteOptionDto
    {
        public string OptionId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public List<DraftQuoteOptionContainerDto> Containers { get; set; } = new();
        public string? SeafreightRef { get; set; }
        public List<string> HaulageRefs { get; set; } = new();
        public List<string> ServiceRefs { get; set; } = new();
        public DraftQuoteOptionSuppliersDto Suppliers { get; set; } = new();
        public List<DraftQuoteServiceOfferedDto> ServicesOffered { get; set; } = new();
        public DraftQuoteScheduleDto? Schedule { get; set; }
        public DraftQuoteMilestonesDto? Milestones { get; set; }
        public DraftQuotePricingPreviewDto? PricingPreview { get; set; }
    }

    /// <summary>
    /// Conteneur dans l'option
    /// </summary>
    public class DraftQuoteOptionContainerDto
    {
        public string ContainerType { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// Fournisseurs de l'option
    /// </summary>
    public class DraftQuoteOptionSuppliersDto
    {
        public string? Carrier { get; set; }
        public List<string> Haulage { get; set; } = new();
        public List<DraftQuoteOptionServiceProviderDto> Services { get; set; } = new();
    }

    /// <summary>
    /// Fournisseur de service dans l'option
    /// </summary>
    public class DraftQuoteOptionServiceProviderDto
    {
        public string ServiceId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
    }

    /// <summary>
    /// Service offert
    /// </summary>
    public class DraftQuoteServiceOfferedDto
    {
        public string Type { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    /// <summary>
    /// Planning
    /// </summary>
    public class DraftQuoteScheduleDto
    {
        public DateTime Etd { get; set; }
        public DateTime Eta { get; set; }
    }

    /// <summary>
    /// Jalons
    /// </summary>
    public class DraftQuoteMilestonesDto
    {
        public DateTime? Pickup { get; set; }
        public DateTime? Vgm { get; set; }
        public DateTime? Si { get; set; }
        public DateTime? Cutoff { get; set; }
    }

    /// <summary>
    /// Aperçu de pricing détaillé
    /// </summary>
    public class DraftQuotePricingPreviewDto
    {
        public string Currency { get; set; } = "EUR";
        public List<DraftQuotePricingLineDto> Lines { get; set; } = new();
        public DraftQuotePricingSubtotalsDto Subtotals { get; set; } = new();
        public decimal TaxTotal { get; set; }
        public decimal GrandTotal { get; set; }
    }

    /// <summary>
    /// Ligne de pricing
    /// </summary>
    public class DraftQuotePricingLineDto
    {
        public string Kind { get; set; } = string.Empty; // "seafreight", "haulage", "service"
        public string Description { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Qty { get; set; } = 1;
        public bool Taxable { get; set; }
        public decimal? TaxRate { get; set; }
    }

    /// <summary>
    /// Sous-totaux
    /// </summary>
    public class DraftQuotePricingSubtotalsDto
    {
        public decimal TaxableBase { get; set; }
        public decimal NontaxableBase { get; set; }
    }

    #endregion

    #region Validation DTOs

    /// <summary>
    /// Validation du brouillon
    /// </summary>
    public class DraftQuoteValidationDto
    {
        public List<DraftQuoteValidationCheckDto> Checks { get; set; } = new();
        public List<DraftQuoteValidationWarningDto> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Vérification de validation
    /// </summary>
    public class DraftQuoteValidationCheckDto
    {
        public string Rule { get; set; } = string.Empty;
        public bool Ok { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Avertissement de validation
    /// </summary>
    public class DraftQuoteValidationWarningDto
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    #endregion

    #region Audit DTOs

    /// <summary>
    /// Audit trail
    /// </summary>
    public class DraftQuoteAuditDto
    {
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string LastUpdatedBy { get; set; } = string.Empty;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public DraftQuoteStepProgressDto StepProgress { get; set; } = new();
    }

    /// <summary>
    /// Progression des étapes
    /// </summary>
    public class DraftQuoteStepProgressDto
    {
        public string GeneralRequestInformation { get; set; } = "not_started";
        public string RoutingAndCargo { get; set; } = "not_started";
        public string Seafreights { get; set; } = "not_started";
        public string Haulages { get; set; } = "not_started";
        public string Services { get; set; } = "not_started";
        public string Options { get; set; } = "not_started";
    }

    #endregion

    #region Request DTOs

    /// <summary>
    /// Requête pour créer un nouveau brouillon de devis
    /// </summary>
    public class CreateDraftQuoteRequest
    {
        [Required]
        public string RequestId { get; set; } = string.Empty;
        public DraftQuoteHeaderDto? Header { get; set; }
        public DraftQuoteWizardDataDto? WizardData { get; set; }
    }

    /// <summary>
    /// Requête pour mettre à jour un brouillon de devis
    /// </summary>
    public class UpdateDraftQuoteRequest
    {
        public DraftQuoteHeaderDto? Header { get; set; }
        public DraftQuoteWizardDataDto? WizardData { get; set; }
        public List<DraftQuoteOptionDto>? Options { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Requête pour ajouter/modifier une option
    /// </summary>
    public class AddDraftQuoteOptionRequest
    {
        [Required]
        public DraftQuoteOptionDto Option { get; set; } = new();
    }

    /// <summary>
    /// Réponse enrichie pour brouillon de devis
    /// </summary>
    public class DraftQuoteResponse
    {
        public string DraftQuoteId { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Version { get; set; }
        public string? ResumeToken { get; set; }
        public DraftQuoteHeaderDto Header { get; set; } = new();
        public DraftQuoteWizardDataDto WizardData { get; set; } = new();
        public List<DraftQuoteOptionDto> Options { get; set; } = new();
        public DraftQuoteValidationDto Validation { get; set; } = new();
        public DraftQuoteAuditDto Audit { get; set; } = new();
        public int TotalOptions { get; set; }
        public decimal? BestPrice { get; set; }
        public string Currency { get; set; } = "EUR";
    }

    #endregion
}
