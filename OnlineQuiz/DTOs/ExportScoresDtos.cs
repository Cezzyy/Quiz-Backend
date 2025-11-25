using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    public class ExportScoresRequestDto
    {
        /// <summary>
        /// Optional quiz ID to export scores for a single quiz
        /// </summary>
        public int? QuizId { get; set; }

        /// <summary>
        /// Optional course ID to export scores for all quizzes in a course
        /// </summary>
        public int? CourseId { get; set; }
    }

    public class ScoreExportDataDto
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string QuizTitle { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? TimeSpentMinutes { get; set; }
    }
}
