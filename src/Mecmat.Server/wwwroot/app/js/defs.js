// Field and column descriptors per collection. Forms and grids are built from these, so a
// field added here appears everywhere it should (CLAUDE.md: never write bespoke form markup).
import { getLang } from './i18n.js';

const tone = {
  status: (v) => ({ Active: 'green', Approved: 'green', Confirmed: 'green', Done: 'green', Published: 'green', Settled: 'green', Completed: 'green', Funded: 'green', Open: 'green', Accepted: 'green',
    Pending: 'yellow', InApproval: 'yellow', Submitted: 'yellow', PendingApproval: 'yellow', InReview: 'yellow', UnderReview: 'yellow', Due: 'yellow', InProgress: 'navy', Review: 'yellow', Drafting: 'navy', Extended: 'orange', New: 'orange',
    Rejected: 'maroon', Cancelled: 'maroon', Suspended: 'maroon', Expired: 'maroon', Late: 'maroon', Broken: 'maroon', Lost: 'maroon', Returned: 'orange', Revise: 'orange', Urgent: 'maroon', High: 'orange' }[v] || ''),
};
export { tone };

const L = () => getLang();
export const nameCol = (thKey, enKey) => ({ key: thKey, label: 'f.name', get: (r) => (L() === 'en' ? r[enKey] || r[thKey] : r[thKey] || r[enKey]) });

export const DEFS = {
  people: {
    fields: [
      { type: 'section', label: 'people.sec.identity' },
      { key: 'kind', type: 'enum', enum: 'PersonKind', required: true },
      { key: 'category', type: 'enum', enum: 'PersonCategory', required: true },
      { key: 'titleTh', label: 'f.titleTh_person' }, { key: 'titleEn', label: 'f.titleEn_person' },
      { key: 'firstNameTh', required: true }, { key: 'lastNameTh', required: true },
      { key: 'firstNameEn' }, { key: 'lastNameEn' },
      { key: 'nickname' }, { key: 'academicRank', type: 'enum', enum: 'AcademicRank' },
      { key: 'hasDoctorate', type: 'bool' }, { key: 'photoFileId', type: 'image', public: true },
      { type: 'section', label: 'people.sec.work' },
      { key: 'organizationId', type: 'ref', ref: 'organizations' }, { key: 'positionText' },
      { key: 'joinedDate', type: 'date' }, { key: 'leftDate', type: 'date' },
      { key: 'advisorPersonId', type: 'ref', ref: 'people', filter: (p) => p.category === 'Faculty' || p.category === 'Researcher' || p.category === 'Postdoc' },
      { key: 'studentId' }, { key: 'programName' }, { key: 'enrolledYear', type: 'year' }, { key: 'expectedGraduation', type: 'year' },
      { type: 'section', label: 'people.sec.contact' },
      { key: 'workEmail', inputType: 'email' }, { key: 'workPhone', inputType: 'tel' }, { key: 'officeRoom' }, { key: 'lineId' },
      { key: 'personalEmail', inputType: 'email' }, { key: 'mobilePhone', inputType: 'tel' },
      { type: 'section', label: 'people.sec.private' },
      { key: 'dateOfBirth', type: 'date', hint: 'people.privateHint' }, { key: 'nationalId', hint: 'people.sensitiveHint' },
      { type: 'section', label: 'people.sec.academic' },
      { key: 'expertiseTh', span2: true }, { key: 'expertiseEn', span2: true },
      { key: 'bioTh', type: 'html' }, { key: 'bioEn', type: 'html' },
      { key: 'orcidId' }, { key: 'scopusAuthorId' }, { key: 'googleScholarId' }, { key: 'researcherId' }, { key: 'researchGateUrl' }, { key: 'websiteUrl' },
      { key: 'publicSlug', hint: 'people.slugHint' }, { key: 'displayOrder', type: 'int' },
    ],
  },
  'person-education': {
    fields: [{ key: 'level', type: 'enum', enum: 'EducationLevel', required: true }, { key: 'degreeNameTh' }, { key: 'degreeNameEn' }, { key: 'field' },
      { key: 'institution', required: true }, { key: 'country' }, { key: 'startYear', type: 'year' }, { key: 'graduatedYear', type: 'year' },
      { key: 'honors' }, { key: 'thesisTitle', span2: true }, { key: 'isPublic', type: 'bool' }],
    columns: [{ key: 'level', type: 'enum', enum: 'EducationLevel' }, { key: 'degreeNameTh', get: (r) => (L() === 'en' ? r.degreeNameEn || r.degreeNameTh : r.degreeNameTh || r.degreeNameEn) },
      { key: 'field' }, { key: 'institution' }, { key: 'country' }, { key: 'graduatedYear', type: 'year' }, { key: 'isPublic', type: 'bool' }],
  },
  'person-employment': {
    fields: [{ key: 'position', required: true }, { key: 'organizationId', type: 'ref', ref: 'organizations' }, { key: 'organizationText' },
      { key: 'startDate', type: 'date' }, { key: 'endDate', type: 'date' }, { key: 'description', type: 'textarea' }, { key: 'isPublic', type: 'bool' }],
    columns: [{ key: 'position' }, { key: 'organizationId', type: 'ref', ref: 'organizations', get: (r) => r.organizationId }, { key: 'organizationText' },
      { key: 'startDate', type: 'date' }, { key: 'endDate', type: 'date' }, { key: 'isPublic', type: 'bool' }],
  },
  'academic-appointments': {
    fields: [{ key: 'rank', type: 'enum', enum: 'AcademicRank', required: true }, { key: 'fieldName' }, { key: 'fieldCode' }, { key: 'subField' },
      { key: 'method', type: 'int', min: 1, max: 3 }, { key: 'appointedDate', type: 'date' }, { key: 'referenceText', span2: true }],
    columns: [{ key: 'rank', type: 'enum', enum: 'AcademicRank' }, { key: 'fieldName' }, { key: 'fieldCode' }, { key: 'method' }, { key: 'appointedDate', type: 'date' }],
  },
  'teaching-records': {
    fields: [{ key: 'academicYear', type: 'year', required: true }, { key: 'semester', type: 'int', min: 1, max: 3 }, { key: 'level', type: 'enum', enum: 'EducationLevel', values: ['Bachelor', 'Master', 'Doctoral'], required: true },
      { key: 'courseCode' }, { key: 'courseName', required: true }, { key: 'credits', type: 'number' }, { key: 'hoursPerWeek', type: 'number' }, { key: 'responsibilityPct', type: 'int', min: 0, max: 100 }],
    columns: [{ key: 'academicYear', type: 'year' }, { key: 'semester' }, { key: 'level', type: 'enum', enum: 'EducationLevel' }, { key: 'courseCode' }, { key: 'courseName' }, { key: 'hoursPerWeek', num: true }, { key: 'responsibilityPct', num: true }],
  },
  licenses: {
    fields: [{ key: 'body', required: true, label: 'people.licenseBody' }, { key: 'level' }, { key: 'branch' }, { key: 'licenseNo' }, { key: 'issuedDate', type: 'date' }, { key: 'expiryDate', type: 'date' }, { key: 'isPublic', type: 'bool' }],
    columns: [{ key: 'body', label: 'people.licenseBody' }, { key: 'level' }, { key: 'branch' }, { key: 'licenseNo' }, { key: 'expiryDate', type: 'date' }],
  },
  'payout-channels': {
    fields: [{ key: 'kind', type: 'enum', enum: 'PayoutKind', required: true }, { key: 'bankName', show: (v) => v.kind !== 'PromptPay' }, { key: 'accountNo', show: (v) => v.kind !== 'PromptPay' },
      { key: 'accountName' }, { key: 'promptPayId', show: (v) => v.kind === 'PromptPay' }, { key: 'isPrimary', type: 'bool' }],
    columns: [{ key: 'kind', type: 'enum', enum: 'PayoutKind' }, { key: 'bankName' }, { key: 'accountNo' }, { key: 'promptPayId' }, { key: 'accountName' }, { key: 'isPrimary', type: 'bool' }],
  },
  'person-mentorships': {
    fields: [{ key: 'studentPersonId', type: 'ref', ref: 'people', required: true, label: 'f.studentPersonId' }, { key: 'mentorPersonId', type: 'ref', ref: 'people', required: true, label: 'f.mentorPersonId' },
      { key: 'role', type: 'enum', enum: 'MentorRole', required: true }, { key: 'degree', type: 'enum', enum: 'EducationLevel', values: ['Bachelor', 'Master', 'Doctoral', 'Other'] },
      { key: 'fromYear', type: 'year' }, { key: 'toYear', type: 'year' }, { key: 'thesisTitle', span2: true }, { key: 'note', span2: true }, { key: 'isPublic', type: 'bool' }],
  },
  'person-events': {
    fields: [{ key: 'kind', type: 'enum', enum: 'CareerEventKind', required: true }, { key: 'date', type: 'date', required: true }, { key: 'endDate', type: 'date' },
      { key: 'dateIsApproximate', type: 'bool', label: 'f.dateIsApproximate' },
      { key: 'titleTh', required: true, span2: true }, { key: 'titleEn', span2: true },
      { key: 'organizationId', type: 'ref', ref: 'organizations' }, { key: 'organizationText' }, { key: 'country' }, { key: 'url', hint: 'tl.urlHint' },
      { key: 'descriptionTh', type: 'textarea' }, { key: 'descriptionEn', type: 'textarea' }, { key: 'sourceUrl', hint: 'tl.sourceHint' }, { key: 'isPublic', type: 'bool' }],
  },
  organizations: {
    fields: [{ key: 'nameTh', required: true }, { key: 'nameEn' }, { key: 'shortName' }, { key: 'kind', type: 'enum', enum: 'OrganizationKind', required: true },
      { key: 'parentOrganizationId', type: 'ref', ref: 'organizations' }, { key: 'country' }, { key: 'website' }, { key: 'phone' }, { key: 'address', type: 'textarea' },
      { key: 'logoFileId', type: 'image', public: true }, { key: 'isPublic', type: 'bool', label: 'org.isPublic' }, { key: 'notes', type: 'textarea' }],
    columns: [nameCol('nameTh', 'nameEn'), { key: 'shortName' }, { key: 'kind', type: 'enum', enum: 'OrganizationKind' }, { key: 'country' }, { key: 'isPublic', type: 'bool', label: 'org.isPublic' }],
  },
  awards: {
    fields: [{ key: 'titleTh', required: true }, { key: 'titleEn' }, { key: 'issuer' }, { key: 'date', type: 'date' }, { key: 'level', type: 'enum', enum: 'AwardLevel' },
      { key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'description', type: 'textarea' }, { key: 'isPublic', type: 'bool' }],
  },
  publications: {
    fields: [
      { key: 'type', type: 'enum', enum: 'PublicationType', required: true }, { key: 'stage', type: 'enum', enum: 'PublicationStage', required: true },
      { key: 'titleEn', required: true, span2: true }, { key: 'titleTh', span2: true },
      { key: 'venueName', span2: true }, { key: 'venueKind', type: 'enum', enum: 'VenueKind' }, { key: 'year', type: 'year' },
      { key: 'volume' }, { key: 'issue' }, { key: 'pages' }, { key: 'articleNumber' }, { key: 'publishedDate', type: 'date' },
      { key: 'doi' }, { key: 'url' }, { key: 'issn' }, { key: 'isbn' }, { key: 'publisher' }, { key: 'edition' },
      { key: 'conferenceLocation', show: (v) => v.type === 'ConferencePaper' }, { key: 'conferenceDates', show: (v) => v.type === 'ConferencePaper' },
      { key: 'patentApplicationNo', show: (v) => v.type === 'Patent' || v.type === 'PettyPatent' }, { key: 'patentNo', show: (v) => v.type === 'Patent' || v.type === 'PettyPatent' },
      { key: 'filedDate', type: 'date', show: (v) => v.type === 'Patent' || v.type === 'PettyPatent' }, { key: 'grantedDate', type: 'date', show: (v) => v.type === 'Patent' || v.type === 'PettyPatent' },
      { key: 'patentCountry', show: (v) => v.type === 'Patent' || v.type === 'PettyPatent' },
      { key: 'courseCode', show: (v) => v.type === 'TeachingMaterial' }, { key: 'courseName', show: (v) => v.type === 'TeachingMaterial' }, { key: 'credits', type: 'number', show: (v) => v.type === 'TeachingMaterial' },
      { key: 'socialStageA', type: 'bool', show: (v) => v.type === 'SocialServiceWork' }, { key: 'socialStageB', type: 'bool', show: (v) => v.type === 'SocialServiceWork' }, { key: 'socialStageC', type: 'bool', show: (v) => v.type === 'SocialServiceWork' },
      { key: 'fromThesisOfPersonId', type: 'ref', ref: 'people' },
      { key: 'citationCount', type: 'int' }, { key: 'citationAsOf', type: 'date' },
      { key: 'keywords', span2: true }, { key: 'abstractEn', type: 'textarea' }, { key: 'abstractTh', type: 'textarea' },
      { key: 'apaCitation', type: 'textarea', hint: 'pub.apaHint' }, { key: 'isPublic', type: 'bool' },
    ],
    columns: [{ key: 'year', type: 'year' }, { key: 'titleEn', label: 'f.title' }, { key: 'venueName' }, { key: 'type', type: 'enum', enum: 'PublicationType' }, { key: 'stage', type: 'enum', enum: 'PublicationStage', tone: tone.status }],
  },
  'publication-authors': {
    fields: [{ key: 'order', type: 'int', required: true }, { key: 'personId', type: 'ref', ref: 'people', hint: 'pub.authorPersonHint' }, { key: 'externalName' }, { key: 'externalAffiliation' },
      { key: 'isFirstAuthor', type: 'bool' }, { key: 'isCorresponding', type: 'bool' }, { key: 'isEssentialContributor', type: 'bool' }, { key: 'isStudentAtTime', type: 'bool' },
      { key: 'contributionDescription', type: 'textarea' }, { key: 'contributionPct', type: 'int', min: 0, max: 100 },
      { key: 'certifiedAt', type: 'date' }, { key: 'certificationFileId', type: 'file' }],
  },
  'publication-indexing': {
    fields: [{ key: 'database', type: 'enum', enum: 'IndexDatabase', required: true }, { key: 'quartile', type: 'enum', enum: 'Quartile', values: ['Q1', 'Q2', 'Q3', 'Q4'] }, { key: 'impactFactor', type: 'number' }, { key: 'sjrYear', type: 'year' }, { key: 'note' }],
    columns: [{ key: 'database', type: 'enum', enum: 'IndexDatabase' }, { key: 'quartile' }, { key: 'impactFactor', num: true }, { key: 'sjrYear', type: 'year' }],
  },
  'publication-usage': {
    fields: [{ key: 'personId', type: 'ref', ref: 'people', required: true }, { key: 'forRank', type: 'enum', enum: 'AcademicRank', values: ['AsstProf', 'AssocProf', 'Prof'], required: true }, { key: 'year', type: 'year' }, { key: 'qualityResult' }, { key: 'note' }],
    columns: [{ key: 'personId', type: 'ref', ref: 'people' }, { key: 'forRank', type: 'enum', enum: 'AcademicRank' }, { key: 'year', type: 'year' }, { key: 'qualityResult' }],
  },
  projects: {
    fields: [
      { type: 'section', label: 'prj.sec.basic' },
      { key: 'kind', type: 'enum', enum: 'ProjectKind', required: true }, { key: 'status', type: 'enum', enum: 'ProjectStatus', required: true },
      { key: 'titleTh', required: true, span2: true }, { key: 'titleEn', span2: true },
      { key: 'code' }, { key: 'externalCode' }, { key: 'parentProjectId', type: 'ref', ref: 'projects', show: (v) => v.kind === 'SubProject' },
      { key: 'keywordsTh' }, { key: 'keywordsEn' },
      { type: 'section', label: 'prj.sec.funding' },
      { key: 'funderOrganizationId', type: 'ref', ref: 'organizations' }, { key: 'fundingScheme' }, { key: 'contractNo' }, { key: 'fiscalYear', type: 'year' },
      { key: 'startDate', type: 'date' }, { key: 'endDate', type: 'date' }, { key: 'extendedEndDate', type: 'date' }, { key: 'totalBudget', type: 'money' }, { key: 'institutionFeePct', type: 'number' },
      { type: 'section', label: 'prj.sec.readiness' },
      { key: 'trlStart', type: 'int', min: 1, max: 9 }, { key: 'trlTarget', type: 'int', min: 1, max: 9 }, { key: 'trlCurrent', type: 'int', min: 1, max: 9 },
      { key: 'srlStart', type: 'int', min: 1, max: 9 }, { key: 'srlTarget', type: 'int', min: 1, max: 9 }, { key: 'srlCurrent', type: 'int', min: 1, max: 9 },
      { key: 'researchSite' }, { key: 'beneficiaryArea' },
      { type: 'section', label: 'prj.sec.content' },
      { key: 'summary', type: 'html' }, { key: 'rationale', type: 'html' }, { key: 'objectives', type: 'html' }, { key: 'framework', type: 'html' }, { key: 'methodology', type: 'html' },
      { type: 'section', label: 'prj.sec.public' },
      { key: 'isPublic', type: 'bool' }, { key: 'coverFileId', type: 'image', public: true }, { key: 'publicSummaryTh', type: 'textarea' }, { key: 'publicSummaryEn', type: 'textarea' },
    ],
    columns: [{ key: 'code' }, { key: 'titleTh', label: 'f.title', get: (r) => (L() === 'en' ? r.titleEn || r.titleTh : r.titleTh) }, { key: 'kind', type: 'enum', enum: 'ProjectKind' },
      { key: 'status', type: 'enum', enum: 'ProjectStatus', tone: tone.status }, { key: 'startDate', type: 'date' }, { key: 'endDate', type: 'date' }, { key: 'totalBudget', type: 'money' }],
  },
  'project-members': {
    fields: [{ key: 'personId', type: 'ref', ref: 'people', required: true }, { key: 'role', type: 'enum', enum: 'ProjectRole', required: true }, { key: 'effortPct', type: 'int', min: 0, max: 100 },
      { key: 'fromDate', type: 'date' }, { key: 'toDate', type: 'date' }, { key: 'isPublic', type: 'bool' }],
    columns: [{ key: 'personId', type: 'ref', ref: 'people' }, { key: 'role', type: 'enum', enum: 'ProjectRole' }, { key: 'effortPct', num: true }, { key: 'fromDate', type: 'date' }, { key: 'toDate', type: 'date' }],
  },
  'project-partners': {
    fields: [{ key: 'organizationId', type: 'ref', ref: 'organizations', required: true }, { key: 'role', type: 'enum', enum: 'PartnerRole', required: true },
      { key: 'inCashAmount', type: 'money' }, { key: 'inKindAmount', type: 'money' }, { key: 'inKindDescription', type: 'textarea' }],
    columns: [{ key: 'organizationId', type: 'ref', ref: 'organizations' }, { key: 'role', type: 'enum', enum: 'PartnerRole' }, { key: 'inCashAmount', type: 'money' }, { key: 'inKindAmount', type: 'money' }],
  },
  activities: {
    fields: [{ key: 'name', required: true, span2: true }, { key: 'startDate', type: 'date' }, { key: 'endDate', type: 'date' }, { key: 'progressPct', type: 'int', min: 0, max: 100 }, { key: 'weightPct', type: 'int', min: 0, max: 100 },
      { key: 'ownerPersonId', type: 'ref', ref: 'people' }, { key: 'order', type: 'int' }, { key: 'deliverable', type: 'textarea' }],
  },
  milestones: {
    fields: [{ key: 'name', required: true, span2: true }, { key: 'dueDate', type: 'date' }, { key: 'status', type: 'enum', enum: 'MilestoneStatus', required: true }, { key: 'doneDate', type: 'date' }, { key: 'note', type: 'textarea' }],
    columns: [{ key: 'name' }, { key: 'dueDate', type: 'date' }, { key: 'status', type: 'enum', enum: 'MilestoneStatus', tone: tone.status }, { key: 'doneDate', type: 'date' }],
  },
  'project-outputs': {
    fields: [{ key: 'outputType', type: 'enum', enum: 'OutputType', required: true, span2: true }, { key: 'description', required: true, type: 'textarea' },
      { key: 'targetQty', type: 'number' }, { key: 'actualQty', type: 'number' }, { key: 'unit' }, { key: 'achievedDate', type: 'date' }],
    columns: [{ key: 'outputType', type: 'enum', enum: 'OutputType' }, { key: 'description' }, { key: 'targetQty', type: 'number', num: true }, { key: 'actualQty', type: 'number', num: true }, { key: 'unit' }],
  },
  'project-outcomes': {
    fields: [{ key: 'outcomeType', type: 'enum', enum: 'OutcomeType', required: true }, { key: 'date', type: 'date' }, { key: 'description', type: 'textarea', required: true }, { key: 'evidence', type: 'textarea' }],
    columns: [{ key: 'outcomeType', type: 'enum', enum: 'OutcomeType' }, { key: 'description' }, { key: 'date', type: 'date' }],
  },
  'project-impacts': {
    fields: [{ key: 'dimension', type: 'enum', enum: 'ImpactDimension', required: true }, { key: 'indicator' }, { key: 'value', type: 'number' }, { key: 'unit' }, { key: 'description', type: 'textarea', required: true }],
    columns: [{ key: 'dimension', type: 'enum', enum: 'ImpactDimension' }, { key: 'description' }, { key: 'indicator' }, { key: 'value', type: 'number', num: true }, { key: 'unit' }],
  },
  'key-results': {
    fields: [{ key: 'objective', required: true, span2: true }, { key: 'keyResultText', required: true, span2: true }, { key: 'target', type: 'number' }, { key: 'actual', type: 'number' }, { key: 'unit' }, { key: 'period' }],
    columns: [{ key: 'objective' }, { key: 'keyResultText' }, { key: 'target', type: 'number', num: true }, { key: 'actual', type: 'number', num: true }, { key: 'unit' }, { key: 'period' }],
  },
  'progress-reports': {
    fields: [{ key: 'kind', type: 'enum', enum: 'ReportKind', required: true }, { key: 'periodNo', type: 'int' }, { key: 'periodFrom', type: 'date' }, { key: 'periodTo', type: 'date' },
      { key: 'dueDate', type: 'date' }, { key: 'submittedDate', type: 'date' }, { key: 'status', type: 'enum', enum: 'ReportStatus', required: true }, { key: 'fileId', type: 'file' }, { key: 'summary', type: 'textarea' }],
    columns: [{ key: 'kind', type: 'enum', enum: 'ReportKind' }, { key: 'periodNo' }, { key: 'dueDate', type: 'date' }, { key: 'submittedDate', type: 'date' }, { key: 'status', type: 'enum', enum: 'ReportStatus', tone: tone.status }],
  },
  recruitments: {
    fields: [{ key: 'titleTh', required: true, span2: true }, { key: 'titleEn', span2: true }, { key: 'positionType', type: 'enum', enum: 'RecruitPositionType', required: true },
      { key: 'status', type: 'enum', enum: 'RecruitmentStatus', required: true }, { key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'seats', type: 'int' },
      { key: 'openDate', type: 'date' }, { key: 'closeDate', type: 'date' }, { key: 'compensationText', span2: true },
      { key: 'descriptionTh', type: 'html' }, { key: 'descriptionEn', type: 'html' }, { key: 'qualifications', type: 'textarea' }, { key: 'isPublic', type: 'bool' }],
    columns: [{ key: 'titleTh', label: 'f.title', get: (r) => (L() === 'en' ? r.titleEn || r.titleTh : r.titleTh) }, { key: 'positionType', type: 'enum', enum: 'RecruitPositionType' },
      { key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'closeDate', type: 'date' }, { key: 'status', type: 'enum', enum: 'RecruitmentStatus', tone: tone.status }],
  },
  applications: {
    fields: [{ key: 'status', type: 'enum', enum: 'ApplicationStatus', required: true }, { key: 'reviewerNotes', type: 'textarea' }],
    columns: [{ key: 'submittedAt', type: 'datetime' }, { key: 'fullName' }, { key: 'email' }, { key: 'recruitmentId', label: 'nav.recruitments', render: (r) => r._recruitment || '' }, { key: 'status', type: 'enum', enum: 'ApplicationStatus', tone: tone.status }],
  },
  accounts: {
    fields: [{ key: 'code', required: true }, { key: 'nameTh', required: true }, { key: 'nameEn' }, { key: 'kind', type: 'enum', enum: 'AccountKind', required: true },
      { key: 'parentAccountId', type: 'ref', ref: 'accounts' }, { key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'budgetCategory', type: 'enum', enum: 'BudgetCategory' },
      { key: 'custodianPersonId', type: 'ref', ref: 'people' }, { key: 'allowNegative', type: 'bool' }, { key: 'description', type: 'textarea' }],
  },
  'account-budgets': {
    fields: [{ key: 'fiscalYear', type: 'year', required: true, hint: 'fin.fyHint' }, { key: 'amount', type: 'money', required: true }, { key: 'note' }],
    columns: [{ key: 'fiscalYear', type: 'year' }, { key: 'amount', type: 'money' }, { key: 'note' }],
  },
  assets: {
    fields: [
      { key: 'nameTh', required: true }, { key: 'nameEn' }, { key: 'assetCode' }, { key: 'internalCode' }, { key: 'category' }, { key: 'status', type: 'enum', enum: 'AssetStatus', required: true },
      { key: 'brand' }, { key: 'model' }, { key: 'serialNo' }, { key: 'location' },
      { key: 'custodianPersonId', type: 'ref', ref: 'people' }, { key: 'acquiredDate', type: 'date' }, { key: 'acquisitionMethod', type: 'enum', enum: 'AcquisitionMethod' },
      { key: 'cost', type: 'money', hint: 'asset.costHint' }, { key: 'fundingProjectId', type: 'ref', ref: 'projects' }, { key: 'fundingAccountId', type: 'ref', ref: 'accounts' },
      { key: 'vendorOrganizationId', type: 'ref', ref: 'organizations' }, { key: 'referenceNo' }, { key: 'usefulLifeYears', type: 'int' }, { key: 'salvageValue', type: 'money' },
      { key: 'disposedDate', type: 'date' }, { key: 'disposalMethod' }, { key: 'photoFileId', type: 'image', public: true }, { key: 'isPublic', type: 'bool', label: 'asset.isPublic' },
      { key: 'specifications', type: 'textarea' }],
    columns: [{ key: 'assetCode' }, nameCol('nameTh', 'nameEn'), { key: 'brand' }, { key: 'model' }, { key: 'location' }, { key: 'custodianPersonId', type: 'ref', ref: 'people' },
      { key: 'cost', type: 'money' }, { key: 'status', type: 'enum', enum: 'AssetStatus', tone: tone.status }],
  },
  'asset-loans': {
    fields: [{ key: 'borrowerPersonId', type: 'ref', ref: 'people', required: true }, { key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'fromDate', type: 'date', required: true }, { key: 'dueDate', type: 'date' },
      { key: 'returnedDate', type: 'date' }, { key: 'conditionOut' }, { key: 'conditionIn' }],
    columns: [{ key: 'borrowerPersonId', type: 'ref', ref: 'people' }, { key: 'fromDate', type: 'date' }, { key: 'dueDate', type: 'date' }, { key: 'returnedDate', type: 'date' }],
  },
  maintenance: {
    fields: [{ key: 'date', type: 'date', required: true }, { key: 'kind', type: 'enum', enum: 'MaintenanceKind', required: true }, { key: 'cost', type: 'money' }, { key: 'vendorOrganizationId', type: 'ref', ref: 'organizations' },
      { key: 'nextDueDate', type: 'date' }, { key: 'certificateFileId', type: 'file' }, { key: 'description', type: 'textarea' }],
    columns: [{ key: 'date', type: 'date' }, { key: 'kind', type: 'enum', enum: 'MaintenanceKind' }, { key: 'description' }, { key: 'cost', type: 'money' }, { key: 'nextDueDate', type: 'date' }],
  },
  materials: {
    fields: [{ key: 'nameTh', required: true }, { key: 'nameEn' }, { key: 'code' }, { key: 'category' }, { key: 'unit', required: true }, { key: 'minStock', type: 'number' }, { key: 'location' }, { key: 'note', type: 'textarea' }],
  },
  'material-movements': {
    fields: [{ key: 'date', type: 'date', required: true }, { key: 'kind', type: 'enum', enum: 'MovementKind', required: true }, { key: 'qty', type: 'number', required: true, hint: 'mat.qtyHint' },
      { key: 'unitCost', type: 'money' }, { key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'personId', type: 'ref', ref: 'people' }, { key: 'note' }],
    columns: [{ key: 'date', type: 'date' }, { key: 'kind', type: 'enum', enum: 'MovementKind' }, { key: 'qty', type: 'number', num: true }, { key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'personId', type: 'ref', ref: 'people' }, { key: 'note' }],
  },
  resources: {
    fields: [{ key: 'nameTh', required: true }, { key: 'nameEn' }, { key: 'kind', type: 'enum', enum: 'ResourceKind', required: true }, { key: 'assetId', type: 'ref', ref: 'assets' },
      { key: 'location' }, { key: 'managerPersonId', type: 'ref', ref: 'people' }, { key: 'requiresApproval', type: 'bool' }, { key: 'isActive', type: 'bool' },
      { key: 'slotMinutes', type: 'int', min: 5, max: 240 }, { key: 'minDurationMinutes', type: 'int' }, { key: 'maxDurationMinutes', type: 'int' }, { key: 'maxAdvanceDays', type: 'int' },
      { key: 'color', inputType: 'color' }, { key: 'openHoursJson', type: 'hours' }, { key: 'description', type: 'textarea' }],
    columns: [nameCol('nameTh', 'nameEn'), { key: 'kind', type: 'enum', enum: 'ResourceKind' }, { key: 'location' }, { key: 'managerPersonId', type: 'ref', ref: 'people' }, { key: 'requiresApproval', type: 'bool' }, { key: 'isActive', type: 'bool' }],
  },
  'resource-blackouts': {
    fields: [{ key: 'startAt', type: 'datetime', required: true }, { key: 'endAt', type: 'datetime', required: true }, { key: 'reason', span2: true }],
    columns: [{ key: 'startAt', type: 'datetime' }, { key: 'endAt', type: 'datetime' }, { key: 'reason' }],
  },
  tasks: {
    fields: [{ key: 'title', required: true, span2: true }, { key: 'status', type: 'enum', enum: 'TaskState', required: true }, { key: 'priority', type: 'enum', enum: 'Priority', required: true },
      { key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'dueDate', type: 'date' }, { key: 'startDate', type: 'date' }, { key: 'tags' },
      { key: 'description', type: 'textarea' }],
  },
  meetings: {
    fields: [{ key: 'title', required: true, span2: true }, { key: 'kind', type: 'enum', enum: 'MeetingKind', required: true }, { key: 'status', type: 'enum', enum: 'MeetingStatus', required: true },
      { key: 'startAt', type: 'datetime', required: true }, { key: 'endAt', type: 'datetime', required: true }, { key: 'location' }, { key: 'onlineUrl' },
      { key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'visibility', type: 'enum', enum: 'MeetingVisibility', required: true }, { key: 'agenda', type: 'textarea' }],
    columns: [{ key: 'startAt', type: 'datetime' }, { key: 'title' }, { key: 'kind', type: 'enum', enum: 'MeetingKind' }, { key: 'location' }, { key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'status', type: 'enum', enum: 'MeetingStatus', tone: tone.status }],
  },
  folders: {
    fields: [{ key: 'name', required: true }, { key: 'parentFolderId', type: 'ref', ref: 'folders' }, { key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'visibility', type: 'enum', enum: 'FolderVisibility', required: true }],
  },
  documents: {
    fields: [{ key: 'title', required: true, span2: true }, { key: 'kind', type: 'enum', enum: 'DocumentKind', required: true }, { key: 'visibility', type: 'enum', enum: 'FolderVisibility', required: true },
      { key: 'folderId', type: 'ref', ref: 'folders' }, { key: 'projectId', type: 'ref', ref: 'projects' }, { key: 'tags', span2: true }],
  },
  news: {
    fields: [{ key: 'kind', type: 'enum', enum: 'NewsKind', required: true }, { key: 'status', type: 'enum', enum: 'PublishStatus', required: true },
      { key: 'titleTh', required: true, span2: true }, { key: 'titleEn', span2: true }, { key: 'slug', hint: 'news.slugHint' }, { key: 'pinned', type: 'bool' },
      { key: 'summaryTh', type: 'textarea' }, { key: 'summaryEn', type: 'textarea' }, { key: 'bodyTh', type: 'html' }, { key: 'bodyEn', type: 'html' },
      { key: 'coverFileId', type: 'image', public: true },
      { key: 'eventStartAt', type: 'datetime', show: (v) => v.kind === 'Event' || v.kind === 'Seminar' }, { key: 'eventEndAt', type: 'datetime', show: (v) => v.kind === 'Event' || v.kind === 'Seminar' },
      { key: 'eventLocation', show: (v) => v.kind === 'Event' || v.kind === 'Seminar' }],
    columns: [{ key: 'publishedAt', type: 'date' }, { key: 'titleTh', label: 'f.title', get: (r) => (L() === 'en' ? r.titleEn || r.titleTh : r.titleTh) }, { key: 'kind', type: 'enum', enum: 'NewsKind' },
      { key: 'authorPersonId', type: 'ref', ref: 'people' }, { key: 'status', type: 'enum', enum: 'PublishStatus', tone: tone.status }, { key: 'pinned', type: 'bool' }],
  },
  'research-areas': {
    fields: [{ key: 'nameTh', required: true }, { key: 'nameEn' }, { key: 'summaryTh', type: 'textarea' }, { key: 'summaryEn', type: 'textarea' }, { key: 'coverFileId', type: 'image', public: true }, { key: 'displayOrder', type: 'int' }],
    columns: [{ key: 'displayOrder', num: true }, nameCol('nameTh', 'nameEn')],
  },
  'site-pages': {
    fields: [{ key: 'key', readonly: true }, { key: 'titleTh', required: true }, { key: 'titleEn' }, { key: 'bodyTh', type: 'html' }, { key: 'bodyEn', type: 'html' }],
    columns: [{ key: 'key' }, { key: 'titleTh', label: 'f.title', get: (r) => (L() === 'en' ? r.titleEn || r.titleTh : r.titleTh) }, { key: 'modifiedAt', type: 'datetime' }],
  },
  'site-settings': {
    fields: [{ key: 'key', readonly: true }, { key: 'value', type: 'textarea' }],
    columns: [{ key: 'key' }, { key: 'value' }],
  },
  'contact-messages': {
    fields: [{ key: 'status', type: 'enum', enum: 'MessageStatus', required: true }, { key: 'note', type: 'textarea' }],
    columns: [{ key: 'submittedAt', type: 'datetime' }, { key: 'name' }, { key: 'email' }, { key: 'subject' }, { key: 'status', type: 'enum', enum: 'MessageStatus', tone: tone.status }],
  },
  positions: {
    fields: [{ key: 'nameTh', required: true }, { key: 'nameEn' }, { key: 'rank', type: 'int', required: true, hint: 'auth2.rankHint' }, { key: 'isPublic', type: 'bool' }],
    columns: [{ key: 'rank', num: true }, nameCol('nameTh', 'nameEn'), { key: 'isPublic', type: 'bool' }],
  },
  'position-assignments': {
    fields: [{ key: 'positionId', type: 'ref', ref: 'positions', required: true }, { key: 'personId', type: 'ref', ref: 'people', required: true }, { key: 'fromDate', type: 'date' }, { key: 'toDate', type: 'date' }],
    columns: [{ key: 'positionId', type: 'ref', ref: 'positions' }, { key: 'personId', type: 'ref', ref: 'people' }, { key: 'fromDate', type: 'date' }, { key: 'toDate', type: 'date' }],
  },
  'approval-policies': {
    fields: [{ key: 'name', required: true, span2: true }, { key: 'subjectType', type: 'enum', enum: 'SubjectType', values: ['MoneyRequest', 'Booking'], required: true },
      { key: 'requestKind', type: 'enum', enum: 'MoneyRequestKind', show: (v) => v.subjectType === 'MoneyRequest' },
      { key: 'minAmount', type: 'money', show: (v) => v.subjectType === 'MoneyRequest' }, { key: 'maxAmount', type: 'money', show: (v) => v.subjectType === 'MoneyRequest' },
      { key: 'accountScopeId', type: 'ref', ref: 'accounts' }, { key: 'projectScopeId', type: 'ref', ref: 'projects' }, { key: 'priority', type: 'int' }, { key: 'isActive', type: 'bool' }],
  },
  'approval-policy-steps': {
    fields: [{ key: 'stepOrder', type: 'int', required: true }, { key: 'approverKind', type: 'enum', enum: 'ApproverKind', required: true },
      { key: 'approverRef', label: 'f.approverRef', type: 'ref', ref: 'positions', show: (v) => v.approverKind === 'Position' },
      { key: 'approverPerson', label: 'f.approverRef', type: 'ref', ref: 'people', show: (v) => v.approverKind === 'Person' },
      { key: 'approverRole', label: 'f.role', type: 'enum', enum: 'ProjectRole', values: ['PI', 'CoPI', 'FinanceContact', 'Coordinator'], show: (v) => v.approverKind === 'ProjectRole' },
      { key: 'rule', type: 'enum', enum: 'StepRule', required: true }],
  },
  delegations: {
    fields: [{ key: 'fromPersonId', type: 'ref', ref: 'people', required: true }, { key: 'toPersonId', type: 'ref', ref: 'people', required: true }, { key: 'fromDate', type: 'date', required: true }, { key: 'toDate', type: 'date', required: true },
      { key: 'subjectType', type: 'enum', enum: 'SubjectType', values: ['MoneyRequest', 'Booking'] }, { key: 'reason', span2: true }],
    columns: [{ key: 'fromPersonId', type: 'ref', ref: 'people' }, { key: 'toPersonId', type: 'ref', ref: 'people' }, { key: 'fromDate', type: 'date' }, { key: 'toDate', type: 'date' }, { key: 'subjectType', type: 'enum', enum: 'SubjectType' }, { key: 'reason' }],
  },
};
