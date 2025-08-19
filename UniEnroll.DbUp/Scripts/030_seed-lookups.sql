SET NOCOUNT ON;
BEGIN TRAN;

-- Departments
IF NOT EXISTS (SELECT 1 FROM dbo.Department)
BEGIN
  INSERT dbo.Department(code, name) VALUES
  ('CS','Computer Science'),
  ('MATH','Mathematics'),
  ('BUS','Business Administration');
END

-- Programs
IF NOT EXISTS (SELECT 1 FROM dbo.Program)
BEGIN
  INSERT dbo.Program(departmentId, code, name)
  SELECT d.departmentId, v.code, v.name
  FROM dbo.Department d
  JOIN (VALUES
    ('CS','BSCS','B.S. Computer Science'),
    ('CS','BSIT','B.S. Information Technology'),
    ('MATH','BSMATH','B.S. Mathematics'),
    ('BUS','BSBA-MKT','B.S.B.A. Marketing'),
    ('BUS','BSBA-FIN','B.S.B.A. Finance')
  ) v(dCode, code, name) ON v.dCode=d.code;
END

-- Time slots (Mon–Sat; 8 blocks)
IF NOT EXISTS (SELECT 1 FROM dbo.TimeSlot)
BEGIN
  ;WITH D AS (
    SELECT v AS dayOfWeek FROM (VALUES(1),(2),(3),(4),(5),(6)) x(v) -- Mon..Sat
  ),
  S AS (
    SELECT * FROM (VALUES
      ('08:00','09:30'), ('09:30','11:00'), ('11:00','12:30'),
      ('13:00','14:30'), ('14:30','16:00'), ('16:00','17:30'),
      ('17:30','19:00'), ('19:00','20:30')
    ) t(startTime,endTime)
  )
  INSERT dbo.TimeSlot(dayOfWeek, startTime, endTime)
  SELECT d.dayOfWeek, CAST(s.startTime AS time), CAST(s.endTime AS time)
  FROM D d CROSS JOIN S s;
END

-- Rooms
IF NOT EXISTS (SELECT 1 FROM dbo.Room)
BEGIN
  INSERT dbo.Room(code, capacity) VALUES
  ('B101', 35), ('B102', 40), ('B201', 30), ('B202', 45), ('LAB-1', 28),
  ('LAB-2', 28), ('AUD-A', 120), ('C101', 35), ('C102', 35), ('C201', 40);
END

COMMIT;
