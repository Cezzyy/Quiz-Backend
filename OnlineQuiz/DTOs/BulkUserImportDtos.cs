namespace OnlineQuiz.DTOs
{
    /// <summary>
    /// Result of bulk user import operation
    /// </summary>
    public class BulkUserImportResultDto
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int TotalRows { get; set; }
        public int LogId { get; set; }
        public List<UserImportErrorDto> Errors { get; set; } = new();
        public List<UserResponseDto> CreatedUsers { get; set; } = new();
    }

    /// <summary>
    /// Represents an error that occurred during import
    /// </summary>
    public class UserImportErrorDto
    {
        public int RowNumber { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
    }

    /// <summary>
    /// Internal DTO representing a parsed Excel row
    /// </summary>
    internal class UserImportRowDto
    {
        public int RowNumber { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? StudentId { get; set; }
        public int? YearLevel { get; set; }
        public string? Section { get; set; }
        public string? Course { get; set; }
        public string? Department { get; set; }
        public string? ContactNumber { get; set; }
        public string? EmergencyContactNumber { get; set; }
    }
}
