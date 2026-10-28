using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mecmat.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalInstance",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectType = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectId = table.Column<string>(type: "TEXT", nullable: false),
                    PolicyId = table.Column<string>(type: "TEXT", nullable: true),
                    PolicySnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CurrentStep = table.Column<int>(type: "INTEGER", nullable: false),
                    RequesterPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalInstance", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    PersonId = table.Column<string>(type: "TEXT", nullable: true),
                    EntityType = table.Column<string>(type: "TEXT", nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    BeforeJson = table.Column<string>(type: "TEXT", nullable: true),
                    AfterJson = table.Column<string>(type: "TEXT", nullable: true),
                    IpHash = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsentRecord",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectPersonId = table.Column<string>(type: "TEXT", nullable: true),
                    SubjectEmail = table.Column<string>(type: "TEXT", nullable: true),
                    Purpose = table.Column<string>(type: "TEXT", nullable: false),
                    PolicyVersion = table.Column<string>(type: "TEXT", nullable: false),
                    GivenAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WithdrawnAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IpHash = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentRecord", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileBlob",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    StoredName = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalName = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileBlob", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GroupPosition",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    NameTh = table.Column<string>(type: "TEXT", nullable: false),
                    NameEn = table.Column<string>(type: "TEXT", nullable: true),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPosition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdCounters",
                columns: table => new
                {
                    Prefix = table.Column<string>(type: "TEXT", nullable: false),
                    DateKey = table.Column<string>(type: "TEXT", nullable: false),
                    LastValue = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdCounters", x => new { x.Prefix, x.DateKey });
                });

            migrationBuilder.CreateTable(
                name: "Material",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: true),
                    NameTh = table.Column<string>(type: "TEXT", nullable: false),
                    NameEn = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    Unit = table.Column<string>(type: "TEXT", nullable: false),
                    MinStock = table.Column<decimal>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Material", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsPost",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", nullable: false),
                    TitleTh = table.Column<string>(type: "TEXT", nullable: false),
                    TitleEn = table.Column<string>(type: "TEXT", nullable: true),
                    SummaryTh = table.Column<string>(type: "TEXT", nullable: true),
                    SummaryEn = table.Column<string>(type: "TEXT", nullable: true),
                    BodyTh = table.Column<string>(type: "TEXT", nullable: true),
                    BodyEn = table.Column<string>(type: "TEXT", nullable: true),
                    CoverFileId = table.Column<string>(type: "TEXT", nullable: true),
                    EventStartAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EventEndAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EventLocation = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AuthorPersonId = table.Column<string>(type: "TEXT", nullable: true),
                    Pinned = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsPost", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationOutbox",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationOutbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organization",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    NameTh = table.Column<string>(type: "TEXT", nullable: false),
                    NameEn = table.Column<string>(type: "TEXT", nullable: true),
                    ShortName = table.Column<string>(type: "TEXT", nullable: true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ParentOrganizationId = table.Column<string>(type: "TEXT", nullable: true),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    Website = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    LogoFileId = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organization_Organization_ParentOrganizationId",
                        column: x => x.ParentOrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Publication",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Stage = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    TitleEn = table.Column<string>(type: "TEXT", nullable: false),
                    TitleTh = table.Column<string>(type: "TEXT", nullable: true),
                    AbstractEn = table.Column<string>(type: "TEXT", nullable: true),
                    AbstractTh = table.Column<string>(type: "TEXT", nullable: true),
                    Keywords = table.Column<string>(type: "TEXT", nullable: true),
                    VenueName = table.Column<string>(type: "TEXT", nullable: true),
                    VenueKind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Volume = table.Column<string>(type: "TEXT", nullable: true),
                    Issue = table.Column<string>(type: "TEXT", nullable: true),
                    Pages = table.Column<string>(type: "TEXT", nullable: true),
                    ArticleNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Year = table.Column<int>(type: "INTEGER", nullable: true),
                    PublishedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Doi = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    Issn = table.Column<string>(type: "TEXT", nullable: true),
                    Isbn = table.Column<string>(type: "TEXT", nullable: true),
                    Publisher = table.Column<string>(type: "TEXT", nullable: true),
                    Edition = table.Column<string>(type: "TEXT", nullable: true),
                    ConferenceLocation = table.Column<string>(type: "TEXT", nullable: true),
                    ConferenceDates = table.Column<string>(type: "TEXT", nullable: true),
                    CitationCount = table.Column<int>(type: "INTEGER", nullable: true),
                    CitationAsOf = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ApaCitation = table.Column<string>(type: "TEXT", nullable: true),
                    PatentApplicationNo = table.Column<string>(type: "TEXT", nullable: true),
                    PatentNo = table.Column<string>(type: "TEXT", nullable: true),
                    FiledDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    GrantedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PatentCountry = table.Column<string>(type: "TEXT", nullable: true),
                    CourseCode = table.Column<string>(type: "TEXT", nullable: true),
                    CourseName = table.Column<string>(type: "TEXT", nullable: true),
                    Credits = table.Column<decimal>(type: "TEXT", nullable: true),
                    SocialStageA = table.Column<bool>(type: "INTEGER", nullable: false),
                    SocialStageB = table.Column<bool>(type: "INTEGER", nullable: false),
                    SocialStageC = table.Column<bool>(type: "INTEGER", nullable: false),
                    FromThesisOfPersonId = table.Column<string>(type: "TEXT", nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publication", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RemindersSent",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemindersSent", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "ResearchArea",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    NameTh = table.Column<string>(type: "TEXT", nullable: false),
                    NameEn = table.Column<string>(type: "TEXT", nullable: true),
                    SummaryTh = table.Column<string>(type: "TEXT", nullable: true),
                    SummaryEn = table.Column<string>(type: "TEXT", nullable: true),
                    CoverFileId = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchArea", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    NameTh = table.Column<string>(type: "TEXT", nullable: false),
                    NameEn = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SensitiveAccessLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ViewerUserId = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    FieldKey = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensitiveAccessLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SitePage",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    TitleTh = table.Column<string>(type: "TEXT", nullable: false),
                    TitleEn = table.Column<string>(type: "TEXT", nullable: true),
                    BodyTh = table.Column<string>(type: "TEXT", nullable: true),
                    BodyEn = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SitePage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSetting",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSetting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContactMessage",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    ConsentRecordId = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    HandledByPersonId = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpHash = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactMessage_ConsentRecord_ConsentRecordId",
                        column: x => x.ConsentRecordId,
                        principalTable: "ConsentRecord",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Attachment",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerType = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: false),
                    FileBlobId = table.Column<string>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attachment_FileBlob_FileBlobId",
                        column: x => x.FileBlobId,
                        principalTable: "FileBlob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NewsLink",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    NewsPostId = table.Column<string>(type: "TEXT", nullable: false),
                    TargetType = table.Column<string>(type: "TEXT", nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsLink_NewsPost_NewsPostId",
                        column: x => x.NewsPostId,
                        principalTable: "NewsPost",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Person",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    TitleTh = table.Column<string>(type: "TEXT", nullable: true),
                    TitleEn = table.Column<string>(type: "TEXT", nullable: true),
                    AcademicRank = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    HasDoctorate = table.Column<bool>(type: "INTEGER", nullable: false),
                    FirstNameTh = table.Column<string>(type: "TEXT", nullable: false),
                    LastNameTh = table.Column<string>(type: "TEXT", nullable: false),
                    FirstNameEn = table.Column<string>(type: "TEXT", nullable: true),
                    LastNameEn = table.Column<string>(type: "TEXT", nullable: true),
                    Nickname = table.Column<string>(type: "TEXT", nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    NationalId = table.Column<string>(type: "TEXT", nullable: true),
                    WorkEmail = table.Column<string>(type: "TEXT", nullable: true),
                    PersonalEmail = table.Column<string>(type: "TEXT", nullable: true),
                    WorkPhone = table.Column<string>(type: "TEXT", nullable: true),
                    MobilePhone = table.Column<string>(type: "TEXT", nullable: true),
                    LineId = table.Column<string>(type: "TEXT", nullable: true),
                    OfficeRoom = table.Column<string>(type: "TEXT", nullable: true),
                    PhotoFileId = table.Column<string>(type: "TEXT", nullable: true),
                    OrganizationId = table.Column<string>(type: "TEXT", nullable: true),
                    PositionText = table.Column<string>(type: "TEXT", nullable: true),
                    AdvisorPersonId = table.Column<string>(type: "TEXT", nullable: true),
                    StudentId = table.Column<string>(type: "TEXT", nullable: true),
                    ProgramName = table.Column<string>(type: "TEXT", nullable: true),
                    EnrolledYear = table.Column<int>(type: "INTEGER", nullable: true),
                    ExpectedGraduation = table.Column<int>(type: "INTEGER", nullable: true),
                    ExpertiseTh = table.Column<string>(type: "TEXT", nullable: true),
                    ExpertiseEn = table.Column<string>(type: "TEXT", nullable: true),
                    BioTh = table.Column<string>(type: "TEXT", nullable: true),
                    BioEn = table.Column<string>(type: "TEXT", nullable: true),
                    OrcidId = table.Column<string>(type: "TEXT", nullable: true),
                    ScopusAuthorId = table.Column<string>(type: "TEXT", nullable: true),
                    GoogleScholarId = table.Column<string>(type: "TEXT", nullable: true),
                    ResearcherId = table.Column<string>(type: "TEXT", nullable: true),
                    ResearchGateUrl = table.Column<string>(type: "TEXT", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "TEXT", nullable: true),
                    JoinedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LeftDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PublicSlug = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Person", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Person_FileBlob_PhotoFileId",
                        column: x => x.PhotoFileId,
                        principalTable: "FileBlob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Person_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Person_Person_AdvisorPersonId",
                        column: x => x.AdvisorPersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalCode = table.Column<string>(type: "TEXT", nullable: true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ParentProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    TitleTh = table.Column<string>(type: "TEXT", nullable: false),
                    TitleEn = table.Column<string>(type: "TEXT", nullable: true),
                    KeywordsTh = table.Column<string>(type: "TEXT", nullable: true),
                    KeywordsEn = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    Rationale = table.Column<string>(type: "TEXT", nullable: true),
                    Objectives = table.Column<string>(type: "TEXT", nullable: true),
                    Framework = table.Column<string>(type: "TEXT", nullable: true),
                    Methodology = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    FunderOrganizationId = table.Column<string>(type: "TEXT", nullable: true),
                    FundingScheme = table.Column<string>(type: "TEXT", nullable: true),
                    ContractNo = table.Column<string>(type: "TEXT", nullable: true),
                    FiscalYear = table.Column<int>(type: "INTEGER", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ExtendedEndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    TotalBudget = table.Column<long>(type: "INTEGER", nullable: true),
                    InstitutionFeePct = table.Column<decimal>(type: "TEXT", nullable: true),
                    TrlStart = table.Column<int>(type: "INTEGER", nullable: true),
                    TrlTarget = table.Column<int>(type: "INTEGER", nullable: true),
                    TrlCurrent = table.Column<int>(type: "INTEGER", nullable: true),
                    SrlStart = table.Column<int>(type: "INTEGER", nullable: true),
                    SrlTarget = table.Column<int>(type: "INTEGER", nullable: true),
                    SrlCurrent = table.Column<int>(type: "INTEGER", nullable: true),
                    ResearchSite = table.Column<string>(type: "TEXT", nullable: true),
                    BeneficiaryArea = table.Column<string>(type: "TEXT", nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    PublicSummaryTh = table.Column<string>(type: "TEXT", nullable: true),
                    PublicSummaryEn = table.Column<string>(type: "TEXT", nullable: true),
                    CoverFileId = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Project_Organization_FunderOrganizationId",
                        column: x => x.FunderOrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Project_Project_ParentProjectId",
                        column: x => x.ParentProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublicationIndexing",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PublicationId = table.Column<string>(type: "TEXT", nullable: false),
                    Database = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Quartile = table.Column<string>(type: "TEXT", nullable: true),
                    ImpactFactor = table.Column<decimal>(type: "TEXT", nullable: true),
                    SjrYear = table.Column<int>(type: "INTEGER", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationIndexing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicationIndexing_Publication_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "Publication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResearchAreaLink",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ResearchAreaId = table.Column<string>(type: "TEXT", nullable: false),
                    TargetType = table.Column<string>(type: "TEXT", nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchAreaLink", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchAreaLink_ResearchArea_ResearchAreaId",
                        column: x => x.ResearchAreaId,
                        principalTable: "ResearchArea",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    Module = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Scope = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermission_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcademicAppointment",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: false),
                    Rank = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    FieldName = table.Column<string>(type: "TEXT", nullable: true),
                    FieldCode = table.Column<string>(type: "TEXT", nullable: true),
                    SubField = table.Column<string>(type: "TEXT", nullable: true),
                    Method = table.Column<int>(type: "INTEGER", nullable: true),
                    AppointedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ReferenceText = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicAppointment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicAppointment_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalAction",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    InstanceId = table.Column<string>(type: "TEXT", nullable: false),
                    StepOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ActorPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    OnBehalfOfPersonId = table.Column<string>(type: "TEXT", nullable: true),
                    Decision = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalAction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalAction_ApprovalInstance_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "ApprovalInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalAction_Person_ActorPersonId",
                        column: x => x.ActorPersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Delegation",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FromPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    ToPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SubjectType = table.Column<string>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Delegation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Delegation_Person_FromPersonId",
                        column: x => x.FromPersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Delegation_Person_ToPersonId",
                        column: x => x.ToPersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayoutChannel",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    BankName = table.Column<string>(type: "TEXT", nullable: true),
                    AccountNo = table.Column<string>(type: "TEXT", nullable: true),
                    AccountName = table.Column<string>(type: "TEXT", nullable: true),
                    PromptPayId = table.Column<string>(type: "TEXT", nullable: true),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutChannel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayoutChannel_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonEducation",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DegreeNameTh = table.Column<string>(type: "TEXT", nullable: true),
                    DegreeNameEn = table.Column<string>(type: "TEXT", nullable: true),
                    Field = table.Column<string>(type: "TEXT", nullable: true),
                    Institution = table.Column<string>(type: "TEXT", nullable: true),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    StartYear = table.Column<int>(type: "INTEGER", nullable: true),
                    GraduatedYear = table.Column<int>(type: "INTEGER", nullable: true),
                    Honors = table.Column<string>(type: "TEXT", nullable: true),
                    ThesisTitle = table.Column<string>(type: "TEXT", nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonEducation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonEducation_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonEmployment",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<string>(type: "TEXT", nullable: true),
                    OrganizationText = table.Column<string>(type: "TEXT", nullable: true),
                    Position = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonEmployment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonEmployment_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonEmployment_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PositionAssignment",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PositionId = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ToDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionAssignment_GroupPosition_PositionId",
                        column: x => x.PositionId,
                        principalTable: "GroupPosition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionAssignment_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfessionalLicense",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<string>(type: "TEXT", nullable: true),
                    Branch = table.Column<string>(type: "TEXT", nullable: true),
                    LicenseNo = table.Column<string>(type: "TEXT", nullable: true),
                    IssuedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalLicense", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfessionalLicense_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublicationAuthor",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PublicationId = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalName = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalAffiliation = table.Column<string>(type: "TEXT", nullable: true),
                    IsFirstAuthor = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCorresponding = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEssentialContributor = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContributionDescription = table.Column<string>(type: "TEXT", nullable: true),
                    ContributionPct = table.Column<int>(type: "INTEGER", nullable: true),
                    CertifiedAt = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CertificationFileId = table.Column<string>(type: "TEXT", nullable: true),
                    IsStudentAtTime = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationAuthor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicationAuthor_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublicationAuthor_Publication_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "Publication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublicationUsage",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PublicationId = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: false),
                    ForRank = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: true),
                    QualityResult = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationUsage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicationUsage_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublicationUsage_Publication_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "Publication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublicFieldVisibility",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: false),
                    FieldKey = table.Column<string>(type: "TEXT", nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicFieldVisibility", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicFieldVisibility_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeachingRecord",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: false),
                    AcademicYear = table.Column<int>(type: "INTEGER", nullable: false),
                    Semester = table.Column<int>(type: "INTEGER", nullable: true),
                    Level = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CourseCode = table.Column<string>(type: "TEXT", nullable: true),
                    CourseName = table.Column<string>(type: "TEXT", nullable: false),
                    Credits = table.Column<decimal>(type: "TEXT", nullable: true),
                    HoursPerWeek = table.Column<decimal>(type: "TEXT", nullable: true),
                    ResponsibilityPct = table.Column<int>(type: "INTEGER", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeachingRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeachingRecord_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAccount",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ExpiresAt = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    TotpSecret = table.Column<string>(type: "TEXT", nullable: true),
                    TotpEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecoveryCodesHash = table.Column<string>(type: "TEXT", nullable: true),
                    FailedLoginCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    Theme = table.Column<string>(type: "TEXT", nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccount_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    NameTh = table.Column<string>(type: "TEXT", nullable: false),
                    NameEn = table.Column<string>(type: "TEXT", nullable: true),
                    ParentAccountId = table.Column<string>(type: "TEXT", nullable: true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    BudgetCategory = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    CustodianPersonId = table.Column<string>(type: "TEXT", nullable: true),
                    AllowNegative = table.Column<bool>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Account_Account_ParentAccountId",
                        column: x => x.ParentAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Account_Person_CustodianPersonId",
                        column: x => x.CustodianPersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Account_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Activity",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    ParentActivityId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ProgressPct = table.Column<int>(type: "INTEGER", nullable: false),
                    WeightPct = table.Column<int>(type: "INTEGER", nullable: true),
                    OwnerPersonId = table.Column<string>(type: "TEXT", nullable: true),
                    Deliverable = table.Column<string>(type: "TEXT", nullable: true),
                    DependsOnActivityId = table.Column<string>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activity_Activity_ParentActivityId",
                        column: x => x.ParentActivityId,
                        principalTable: "Activity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Activity_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Award",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TitleTh = table.Column<string>(type: "TEXT", nullable: false),
                    TitleEn = table.Column<string>(type: "TEXT", nullable: true),
                    Issuer = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Level = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    PublicationId = table.Column<string>(type: "TEXT", nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Award", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Award_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Award_Publication_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "Publication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Folder",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ParentFolderId = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    Visibility = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Folder_Folder_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "Folder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Folder_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KeyResult",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    Objective = table.Column<string>(type: "TEXT", nullable: false),
                    KeyResultText = table.Column<string>(type: "TEXT", nullable: false),
                    Target = table.Column<decimal>(type: "TEXT", nullable: true),
                    Actual = table.Column<decimal>(type: "TEXT", nullable: true),
                    Unit = table.Column<string>(type: "TEXT", nullable: true),
                    Period = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeyResult_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialMovement",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MaterialId = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Qty = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitCost = table.Column<long>(type: "INTEGER", nullable: true),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    PersonId = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialMovement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialMovement_Material_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Material",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialMovement_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Meeting",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    StartAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    OnlineUrl = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    OrganizerPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    Agenda = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Visibility = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meeting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meeting_Person_OrganizerPersonId",
                        column: x => x.OrganizerPersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Meeting_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Milestone",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DoneDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestone", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Milestone_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProgressReport",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    PeriodNo = table.Column<int>(type: "INTEGER", nullable: true),
                    PeriodFrom = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PeriodTo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    SubmittedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    FileId = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressReport", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressReport_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectImpact",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    Dimension = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Indicator = table.Column<string>(type: "TEXT", nullable: true),
                    Value = table.Column<decimal>(type: "TEXT", nullable: true),
                    Unit = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectImpact", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectImpact_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMember",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    EffortPct = table.Column<int>(type: "INTEGER", nullable: true),
                    FromDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ToDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMember_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMember_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectOutcome",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    OutcomeType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Evidence = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectOutcome", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectOutcome_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectOutput",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    OutputType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    TargetQty = table.Column<decimal>(type: "TEXT", nullable: true),
                    Unit = table.Column<string>(type: "TEXT", nullable: true),
                    ActualQty = table.Column<decimal>(type: "TEXT", nullable: true),
                    AchievedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LinkedType = table.Column<string>(type: "TEXT", nullable: true),
                    LinkedId = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectOutput", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectOutput_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPartner",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    OrganizationId = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    InCashAmount = table.Column<long>(type: "INTEGER", nullable: true),
                    InKindAmount = table.Column<long>(type: "INTEGER", nullable: true),
                    InKindDescription = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPartner", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectPartner_Organization_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectPartner_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PublicationProject",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PublicationId = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationProject", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicationProject_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PublicationProject_Publication_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "Publication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recruitment",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    TitleTh = table.Column<string>(type: "TEXT", nullable: false),
                    TitleEn = table.Column<string>(type: "TEXT", nullable: true),
                    PositionType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DescriptionTh = table.Column<string>(type: "TEXT", nullable: true),
                    DescriptionEn = table.Column<string>(type: "TEXT", nullable: true),
                    Qualifications = table.Column<string>(type: "TEXT", nullable: true),
                    CompensationText = table.Column<string>(type: "TEXT", nullable: true),
                    Seats = table.Column<int>(type: "INTEGER", nullable: true),
                    OpenDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CloseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recruitment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recruitment_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    RecipientUserId = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: true),
                    Link = table.Column<string>(type: "TEXT", nullable: true),
                    SubjectType = table.Column<string>(type: "TEXT", nullable: true),
                    SubjectId = table.Column<string>(type: "TEXT", nullable: true),
                    GroupKey = table.Column<string>(type: "TEXT", nullable: true),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notification_UserAccount_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetRequest",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    UsernameTyped = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    HandledBy = table.Column<string>(type: "TEXT", nullable: true),
                    HandledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetRequest_UserAccount_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Session",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CsrfToken = table.Column<string>(type: "TEXT", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IdleExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AbsoluteExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotpPassed = table.Column<bool>(type: "INTEGER", nullable: false),
                    IpHash = table.Column<string>(type: "TEXT", nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevokeReason = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Session", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Session_UserAccount_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissionOverride",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Module = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Scope = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissionOverride", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermissionOverride_UserAccount_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRole_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRole_UserAccount_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountBudget",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    FiscalYear = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountBudget", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountBudget_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalPolicy",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectType = table.Column<string>(type: "TEXT", nullable: false),
                    RequestKind = table.Column<string>(type: "TEXT", nullable: true),
                    MinAmount = table.Column<long>(type: "INTEGER", nullable: true),
                    MaxAmount = table.Column<long>(type: "INTEGER", nullable: true),
                    AccountScopeId = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectScopeId = table.Column<string>(type: "TEXT", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalPolicy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalPolicy_Account_AccountScopeId",
                        column: x => x.AccountScopeId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovalPolicy_Project_ProjectScopeId",
                        column: x => x.ProjectScopeId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Asset",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AssetCode = table.Column<string>(type: "TEXT", nullable: true),
                    InternalCode = table.Column<string>(type: "TEXT", nullable: true),
                    NameTh = table.Column<string>(type: "TEXT", nullable: false),
                    NameEn = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    Brand = table.Column<string>(type: "TEXT", nullable: true),
                    Model = table.Column<string>(type: "TEXT", nullable: true),
                    SerialNo = table.Column<string>(type: "TEXT", nullable: true),
                    AcquiredDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AcquisitionMethod = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    FundingProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    FundingAccountId = table.Column<string>(type: "TEXT", nullable: true),
                    Cost = table.Column<long>(type: "INTEGER", nullable: true),
                    VendorOrganizationId = table.Column<string>(type: "TEXT", nullable: true),
                    ReferenceNo = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    CustodianPersonId = table.Column<string>(type: "TEXT", nullable: true),
                    UsefulLifeYears = table.Column<int>(type: "INTEGER", nullable: true),
                    SalvageValue = table.Column<long>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DisposedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DisposalMethod = table.Column<string>(type: "TEXT", nullable: true),
                    Specifications = table.Column<string>(type: "TEXT", nullable: true),
                    PhotoFileId = table.Column<string>(type: "TEXT", nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asset_Account_FundingAccountId",
                        column: x => x.FundingAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Asset_Organization_VendorOrganizationId",
                        column: x => x.VendorOrganizationId,
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Asset_Person_CustodianPersonId",
                        column: x => x.CustodianPersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Asset_Project_FundingProjectId",
                        column: x => x.FundingProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MoneyRequest",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RequesterPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    ToAccountId = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    Purpose = table.Column<string>(type: "TEXT", nullable: false),
                    PayeePersonId = table.Column<string>(type: "TEXT", nullable: true),
                    PayeeOrganizationId = table.Column<string>(type: "TEXT", nullable: true),
                    PayeeText = table.Column<string>(type: "TEXT", nullable: true),
                    TotalAmount = table.Column<long>(type: "INTEGER", nullable: false),
                    NeededBy = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AdvanceDueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RelatedAdvanceId = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ApprovalInstanceId = table.Column<string>(type: "TEXT", nullable: true),
                    SettledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SettledBy = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoneyRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MoneyRequest_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MoneyRequest_Account_ToAccountId",
                        column: x => x.ToAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MoneyRequest_MoneyRequest_RelatedAdvanceId",
                        column: x => x.RelatedAdvanceId,
                        principalTable: "MoneyRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MoneyRequest_Person_RequesterPersonId",
                        column: x => x.RequesterPersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MoneyRequest_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AwardRecipient",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AwardId = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardRecipient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AwardRecipient_Award_AwardId",
                        column: x => x.AwardId,
                        principalTable: "Award",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardRecipient_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Document",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FolderId = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    PublicationId = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentVersionId = table.Column<string>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: true),
                    Visibility = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    OwnerPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Document", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Document_Folder_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Document_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Document_Publication_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "Publication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingAttendee",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MeetingId = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: true),
                    ExternalName = table.Column<string>(type: "TEXT", nullable: true),
                    Response = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Attended = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingAttendee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingAttendee_Meeting_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meeting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingAttendee_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingMinutes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    MeetingId = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedByPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingMinutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingMinutes_Meeting_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meeting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskItem",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    ActivityId = table.Column<string>(type: "TEXT", nullable: true),
                    ParentTaskId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatorPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: true),
                    SourceMeetingId = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskItem_Activity_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskItem_Meeting_SourceMeetingId",
                        column: x => x.SourceMeetingId,
                        principalTable: "Meeting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskItem_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskItem_TaskItem_ParentTaskId",
                        column: x => x.ParentTaskId,
                        principalTable: "TaskItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Application",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    RecruitmentId = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    CvFileId = table.Column<string>(type: "TEXT", nullable: true),
                    ConsentRecordId = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ReviewerNotes = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpHash = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Application", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Application_ConsentRecord_ConsentRecordId",
                        column: x => x.ConsentRecordId,
                        principalTable: "ConsentRecord",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Application_Recruitment_RecruitmentId",
                        column: x => x.RecruitmentId,
                        principalTable: "Recruitment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalPolicyStep",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    PolicyId = table.Column<string>(type: "TEXT", nullable: false),
                    StepOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ApproverKind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ApproverRef = table.Column<string>(type: "TEXT", nullable: true),
                    Rule = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalPolicyStep", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalPolicyStep_ApprovalPolicy_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "ApprovalPolicy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetLoan",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AssetId = table.Column<string>(type: "TEXT", nullable: false),
                    BorrowerPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    FromDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ReturnedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ConditionOut = table.Column<string>(type: "TEXT", nullable: true),
                    ConditionIn = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetLoan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetLoan_Asset_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetLoan_Person_BorrowerPersonId",
                        column: x => x.BorrowerPersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Maintenance",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AssetId = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Cost = table.Column<long>(type: "INTEGER", nullable: true),
                    VendorOrganizationId = table.Column<string>(type: "TEXT", nullable: true),
                    NextDueDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CertificateFileId = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maintenance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Maintenance_Asset_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Resource",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    NameTh = table.Column<string>(type: "TEXT", nullable: false),
                    NameEn = table.Column<string>(type: "TEXT", nullable: true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    AssetId = table.Column<string>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ManagerPersonId = table.Column<string>(type: "TEXT", nullable: true),
                    RequiresApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    SlotMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    MinDurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxDurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxAdvanceDays = table.Column<int>(type: "INTEGER", nullable: true),
                    OpenHoursJson = table.Column<string>(type: "TEXT", nullable: true),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resource", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resource_Asset_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Asset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resource_Person_ManagerPersonId",
                        column: x => x.ManagerPersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LedgerEntry",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    EntryDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    FromAccountId = table.Column<string>(type: "TEXT", nullable: true),
                    ToAccountId = table.Column<string>(type: "TEXT", nullable: true),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CounterpartyPersonId = table.Column<string>(type: "TEXT", nullable: true),
                    CounterpartyOrganizationId = table.Column<string>(type: "TEXT", nullable: true),
                    CounterpartyText = table.Column<string>(type: "TEXT", nullable: true),
                    MoneyRequestId = table.Column<string>(type: "TEXT", nullable: true),
                    ReversesEntryId = table.Column<string>(type: "TEXT", nullable: true),
                    BudgetCategory = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    ReferenceText = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LedgerEntry_Account_FromAccountId",
                        column: x => x.FromAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LedgerEntry_Account_ToAccountId",
                        column: x => x.ToAccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LedgerEntry_LedgerEntry_ReversesEntryId",
                        column: x => x.ReversesEntryId,
                        principalTable: "LedgerEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LedgerEntry_MoneyRequest_MoneyRequestId",
                        column: x => x.MoneyRequestId,
                        principalTable: "MoneyRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MoneyRequestLine",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    RequestId = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Qty = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<long>(type: "INTEGER", nullable: false),
                    Amount = table.Column<long>(type: "INTEGER", nullable: false),
                    BudgetCategory = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoneyRequestLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MoneyRequestLine_MoneyRequest_RequestId",
                        column: x => x.RequestId,
                        principalTable: "MoneyRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVersion",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<string>(type: "TEXT", nullable: false),
                    VersionNo = table.Column<int>(type: "INTEGER", nullable: false),
                    FileBlobId = table.Column<string>(type: "TEXT", nullable: false),
                    ChangeNote = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentVersion_Document_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentVersion_FileBlob_FileBlobId",
                        column: x => x.FileBlobId,
                        principalTable: "FileBlob",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskAssignee",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TaskId = table.Column<string>(type: "TEXT", nullable: false),
                    PersonId = table.Column<string>(type: "TEXT", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAssignee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskAssignee_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskAssignee_TaskItem_TaskId",
                        column: x => x.TaskId,
                        principalTable: "TaskItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskComment",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TaskId = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskComment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskComment_TaskItem_TaskId",
                        column: x => x.TaskId,
                        principalTable: "TaskItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Booking",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ResourceId = table.Column<string>(type: "TEXT", nullable: false),
                    BookerPersonId = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    StartAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    RecurrenceGroupId = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovalInstanceId = table.Column<string>(type: "TEXT", nullable: true),
                    CancelledReason = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Booking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Booking_Person_BookerPersonId",
                        column: x => x.BookerPersonId,
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Booking_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Booking_Resource_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceBlackout",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ResourceId = table.Column<string>(type: "TEXT", nullable: false),
                    StartAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    RecordStatus = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStamp = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceBlackout", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceBlackout_Resource_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resource",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicAppointment_PersonId",
                table: "AcademicAppointment",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicAppointment_RecordStatus",
                table: "AcademicAppointment",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Account_Code",
                table: "Account",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Account_CustodianPersonId",
                table: "Account",
                column: "CustodianPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Account_ParentAccountId",
                table: "Account",
                column: "ParentAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Account_ProjectId",
                table: "Account",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Account_RecordStatus",
                table: "Account",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AccountBudget_AccountId",
                table: "AccountBudget",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountBudget_RecordStatus",
                table: "AccountBudget",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_ParentActivityId",
                table: "Activity",
                column: "ParentActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_ProjectId",
                table: "Activity",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_RecordStatus",
                table: "Activity",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Application_ConsentRecordId",
                table: "Application",
                column: "ConsentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Application_RecordStatus",
                table: "Application",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Application_RecruitmentId",
                table: "Application",
                column: "RecruitmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalAction_ActorPersonId",
                table: "ApprovalAction",
                column: "ActorPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalAction_InstanceId",
                table: "ApprovalAction",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalAction_RecordStatus",
                table: "ApprovalAction",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalInstance_RecordStatus",
                table: "ApprovalInstance",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalPolicy_AccountScopeId",
                table: "ApprovalPolicy",
                column: "AccountScopeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalPolicy_ProjectScopeId",
                table: "ApprovalPolicy",
                column: "ProjectScopeId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalPolicy_RecordStatus",
                table: "ApprovalPolicy",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalPolicyStep_PolicyId",
                table: "ApprovalPolicyStep",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalPolicyStep_RecordStatus",
                table: "ApprovalPolicyStep",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_CustodianPersonId",
                table: "Asset",
                column: "CustodianPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_FundingAccountId",
                table: "Asset",
                column: "FundingAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_FundingProjectId",
                table: "Asset",
                column: "FundingProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_RecordStatus",
                table: "Asset",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Asset_VendorOrganizationId",
                table: "Asset",
                column: "VendorOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLoan_AssetId",
                table: "AssetLoan",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLoan_BorrowerPersonId",
                table: "AssetLoan",
                column: "BorrowerPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetLoan_RecordStatus",
                table: "AssetLoan",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_FileBlobId",
                table: "Attachment",
                column: "FileBlobId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_OwnerType_OwnerId",
                table: "Attachment",
                columns: new[] { "OwnerType", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_RecordStatus",
                table: "Attachment",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Award_ProjectId",
                table: "Award",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Award_PublicationId",
                table: "Award",
                column: "PublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Award_RecordStatus",
                table: "Award",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AwardRecipient_AwardId",
                table: "AwardRecipient",
                column: "AwardId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardRecipient_PersonId",
                table: "AwardRecipient",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardRecipient_RecordStatus",
                table: "AwardRecipient",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_BookerPersonId",
                table: "Booking",
                column: "BookerPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_ProjectId",
                table: "Booking",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_RecordStatus",
                table: "Booking",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_ResourceId_StartAt",
                table: "Booking",
                columns: new[] { "ResourceId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChangeLogs_At",
                table: "ChangeLogs",
                column: "At");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeLogs_EntityType_EntityId",
                table: "ChangeLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentRecord_RecordStatus",
                table: "ConsentRecord",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessage_ConsentRecordId",
                table: "ContactMessage",
                column: "ConsentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessage_RecordStatus",
                table: "ContactMessage",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Delegation_FromPersonId",
                table: "Delegation",
                column: "FromPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Delegation_RecordStatus",
                table: "Delegation",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Delegation_ToPersonId",
                table: "Delegation",
                column: "ToPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Document_FolderId",
                table: "Document",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Document_ProjectId",
                table: "Document",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Document_PublicationId",
                table: "Document",
                column: "PublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Document_RecordStatus",
                table: "Document",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersion_DocumentId",
                table: "DocumentVersion",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersion_FileBlobId",
                table: "DocumentVersion",
                column: "FileBlobId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersion_RecordStatus",
                table: "DocumentVersion",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_FileBlob_RecordStatus",
                table: "FileBlob",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Folder_ParentFolderId",
                table: "Folder",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Folder_ProjectId",
                table: "Folder",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Folder_RecordStatus",
                table: "Folder",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_GroupPosition_RecordStatus",
                table: "GroupPosition",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_KeyResult_ProjectId",
                table: "KeyResult",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyResult_RecordStatus",
                table: "KeyResult",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntry_EntryDate",
                table: "LedgerEntry",
                column: "EntryDate");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntry_FromAccountId",
                table: "LedgerEntry",
                column: "FromAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntry_MoneyRequestId",
                table: "LedgerEntry",
                column: "MoneyRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntry_RecordStatus",
                table: "LedgerEntry",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntry_ReversesEntryId",
                table: "LedgerEntry",
                column: "ReversesEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntry_ToAccountId",
                table: "LedgerEntry",
                column: "ToAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Maintenance_AssetId",
                table: "Maintenance",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Maintenance_RecordStatus",
                table: "Maintenance",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Material_RecordStatus",
                table: "Material",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialMovement_MaterialId",
                table: "MaterialMovement",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialMovement_ProjectId",
                table: "MaterialMovement",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialMovement_RecordStatus",
                table: "MaterialMovement",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Meeting_OrganizerPersonId",
                table: "Meeting",
                column: "OrganizerPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Meeting_ProjectId",
                table: "Meeting",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Meeting_RecordStatus",
                table: "Meeting",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendee_MeetingId",
                table: "MeetingAttendee",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendee_PersonId",
                table: "MeetingAttendee",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendee_RecordStatus",
                table: "MeetingAttendee",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingMinutes_MeetingId",
                table: "MeetingMinutes",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingMinutes_RecordStatus",
                table: "MeetingMinutes",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Milestone_ProjectId",
                table: "Milestone",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestone_RecordStatus",
                table: "Milestone",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyRequest_AccountId",
                table: "MoneyRequest",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyRequest_ProjectId",
                table: "MoneyRequest",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyRequest_RecordStatus",
                table: "MoneyRequest",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyRequest_RelatedAdvanceId",
                table: "MoneyRequest",
                column: "RelatedAdvanceId");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyRequest_RequesterPersonId",
                table: "MoneyRequest",
                column: "RequesterPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyRequest_ToAccountId",
                table: "MoneyRequest",
                column: "ToAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyRequestLine_RecordStatus",
                table: "MoneyRequestLine",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MoneyRequestLine_RequestId",
                table: "MoneyRequestLine",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsLink_NewsPostId",
                table: "NewsLink",
                column: "NewsPostId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsLink_RecordStatus",
                table: "NewsLink",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_NewsLink_TargetType_TargetId",
                table: "NewsLink",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsPost_RecordStatus",
                table: "NewsPost",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_NewsPost_Slug",
                table: "NewsPost",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notification_RecipientUserId_ReadAt",
                table: "Notification",
                columns: new[] { "RecipientUserId", "ReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_RecordStatus",
                table: "Notification",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_ParentOrganizationId",
                table: "Organization",
                column: "ParentOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organization_RecordStatus",
                table: "Organization",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequest_RecordStatus",
                table: "PasswordResetRequest",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequest_UserId",
                table: "PasswordResetRequest",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutChannel_PersonId",
                table: "PayoutChannel",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoutChannel_RecordStatus",
                table: "PayoutChannel",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Person_AdvisorPersonId",
                table: "Person",
                column: "AdvisorPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Person_OrganizationId",
                table: "Person",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Person_PhotoFileId",
                table: "Person",
                column: "PhotoFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Person_PublicSlug",
                table: "Person",
                column: "PublicSlug",
                unique: true,
                filter: "PublicSlug IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Person_RecordStatus",
                table: "Person",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PersonEducation_PersonId",
                table: "PersonEducation",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonEducation_RecordStatus",
                table: "PersonEducation",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PersonEmployment_OrganizationId",
                table: "PersonEmployment",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonEmployment_PersonId",
                table: "PersonEmployment",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonEmployment_RecordStatus",
                table: "PersonEmployment",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PositionAssignment_PersonId",
                table: "PositionAssignment",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionAssignment_PositionId",
                table: "PositionAssignment",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionAssignment_RecordStatus",
                table: "PositionAssignment",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalLicense_PersonId",
                table: "ProfessionalLicense",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalLicense_RecordStatus",
                table: "ProfessionalLicense",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressReport_ProjectId",
                table: "ProgressReport",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressReport_RecordStatus",
                table: "ProgressReport",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Project_FunderOrganizationId",
                table: "Project",
                column: "FunderOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_ParentProjectId",
                table: "Project",
                column: "ParentProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_RecordStatus",
                table: "Project",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectImpact_ProjectId",
                table: "ProjectImpact",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectImpact_RecordStatus",
                table: "ProjectImpact",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMember_PersonId",
                table: "ProjectMember",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMember_ProjectId",
                table: "ProjectMember",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMember_RecordStatus",
                table: "ProjectMember",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOutcome_ProjectId",
                table: "ProjectOutcome",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOutcome_RecordStatus",
                table: "ProjectOutcome",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOutput_ProjectId",
                table: "ProjectOutput",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOutput_RecordStatus",
                table: "ProjectOutput",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPartner_OrganizationId",
                table: "ProjectPartner",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPartner_ProjectId",
                table: "ProjectPartner",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPartner_RecordStatus",
                table: "ProjectPartner",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Publication_RecordStatus",
                table: "Publication",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationAuthor_PersonId",
                table: "PublicationAuthor",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationAuthor_PublicationId",
                table: "PublicationAuthor",
                column: "PublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationAuthor_RecordStatus",
                table: "PublicationAuthor",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationIndexing_PublicationId",
                table: "PublicationIndexing",
                column: "PublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationIndexing_RecordStatus",
                table: "PublicationIndexing",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationProject_ProjectId",
                table: "PublicationProject",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationProject_PublicationId",
                table: "PublicationProject",
                column: "PublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationProject_RecordStatus",
                table: "PublicationProject",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationUsage_PersonId",
                table: "PublicationUsage",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationUsage_PublicationId",
                table: "PublicationUsage",
                column: "PublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationUsage_RecordStatus",
                table: "PublicationUsage",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PublicFieldVisibility_PersonId_FieldKey",
                table: "PublicFieldVisibility",
                columns: new[] { "PersonId", "FieldKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicFieldVisibility_RecordStatus",
                table: "PublicFieldVisibility",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_ProjectId",
                table: "Recruitment",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Recruitment_RecordStatus",
                table: "Recruitment",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchArea_RecordStatus",
                table: "ResearchArea",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchAreaLink_RecordStatus",
                table: "ResearchAreaLink",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchAreaLink_ResearchAreaId",
                table: "ResearchAreaLink",
                column: "ResearchAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchAreaLink_TargetType_TargetId",
                table: "ResearchAreaLink",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_Resource_AssetId",
                table: "Resource",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Resource_ManagerPersonId",
                table: "Resource",
                column: "ManagerPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Resource_RecordStatus",
                table: "Resource",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceBlackout_RecordStatus",
                table: "ResourceBlackout",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceBlackout_ResourceId",
                table: "ResourceBlackout",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_Key",
                table: "Role",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Role_RecordStatus",
                table: "Role",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_RecordStatus",
                table: "RolePermission",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_RoleId_Module",
                table: "RolePermission",
                columns: new[] { "RoleId", "Module" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveAccessLogs_SubjectPersonId",
                table: "SensitiveAccessLogs",
                column: "SubjectPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Session_RecordStatus",
                table: "Session",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Session_TokenHash",
                table: "Session",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Session_UserId",
                table: "Session",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SitePage_Key",
                table: "SitePage",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SitePage_RecordStatus",
                table: "SitePage",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSetting_Key",
                table: "SiteSetting",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiteSetting_RecordStatus",
                table: "SiteSetting",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignee_PersonId",
                table: "TaskAssignee",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignee_RecordStatus",
                table: "TaskAssignee",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignee_TaskId",
                table: "TaskAssignee",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComment_RecordStatus",
                table: "TaskComment",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TaskComment_TaskId",
                table: "TaskComment",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_ActivityId",
                table: "TaskItem",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_ParentTaskId",
                table: "TaskItem",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_ProjectId",
                table: "TaskItem",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_RecordStatus",
                table: "TaskItem",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItem_SourceMeetingId",
                table: "TaskItem",
                column: "SourceMeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingRecord_PersonId",
                table: "TeachingRecord",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingRecord_RecordStatus",
                table: "TeachingRecord",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_PersonId",
                table: "UserAccount",
                column: "PersonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_RecordStatus",
                table: "UserAccount",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_Username",
                table: "UserAccount",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissionOverride_RecordStatus",
                table: "UserPermissionOverride",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissionOverride_UserId",
                table: "UserPermissionOverride",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RecordStatus",
                table: "UserRole",
                column: "RecordStatus");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleId",
                table: "UserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_UserId",
                table: "UserRole",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicAppointment");

            migrationBuilder.DropTable(
                name: "AccountBudget");

            migrationBuilder.DropTable(
                name: "Application");

            migrationBuilder.DropTable(
                name: "ApprovalAction");

            migrationBuilder.DropTable(
                name: "ApprovalPolicyStep");

            migrationBuilder.DropTable(
                name: "AssetLoan");

            migrationBuilder.DropTable(
                name: "Attachment");

            migrationBuilder.DropTable(
                name: "AwardRecipient");

            migrationBuilder.DropTable(
                name: "Booking");

            migrationBuilder.DropTable(
                name: "ChangeLogs");

            migrationBuilder.DropTable(
                name: "ContactMessage");

            migrationBuilder.DropTable(
                name: "Delegation");

            migrationBuilder.DropTable(
                name: "DocumentVersion");

            migrationBuilder.DropTable(
                name: "IdCounters");

            migrationBuilder.DropTable(
                name: "KeyResult");

            migrationBuilder.DropTable(
                name: "LedgerEntry");

            migrationBuilder.DropTable(
                name: "Maintenance");

            migrationBuilder.DropTable(
                name: "MaterialMovement");

            migrationBuilder.DropTable(
                name: "MeetingAttendee");

            migrationBuilder.DropTable(
                name: "MeetingMinutes");

            migrationBuilder.DropTable(
                name: "Milestone");

            migrationBuilder.DropTable(
                name: "MoneyRequestLine");

            migrationBuilder.DropTable(
                name: "NewsLink");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "NotificationOutbox");

            migrationBuilder.DropTable(
                name: "PasswordResetRequest");

            migrationBuilder.DropTable(
                name: "PayoutChannel");

            migrationBuilder.DropTable(
                name: "PersonEducation");

            migrationBuilder.DropTable(
                name: "PersonEmployment");

            migrationBuilder.DropTable(
                name: "PositionAssignment");

            migrationBuilder.DropTable(
                name: "ProfessionalLicense");

            migrationBuilder.DropTable(
                name: "ProgressReport");

            migrationBuilder.DropTable(
                name: "ProjectImpact");

            migrationBuilder.DropTable(
                name: "ProjectMember");

            migrationBuilder.DropTable(
                name: "ProjectOutcome");

            migrationBuilder.DropTable(
                name: "ProjectOutput");

            migrationBuilder.DropTable(
                name: "ProjectPartner");

            migrationBuilder.DropTable(
                name: "PublicationAuthor");

            migrationBuilder.DropTable(
                name: "PublicationIndexing");

            migrationBuilder.DropTable(
                name: "PublicationProject");

            migrationBuilder.DropTable(
                name: "PublicationUsage");

            migrationBuilder.DropTable(
                name: "PublicFieldVisibility");

            migrationBuilder.DropTable(
                name: "RemindersSent");

            migrationBuilder.DropTable(
                name: "ResearchAreaLink");

            migrationBuilder.DropTable(
                name: "ResourceBlackout");

            migrationBuilder.DropTable(
                name: "RolePermission");

            migrationBuilder.DropTable(
                name: "SensitiveAccessLogs");

            migrationBuilder.DropTable(
                name: "Session");

            migrationBuilder.DropTable(
                name: "SitePage");

            migrationBuilder.DropTable(
                name: "SiteSetting");

            migrationBuilder.DropTable(
                name: "TaskAssignee");

            migrationBuilder.DropTable(
                name: "TaskComment");

            migrationBuilder.DropTable(
                name: "TeachingRecord");

            migrationBuilder.DropTable(
                name: "UserPermissionOverride");

            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "Recruitment");

            migrationBuilder.DropTable(
                name: "ApprovalInstance");

            migrationBuilder.DropTable(
                name: "ApprovalPolicy");

            migrationBuilder.DropTable(
                name: "Award");

            migrationBuilder.DropTable(
                name: "ConsentRecord");

            migrationBuilder.DropTable(
                name: "Document");

            migrationBuilder.DropTable(
                name: "Material");

            migrationBuilder.DropTable(
                name: "MoneyRequest");

            migrationBuilder.DropTable(
                name: "NewsPost");

            migrationBuilder.DropTable(
                name: "GroupPosition");

            migrationBuilder.DropTable(
                name: "ResearchArea");

            migrationBuilder.DropTable(
                name: "Resource");

            migrationBuilder.DropTable(
                name: "TaskItem");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "UserAccount");

            migrationBuilder.DropTable(
                name: "Folder");

            migrationBuilder.DropTable(
                name: "Publication");

            migrationBuilder.DropTable(
                name: "Asset");

            migrationBuilder.DropTable(
                name: "Activity");

            migrationBuilder.DropTable(
                name: "Meeting");

            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "Person");

            migrationBuilder.DropTable(
                name: "Project");

            migrationBuilder.DropTable(
                name: "FileBlob");

            migrationBuilder.DropTable(
                name: "Organization");
        }
    }
}
