namespace Mecmat.Domain;

public enum MentorRole { MainAdvisor, CoAdvisor, Supervisor, Mentor }
public enum CareerEventKind { Position, Education, Award, News, Visit, Talk, Collaboration, Grant, Milestone, Other }

/// <summary>Academic lineage: who studied or worked under whom. A student can have several rows
/// (main advisor and co-advisor), which a single AdvisorPersonId column could not hold.</summary>
[IdPrefix("MEN")]
public class PersonMentorship : Entity
{
    public string StudentPersonId { get; set; } = "";
    public string MentorPersonId { get; set; } = "";
    public MentorRole Role { get; set; } = MentorRole.MainAdvisor;
    public EducationLevel? Degree { get; set; }
    public int? FromYear { get; set; }
    public int? ToYear { get; set; }
    public string? ThesisTitle { get; set; }
    public string? Note { get; set; }
    public bool IsPublic { get; set; } = true;
}

/// <summary>A dated moment in someone's career that has no table of its own — a news story, a
/// research visit abroad, an invited talk, a collaboration, a grant. Education, jobs, awards,
/// publications and projects already have tables and join the timeline from there.</summary>
[IdPrefix("PEV")]
public class PersonEvent : Entity
{
    public string PersonId { get; set; } = "";
    public CareerEventKind Kind { get; set; } = CareerEventKind.Other;
    public DateOnly Date { get; set; }
    public DateOnly? EndDate { get; set; }
    /// <summary>True when only the year (or year and month) is known; the UI then shows no day.</summary>
    public bool DateIsApproximate { get; set; }
    public string TitleTh { get; set; } = "";
    public string? TitleEn { get; set; }
    public string? DescriptionTh { get; set; }
    public string? DescriptionEn { get; set; }
    public string? OrganizationId { get; set; }
    public string? OrganizationText { get; set; }
    public string? Country { get; set; }
    public string? Url { get; set; }
    public string? SourceUrl { get; set; }
    public bool IsPublic { get; set; } = true;
}
