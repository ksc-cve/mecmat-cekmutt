namespace Mecmat.Domain;

public enum AcquisitionMethod { Purchase, Donation, Transfer, BuiltInHouse }
public enum AssetStatus { InUse, OnLoan, UnderRepair, Broken, Disposed, Lost }
public enum MaintenanceKind { Calibration, Repair, Preventive, Inspection }
public enum MovementKind { In, Out, Adjust }

[IdPrefix("AST")]
public class Asset : Entity
{
    /// <summary>Unit cost from which an item counts as ครุภัณฑ์ (Comptroller General threshold
    /// since FY2563, in satang); cheaper durable items go on the control register.</summary>
    public const long CapitalThreshold = 10_000_00;

    public string? AssetCode { get; set; }
    public string? InternalCode { get; set; }
    public string NameTh { get; set; } = "";
    public string? NameEn { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNo { get; set; }
    public DateOnly? AcquiredDate { get; set; }
    public AcquisitionMethod AcquisitionMethod { get; set; } = AcquisitionMethod.Purchase;
    public string? FundingProjectId { get; set; }
    public string? FundingAccountId { get; set; }
    public long? Cost { get; set; }
    public string? VendorOrganizationId { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Location { get; set; }
    public string? CustodianPersonId { get; set; }
    public int? UsefulLifeYears { get; set; }
    public long? SalvageValue { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.InUse;
    public DateOnly? DisposedDate { get; set; }
    public string? DisposalMethod { get; set; }
    public string? Specifications { get; set; }
    public string? PhotoFileId { get; set; }
    public bool IsPublic { get; set; }
}

[IdPrefix("ALN")]
public class AssetLoan : Entity
{
    public string AssetId { get; set; } = "";
    public string BorrowerPersonId { get; set; } = "";
    public string? ProjectId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? ReturnedDate { get; set; }
    public string? ConditionOut { get; set; }
    public string? ConditionIn { get; set; }
}

[IdPrefix("MNT")]
public class Maintenance : Entity
{
    public string AssetId { get; set; } = "";
    public DateOnly Date { get; set; }
    public MaintenanceKind Kind { get; set; }
    public string? Description { get; set; }
    public long? Cost { get; set; }
    public string? VendorOrganizationId { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public string? CertificateFileId { get; set; }
}

[IdPrefix("MAT")]
public class Material : Entity
{
    public string? Code { get; set; }
    public string NameTh { get; set; } = "";
    public string? NameEn { get; set; }
    public string? Category { get; set; }
    public string Unit { get; set; } = "";
    public decimal? MinStock { get; set; }
    public string? Location { get; set; }
    public string? Note { get; set; }
}

[IdPrefix("MMV")]
public class MaterialMovement : Entity
{
    public string MaterialId { get; set; } = "";
    public DateOnly Date { get; set; }
    public MovementKind Kind { get; set; }
    /// <summary>Signed for Adjust; always positive for In/Out.</summary>
    public decimal Qty { get; set; }
    public long? UnitCost { get; set; }
    public string? ProjectId { get; set; }
    public string? PersonId { get; set; }
    public string? Note { get; set; }
}
