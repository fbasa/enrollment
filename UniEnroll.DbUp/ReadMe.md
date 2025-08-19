How to run (now) — quick check

From /db/UniEnroll.DbUp:

# PowerShell
$env:DB_CONNECTION="Server=LAPTOP-T2AEOQQQ;Database=UniEnroll;Integrated Security=True;TrustServerCertificate=True;"
dotnet run


You should end up with:

~30+ courses, 50 instructors, 1000 students, ~300 offerings

Mixed Enrolled/Waitlisted populations

Demo invoices/payments for reporting

Triggered EnrollmentAudit rows