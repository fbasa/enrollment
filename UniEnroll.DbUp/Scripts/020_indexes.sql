BEGIN TRAN;

-- Covering indexes for common filters/sorts
CREATE INDEX IX_Program_Department ON dbo.Program(departmentId);

CREATE INDEX IX_Course_Department_Code ON dbo.Course(departmentId, code)
  INCLUDE(title, units);

CREATE INDEX IX_Offering_TermCourseSection ON dbo.CourseOffering(termId, courseId, section);
CREATE INDEX IX_Offering_TermCourse ON dbo.CourseOffering(termId, courseId)
  INCLUDE(roomId, capacity, waitlistCapacity);

CREATE INDEX IX_OfferingSchedule_Offering ON dbo.OfferingSchedule(offeringId, timeSlotId);

CREATE INDEX IX_InstructorAssignment_Offering ON dbo.InstructorAssignment(offeringId, instructorId);

CREATE INDEX IX_Enrollment_Student_Term_Status ON dbo.Enrollment(studentId, status, offeringId)
  INCLUDE(createdAtUtc, updatedAtUtc);

-- For student schedule queries (join offering->term fast)
CREATE INDEX IX_Enrollment_Offering ON dbo.Enrollment(offeringId);

-- Reports: enrollment by course/department/term
CREATE INDEX IX_Offering_Term ON dbo.CourseOffering(termId);

-- Finance
CREATE INDEX IX_Invoice_Student_Term ON dbo.Invoice(studentId, termId) INCLUDE(status, amount);
CREATE INDEX IX_Payment_Invoice ON dbo.Payment(invoiceId);

COMMIT;
