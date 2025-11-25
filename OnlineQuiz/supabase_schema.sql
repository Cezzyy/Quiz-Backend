-- ============================================
-- Online Quiz Mobile - Supabase Database Schema
-- ============================================
-- This script creates all tables for the Online Quiz Mobile application
-- Execute this in Supabase SQL Editor
-- ============================================
-- 
-- ACCESS CONTROL WORKFLOW:
-- 1. ADMIN creates all user accounts (Admin, Teacher, Student)
-- 2. ADMIN creates courses and assigns them to Teachers
-- 3. TEACHERS manage their assigned courses and create quizzes
-- 4. TEACHERS assign students to their courses for quiz access
-- 5. STUDENTS can only access quizzes in courses they're enrolled in
-- ============================================

-- Enable necessary extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================
-- 1. USERS TABLE
-- ============================================
-- All user accounts (Admin, Teacher, Student)
-- Only ADMIN can create accounts
CREATE TABLE IF NOT EXISTS "User" (
    "UserId" SERIAL PRIMARY KEY,
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(255) NOT NULL,
    "FullName" VARCHAR(255) NOT NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Active' CHECK ("Status" IN ('Active', 'Inactive')),
    "ContactNumber" VARCHAR(50) DEFAULT '',
    "EmergencyContactNumber" VARCHAR(50) DEFAULT '',
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy" INTEGER REFERENCES "User"("UserId") ON DELETE SET NULL
);

CREATE INDEX idx_user_email ON "User"("Email");
CREATE INDEX idx_user_status ON "User"("Status");

-- ============================================
-- 2. ROLE TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS "Role" (
    "RoleId" SERIAL PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL UNIQUE
);

-- Insert predefined roles
INSERT INTO "Role" ("RoleId", "Name") VALUES
    (1, 'Admin'),
    (2, 'Teacher'),
    (3, 'Student')
ON CONFLICT ("RoleId") DO NOTHING;

-- ============================================
-- 3. USER_ROLE TABLE (Junction)
-- ============================================
CREATE TABLE IF NOT EXISTS "UserRole" (
    "UserId" INTEGER NOT NULL,
    "RoleId" INTEGER NOT NULL,
    PRIMARY KEY ("UserId", "RoleId"),
    FOREIGN KEY ("UserId") REFERENCES "User"("UserId") ON DELETE CASCADE,
    FOREIGN KEY ("RoleId") REFERENCES "Role"("RoleId") ON DELETE CASCADE
);

CREATE INDEX idx_userrole_userid ON "UserRole"("UserId");
CREATE INDEX idx_userrole_roleid ON "UserRole"("RoleId");

-- ============================================
-- 4. STUDENT TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS "Student" (
    "UserId" INTEGER PRIMARY KEY,
    "StudentId" VARCHAR(50) NOT NULL UNIQUE,
    "Year_Level" INTEGER,
    "Section" VARCHAR(100),
    "Course" VARCHAR(255),
    FOREIGN KEY ("UserId") REFERENCES "User"("UserId") ON DELETE CASCADE
);

CREATE INDEX idx_student_studentid ON "Student"("StudentId");
CREATE INDEX idx_student_yearlevel ON "Student"("Year_Level");
CREATE INDEX idx_student_section ON "Student"("Section");

-- ============================================
-- 5. TEACHER TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS "Teacher" (
    "UserId" INTEGER PRIMARY KEY,
    "Department" VARCHAR(255),
    FOREIGN KEY ("UserId") REFERENCES "User"("UserId") ON DELETE CASCADE
);

CREATE INDEX idx_teacher_department ON "Teacher"("Department");

-- ============================================
-- 6. COURSE TABLE
-- ============================================
-- Courses created by ADMIN and assigned to Teachers
-- Only ADMIN can create and assign courses
CREATE TABLE IF NOT EXISTS "Course" (
    "CourseId" SERIAL PRIMARY KEY,
    "Code" VARCHAR(50) NOT NULL,
    "Name" VARCHAR(255) NOT NULL,
    "Instructor_UserId" INTEGER NOT NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Active' CHECK ("Status" IN ('Active', 'Inactive', 'Archived')),
    "Category" VARCHAR(100),
    "Section" VARCHAR(100),
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy" INTEGER NOT NULL,
    FOREIGN KEY ("Instructor_UserId") REFERENCES "Teacher"("UserId") ON DELETE RESTRICT,
    FOREIGN KEY ("CreatedBy") REFERENCES "User"("UserId") ON DELETE RESTRICT
);

CREATE INDEX idx_course_code ON "Course"("Code");
CREATE INDEX idx_course_instructor ON "Course"("Instructor_UserId");
CREATE INDEX idx_course_status ON "Course"("Status");
CREATE INDEX idx_course_section ON "Course"("Section");

-- ============================================
-- 7. ENROLLMENT TABLE
-- ============================================
-- Students enrolled in courses by Teachers
-- Teachers assign students to their courses for quiz access
CREATE TABLE IF NOT EXISTS "Enrollment" (
    "EnrollmentId" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "CourseId" INTEGER NOT NULL,
    "EnrolledAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "Section" VARCHAR(100),
    "EnrolledBy" INTEGER NOT NULL,
    FOREIGN KEY ("UserId") REFERENCES "Student"("UserId") ON DELETE CASCADE,
    FOREIGN KEY ("CourseId") REFERENCES "Course"("CourseId") ON DELETE CASCADE,
    FOREIGN KEY ("EnrolledBy") REFERENCES "User"("UserId") ON DELETE RESTRICT,
    UNIQUE ("UserId", "CourseId")
);

CREATE INDEX idx_enrollment_userid ON "Enrollment"("UserId");
CREATE INDEX idx_enrollment_courseid ON "Enrollment"("CourseId");
CREATE INDEX idx_enrollment_enrolledat ON "Enrollment"("EnrolledAt");
CREATE INDEX idx_enrollment_enrolledby ON "Enrollment"("EnrolledBy");

-- ============================================
-- 8. QUIZ TABLE
-- ============================================
-- Quizzes created by Teachers for their assigned courses
CREATE TABLE IF NOT EXISTS "Quiz" (
    "QuizId" SERIAL PRIMARY KEY,
    "CourseId" INTEGER NOT NULL,
    "Title" VARCHAR(255) NOT NULL,
    "Due_At" TIMESTAMPTZ,
    "Time_Limit_Minutes" INTEGER,
    "Is_Published" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy" INTEGER NOT NULL,
    FOREIGN KEY ("CourseId") REFERENCES "Course"("CourseId") ON DELETE CASCADE,
    FOREIGN KEY ("CreatedBy") REFERENCES "Teacher"("UserId") ON DELETE RESTRICT
);

CREATE INDEX idx_quiz_courseid ON "Quiz"("CourseId");
CREATE INDEX idx_quiz_published ON "Quiz"("Is_Published");
CREATE INDEX idx_quiz_dueat ON "Quiz"("Due_At");
CREATE INDEX idx_quiz_createdby ON "Quiz"("CreatedBy");

-- ============================================
-- 9. QUESTION TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS "Question" (
    "QuestionId" SERIAL PRIMARY KEY,
    "QuizId" INTEGER NOT NULL,
    "Type" VARCHAR(50) NOT NULL CHECK ("Type" IN ('Single', 'Multiple', 'Text')),
    "Body" TEXT NOT NULL,
    "Points" DECIMAL(10, 2) NOT NULL DEFAULT 1.0,
    "Sort_Order" INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY ("QuizId") REFERENCES "Quiz"("QuizId") ON DELETE CASCADE
);

CREATE INDEX idx_question_quizid ON "Question"("QuizId");
CREATE INDEX idx_question_sortorder ON "Question"("Sort_Order");
CREATE INDEX idx_question_type ON "Question"("Type");

-- ============================================
-- 10. CHOICE TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS "Choice" (
    "ChoiceId" SERIAL PRIMARY KEY,
    "QuestionId" INTEGER NOT NULL,
    "Body" TEXT NOT NULL,
    "Is_Correct" BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY ("QuestionId") REFERENCES "Question"("QuestionId") ON DELETE CASCADE
);

CREATE INDEX idx_choice_questionid ON "Choice"("QuestionId");
CREATE INDEX idx_choice_iscorrect ON "Choice"("Is_Correct");

-- ============================================
-- 11. ATTEMPT TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS "Attempt" (
    "AttemptId" SERIAL PRIMARY KEY,
    "QuizId" INTEGER NOT NULL,
    "UserId" INTEGER NOT NULL,
    "StartedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "SubmittedAt" TIMESTAMPTZ,
    "Score" DECIMAL(10, 2) NOT NULL DEFAULT 0.0,
    "Time_Spent_Seconds" INTEGER,
    FOREIGN KEY ("QuizId") REFERENCES "Quiz"("QuizId") ON DELETE CASCADE,
    FOREIGN KEY ("UserId") REFERENCES "Student"("UserId") ON DELETE CASCADE
);

CREATE INDEX idx_attempt_quizid ON "Attempt"("QuizId");
CREATE INDEX idx_attempt_userid ON "Attempt"("UserId");
CREATE INDEX idx_attempt_startedat ON "Attempt"("StartedAt");
CREATE INDEX idx_attempt_submittedat ON "Attempt"("SubmittedAt");

-- ============================================
-- 12. ATTEMPT_ANSWER TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS "AttemptAnswer" (
    "AttemptAnswerId" SERIAL PRIMARY KEY,
    "AttemptId" INTEGER NOT NULL,
    "QuestionId" INTEGER NOT NULL,
    "ChoiceId" INTEGER,
    "Free_Text" TEXT,
    "Is_Correct" BOOLEAN,
    FOREIGN KEY ("AttemptId") REFERENCES "Attempt"("AttemptId") ON DELETE CASCADE,
    FOREIGN KEY ("QuestionId") REFERENCES "Question"("QuestionId") ON DELETE CASCADE,
    FOREIGN KEY ("ChoiceId") REFERENCES "Choice"("ChoiceId") ON DELETE CASCADE,
    CONSTRAINT chk_answer_type CHECK (
        ("ChoiceId" IS NOT NULL AND "Free_Text" IS NULL) OR
        ("ChoiceId" IS NULL AND "Free_Text" IS NOT NULL) OR
        ("ChoiceId" IS NULL AND "Free_Text" IS NULL)
    )
);

CREATE INDEX idx_attemptanswer_attemptid ON "AttemptAnswer"("AttemptId");
CREATE INDEX idx_attemptanswer_questionid ON "AttemptAnswer"("QuestionId");
CREATE INDEX idx_attemptanswer_choiceid ON "AttemptAnswer"("ChoiceId");

-- ============================================
-- 13. NOTIFICATION TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS "Notification" (
    "NotificationId" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Type" VARCHAR(50) NOT NULL CHECK ("Type" IN ('Quiz', 'Course', 'System', 'Reminder')),
    "Title" VARCHAR(255) NOT NULL,
    "Message" TEXT NOT NULL,
    "Is_Read" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    FOREIGN KEY ("UserId") REFERENCES "User"("UserId") ON DELETE CASCADE
);

CREATE INDEX idx_notification_userid ON "Notification"("UserId");
CREATE INDEX idx_notification_isread ON "Notification"("Is_Read");
CREATE INDEX idx_notification_createdat ON "Notification"("CreatedAt");
CREATE INDEX idx_notification_type ON "Notification"("Type");

-- ============================================
-- 14. EXPORT_IMPORT_LOG TABLE
-- ============================================
CREATE TABLE IF NOT EXISTS "ExportImportLog" (
    "LogId" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Type" VARCHAR(50) NOT NULL CHECK ("Type" IN ('Export', 'Import')),
    "FileName" VARCHAR(255) NOT NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending' CHECK ("Status" IN ('Pending', 'In Progress', 'Completed', 'Failed')),
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CompletedAt" TIMESTAMPTZ,
    "ErrorMessage" TEXT,
    FOREIGN KEY ("UserId") REFERENCES "User"("UserId") ON DELETE CASCADE
);

CREATE INDEX idx_exportimportlog_userid ON "ExportImportLog"("UserId");
CREATE INDEX idx_exportimportlog_type ON "ExportImportLog"("Type");
CREATE INDEX idx_exportimportlog_status ON "ExportImportLog"("Status");
CREATE INDEX idx_exportimportlog_createdat ON "ExportImportLog"("CreatedAt");

-- ============================================
-- TRIGGERS FOR AUTOMATIC TIMESTAMP UPDATES
-- ============================================

-- Function to update UpdatedAt timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply trigger to User table
CREATE TRIGGER update_user_updated_at BEFORE UPDATE ON "User"
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Apply trigger to Course table
CREATE TRIGGER update_course_updated_at BEFORE UPDATE ON "Course"
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Apply trigger to Quiz table
CREATE TRIGGER update_quiz_updated_at BEFORE UPDATE ON "Quiz"
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- ROW LEVEL SECURITY (RLS) - Enable for Supabase
-- ============================================

-- Enable RLS on all tables
ALTER TABLE "User" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Role" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "UserRole" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Student" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Teacher" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Course" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Enrollment" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Quiz" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Question" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Choice" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Attempt" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "AttemptAnswer" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Notification" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "ExportImportLog" ENABLE ROW LEVEL SECURITY;

-- ============================================
-- HELPER FUNCTIONS FOR RLS
-- ============================================

-- Function to check if user is an Admin
CREATE OR REPLACE FUNCTION is_admin(user_id INTEGER)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 FROM "UserRole" 
        WHERE "UserId" = user_id AND "RoleId" = 1
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to check if user is a Teacher
CREATE OR REPLACE FUNCTION is_teacher(user_id INTEGER)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 FROM "UserRole" 
        WHERE "UserId" = user_id AND "RoleId" = 2
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to check if user is a Student
CREATE OR REPLACE FUNCTION is_student(user_id INTEGER)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 FROM "UserRole" 
        WHERE "UserId" = user_id AND "RoleId" = 3
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to check if teacher is assigned to course
CREATE OR REPLACE FUNCTION teacher_owns_course(teacher_id INTEGER, course_id INTEGER)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 FROM "Course" 
        WHERE "CourseId" = course_id AND "Instructor_UserId" = teacher_id
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to check if student is enrolled in course
CREATE OR REPLACE FUNCTION student_enrolled_in_course(student_id INTEGER, course_id INTEGER)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 FROM "Enrollment" 
        WHERE "UserId" = student_id AND "CourseId" = course_id
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================
-- RLS POLICIES - ADMIN, TEACHER, STUDENT HIERARCHY
-- ============================================
-- Note: These policies enforce the access control workflow
-- ADMIN → creates accounts and courses, assigns teachers
-- TEACHER → manages assigned courses, creates quizzes, enrolls students
-- STUDENT → accesses enrolled courses and takes quizzes
-- ============================================

-- Role table: Everyone can read roles
CREATE POLICY "Anyone can read roles" ON "Role"
    FOR SELECT USING (true);

-- ============================================
-- USER TABLE POLICIES
-- ============================================

-- Admin can manage all users
CREATE POLICY "Admin can read all users" ON "User"
    FOR SELECT USING (is_admin("UserId"));

CREATE POLICY "Admin can create users" ON "User"
    FOR INSERT WITH CHECK (is_admin("CreatedBy"));

CREATE POLICY "Admin can update users" ON "User"
    FOR UPDATE USING (is_admin((SELECT "CreatedBy" FROM "User" WHERE "UserId" = "User"."UserId")));

-- Users can read their own data
CREATE POLICY "Users can read own data" ON "User"
    FOR SELECT USING ("UserId" = (SELECT "UserId" FROM "User" WHERE "UserId" = current_setting('app.current_user_id')::INTEGER));

-- ============================================
-- COURSE TABLE POLICIES
-- ============================================

-- Admin can manage all courses
CREATE POLICY "Admin can create courses" ON "Course"
    FOR INSERT WITH CHECK (is_admin("CreatedBy"));

CREATE POLICY "Admin can update courses" ON "Course"
    FOR UPDATE USING (is_admin("CreatedBy"));

-- Teachers can read their assigned courses
CREATE POLICY "Teachers can read assigned courses" ON "Course"
    FOR SELECT USING (
        is_teacher("Instructor_UserId") OR 
        is_admin((SELECT "UserId" FROM "User" LIMIT 1))
    );

-- Students can read courses they're enrolled in
CREATE POLICY "Students can read enrolled courses" ON "Course"
    FOR SELECT USING (
        EXISTS (
            SELECT 1 FROM "Enrollment" e
            WHERE e."CourseId" = "Course"."CourseId" 
            AND e."UserId" = current_setting('app.current_user_id')::INTEGER
        )
    );

-- ============================================
-- ENROLLMENT TABLE POLICIES
-- ============================================

-- Teachers can enroll students in their courses
CREATE POLICY "Teachers can enroll students" ON "Enrollment"
    FOR INSERT WITH CHECK (
        teacher_owns_course("EnrolledBy", "CourseId") OR
        is_admin("EnrolledBy")
    );

-- Teachers and admins can view enrollments
CREATE POLICY "Teachers and admins can view enrollments" ON "Enrollment"
    FOR SELECT USING (
        is_admin("EnrolledBy") OR
        teacher_owns_course("EnrolledBy", "CourseId")
    );

-- Students can view their own enrollments
CREATE POLICY "Students can view own enrollments" ON "Enrollment"
    FOR SELECT USING ("UserId" = current_setting('app.current_user_id')::INTEGER);

-- ============================================
-- QUIZ TABLE POLICIES
-- ============================================

-- Teachers can create quizzes in their courses
CREATE POLICY "Teachers can create quizzes" ON "Quiz"
    FOR INSERT WITH CHECK (teacher_owns_course("CreatedBy", "CourseId"));

-- Teachers can manage their course quizzes
CREATE POLICY "Teachers can manage course quizzes" ON "Quiz"
    FOR ALL USING (teacher_owns_course("CreatedBy", "CourseId"));

-- Students can view published quizzes in enrolled courses
CREATE POLICY "Students can view published quizzes" ON "Quiz"
    FOR SELECT USING (
        "Is_Published" = true AND
        student_enrolled_in_course(current_setting('app.current_user_id')::INTEGER, "CourseId")
    );

-- ============================================
-- NOTIFICATION TABLE POLICIES
-- ============================================

-- Users can read their own notifications
CREATE POLICY "Users can read own notifications" ON "Notification"
    FOR SELECT USING ("UserId" = current_setting('app.current_user_id')::INTEGER);

-- Users can update their own notifications
CREATE POLICY "Users can update own notifications" ON "Notification"
    FOR UPDATE USING ("UserId" = current_setting('app.current_user_id')::INTEGER);

-- System/Admins can create notifications
CREATE POLICY "Admins can create notifications" ON "Notification"
    FOR INSERT WITH CHECK (is_admin(current_setting('app.current_user_id')::INTEGER));

-- ============================================
-- ATTEMPT TABLE POLICIES
-- ============================================

-- Students can create attempts for published quizzes in enrolled courses
CREATE POLICY "Students can create attempts" ON "Attempt"
    FOR INSERT WITH CHECK (
        EXISTS (
            SELECT 1 FROM "Quiz" q
            JOIN "Enrollment" e ON e."CourseId" = q."CourseId"
            WHERE q."QuizId" = "Attempt"."QuizId"
            AND q."Is_Published" = true
            AND e."UserId" = "Attempt"."UserId"
        )
    );

-- Students can view their own attempts
CREATE POLICY "Students can view own attempts" ON "Attempt"
    FOR SELECT USING ("UserId" = current_setting('app.current_user_id')::INTEGER);

-- Teachers can view attempts in their courses
CREATE POLICY "Teachers can view course attempts" ON "Attempt"
    FOR SELECT USING (
        EXISTS (
            SELECT 1 FROM "Quiz" q
            JOIN "Course" c ON c."CourseId" = q."CourseId"
            WHERE q."QuizId" = "Attempt"."QuizId"
            AND c."Instructor_UserId" = current_setting('app.current_user_id')::INTEGER
        )
    );

-- ============================================
-- HELPFUL VIEWS (Optional)
-- ============================================

-- View for active courses with instructor details
CREATE OR REPLACE VIEW "ActiveCoursesWithInstructor" AS
SELECT 
    c."CourseId",
    c."Code",
    c."Name",
    c."Status",
    c."Category",
    c."Section",
    u."FullName" AS "InstructorName",
    u."Email" AS "InstructorEmail",
    c."CreatedAt"
FROM "Course" c
JOIN "Teacher" t ON c."Instructor_UserId" = t."UserId"
JOIN "User" u ON t."UserId" = u."UserId"
WHERE c."Status" = 'Active';

-- View for student enrollments with course details
CREATE OR REPLACE VIEW "StudentEnrollmentDetails" AS
SELECT 
    e."EnrollmentId",
    e."UserId",
    u."FullName" AS "StudentName",
    u."Email" AS "StudentEmail",
    s."StudentId",
    c."CourseId",
    c."Code" AS "CourseCode",
    c."Name" AS "CourseName",
    c."Section",
    e."EnrolledAt"
FROM "Enrollment" e
JOIN "Student" s ON e."UserId" = s."UserId"
JOIN "User" u ON s."UserId" = u."UserId"
JOIN "Course" c ON e."CourseId" = c."CourseId";

-- View for quiz attempts with scores
CREATE OR REPLACE VIEW "QuizAttemptSummary" AS
SELECT 
    a."AttemptId",
    a."QuizId",
    q."Title" AS "QuizTitle",
    a."UserId",
    u."FullName" AS "StudentName",
    s."StudentId",
    a."StartedAt",
    a."SubmittedAt",
    a."Score",
    a."Time_Spent_Seconds",
    CASE 
        WHEN a."SubmittedAt" IS NULL THEN 'In Progress'
        ELSE 'Completed'
    END AS "Status"
FROM "Attempt" a
JOIN "Quiz" q ON a."QuizId" = q."QuizId"
JOIN "Student" s ON a."UserId" = s."UserId"
JOIN "User" u ON s."UserId" = u."UserId";

-- ============================================
-- COMPLETION MESSAGE
-- ============================================
DO $$
BEGIN
    RAISE NOTICE '==========================================';
    RAISE NOTICE 'Database schema created successfully!';
    RAISE NOTICE 'All tables, indexes, and triggers are ready.';
    RAISE NOTICE '==========================================';
END $$;
