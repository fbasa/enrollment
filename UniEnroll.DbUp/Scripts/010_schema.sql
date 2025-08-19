SET XACT_ABORT ON;
BEGIN TRAN;

-- 1) Core reference tables
IF OBJECT_ID('dbo.Department','U') IS NULL
CREATE TABLE dbo.Department(
  departmentId bigint IDENTITY(1,1) PRIMARY KEY,
  code         varchar(16)  NOT NULL UNIQUE,
  name         nvarchar(200) NOT NULL
);

IF OBJECT_ID('dbo.Program','U') IS NULL
CREATE TABLE dbo.Program(
  programId bigint IDENTITY(1,1) PRIMARY KEY,
  departmentId bigint NOT NULL FOREIGN KEY REFERENCES dbo.Department(departmentId),
  code         varchar(32) NOT NULL UNIQUE,
  name         nvarchar(200) NOT NULL
);

IF OBJECT_ID('dbo.Term','U') IS NULL
CREATE TABLE dbo.Term(
  termId bigint IDENTITY(1,1) PRIMARY KEY,
  code   varchar(32) NOT NULL UNIQUE,
  startDate date NOT NULL,
  endDate   date NOT NULL,
  addDropDeadlineDate date NOT NULL,
  CONSTRAINT CK_Term_DateRange CHECK (endDate > startDate AND addDropDeadlineDate >= startDate AND addDropDeadlineDate <= endDate)
);

IF OBJECT_ID('dbo.Course','U') IS NULL
CREATE TABLE dbo.Course(
  courseId bigint IDENTITY(1,1) PRIMARY KEY,
  departmentId bigint NOT NULL FOREIGN KEY REFERENCES dbo.Department(departmentId),
  code   varchar(32) NOT NULL UNIQUE,
  title  nvarchar(200) NOT NULL,
  units  int NOT NULL CHECK (units BETWEEN 1 AND 6),
  description nvarchar(1000) NULL
);

IF OBJECT_ID('dbo.Prerequisite','U') IS NULL
CREATE TABLE dbo.Prerequisite(
  courseId bigint NOT NULL FOREIGN KEY REFERENCES dbo.Course(courseId) ON DELETE NO ACTION,
  prerequisiteCourseId bigint NOT NULL FOREIGN KEY REFERENCES dbo.Course(courseId) ON DELETE NO ACTION,
  CONSTRAINT PK_Prerequisite PRIMARY KEY(courseId, prerequisiteCourseId),
  CONSTRAINT CK_Prereq_NoSelf CHECK (courseId <> prerequisiteCourseId)
);

IF OBJECT_ID('dbo.CoRequisite','U') IS NULL
CREATE TABLE dbo.CoRequisite(
  courseId bigint NOT NULL FOREIGN KEY REFERENCES dbo.Course(courseId) ON DELETE NO ACTION,
  corequisiteCourseId bigint NOT NULL FOREIGN KEY REFERENCES dbo.Course(courseId) ON DELETE NO ACTION,
  CONSTRAINT PK_Corequisite PRIMARY KEY(courseId, corequisiteCourseId),
  CONSTRAINT CK_Coreq_NoSelf CHECK (courseId <> corequisiteCourseId)
);

IF OBJECT_ID('dbo.Room','U') IS NULL
CREATE TABLE dbo.Room(
  roomId bigint IDENTITY(1,1) PRIMARY KEY,
  code varchar(32) NOT NULL UNIQUE,
  capacity int NOT NULL CHECK (capacity BETWEEN 5 AND 300)
);

IF OBJECT_ID('dbo.TimeSlot','U') IS NULL
CREATE TABLE dbo.TimeSlot(
  timeSlotId bigint IDENTITY(1,1) PRIMARY KEY,
  dayOfWeek tinyint NOT NULL CHECK (dayOfWeek BETWEEN 1 AND 7), -- 1=Mon
  startTime time(0) NOT NULL,
  endTime   time(0) NOT NULL,
  CONSTRAINT CK_TimeSlot_Range CHECK (endTime > startTime)
);

-- 2) Offerings & schedules
IF OBJECT_ID('dbo.CourseOffering','U') IS NULL
CREATE TABLE dbo.CourseOffering(
  offeringId bigint IDENTITY(1,1) PRIMARY KEY,
  termId   bigint NOT NULL FOREIGN KEY REFERENCES dbo.Term(termId),
  courseId bigint NOT NULL FOREIGN KEY REFERENCES dbo.Course(courseId),
  section  varchar(8) NOT NULL,
  roomId   bigint NOT NULL FOREIGN KEY REFERENCES dbo.Room(roomId),
  capacity int NOT NULL CHECK (capacity BETWEEN 1 AND 500),
  waitlistCapacity int NOT NULL CHECK (waitlistCapacity BETWEEN 0 AND 200),
  rowversion rowversion NOT NULL,
  CONSTRAINT UQ_Offering UNIQUE(termId, courseId, section)
);

IF OBJECT_ID('dbo.OfferingSchedule','U') IS NULL
CREATE TABLE dbo.OfferingSchedule(
  offeringScheduleId bigint IDENTITY(1,1) PRIMARY KEY,
  offeringId bigint NOT NULL FOREIGN KEY REFERENCES dbo.CourseOffering(offeringId) ON DELETE NO ACTION,
  timeSlotId bigint NOT NULL FOREIGN KEY REFERENCES dbo.TimeSlot(timeSlotId),
  CONSTRAINT UQ_OfferingSchedule UNIQUE(offeringId, timeSlotId)
);

-- 3) Users & roles
IF OBJECT_ID('dbo.[User]','U') IS NULL
CREATE TABLE dbo.[User](
  userId bigint IDENTITY(1,1) PRIMARY KEY,
  username varchar(64) NOT NULL UNIQUE,
  email    varchar(256) NOT NULL UNIQUE,
  passwordHash varbinary(512) NULL,            -- if local identity enabled
  passwordHashFormat varchar(32) NULL,         -- e.g., 'PBKDF2v3'
  externalSubject varchar(256) NULL,           -- for external IdP subject
  role     varchar(32) NOT NULL CHECK (role IN ('Admin','Registrar','Instructor','Student')),
  createdAtUtc datetime2(0) NOT NULL CONSTRAINT DF_User_Created DEFAULT (SYSUTCDATETIME())
);

IF OBJECT_ID('dbo.Instructor','U') IS NULL
CREATE TABLE dbo.Instructor(
  instructorId bigint IDENTITY(1,1) PRIMARY KEY,
  userId bigint NOT NULL UNIQUE FOREIGN KEY REFERENCES dbo.[User](userId) ON DELETE NO ACTION
);

IF OBJECT_ID('dbo.Student','U') IS NULL
CREATE TABLE dbo.Student(
  studentId bigint IDENTITY(1,1) PRIMARY KEY,
  userId bigint NOT NULL UNIQUE FOREIGN KEY REFERENCES dbo.[User](userId) ON DELETE NO ACTION,
  studentNo varchar(32) NOT NULL UNIQUE,
  programId bigint NOT NULL FOREIGN KEY REFERENCES dbo.Program(programId)
);

IF OBJECT_ID('dbo.InstructorAssignment','U') IS NULL
CREATE TABLE dbo.InstructorAssignment(
  assignmentId bigint IDENTITY(1,1) PRIMARY KEY,
  offeringId bigint NOT NULL FOREIGN KEY REFERENCES dbo.CourseOffering(offeringId) ON DELETE NO ACTION,
  instructorId bigint NOT NULL FOREIGN KEY REFERENCES dbo.Instructor(instructorId),
  CONSTRAINT UQ_InstructorAssignment UNIQUE(offeringId, instructorId)
);

-- 4) Enrollment & audits
IF OBJECT_ID('dbo.Enrollment','U') IS NULL
CREATE TABLE dbo.Enrollment(
  enrollmentId bigint IDENTITY(1,1) PRIMARY KEY,
  offeringId bigint NOT NULL FOREIGN KEY REFERENCES dbo.CourseOffering(offeringId) ON DELETE NO ACTION,
  studentId  bigint NOT NULL FOREIGN KEY REFERENCES dbo.Student(studentId) ON DELETE NO ACTION,
  status varchar(16) NOT NULL CHECK (status IN ('Enrolled','Waitlisted','Dropped')),
  createdAtUtc datetime2(0) NOT NULL CONSTRAINT DF_Enroll_Created DEFAULT (SYSUTCDATETIME()),
  updatedAtUtc datetime2(0) NOT NULL CONSTRAINT DF_Enroll_Updated DEFAULT (SYSUTCDATETIME()),
  rowversion rowversion NOT NULL,
  CONSTRAINT UQ_Enrollment UNIQUE(offeringId, studentId)
);

IF OBJECT_ID('dbo.EnrollmentAudit','U') IS NULL
CREATE TABLE dbo.EnrollmentAudit(
  auditId bigint IDENTITY(1,1) PRIMARY KEY,
  enrollmentId bigint NOT NULL FOREIGN KEY REFERENCES dbo.Enrollment(enrollmentId) ON DELETE NO ACTION,
  action varchar(32) NOT NULL, -- Created, StatusChanged, Dropped, Promoted, etc.
  actorUserId bigint NULL FOREIGN KEY REFERENCES dbo.[User](userId),
  atUtc datetime2(0) NOT NULL CONSTRAINT DF_EnrollAudit_At DEFAULT (SYSUTCDATETIME()),
  details nvarchar(max) NULL,
  CONSTRAINT CK_EnrollAudit_Details_JSON CHECK (details IS NULL OR ISJSON(details)=1)
);

-- 5) Holidays / Blackouts (for scheduling rules)
IF OBJECT_ID('dbo.Holiday','U') IS NULL
CREATE TABLE dbo.Holiday(
  holidayId bigint IDENTITY(1,1) PRIMARY KEY,
  termId bigint NULL FOREIGN KEY REFERENCES dbo.Term(termId),
  date date NOT NULL,
  description nvarchar(200) NOT NULL
);

-- 6) Finance (feature-flagged in API)
IF OBJECT_ID('dbo.Invoice','U') IS NULL
CREATE TABLE dbo.Invoice(
  invoiceId bigint IDENTITY(1,1) PRIMARY KEY,
  studentId bigint NOT NULL FOREIGN KEY REFERENCES dbo.Student(studentId) ON DELETE NO ACTION,
  termId bigint NOT NULL FOREIGN KEY REFERENCES dbo.Term(termId),
  amount decimal(12,2) NOT NULL CHECK (amount >= 0),
  status varchar(16) NOT NULL CHECK (status IN ('Draft','Open','Paid','Refunded','Cancelled')),
  createdAtUtc datetime2(0) NOT NULL CONSTRAINT DF_Invoice_Created DEFAULT (SYSUTCDATETIME()),
  CONSTRAINT UQ_Invoice UNIQUE(studentId, termId)
);

IF OBJECT_ID('dbo.Payment','U') IS NULL
CREATE TABLE dbo.Payment(
  paymentId bigint IDENTITY(1,1) PRIMARY KEY,
  invoiceId bigint NOT NULL FOREIGN KEY REFERENCES dbo.Invoice(invoiceId) ON DELETE CASCADE,
  amount decimal(12,2) NOT NULL CHECK (amount > 0),
  paidAtUtc datetime2(0) NOT NULL,
  method varchar(24) NOT NULL CHECK (method IN ('Cash','Card','Online','Scholarship','Adjustment'))
);

-- 7) TVP for bulk schedule insert (used by API when creating/updating offerings)
IF TYPE_ID(N'dbo.OfferingScheduleTvp') IS NULL
CREATE TYPE dbo.OfferingScheduleTvp AS TABLE(
  timeSlotId bigint NOT NULL
);

-- 8) SESSION_CONTEXT helper for auditing (app sets actor once per connection/tx)
IF OBJECT_ID('dbo.sp_set_actor_context','P') IS NOT NULL DROP PROCEDURE dbo.sp_set_actor_context;
GO
CREATE PROCEDURE dbo.sp_set_actor_context
  @actorUserId bigint
AS
BEGIN
  SET NOCOUNT ON;
  EXEC sys.sp_set_session_context @key=N'actorUserId', @value=@actorUserId, @read_only=0;
END
GO

-- 9) Trigger to write audit rows on Enrollment changes
IF OBJECT_ID('dbo.TR_Enrollment_Audit','TR') IS NOT NULL
  DROP TRIGGER dbo.TR_Enrollment_Audit;
GO
CREATE TRIGGER dbo.TR_Enrollment_Audit
ON dbo.Enrollment
AFTER INSERT, UPDATE
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE @actorUserId bigint = TRY_CAST(SESSION_CONTEXT(N'actorUserId') AS bigint);

  -- Insert audit for new rows
  INSERT dbo.EnrollmentAudit(enrollmentId, action, actorUserId, details)
  SELECT i.enrollmentId, 'Created', @actorUserId,
         JSON_OBJECT('status':i.status, 'createdAtUtc':CONVERT(varchar(19), i.createdAtUtc, 126))
  FROM inserted i
  LEFT JOIN deleted d ON 1=0 -- ensure only new

  -- Insert audit for updates where status changed
  INSERT dbo.EnrollmentAudit(enrollmentId, action, actorUserId, details)
  SELECT i.enrollmentId, 'StatusChanged', @actorUserId,
         JSON_OBJECT('from':d.status, 'to':i.status, 'updatedAtUtc':CONVERT(varchar(19), i.updatedAtUtc, 126))
  FROM inserted i
  JOIN deleted d ON d.enrollmentId=i.enrollmentId
  WHERE ISNULL(d.status,'') <> ISNULL(i.status,'');
END
GO

IF OBJECT_ID('dbo.EmailOutbox','U') IS NULL
BEGIN
  CREATE TABLE dbo.EmailOutbox(
    outboxId           bigint IDENTITY(1,1) PRIMARY KEY,
    toEmail            nvarchar(256) NOT NULL,
    toName             nvarchar(128) NULL,
    subject            nvarchar(256) NOT NULL,
    bodyText           nvarchar(max) NULL,
    bodyHtml           nvarchar(max) NULL,
    meta               nvarchar(max) NULL, -- JSON (e.g., offeringId, studentId)
    createdAtUtc       datetime2 NOT NULL CONSTRAINT DF_EmailOutbox_Created DEFAULT (SYSUTCDATETIME()),
    status             varchar(32) NOT NULL CONSTRAINT DF_EmailOutbox_Status DEFAULT ('Pending'), -- Pending|Enqueued|Sent|Failed
    attemptCount       int NOT NULL CONSTRAINT DF_EmailOutbox_Attempts DEFAULT 0,
    lastError          nvarchar(1024) NULL,
    rowversion         rowversion
  );
  CREATE INDEX IX_EmailOutbox_Status_Created ON dbo.EmailOutbox(status, createdAtUtc);
END
GO

COMMIT;
