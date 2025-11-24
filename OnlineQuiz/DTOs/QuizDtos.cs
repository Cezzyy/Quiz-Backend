using System.ComponentModel.DataAnnotations;

namespace OnlineQuiz.DTOs
{
    public class CreateQuizDto
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public DateTime? DueAt { get; set; }

        public int? TimeLimitMinutes { get; set; }

        [Required]
        public int CreatedBy { get; set; }
        
        public List<CreateQuestionDto> Questions { get; set; } = new();
    }

    public class UpdateQuizDto
    {
        public string? Title { get; set; }
        public DateTime? DueAt { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public bool? IsPublished { get; set; }
    }

    public class QuizResponseDto
    {
        public int QuizId { get; set; }
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? DueAt { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<QuestionResponseDto> Questions { get; set; } = new();
    }

    public class CreateQuestionDto
    {
        [Required]
        public string Type { get; set; } = "Single"; // Single, Multiple, Text

        [Required]
        public string Body { get; set; } = string.Empty;

        public decimal Points { get; set; } = 1.0m;
        
        public int SortOrder { get; set; }

        public List<CreateChoiceDto> Choices { get; set; } = new();
    }

    public class QuestionResponseDto
    {
        public int QuestionId { get; set; }
        public int QuizId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public decimal Points { get; set; }
        public int SortOrder { get; set; }
        public List<ChoiceResponseDto> Choices { get; set; } = new();
    }

    public class CreateChoiceDto
    {
        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsCorrect { get; set; } = false;
    }

    public class ChoiceResponseDto
    {
        public int ChoiceId { get; set; }
        public int QuestionId { get; set; }
        public string Body { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
