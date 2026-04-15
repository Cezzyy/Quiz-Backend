namespace OnlineQuiz.Utilities
{
    /// <summary>
    /// Constants for entity status values used across User, Course, and Quiz entities
    /// </summary>
    public static class EntityStatusConstants
    {
        /// <summary>
        /// Entity is active and available for normal operations
        /// </summary>
        public const string Active = "Active";

        /// <summary>
        /// Entity is archived (soft-deleted) but data is preserved
        /// </summary>
        public const string Archived = "Archived";

        /// <summary>
        /// Entity is temporarily inactive/disabled but not archived
        /// </summary>
        public const string Inactive = "Inactive";

        /// <summary>
        /// Get all valid status values
        /// </summary>
        public static readonly string[] ValidStatuses = { Active, Archived, Inactive };

        /// <summary>
        /// Check if a status value is valid
        /// </summary>
        public static bool IsValidStatus(string status)
        {
            return Array.Exists(ValidStatuses, s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
        }
    }
}
