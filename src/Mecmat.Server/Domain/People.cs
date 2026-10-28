namespace Mecmat.Domain;

public enum PersonKind { Internal, External }
public enum PersonCategory
{
    Faculty, Researcher, Postdoc, ResearchAssistant, PhdStudent, MasterStudent, BachelorStudent,
    Intern, Staff, Visiting, IndustryPartner, Collaborator, Sponsor
}
public enum AcademicRank { None, AsstProf, AssocProf, Prof }
public enum EducationLevel { Bachelor, Master, Doctoral, Diploma, Certificate, Exchange, Other }
public enum OrganizationKind { University, Faculty, Department, Company, Government, FundingAgency, Ngo, Other }
public enum PayoutKind { BankAccount, PromptPay }
public enum AwardLevel { International, National, Institutional, Other }

[IdPrefix("PER")]
public class Person : Entity
{
    public PersonKind Kind { get; set; } = PersonKind.Internal;
    public PersonCategory Category { get; set; } = PersonCategory.Researcher;
    public string? TitleTh { get; set; }
    public string? TitleEn { get; set; }
    public AcademicRank AcademicRank { get; set; } = AcademicRank.None;
    public bool HasDoctorate { get; set; }
    public string FirstNameTh { get; set; } = "";
    public string LastNameTh { get; set; } = "";
    public string? FirstNameEn { get; set; }
    public string? LastNameEn { get; set; }
    public string? Nickname { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [Sensitive] public string? NationalId { get; set; }
    public string? WorkEmail { get; set; }
    public string? PersonalEmail { get; set; }
    public string? WorkPhone { get; set; }
    public string? MobilePhone { get; set; }
    public string? LineId { get; set; }
    public string? OfficeRoom { get; set; }
    public string? PhotoFileId { get; set; }
    public string? OrganizationId { get; set; }
    public string? PositionText { get; set; }
    public string? AdvisorPersonId { get; set; }
    public string? StudentId { get; set; }
    public string? ProgramName { get; set; }
    public int? EnrolledYear { get; set; }
    public int? ExpectedGraduation { get; set; }
    public string? ExpertiseTh { get; set; }
    public string? ExpertiseEn { get; set; }
    public string? BioTh { get; set; }
    public string? BioEn { get; set; }
    public string? OrcidId { get; set; }
    public string? ScopusAuthorId { get; set; }
    public string? GoogleScholarId { get; set; }
    public string? ResearcherId { get; set; }
    public string? ResearchGateUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public DateOnly? JoinedDate { get; set; }
    public DateOnly? LeftDate { get; set; }
    public string? PublicSlug { get; set; }
    public int DisplayOrder { get; set; } = 100;
}

[IdPrefix("EDU")]
public class PersonEducation : Entity
{
    public string PersonId { get; set; } = "";
    public EducationLevel Level { get; set; }
    public string? DegreeNameTh { get; set; }
    public string? DegreeNameEn { get; set; }
    public string? Field { get; set; }
    public string? Institution { get; set; }
    public string? Country { get; set; }
    public int? StartYear { get; set; }
    public int? GraduatedYear { get; set; }
    public string? Honors { get; set; }
    public string? ThesisTitle { get; set; }
    public bool IsPublic { get; set; } = true;
}

[IdPrefix("EMP")]
public class PersonEmployment : Entity
{
    public string PersonId { get; set; } = "";
    public string? OrganizationId { get; set; }
    public string? OrganizationText { get; set; }
    public string Position { get; set; } = "";
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;
}

[IdPrefix("APT")]
public class AcademicAppointment : Entity
{
    public string PersonId { get; set; } = "";
    public AcademicRank Rank { get; set; }
    public string? FieldName { get; set; }
    public string? FieldCode { get; set; }
    public string? SubField { get; set; }
    public int? Method { get; set; }
    public DateOnly? AppointedDate { get; set; }
    public string? ReferenceText { get; set; }
}

[IdPrefix("TCH")]
public class TeachingRecord : Entity
{
    public string PersonId { get; set; } = "";
    public int AcademicYear { get; set; }
    public int? Semester { get; set; }
    public EducationLevel Level { get; set; }
    public string? CourseCode { get; set; }
    public string CourseName { get; set; } = "";
    public decimal? Credits { get; set; }
    public decimal? HoursPerWeek { get; set; }
    public int? ResponsibilityPct { get; set; }
}

[IdPrefix("LIC")]
public class ProfessionalLicense : Entity
{
    public string PersonId { get; set; } = "";
    public string Body { get; set; } = "";
    public string? Level { get; set; }
    public string? Branch { get; set; }
    public string? LicenseNo { get; set; }
    public DateOnly? IssuedDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public bool IsPublic { get; set; }
}

[IdPrefix("PAY")]
public class PayoutChannel : Entity
{
    public string PersonId { get; set; } = "";
    public PayoutKind Kind { get; set; }
    public string? BankName { get; set; }
    [Sensitive] public string? AccountNo { get; set; }
    public string? AccountName { get; set; }
    [Sensitive] public string? PromptPayId { get; set; }
    public bool IsPrimary { get; set; }
}

[IdPrefix("PFV")]
public class PublicFieldVisibility : Entity
{
    public string PersonId { get; set; } = "";
    public string FieldKey { get; set; } = "";
    public bool IsPublic { get; set; }
}

[IdPrefix("ORG")]
public class Organization : Entity
{
    public string NameTh { get; set; } = "";
    public string? NameEn { get; set; }
    public string? ShortName { get; set; }
    public OrganizationKind Kind { get; set; } = OrganizationKind.Other;
    public string? ParentOrganizationId { get; set; }
    public string? Country { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? LogoFileId { get; set; }
    public string? Notes { get; set; }
    public bool IsPublic { get; set; }
}

[IdPrefix("AWD")]
public class Award : Entity
{
    public string TitleTh { get; set; } = "";
    public string? TitleEn { get; set; }
    public string? Issuer { get; set; }
    public DateOnly? Date { get; set; }
    public AwardLevel Level { get; set; } = AwardLevel.Other;
    public string? Description { get; set; }
    public string? ProjectId { get; set; }
    public string? PublicationId { get; set; }
    public bool IsPublic { get; set; } = true;
}

[IdPrefix("AWR")]
public class AwardRecipient : Entity
{
    public string AwardId { get; set; } = "";
    public string PersonId { get; set; } = "";
}

[IdPrefix("RAR")]
public class ResearchArea : Entity
{
    public string NameTh { get; set; } = "";
    public string? NameEn { get; set; }
    public string? SummaryTh { get; set; }
    public string? SummaryEn { get; set; }
    public string? CoverFileId { get; set; }
    public int DisplayOrder { get; set; } = 100;
}

/// <summary>Tag link between a research area and a person, project or publication.</summary>
[IdPrefix("RAL")]
public class ResearchAreaLink : Entity
{
    public string ResearchAreaId { get; set; } = "";
    public string TargetType { get; set; } = "";
    public string TargetId { get; set; } = "";
}
