namespace Mecmat.Domain;

public enum PublicationType
{
    JournalArticle, ConferencePaper, BookChapter, Book, Textbook, AcademicArticle, TeachingMaterial,
    ResearchReport, SocialServiceWork, Patent, PettyPatent, Software, IndustryWork, CreativeWork, Thesis, Other
}
public enum PublicationStage { Idea, Drafting, Submitted, UnderReview, Revision, Accepted, Published, Rejected, Withdrawn }
public enum VenueKind { Journal, Conference, Publisher, Office, Other }
public enum IndexDatabase { Scopus, WoS, TCI1, TCI2, PubMed, ACI, Other }

[IdPrefix("PUB")]
public class Publication : Entity
{
    public PublicationType Type { get; set; } = PublicationType.JournalArticle;
    public PublicationStage Stage { get; set; } = PublicationStage.Published;
    public string TitleEn { get; set; } = "";
    public string? TitleTh { get; set; }
    public string? AbstractEn { get; set; }
    public string? AbstractTh { get; set; }
    public string? Keywords { get; set; }
    public string? VenueName { get; set; }
    public VenueKind VenueKind { get; set; } = VenueKind.Journal;
    public string? Volume { get; set; }
    public string? Issue { get; set; }
    public string? Pages { get; set; }
    public string? ArticleNumber { get; set; }
    public int? Year { get; set; }
    public DateOnly? PublishedDate { get; set; }
    public string? Doi { get; set; }
    public string? Url { get; set; }
    public string? Issn { get; set; }
    public string? Isbn { get; set; }
    public string? Publisher { get; set; }
    public string? Edition { get; set; }
    public string? ConferenceLocation { get; set; }
    public string? ConferenceDates { get; set; }
    public int? CitationCount { get; set; }
    public DateOnly? CitationAsOf { get; set; }
    public string? ApaCitation { get; set; }
    public string? PatentApplicationNo { get; set; }
    public string? PatentNo { get; set; }
    public DateOnly? FiledDate { get; set; }
    public DateOnly? GrantedDate { get; set; }
    public string? PatentCountry { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseName { get; set; }
    public decimal? Credits { get; set; }
    /// <summary>ก.พ.อ. requires a role in at least three stages of a social-service work:
    /// (A) situation analysis, (B) design/development, (C) outcome evaluation.</summary>
    public bool SocialStageA { get; set; }
    public bool SocialStageB { get; set; }
    public bool SocialStageC { get; set; }
    public string? FromThesisOfPersonId { get; set; }
    public bool IsPublic { get; set; } = true;
}

[IdPrefix("PBA")]
public class PublicationAuthor : Entity
{
    public string PublicationId { get; set; } = "";
    public int Order { get; set; }
    public string? PersonId { get; set; }
    public string? ExternalName { get; set; }
    public string? ExternalAffiliation { get; set; }
    public bool IsFirstAuthor { get; set; }
    public bool IsCorresponding { get; set; }
    public bool IsEssentialContributor { get; set; }
    public string? ContributionDescription { get; set; }
    public int? ContributionPct { get; set; }
    /// <summary>Set once the signed ก.พ.อ. contribution form exists; the row is then locked
    /// because the signed form cannot be amended.</summary>
    public DateOnly? CertifiedAt { get; set; }
    public string? CertificationFileId { get; set; }
    public bool IsStudentAtTime { get; set; }
}

[IdPrefix("PBI")]
public class PublicationIndexing : Entity
{
    public string PublicationId { get; set; } = "";
    public IndexDatabase Database { get; set; }
    public string? Quartile { get; set; }
    public decimal? ImpactFactor { get; set; }
    public int? SjrYear { get; set; }
    public string? Note { get; set; }
}

[IdPrefix("PBU")]
public class PublicationUsage : Entity
{
    public string PublicationId { get; set; } = "";
    public string PersonId { get; set; } = "";
    public AcademicRank ForRank { get; set; }
    public int? Year { get; set; }
    public string? QualityResult { get; set; }
    public string? Note { get; set; }
}

[IdPrefix("PBP")]
public class PublicationProject : Entity
{
    public string PublicationId { get; set; } = "";
    public string ProjectId { get; set; } = "";
}
