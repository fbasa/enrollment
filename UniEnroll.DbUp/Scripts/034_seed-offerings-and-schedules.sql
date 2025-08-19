SET NOCOUNT ON;
BEGIN TRAN;

DECLARE @t1 bigint = (SELECT termId FROM dbo.Term WHERE code='AY25-26-S1');
DECLARE @t2 bigint = (SELECT termId FROM dbo.Term WHERE code='AY25-26-S2');

-- Create ~300 offerings across two terms
IF NOT EXISTS (SELECT 1 FROM dbo.CourseOffering)
BEGIN
  ;WITH C AS (
    SELECT courseId, ROW_NUMBER() OVER (ORDER BY courseId) AS rn FROM dbo.Course
  ),
  Sects AS (
    SELECT * FROM (VALUES('A'),('B'),('C'),('D')) s(section)
  ),
  Terms AS (
    SELECT termId FROM dbo.Term WHERE termId IN (@t1,@t2)
  ),
  Base AS (
    SELECT TOP (300)
      t.termId, c.courseId, s.section,
      (SELECT TOP 1 roomId FROM dbo.Room r ORDER BY NEWID()) AS roomId,
      CAST(25 + ABS(CHECKSUM(NEWID())) % 26 AS int) AS capacity,
      10 AS waitlistCapacity
    FROM Terms t
    CROSS JOIN C c
    CROSS JOIN Sects s
    ORDER BY NEWID()
  )
  INSERT dbo.CourseOffering(termId, courseId, section, roomId, capacity, waitlistCapacity)
  SELECT termId, courseId, section, roomId, capacity, waitlistCapacity
  FROM Base;
END

-- Assign 1–2 timeslots per offering
IF NOT EXISTS (SELECT 1 FROM dbo.OfferingSchedule)
BEGIN
  ;WITH O AS (
    SELECT offeringId FROM dbo.CourseOffering
  ),
  TS AS (
    SELECT TOP (2) timeSlotId FROM dbo.TimeSlot ORDER BY NEWID()
  )
  INSERT dbo.OfferingSchedule(offeringId, timeSlotId)
  SELECT o.offeringId, ts.timeSlotId
  FROM dbo.CourseOffering o
  CROSS APPLY (SELECT TOP (1 + ABS(CHECKSUM(NEWID())) % 2) timeSlotId FROM dbo.TimeSlot ORDER BY NEWID()) ts;
END

COMMIT;
