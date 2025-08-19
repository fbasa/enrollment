-- Minimal finance demo: invoices for current term, partial payments
SET NOCOUNT ON;
BEGIN TRAN;

DECLARE @term bigint = (SELECT TOP 1 termId FROM dbo.Term ORDER BY startDate DESC);

IF NOT EXISTS (SELECT 1 FROM dbo.Invoice WHERE termId=@term)
BEGIN
  -- Pick first 200 students deterministically
  DECLARE @S TABLE (studentId bigint NOT NULL);
  INSERT INTO @S(studentId)
  SELECT TOP (200) studentId FROM dbo.Student ORDER BY studentId;

  -- Capture inserted invoices (id + amount) for follow-up Payment rows
  DECLARE @I TABLE (invoiceId bigint NOT NULL, amount decimal(12,2) NOT NULL);

  INSERT dbo.Invoice(studentId, termId, amount, status)
  OUTPUT INSERTED.invoiceId, INSERTED.amount INTO @I(invoiceId, amount)
  SELECT s.studentId,
         @term,
         CAST(10000 + (ABS(CHECKSUM(NEWID())) % 10) * 500 AS decimal(12,2)),
         'Open'
  FROM @S s;

  INSERT dbo.Payment(invoiceId, amount, paidAtUtc, method)
  SELECT i.invoiceId,
         i.amount * 0.5,
         SYSUTCDATETIME(),
         'Online'
  FROM @I i;
END

COMMIT;
