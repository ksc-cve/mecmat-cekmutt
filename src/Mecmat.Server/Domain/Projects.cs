namespace Mecmat.Domain;

public enum ProjectKind { Program, Project, SubProject, AcademicService, Consulting, Internal }
public enum ProjectStatus { Idea, Drafting, Submitted, UnderReview, Funded, Active, Extended, Completed, Closed, Rejected, Cancelled }
public enum ProjectRole { PI, CoPI, Researcher, ResearchAssistant, Student, Consultant, IndustryPartner, Coordinator, FinanceContact }
public enum PartnerRole { Funder, CoFunder, Industry, Collaborator, Beneficiary }
public enum MilestoneStatus { Pending, Done, Late, Waived }
/// <summary>The ten TSRI (สกสว.) output types.</summary>
public enum OutputType
{
    Manpower, Manuscript, Book, Prototype, IntellectualProperty, Infrastructure, DatabaseSystem,
    Network, Investment, PolicyRecommendation
}
public enum OutcomeType { Publication, Citation, Tool, Database, NextDestination, Award, FacilityUse, IpLicensing, Other }
public enum ImpactDimension { Academic, Social, Economic, Environmental, Policy }
public enum ReportKind { Progress, Annual, Final, Financial, Other }
public enum ReportStatus { Upcoming, Due, Submitted, Accepted, Revise }
public enum RecruitPositionType { ResearchAssistant, MasterStudent, PhdStudent, Postdoc, Intern, Engineer, Other }
public enum RecruitmentStatus { Draft, Open, Closed, Filled }
public enum ApplicationStatus { New, Shortlisted, Interview, Offered, Accepted, Rejected, Withdrawn }

[IdPrefix("PRJ")]
public class Project : Entity
{
    public string? Code { get; set; }
    public string? ExternalCode { get; set; }
    public ProjectKind Kind { get; set; } = ProjectKind.Project;
    public string? ParentProjectId { get; set; }
    public string TitleTh { get; set; } = "";
    public string? TitleEn { get; set; }
    public string? KeywordsTh { get; set; }
    public string? KeywordsEn { get; set; }
    public string? Summary { get; set; }
    public string? Rationale { get; set; }
    public string? Objectives { get; set; }
    public string? Framework { get; set; }
    public string? Methodology { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Idea;
    public string? FunderOrganizationId { get; set; }
    public string? FundingScheme { get; set; }
    public string? ContractNo { get; set; }
    public int? FiscalYear { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? ExtendedEndDate { get; set; }
    public long? TotalBudget { get; set; }
    public decimal? InstitutionFeePct { get; set; }
    public int? TrlStart { get; set; }
    public int? TrlTarget { get; set; }
    public int? TrlCurrent { get; set; }
    public int? SrlStart { get; set; }
    public int? SrlTarget { get; set; }
    public int? SrlCurrent { get; set; }
    public string? ResearchSite { get; set; }
    public string? BeneficiaryArea { get; set; }
    public bool IsPublic { get; set; }
    public string? PublicSummaryTh { get; set; }
    public string? PublicSummaryEn { get; set; }
    public string? CoverFileId { get; set; }
}

[IdPrefix("PMB")]
public class ProjectMember : Entity
{
    public string ProjectId { get; set; } = "";
    public string PersonId { get; set; } = "";
    public ProjectRole Role { get; set; } = ProjectRole.Researcher;
    public int? EffortPct { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public bool IsPublic { get; set; } = true;
}

[IdPrefix("PPT")]
public class ProjectPartner : Entity
{
    public string ProjectId { get; set; } = "";
    public string OrganizationId { get; set; } = "";
    public PartnerRole Role { get; set; }
    public long? InCashAmount { get; set; }
    public long? InKindAmount { get; set; }
    public string? InKindDescription { get; set; }
}

[IdPrefix("ACT")]
public class Activity : Entity
{
    public string ProjectId { get; set; } = "";
    public string? ParentActivityId { get; set; }
    public string Name { get; set; } = "";
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int ProgressPct { get; set; }
    public int? WeightPct { get; set; }
    public string? OwnerPersonId { get; set; }
    public string? Deliverable { get; set; }
    public string? DependsOnActivityId { get; set; }
    public int Order { get; set; }
}

[IdPrefix("MLS")]
public class Milestone : Entity
{
    public string ProjectId { get; set; } = "";
    public string Name { get; set; } = "";
    public DateOnly? DueDate { get; set; }
    public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;
    public DateOnly? DoneDate { get; set; }
    public string? Note { get; set; }
}

[IdPrefix("OUT")]
public class ProjectOutput : Entity
{
    public string ProjectId { get; set; } = "";
    public OutputType OutputType { get; set; }
    public string Description { get; set; } = "";
    public decimal? TargetQty { get; set; }
    public string? Unit { get; set; }
    public decimal? ActualQty { get; set; }
    public DateOnly? AchievedDate { get; set; }
    public string? LinkedType { get; set; }
    public string? LinkedId { get; set; }
}

[IdPrefix("OCM")]
public class ProjectOutcome : Entity
{
    public string ProjectId { get; set; } = "";
    public OutcomeType OutcomeType { get; set; }
    public string Description { get; set; } = "";
    public string? Evidence { get; set; }
    public DateOnly? Date { get; set; }
}

[IdPrefix("IMP")]
public class ProjectImpact : Entity
{
    public string ProjectId { get; set; } = "";
    public ImpactDimension Dimension { get; set; }
    public string Description { get; set; } = "";
    public string? Indicator { get; set; }
    public decimal? Value { get; set; }
    public string? Unit { get; set; }
}

[IdPrefix("OKR")]
public class KeyResult : Entity
{
    public string? ProjectId { get; set; }
    public string Objective { get; set; } = "";
    public string KeyResultText { get; set; } = "";
    public decimal? Target { get; set; }
    public decimal? Actual { get; set; }
    public string? Unit { get; set; }
    public string? Period { get; set; }
}

[IdPrefix("RPT")]
public class ProgressReport : Entity
{
    public string ProjectId { get; set; } = "";
    public ReportKind Kind { get; set; }
    public int? PeriodNo { get; set; }
    public DateOnly? PeriodFrom { get; set; }
    public DateOnly? PeriodTo { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? SubmittedDate { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Upcoming;
    public string? Summary { get; set; }
    public string? FileId { get; set; }
}

[IdPrefix("RCT")]
public class Recruitment : Entity
{
    public string? ProjectId { get; set; }
    public string TitleTh { get; set; } = "";
    public string? TitleEn { get; set; }
    public RecruitPositionType PositionType { get; set; }
    public string? DescriptionTh { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Qualifications { get; set; }
    public string? CompensationText { get; set; }
    public int? Seats { get; set; }
    public DateOnly? OpenDate { get; set; }
    public DateOnly? CloseDate { get; set; }
    public RecruitmentStatus Status { get; set; } = RecruitmentStatus.Draft;
    public bool IsPublic { get; set; } = true;
}

[IdPrefix("APL")]
public class Application : Entity
{
    public string RecruitmentId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? Message { get; set; }
    public string? CvFileId { get; set; }
    public string? ConsentRecordId { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.New;
    public string? ReviewerNotes { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? IpHash { get; set; }
}
