-- Handy views for reporting/teaching (indexable if needed)
IF OBJECT_ID('dbo.vw_EnrollmentByCourse','V') IS NOT NULL DROP VIEW dbo.vw_EnrollmentByCourse;
GO
CREATE VIEW dbo.vw_EnrollmentByCourse AS
SELECT t.code AS termCode, c.code AS courseCode, c.title,
       COUNT(CASE WHEN e.status='Enrolled' THEN 1 END) AS enrolledCount,
       COUNT(CASE WHEN e.status='Waitlisted' THEN 1 END) AS waitlistedCount
FROM dbo.CourseOffering o
JOIN dbo.Term t ON t.termId=o.termId
JOIN dbo.Course c ON c.courseId=o.courseId
LEFT JOIN dbo.Enrollment e ON e.offeringId=o.offeringId
GROUP BY t.code, c.code, c.title;
GO
