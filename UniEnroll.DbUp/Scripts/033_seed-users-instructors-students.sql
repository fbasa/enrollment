SET NOCOUNT ON;
BEGIN TRAN;

-- Admin & Registrar demo users (externalSubject only; local dev login will mint JWTs)
IF NOT EXISTS (SELECT 1 FROM dbo.[User] WHERE username='admin')
  INSERT dbo.[User](username,email,externalSubject,role) VALUES ('admin','admin@demo.local','ext:admin','Admin');
IF NOT EXISTS (SELECT 1 FROM dbo.[User] WHERE username='registrar')
  INSERT dbo.[User](username,email,externalSubject,role) VALUES ('registrar','registrar@demo.local','ext:registrar','Registrar');

-- Instructors (50)
IF NOT EXISTS (SELECT 1 FROM dbo.Instructor)
BEGIN
  SET NOCOUNT ON;

  -- Capture userIds created for instructors
  DECLARE @U1 TABLE (userId bigint NOT NULL);

  ;WITH N AS (
    SELECT TOP (50) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects
  )
  INSERT dbo.[User] (username, email, externalSubject, role)
  OUTPUT INSERTED.userId INTO @U1(userId)
  SELECT CONCAT('instructor', RIGHT('000'+CAST(n AS varchar(3)),3)),
         CONCAT('instructor', RIGHT('000'+CAST(n AS varchar(3)),3), '@demo.local'),
         CONCAT('ext:instructor:', n),
         'Instructor'
  FROM N;

  INSERT dbo.Instructor(userId)
  SELECT userId FROM @U1;
END


-- Students (1000) spread across programs
IF NOT EXISTS (SELECT 1 FROM dbo.Student)
BEGIN
  SET NOCOUNT ON;

  DECLARE @pCount int = (SELECT COUNT(*) FROM dbo.Program);

  -- Capture userId + username for mapping to Student rows
  DECLARE @U2 TABLE (userId bigint NOT NULL, username varchar(64) NOT NULL);

  ;WITH N AS (
    SELECT TOP (1000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects a CROSS JOIN sys.all_objects b
  )
  INSERT dbo.[User] (username, email, externalSubject, role)
  OUTPUT INSERTED.userId, INSERTED.username INTO @U2(userId, username)
  SELECT CONCAT('student', RIGHT(REPLICATE('0',4)+CAST(n AS varchar(4)),4)),
         CONCAT('student', RIGHT(REPLICATE('0',4)+CAST(n AS varchar(4)),4), '@demo.local'),
         CONCAT('ext:student:', n),
         'Student'
  FROM N;

  INSERT dbo.Student(userId, studentNo, programId)
  SELECT u.userId,
         CONCAT('S', RIGHT(REPLICATE('0',7)+CAST(TRY_CAST(RIGHT(u.username,4) AS int) AS varchar(7)),7)),
         (SELECT TOP 1 programId FROM (
            SELECT programId, ROW_NUMBER() OVER (ORDER BY programId) AS rn FROM dbo.Program
          ) p
          WHERE p.rn = ((TRY_CAST(RIGHT(u.username,4) AS int)-1) % @pCount) + 1)
  FROM @U2 u;
END


COMMIT;
