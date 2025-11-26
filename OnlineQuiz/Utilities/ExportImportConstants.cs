namespace OnlineQuiz.Utilities
{
    public static class ExportImportConstants
    {
        public static class Types
        {
            public const string Export = "Export";
            public const string Import = "Import";
            public const string BulkImport = "Import"; // Changed to match DB constraint
            public const string ScoresExport = "Export"; // Changed to match DB constraint
        }

        public static class Statuses
        {
            public const string Pending = "Pending";
            public const string InProgress = "In Progress"; // Changed to match DB constraint
            public const string Completed = "Completed";
            public const string Failed = "Failed";
            public const string PartiallyCompleted = "Completed"; // Changed to match DB constraint (no partial status in DB)
        }
    }
}
