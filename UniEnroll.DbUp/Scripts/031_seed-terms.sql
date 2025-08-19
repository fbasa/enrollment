DECLARE @y int = YEAR(SYSUTCDATETIME());
-- Assume academic year 2025-2026 style; tune as needed
IF NOT EXISTS (SELECT 1 FROM dbo.Term WHERE code='AY25-26-S1')
BEGIN
  INSERT dbo.Term(code,startDate,endDate,addDropDeadlineDate)
  VALUES ('AY25-26-S1','2025-08-04','2025-12-15','2025-08-18');
END
IF NOT EXISTS (SELECT 1 FROM dbo.Term WHERE code='AY25-26-S2')
BEGIN
  INSERT dbo.Term(code,startDate,endDate,addDropDeadlineDate)
  VALUES ('AY25-26-S2','2026-01-06','2026-05-15','2026-01-20');
END

-- Holidays examples
IF NOT EXISTS (SELECT 1 FROM dbo.Holiday)
BEGIN
  DECLARE @t1 bigint = (SELECT termId FROM dbo.Term WHERE code='AY25-26-S1');
  INSERT dbo.Holiday(termId, [date], description)
  VALUES (@t1, '2025-11-01','All Saints'' Day'), (@t1,'2025-11-30','Bonifacio Day');
END
