namespace OnlineQuiz.Utilities
{
    public static class ExportImportConstants
    {
        public static class Types
        {
            public const string Export = "Export";
            public const string Import = "Import";
            public const string BulkImport = "BulkImport";
            public const string ScoresExport = "ScoresExport";
        }

        public static class Statuses
        {
            public const string Pending = "Pending";
            public const string InProgress = "InProgress";
            public const string Completed = "Completed";
            public const string Failed = "Failed";
            public const string PartiallyCompleted = "PartiallyCompleted";
        }
    }
}
