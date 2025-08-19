-- 30+ courses across departments with prereq chains
IF NOT EXISTS (SELECT 1 FROM dbo.Course)
BEGIN
  DECLARE @cs bigint = (SELECT departmentId FROM dbo.Department WHERE code='CS');
  DECLARE @ma bigint = (SELECT departmentId FROM dbo.Department WHERE code='MATH');
  DECLARE @bu bigint = (SELECT departmentId FROM dbo.Department WHERE code='BUS');

  INSERT dbo.Course(departmentId, code, title, units, description) VALUES
   (@cs,'CS101','Intro to Programming',3,'Basics of programming using TypeScript/C#'),
   (@cs,'CS102','Data Structures',3,'Lists, trees, graphs; complexity'),
   (@cs,'CS201','Algorithms',3,'Design & analysis of algorithms'),
   (@cs,'CS202','Databases',3,'Relational design and SQL'),
   (@cs,'CS203','Computer Architecture',3,'CPU, memory, ISA'),
   (@cs,'CS204','Operating Systems',3,'Processes, threads, scheduling'),
   (@cs,'CS205','Computer Networks',3,'TCP/IP, routing, congestion'),
   (@cs,'CS206','Web Development',3,'Front-end and API basics'),
   (@cs,'CS207','Software Engineering',3,'Reqs, design, testing'),
   (@cs,'CS301','Machine Learning',3,'Supervised & unsupervised'),
   (@cs,'CS302','Distributed Systems',3,'Consensus, scaling'),
   (@cs,'CS303','Information Security',3,'CIA triad, crypto'),
   (@ma,'MATH101','Calculus I',4,'Limits, derivatives, integrals'),
   (@ma,'MATH102','Calculus II',4,'Series, multivariable'),
   (@ma,'MATH201','Discrete Math',3,'Logic, sets, combinatorics'),
   (@ma,'MATH202','Linear Algebra',3,'Vectors, matrices'),
   (@ma,'MATH203','Probability',3,'Random variables, distributions'),
   (@bu,'BUS101','Principles of Management',3,'Intro to management'),
   (@bu,'BUS102','Accounting Fundamentals',3,'Accounting cycle'),
   (@bu,'BUS201','Marketing',3,'4Ps, segmentation'),
   (@bu,'BUS202','Finance',3,'Time value of money'),
   (@bu,'BUS203','Business Analytics',3,'Descriptive & predictive'),
   (@bu,'BUS204','Operations Management',3,'Processes, quality'),
   (@bu,'BUS205','Business Law',3,'Legal environment'),
   (@cs,'CS208','Human-Computer Interaction',3,'UX basics'),
   (@cs,'CS209','Mobile App Dev',3,'Native & cross-platform'),
   (@cs,'CS210','Cloud Computing',3,'Azure fundamentals'),
   (@cs,'CS211','Parallel Programming',3,'GPUs, multithreading'),
   (@cs,'CS212','Compiler Design',3,'Parsing, codegen'),
   (@cs,'CS213','Data Mining',3,'Patterns & pipelines'),
   (@cs,'CS214','Big Data Systems',3,'Hadoop, Spark');

  -- prereqs
  INSERT dbo.Prerequisite(courseId, prerequisiteCourseId)
  SELECT (SELECT courseId FROM dbo.Course WHERE code='CS102'),
         (SELECT courseId FROM dbo.Course WHERE code='CS101')
  UNION ALL
  SELECT (SELECT courseId FROM dbo.Course WHERE code='CS201'),
         (SELECT courseId FROM dbo.Course WHERE code='CS102')
  UNION ALL
  SELECT (SELECT courseId FROM dbo.Course WHERE code='CS202'),
         (SELECT courseId FROM dbo.Course WHERE code='CS101')
  UNION ALL
  SELECT (SELECT courseId FROM dbo.Course WHERE code='CS301'),
         (SELECT courseId FROM dbo.Course WHERE code='MATH202')
  UNION ALL
  SELECT (SELECT courseId FROM dbo.Course WHERE code='CS301'),
         (SELECT courseId FROM dbo.Course WHERE code='MATH203')
  UNION ALL
  SELECT (SELECT courseId FROM dbo.Course WHERE code='CS302'),
         (SELECT courseId FROM dbo.Course WHERE code='CS201')
  UNION ALL
  SELECT (SELECT courseId FROM dbo.Course WHERE code='CS204'),
         (SELECT courseId FROM dbo.Course WHERE code='CS203')
  UNION ALL
  SELECT (SELECT courseId FROM dbo.Course WHERE code='CS205'),
         (SELECT courseId FROM dbo.Course WHERE code='CS204');

  -- coreqs (example: CS206 requires MATH201 concurrently)
  INSERT dbo.CoRequisite(courseId, corequisiteCourseId)
  SELECT (SELECT courseId FROM dbo.Course WHERE code='CS206'),
         (SELECT courseId FROM dbo.Course WHERE code='MATH201');
END
