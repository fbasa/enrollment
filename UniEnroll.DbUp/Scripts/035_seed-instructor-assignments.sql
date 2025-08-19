SET NOCOUNT ON;
BEGIN TRAN;

-- Each offering gets one primary instructor
IF NOT EXISTS (SELECT 1 FROM dbo.InstructorAssignment)
BEGIN
  INSERT dbo.InstructorAssignment(offeringId, instructorId)
  SELECT o.offeringId,
         (SELECT TOP 1 instructorId FROM dbo.Instructor ORDER BY NEWID())
  FROM dbo.CourseOffering o;
END

COMMIT;
