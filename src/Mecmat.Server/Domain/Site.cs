namespace Mecmat.Domain;

public enum NewsKind { News, Event, Announcement, Award, Seminar }
public enum PublishStatus { Draft, InReview, Published, Archived }
public enum MessageStatus { New, Read, Replied, Spam, Closed }

[IdPrefix("NEW")]
public class NewsPost : Entity
{
    public NewsKind Kind { get; set; } = NewsKind.News;
    public string Slug { get; set; } = "";
    public string TitleTh { get; set; } = "";
    public string? TitleEn { get; set; }
    public string? SummaryTh { get; set; }
    public string? SummaryEn { get; set; }
    public string? BodyTh { get; set; }
    public string? BodyEn { get; set; }
    public string? CoverFileId { get; set; }
    public DateTime? EventStartAt { get; set; }
    public DateTime? EventEndAt { get; set; }
    public string? EventLocation { get; set; }
    public PublishStatus Status { get; set; } = PublishStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public string? AuthorPersonId { get; set; }
    public bool Pinned { get; set; }
}

[IdPrefix("NWL")]
public class NewsLink : Entity
{
    public string NewsPostId { get; set; } = "";
    public string TargetType { get; set; } = "";
    public string TargetId { get; set; } = "";
}

[IdPrefix("PAG")]
public class SitePage : Entity
{
    public string Key { get; set; } = "";
    public string TitleTh { get; set; } = "";
    public string? TitleEn { get; set; }
    public string? BodyTh { get; set; }
    public string? BodyEn { get; set; }
}

[IdPrefix("SET")]
public class SiteSetting : Entity
{
    public string Key { get; set; } = "";
    public string? Value { get; set; }
}

[IdPrefix("MSG")]
public class ContactMessage : Entity
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";
    public string? ConsentRecordId { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.New;
    public string? HandledByPersonId { get; set; }
    public string? Note { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? IpHash { get; set; }
}
