using Microsoft.EntityFrameworkCore;
using Mecmat.Auth;
using Mecmat.Domain;

namespace Mecmat.Data;

/// <summary>First-run data: roles and the permission matrix from docs/authorization.md, positions,
/// default approval policies (ADR 0005), site settings, and the first admin. Every row is ordinary
/// data the admin can change afterwards; nothing here is enforced by code.</summary>
public static class Seeder
{
    // Cell = "related/all" with V C E M or '-' (docs/authorization.md §2).
    private static readonly string[] ModuleOrder =
    [
        Modules.People, Modules.Sensitive, Modules.Organizations, Modules.Users, Modules.ApprovalsConfig,
        Modules.Publications, Modules.Projects, Modules.Finance, Modules.Assets, Modules.Resources, Modules.Bookings,
        Modules.Tasks, Modules.Meetings, Modules.Documents, Modules.Cms, Modules.Inbox, Modules.Dashboard, Modules.Audit,
    ];

    private static readonly (string Key, string Th, string En, string Matrix)[] RoleDefs =
    [
        ("admin", "ผู้ดูแลระบบ", "Administrator",
            "M/M M/M M/M M/M M/M M/M M/M M/M M/M M/M M/M M/M M/M M/M M/M M/M M/M V/V"),
        ("group-head", "หัวหน้ากลุ่มวิจัย", "Group head",
            "E/E V/V E/E E/E E/E E/E E/E E/E E/E E/E E/E E/E E/E E/E E/E E/E V/V V/V"),
        ("faculty", "อาจารย์/หัวหน้าโครงการ", "Faculty / PI",
            "E/V E/- C/C -/- V/V E/V E/C E/- V/V V/V C/C E/C E/C E/C C/C V/V V/V -/-"),
        ("researcher", "นักวิจัย/เจ้าหน้าที่", "Researcher / staff",
            "E/V E/- C/C -/- V/V E/V E/V C/- V/V V/V C/C E/C E/C E/C C/C -/- V/V -/-"),
        ("finance", "ผู้จัดการการเงิน", "Finance manager",
            "V/V V/V C/C -/- V/V V/V V/V E/E V/V -/- C/C E/- E/C E/- -/- -/- V/V -/-"),
        ("lab-manager", "ผู้ดูแลห้องปฏิบัติการ", "Lab manager",
            "V/V -/- V/V -/- -/- V/V V/V C/- M/M M/M E/E E/- E/C E/- -/- -/- V/V -/-"),
        ("student", "นักศึกษา", "Student",
            "E/V E/- V/V -/- -/- E/V V/- C/- V/V V/V C/C E/- E/- E/- C/C -/- -/- -/-"),
        ("external", "บุคคลภายนอก", "External collaborator",
            "E/- E/- -/- -/- -/- E/- V/- -/- -/- -/- -/- E/- V/- E/- -/- -/- -/- -/-"),
        ("transient", "บุคลากรชั่วคราว (ขาจร)", "Short-term member",
            "E/- E/- -/- -/- -/- V/- V/- C/- V/V V/V C/C E/- E/- E/- -/- -/- -/- -/-"),
    ];

    private static PermAction P(char c) => c switch
    {
        'V' => PermAction.View, 'C' => PermAction.Create, 'E' => PermAction.Edit, 'M' => PermAction.Manage, _ => PermAction.None,
    };

    public static async Task<string?> RunAsync(AppDbContext db, ILogger log)
    {
        if (await db.Set<Role>().AnyAsync()) return null;

        var roles = new Dictionary<string, Role>();
        foreach (var (key, th, en, matrix) in RoleDefs)
        {
            var r = new Role { Key = key, NameTh = th, NameEn = en, IsSystem = true };
            db.Add(r);
            roles[key] = r;
        }
        await db.SaveChangesAsync();
        foreach (var (key, _, _, matrix) in RoleDefs)
        {
            var cells = matrix.Split(' ');
            for (var i = 0; i < ModuleOrder.Length; i++)
                db.Add(new RolePermission
                {
                    RoleId = roles[key].Id, Module = ModuleOrder[i], RelatedAction = P(cells[i][0]), AllAction = P(cells[i][2]),
                });
        }

        var head = new GroupPosition { NameTh = "หัวหน้ากลุ่มวิจัย", NameEn = "Group Head", Rank = 1 };
        var deputy = new GroupPosition { NameTh = "รองหัวหน้ากลุ่มวิจัย", NameEn = "Deputy Head", Rank = 2 };
        var financeMgr = new GroupPosition { NameTh = "ผู้จัดการการเงิน", NameEn = "Finance Manager", Rank = 3 };
        var secretary = new GroupPosition { NameTh = "เลขานุการกลุ่ม", NameEn = "Group Secretary", Rank = 4 };
        var labMgr = new GroupPosition { NameTh = "ผู้ดูแลห้องปฏิบัติการ", NameEn = "Laboratory Manager", Rank = 5 };
        db.AddRange(head, deputy, financeMgr, secretary, labMgr);
        await db.SaveChangesAsync();

        // Default money routes (ADR 0005). Amounts in satang.
        await AddPolicyAsync(db, "เงิน ≤ 5,000 บาท", nameof(MoneyRequest), null, 500_000,
            (ApproverKind.ProjectRole, nameof(ProjectRole.PI)));
        await AddPolicyAsync(db, "เงิน 5,000.01–50,000 บาท", nameof(MoneyRequest), 500_001, 5_000_000,
            (ApproverKind.ProjectRole, nameof(ProjectRole.PI)), (ApproverKind.Position, head.Id));
        await AddPolicyAsync(db, "เงิน > 50,000 บาท", nameof(MoneyRequest), 5_000_001, null,
            (ApproverKind.ProjectRole, nameof(ProjectRole.PI)), (ApproverKind.Position, financeMgr.Id), (ApproverKind.Position, head.Id));
        await AddPolicyAsync(db, "การจองทรัพยากรที่ต้องอนุมัติ", nameof(Booking), null, null,
            (ApproverKind.ResourceManager, null));

        foreach (var (k, v) in new (string, string)[]
                 {
                     ("site.nameTh", "กลุ่มวิจัยกลศาสตร์และวัสดุ (MECMAT)"),
                     ("site.nameEn", "Mechanics and Materials Research Group (MECMAT)"),
                     ("site.departmentTh", "ภาควิชาวิศวกรรมโยธา คณะวิศวกรรมศาสตร์ มหาวิทยาลัยเทคโนโลยีพระจอมเกล้าธนบุรี"),
                     ("site.departmentEn", "Department of Civil Engineering, Faculty of Engineering, King Mongkut's University of Technology Thonburi"),
                     ("site.addressTh", "126 ถนนประชาอุทิศ แขวงบางมด เขตทุ่งครุ กรุงเทพฯ 10140"),
                     ("site.addressEn", "126 Pracha Uthit Rd., Bang Mod, Thung Khru, Bangkok 10140, Thailand"),
                     ("site.phone", "02 470 9313"),
                     ("site.email", "kasan.cha@kmutt.ac.th"),
                     ("privacy.version", "2026-10-29"),
                     ("finance.advanceDays", "30"),
                     ("recruitment.cvRetentionDays", "365"),
                 })
            db.Add(new SiteSetting { Key = k, Value = v });

        db.AddRange(
            new SitePage
            {
                Key = "about", TitleTh = "เกี่ยวกับเรา", TitleEn = "About us",
                BodyTh = "<p>กลุ่มวิจัยกลศาสตร์และวัสดุ (MECMAT) ภาควิชาวิศวกรรมโยธา มจธ. ศึกษาพฤติกรรมเชิงกลของโครงสร้างและวัสดุก่อสร้าง ตั้งแต่ไม้และวัสดุชีวภาพ วัสดุคอมโพสิต ไปจนถึงกลศาสตร์ของโครงสร้างเปลือกบาง ผ่านการทดสอบในห้องปฏิบัติการ การวิเคราะห์เชิงทฤษฎี และแบบจำลองเชิงตัวเลข</p>",
                BodyEn = "<p>The Mechanics and Materials Research Group (MECMAT), Department of Civil Engineering, KMUTT, studies the mechanical behaviour of structures and construction materials — from timber and bio-based materials and composites to the mechanics of shell structures — through laboratory testing, analytical solutions and numerical modelling.</p>",
            },
            new SitePage { Key = "contact", TitleTh = "ติดต่อเรา", TitleEn = "Contact us" },
            new SitePage
            {
                Key = "privacy", TitleTh = "นโยบายความเป็นส่วนตัว", TitleEn = "Privacy policy",
                BodyTh = "<p>เว็บไซต์นี้เก็บข้อมูลที่ท่านกรอกในแบบฟอร์มติดต่อและแบบฟอร์มสมัครงาน (ชื่อ อีเมล เบอร์โทร ข้อความ และไฟล์ประวัติ) เพื่อใช้ติดต่อกลับและพิจารณาการสมัครเท่านั้น ไม่เปิดเผยแก่บุคคลภายนอก ไฟล์ประวัติของผู้ที่ไม่ได้รับการคัดเลือกจะถูกลบภายใน 1 ปี ท่านขอดู แก้ไข หรือลบข้อมูลของท่านได้โดยติดต่อผู้ดูแลกลุ่มวิจัย</p>",
                BodyEn = "<p>This website keeps what you enter in the contact and application forms (name, e-mail, phone, message and CV) only to reply to you and to consider your application. It is not disclosed to third parties. CVs of applicants who are not selected are deleted within one year. You may ask to see, correct or delete your data by contacting the group.</p>",
            },
            new SitePage { Key = "join-us", TitleTh = "ร่วมงานกับเรา", TitleEn = "Join us" });

        string[] areas =
        [
            "โครงสร้างไม้และวัสดุชีวภาพ|Timber and bio-based structures",
            "กลศาสตร์โครงสร้างและโครงสร้างเปลือกบาง|Structural mechanics and shell structures",
            "ชิ้นส่วนโครงสร้างคอมโพสิตและไฮบริด|Composite and hybrid structural members",
            "อาคารยั่งยืนและพลังงาน|Sustainable and energy-efficient buildings",
        ];
        var areaRows = areas.Select((a, i) => new ResearchArea { NameTh = a.Split('|')[0], NameEn = a.Split('|')[1], DisplayOrder = (i + 1) * 10 }).ToList();
        db.AddRange(areaRows);

        db.Add(new Account { Code = "MECMAT", NameTh = "กองทุนกลางกลุ่ม MECMAT", NameEn = "MECMAT central fund", Kind = AccountKind.FundingSource });
        await db.SaveChangesAsync();

        var (otp, person) = await SeedAdminAsync(db, roles["admin"], areaRows);
        db.Add(new PositionAssignment { PositionId = head.Id, PersonId = person.Id, FromDate = Services.Clock.TodayBangkok() });
        await db.SaveChangesAsync();
        // The password itself is printed once by Program on the console, never written to the log.
        log.LogWarning("First admin created: username kasan.cha (one-time password shown on the console)");
        return otp;
    }

    private static async Task AddPolicyAsync(AppDbContext db, string name, string subject, long? min, long? max,
        params (ApproverKind Kind, string? Ref)[] steps)
    {
        var p = new ApprovalPolicy { Name = name, SubjectType = subject, MinAmount = min, MaxAmount = max };
        db.Add(p);
        await db.SaveChangesAsync();
        for (var i = 0; i < steps.Length; i++)
            db.Add(new ApprovalPolicyStep { PolicyId = p.Id, StepOrder = i + 1, ApproverKind = steps[i].Kind, ApproverRef = steps[i].Ref });
    }

    /// <summary>Dr. Kasan Chanto, from public sources (KMUTT CE staff page, ORCID 0000-0003-2312-4201,
    /// Google Scholar). The owner asked to skip his review for now; every field is editable in the app.</summary>
    private static async Task<(string Otp, Person Person)> SeedAdminAsync(AppDbContext db, Role adminRole, List<ResearchArea> areas)
    {
        var kmutt = new Organization { NameTh = "มหาวิทยาลัยเทคโนโลยีพระจอมเกล้าธนบุรี", NameEn = "King Mongkut's University of Technology Thonburi", ShortName = "KMUTT", Kind = OrganizationKind.University, Country = "Thailand", Website = "https://www.kmutt.ac.th" };
        var tohoku = new Organization { NameTh = "มหาวิทยาลัยโทโฮคุ", NameEn = "Tohoku University", Kind = OrganizationKind.University, Country = "Japan" };
        db.AddRange(kmutt, tohoku);
        await db.SaveChangesAsync();
        var ce = new Organization { NameTh = "ภาควิชาวิศวกรรมโยธา คณะวิศวกรรมศาสตร์", NameEn = "Department of Civil Engineering, Faculty of Engineering", Kind = OrganizationKind.Department, ParentOrganizationId = kmutt.Id, Country = "Thailand", Website = "https://ce.kmutt.ac.th" };
        db.Add(ce);
        await db.SaveChangesAsync();

        var p = new Person
        {
            Kind = PersonKind.Internal, Category = PersonCategory.Faculty, TitleTh = "นาย", TitleEn = "Mr.", HasDoctorate = true,
            FirstNameTh = "กสาน", LastNameTh = "จันทร์โต", FirstNameEn = "Kasan", LastNameEn = "Chanto",
            WorkEmail = "kasan.cha@kmutt.ac.th", WorkPhone = "02 470 9313", OrganizationId = ce.Id,
            PositionText = "อาจารย์ ภาควิชาวิศวกรรมโยธา (กลุ่มวิศวกรรมโครงสร้าง)",
            ExpertiseTh = "วิศวกรรมโครงสร้าง, โครงสร้างไม้, กลศาสตร์โครงสร้าง, การออกแบบโครงสร้าง",
            ExpertiseEn = "Structural engineering, Timber structures, Structural mechanics, Structural design",
            OrcidId = "0000-0003-2312-4201", ScopusAuthorId = "57216418823", GoogleScholarId = "N2BcDtYAAAAJ",
            ResearchGateUrl = "https://www.researchgate.net/profile/Kasan-Chanto-2",
            PublicSlug = "kasan-chanto", DisplayOrder = 1,
        };
        db.Add(p);
        await db.SaveChangesAsync();

        db.AddRange(
            new PersonEducation { PersonId = p.Id, Level = EducationLevel.Doctoral, DegreeNameTh = "ปร.ด.", DegreeNameEn = "Ph.D.", Field = "Civil Engineering", Institution = "King Mongkut's University of Technology Thonburi", Country = "Thailand", StartYear = 2018, GraduatedYear = 2024 },
            new PersonEducation { PersonId = p.Id, Level = EducationLevel.Master, DegreeNameTh = "วศ.ม.", DegreeNameEn = "M.Eng.", Field = "Civil Engineering", Institution = "King Mongkut's University of Technology Thonburi", Country = "Thailand" },
            new PersonEducation { PersonId = p.Id, Level = EducationLevel.Bachelor, DegreeNameTh = "วศ.บ.", DegreeNameEn = "B.Eng.", Field = "Civil Engineering", Institution = "King Mongkut's University of Technology Thonburi", Country = "Thailand", Honors = "First Class Honours" },
            new PersonEducation { PersonId = p.Id, Level = EducationLevel.Exchange, DegreeNameEn = "JYPE Program", Field = "Civil and Environmental Engineering", Institution = "Tohoku University", Country = "Japan", StartYear = 2014, GraduatedYear = 2015 },
            new PersonEmployment { PersonId = p.Id, OrganizationId = ce.Id, Position = "อาจารย์ (Lecturer)" },
            new PersonEmployment { PersonId = p.Id, OrganizationId = ce.Id, Position = "เลขานุการภาควิชาวิศวกรรมโยธา (Department Secretary)" },
            new PersonEmployment { PersonId = p.Id, OrganizationId = kmutt.Id, OrganizationText = "School of Architecture and Design, KMUTT", Position = "อาจารย์พิเศษ (Adjunct Lecturer)" });

        foreach (var key in new[] { "photo", "positionText", "academicRank", "workEmail", "workPhone", "expertise", "bio", "orcid", "scopus", "scholar", "researchgate", "education", "employment", "awards", "publications", "projects" })
            db.Add(new PublicFieldVisibility { PersonId = p.Id, FieldKey = key, IsPublic = true });

        foreach (var a in areas.Take(3))
            db.Add(new ResearchAreaLink { ResearchAreaId = a.Id, TargetType = nameof(Person), TargetId = p.Id });

        // Journal articles with DOIs found on Google Scholar / ORCID. Author lists are as listed
        // there; ก.พ.อ. roles beyond first author are left for the owner to confirm.
        (string Title, string Venue, int Year, string? Vol, string? Pages, string? Doi, string[] Authors)[] pubs =
        [
            ("Static and cyclic flexural behavior of hybrid hollow core slabs with thermally modified rubberwood-WPVC composites: Experimental, analytical, and numerical studies",
                "Structures", 2026, "88", "111895", "10.1016/j.istruc.2026.111895", ["T. Udtaranakron", "K. Chanto", "T. Pulngern", "Y. Yamazaki", "V. Rosarpitak"]),
            ("Experimental, analytical, and numerical studies of steel-strengthened glulam wood–PVC composite hollow members under four-point bending",
                "Journal of Reinforced Plastics and Composites", 2026, "45(9-10)", "2051–2076", null, ["P. Aunyingcharoen", "T. Pulngern", "K. Chanto", "V. Rosarpitak"]),
            ("Behavior of sandwich glued laminated bamboo structures with a core formed by bioplastic fiber using 3D printing technology",
                "Journal of Renewable Materials", 2025, "13(12)", "2453–2478", "10.32604/jrm.2025.02025-0137", ["N. Mahasuwanchai", "T. Udtaranakron", "K. Chanto", "T. Pulngern"]),
            ("Analytical solution and buckling of hemi-ellipsoidal shell structures of revolution under uniformly distributed load",
                "International Journal of Structural Stability and Dynamics", 2024, "24(03)", "2450026", "10.1142/S0219455424500263", ["K. Chanto", "T. Pulngern", "C. Tangbanjongkij", "W. Jiammeepreecha", "S. Chucheepsakul"]),
            ("Effect of bending rigidity and nonlinear strains on free vibration of hemi-ellipsoidal shells",
                "Journal of Vibration and Acoustics", 2023, "145(5)", "051004", "10.1115/1.4063114", ["K. Chanto", "T. Pulngern", "C. Tangbanjongkij", "W. Jiammeepreecha"]),
            ("Physical and mechanical behaviors of thermally modified rubberwood glulam beam under sustained and cyclic loading",
                "Wood and Fiber Science", 2020, "52(3)", "298–312", "10.22382/wfs-2020-028", ["T. Pulngern", "T. Udtaranakron", "K. Chanto"]),
            ("Effect of lamina thickness on flexural performance and creep behavior of Douglas fir glued laminated timber beam",
                "Wood Research", 2020, "65(5)", "715–725", "10.37763/wr.1336-4561/65.5.715726", ["T. Pulngern", "K. Chanto", "W. Pansuwan", "W. Pattaraumpornsak"]),
            ("Solar Decathlon Europe 2019: The Resilient Nest as a solar powered and energy efficiency rooftop house for urban density",
                "IOP Conference Series: Earth and Environmental Science", 2020, "463", "012033", "10.1088/1755-1315/463/1/012033", ["K. Chanto", "P. Mahasuwanchai", "W. Eakintumas", "C. Lairat", "N. Thavornsettawat"]),
            ("Embodied energy and life-cycle greenhouse gas of conventional and the resilient nest house",
                "IOP Conference Series: Earth and Environmental Science", 2020, "463", "012034", "10.1088/1755-1315/463/1/012034", ["K. Tanthanawiwat", "K. Chanto"]),
        ];
        foreach (var (title, venue, year, vol, pages, doi, authors) in pubs)
        {
            var conf = venue.StartsWith("IOP", StringComparison.Ordinal);
            var pub = new Publication
            {
                Type = conf ? PublicationType.ConferencePaper : PublicationType.JournalArticle,
                VenueKind = conf ? VenueKind.Conference : VenueKind.Journal,
                Stage = PublicationStage.Published, TitleEn = title, VenueName = venue, Year = year, Volume = vol, Pages = pages,
                Doi = doi, Url = doi == null ? null : "https://doi.org/" + doi,
            };
            db.Add(pub);
            await db.SaveChangesAsync();
            for (var i = 0; i < authors.Length; i++)
            {
                var me = authors[i] == "K. Chanto";
                db.Add(new PublicationAuthor
                {
                    PublicationId = pub.Id, Order = i + 1, PersonId = me ? p.Id : null, ExternalName = me ? null : authors[i],
                    IsFirstAuthor = i == 0,
                });
            }
            if (!conf) db.Add(new PublicationIndexing { PublicationId = pub.Id, Database = IndexDatabase.Scopus });
        }

        var sde = new Award
        {
            TitleTh = "4 รางวัลจาก Solar Decathlon Europe 2019 (ทีม The Resilient Nest)",
            TitleEn = "Four awards at Solar Decathlon Europe 2019 (team The Resilient Nest)",
            Issuer = "Solar Decathlon Europe 2019, Hungary", Date = new DateOnly(2019, 9, 1), Level = AwardLevel.International,
            Description = "People's Choice Award; 2nd place Comfort Conditions; 3rd place Neighbourhood Integration & Impact; 3rd place Innovation & Viability. Project Manager and team leader.",
        };
        db.Add(sde);
        await db.SaveChangesAsync();
        db.Add(new AwardRecipient { AwardId = sde.Id, PersonId = p.Id });

        var otp = PasswordHasher.GenerateOneTime();
        var user = new UserAccount
        {
            PersonId = p.Id, Username = "kasan.cha", PasswordHash = PasswordHasher.Hash(otp), PasswordChangedAt = DateTime.UtcNow,
            MustChangePassword = true, Status = AccountStatus.Active, IsAdmin = true,
        };
        db.Add(user);
        await db.SaveChangesAsync();
        db.Add(new UserRole { UserId = user.Id, RoleId = adminRole.Id });
        await db.SaveChangesAsync();
        return (otp, p);
    }

    /// <summary>`Mecmat.Server reset-admin &lt;username&gt;` — the way back in if the only admin forgets the
    /// password, since there is no e-mail recovery. Needs shell access to the server, which is the point.</summary>
    public static async Task<string> ResetPasswordAsync(AppDbContext db, string username)
    {
        var u = await db.Set<UserAccount>().FirstOrDefaultAsync(x => x.Username == username)
                ?? throw new InvalidOperationException($"No user '{username}'.");
        var otp = PasswordHasher.GenerateOneTime();
        u.PasswordHash = PasswordHasher.Hash(otp);
        u.PasswordChangedAt = DateTime.UtcNow;
        u.MustChangePassword = true;
        u.Status = AccountStatus.Active;
        db.LogEvent(nameof(UserAccount), u.Id, "PasswordResetFromShell");
        await db.SaveChangesAsync();
        return otp;
    }
}
