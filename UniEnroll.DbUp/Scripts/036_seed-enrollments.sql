SET NOCOUNT ON;
BEGIN TRAN;

-- Populate enrollments to create realistic load & some waitlists
IF NOT EXISTS (SELECT 1 FROM dbo.Enrollment)
BEGIN
  ;WITH O AS (
    SELECT offeringId, capacity, waitlistCapacity FROM dbo.CourseOffering
  ),
  TargetCounts AS (
    -- aim for capacity -5 .. capacity + min(5, waitlistCapacity)
    SELECT offeringId,
           capacity,
           waitlistCapacity,
           capacity + (ABS(CHECKSUM(NEWID())) % (CASE WHEN waitlistCapacity < 5 THEN waitlistCapacity+1 ELSE 6 END)) - 2 AS targetCount
    FROM O
  ),
  S AS (
    SELECT studentId, ROW_NUMBER() OVER (ORDER BY studentId) AS rn FROM dbo.Student
  ),
  Pick AS (
    SELECT tc.offeringId, s.studentId,
           ROW_NUMBER() OVER (PARTITION BY tc.offeringId ORDER BY NEWID()) AS ord,
           tc.capacity, tc.waitlistCapacity, tc.targetCount
    FROM TargetCounts tc
    JOIN S ON 1=1
  )
  INSERT dbo.Enrollment(offeringId, studentId, status)
  SELECT p.offeringId, p.studentId,
         CASE WHEN p.ord <= p.capacity THEN 'Enrolled'
              WHEN p.ord <= p.targetCount THEN 'Waitlisted'
              ELSE 'Dropped' END
  FROM Pick p
  WHERE p.ord <= p.targetCount;
END

COMMIT;
