using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    public class StartAttemptDto
    {
        [Required]
        public int QuizId { get; set; }

        [Required]
        public int StudentId { get; set; }
    }

    public class SubmitAttemptDto
    {
        // Score removed - calculated server-side to prevent manipulation
        public int TimeSpentSeconds { get; set; }
    }


    public class AttemptResponseDto
    {
        public int AttemptId { get; set; }
        public int UserId { get; set; }
        public string? StudentName { get; set; }
        public int QuizId { get; set; }
        public string? QuizTitle { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public decimal? Score { get; set; }
        public int? TimeSpentSeconds { get; set; }
    }

    public class CreateAnswerDto
    {
        [Required]
        public int AttemptId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        public int? ChoiceId { get; set; } // For Single/Multiple choice
        public string? TextAnswer { get; set; } // For Text questions
    }

    public class AnswerResponseDto
    {
        public int AnswerId { get; set; }
        public int AttemptId { get; set; }
        public int QuestionId { get; set; }
        public int? ChoiceId { get; set; }
        public string? TextAnswer { get; set; }
        public DateTime AnsweredAt { get; set; }
        public bool? IsCorrect { get; set; } // Only for submitted attempts
    }
    public class BulkAnswerRequestDto
    {
        [Required]
        public int AttemptId { get; set; }

        [Required]
        public List<AnswerSubmissionDto> Answers { get; set; } = new();
    }

    public class AnswerSubmissionDto
    {
        [Required]
        public int QuestionId { get; set; }

        public int? ChoiceId { get; set; }
        public string? TextAnswer { get; set; }
    }
}
