namespace UniEnroll.Infrastructure;

public static class SqlTemplates
{
    // Reports
    public const string EnrollmentByCourse =
        """
            SELECT t.code AS TermCode, c.code AS CourseCode, c.title AS Title,
                   COUNT(CASE WHEN e.status='Enrolled' THEN 1 END) AS EnrolledCount,
                   COUNT(CASE WHEN e.status='Waitlisted' THEN 1 END) AS WaitlistedCount
            FROM dbo.CourseOffering o
            JOIN dbo.Term t ON t.termId=o.termId
            JOIN dbo.Course c ON c.courseId=o.courseId
            LEFT JOIN dbo.Enrollment e ON e.offeringId=o.offeringId
            WHERE (@termId IS NULL OR o.termId=@termId)
            GROUP BY t.code, c.code, c.title
            ORDER BY t.code DESC, c.code;
        """;

    public const string InstructorLoad =
        """
            WITH counts AS (
              SELECT ia.instructorId, o.termId,
                     COUNT(DISTINCT ia.offeringId) AS OfferingCount,
                     SUM(CASE WHEN e.status='Enrolled' THEN 1 ELSE 0 END) AS EnrolledStudents
              FROM dbo.InstructorAssignment ia
              JOIN dbo.CourseOffering o ON o.offeringId=ia.offeringId
              LEFT JOIN dbo.Enrollment e ON e.offeringId=o.offeringId
              GROUP BY ia.instructorId, o.termId
            )
            SELECT t.code AS TermCode, i.instructorId, u.username AS InstructorUserName,
                   ISNULL(c.OfferingCount,0) AS OfferingCount, ISNULL(c.EnrolledStudents,0) AS EnrolledStudents
            FROM dbo.Instructor i
            JOIN dbo.[User] u ON u.userId=i.userId
            LEFT JOIN counts c ON c.instructorId=i.instructorId
            LEFT JOIN dbo.Term t ON t.termId=c.termId
            WHERE (@termId IS NULL OR c.termId=@termId)
            ORDER BY t.code DESC, u.username;
        """;

    public const string RoomUtilization =
        """
            WITH counts AS (
              SELECT o.roomId, o.termId, r.capacity,
                     SUM(CASE WHEN e.status='Enrolled' THEN 1 ELSE 0 END) AS Enrolled
              FROM dbo.CourseOffering o
              JOIN dbo.Room r ON r.roomId=o.roomId
              LEFT JOIN dbo.Enrollment e ON e.offeringId=o.offeringId
              WHERE (@termId IS NULL OR o.termId=@termId)
              GROUP BY o.roomId, o.termId, r.capacity
            )
            SELECT t.code AS TermCode, r.code AS RoomCode, c.capacity AS Capacity,
                   ISNULL(c.Enrolled,0) AS Enrolled,
                   CASE WHEN c.capacity>0 THEN (ISNULL(c.Enrolled,0)*100)/c.capacity ELSE 0 END AS UtilizationPercent
            FROM counts c
            JOIN dbo.Room r ON r.roomId=c.roomId
            JOIN dbo.Term t ON t.termId=c.termId
            ORDER BY UtilizationPercent DESC, RoomCode;
        """;

    // Finance
    public const string ListInvoicesPaged =
        """
            SELECT i.invoiceId AS InvoiceId, i.studentId AS StudentId, i.termId AS TermId, i.amount AS Amount, i.status AS Status,
                   CAST(i.createdAtUtc AS datetime2) AS CreatedAtUtc
            FROM dbo.Invoice i
            WHERE (@studentId IS NULL OR i.studentId=@studentId)
              AND (@termId IS NULL OR i.termId=@termId)
            ORDER BY i.invoiceId DESC
            OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;

            SELECT COUNT(*) FROM dbo.Invoice i
            WHERE (@studentId IS NULL OR i.studentId=@studentId)
              AND (@termId IS NULL OR i.termId=@termId);
        """;

    public const string CreateInvoice =
        """
            IF EXISTS (SELECT 1 FROM dbo.Invoice WHERE studentId=@StudentId AND termId=@TermId)
              THROW 51000, 'Invoice already exists for student/term.', 1;

            INSERT dbo.Invoice(studentId, termId, amount, status)
            VALUES(@StudentId, @TermId, @Amount, 'Open');
            SELECT SCOPE_IDENTITY();
        """;

    public const string AddPayment =
        """
        INSERT dbo.Payment(invoiceId, amount, paidAtUtc, method)
        VALUES(@InvoiceId, @Amount, @PaidAtUtc, @Method);
        """;

    public const string FinanceCsv =
        """
            SELECT i.invoiceId, i.studentId, i.termId, i.amount, i.status, i.createdAtUtc,
                   p.paymentId, p.amount AS paymentAmount, p.paidAtUtc, p.method
            FROM dbo.Invoice i
            LEFT JOIN dbo.Payment p ON p.invoiceId=i.invoiceId
            WHERE (@termId IS NULL OR i.termId=@termId)
            ORDER BY i.invoiceId, p.paymentId;
        """;

    //Terms
    public const string ListTerms =
        """
            SELECT termId AS TermId, code AS Code,
                    CAST(startDate AS date) AS StartDate,
                    CAST(endDate AS date) AS EndDate,
                    CAST(addDropDeadlineDate AS date) AS AddDropDeadlineDate
            FROM dbo.Term ORDER BY startDate DESC;
        """;

    //Enroll
    public const string ValidateStudent =
        """
            DECLARE @errors TABLE(msg nvarchar(400));
            IF NOT EXISTS (SELECT 1 FROM dbo.CourseOffering o JOIN dbo.Term t ON t.termId=o.termId
                           WHERE o.offeringId=@OfferingId AND SYSUTCDATETIME() <= DATEADD(day,1,t.addDropDeadlineDate))
               INSERT @errors VALUES (N'Add/drop window closed or offering not found.');

            IF EXISTS (SELECT 1 FROM dbo.Enrollment WHERE offeringId=@OfferingId AND studentId=@StudentId AND status IN ('Enrolled','Waitlisted'))
               INSERT @errors VALUES (N'Already enrolled or waitlisted for this offering.');

            IF EXISTS (
                SELECT 1
                FROM dbo.Prerequisite p
                JOIN dbo.CourseOffering o ON o.offeringId=@OfferingId AND o.courseId=p.courseId
                JOIN dbo.Enrollment e ON e.studentId=@StudentId
                JOIN dbo.CourseOffering po ON po.offeringId=e.offeringId AND po.courseId=p.prerequisiteCourseId
                JOIN dbo.Term toff ON toff.termId=o.termId
                JOIN dbo.Term tprev ON tprev.termId=po.termId
                WHERE e.status='Enrolled' AND tprev.endDate >= toff.startDate
            )
               INSERT @errors VALUES (N'Prerequisites not satisfied.');

            IF EXISTS (
              SELECT 1
              FROM dbo.Enrollment e
              JOIN dbo.OfferingSchedule eos ON eos.offeringId=e.offeringId
              JOIN dbo.TimeSlot ets ON ets.timeSlotId=eos.timeSlotId
              JOIN dbo.OfferingSchedule nos ON nos.offeringId=@OfferingId
              JOIN dbo.TimeSlot nts ON nts.timeSlotId=nos.timeSlotId
              WHERE e.studentId=@StudentId AND e.status='Enrolled'
                AND ets.dayOfWeek = nts.dayOfWeek
                AND ets.startTime < nts.endTime AND nts.startTime < ets.endTime
            )
               INSERT @errors VALUES (N'Schedule conflict with another enrolled class.');

            SELECT msg FROM @errors;
        """;

    public const string SeatSnapshotForUpdate =
        """
            SELECT o.capacity, o.waitlistCapacity,
                   (SELECT COUNT(*) FROM dbo.Enrollment WITH (UPDLOCK, ROWLOCK) WHERE offeringId=o.offeringId AND status='Enrolled') AS enrolled,
                   (SELECT COUNT(*) FROM dbo.Enrollment WITH (UPDLOCK, ROWLOCK) WHERE offeringId=o.offeringId AND status='Waitlisted') AS waitlisted
            FROM dbo.CourseOffering o WITH (UPDLOCK, ROWLOCK)
            WHERE o.offeringId=@OfferingId;
        """;

    public const string FirstWaitlisted =
        """
            SELECT TOP(1) e.enrollmentId
            FROM dbo.Enrollment e
            JOIN dbo.Enrollment e2 ON e2.enrollmentId=@Id
            WHERE e.offeringId=e2.offeringId AND e.status='Waitlisted'
            ORDER BY e.createdAtUtc ASC;
        """;
}
