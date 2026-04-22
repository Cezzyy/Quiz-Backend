using Mapster;
using OnlineQuiz.DTOs;
using OnlineQuiz.Mappings;
using OnlineQuiz.Models;
using OnlineQuiz.Utilities;

namespace OnlineQuiz.Tests
{
    /// <summary>
    /// Runs once for the entire test collection via ICollectionFixture.
    /// Eliminates ~1s cold-start penalties from three sources:
    ///   1. BCrypt JIT        — Blowfish internals compiled before any test runs
    ///   2. JWT JIT           — JwtSecurityTokenHandler pipeline compiled once
    ///   3. Mapster JIT       — all Adapt delegates compiled before any test runs
    /// </summary>
    public class MapsterWarmupFixture : IDisposable
    {
        static MapsterWarmupFixture()
        {
            // ── 1. BCrypt JIT warm-up ─────────────────────────────────────
            // Forces BCrypt's internal Blowfish cipher to JIT-compile once here
            // using workFactor 4 (minimum, ~5ms). Without this the first test
            // that calls BCrypt pays an extra ~200ms cold-start on top of the
            // actual hash cost.
            _ = BCrypt.Net.BCrypt.HashPassword("warmup", workFactor: 4);
            _ = BCrypt.Net.BCrypt.Verify("warmup", BCrypt.Net.BCrypt.HashPassword("warmup", workFactor: 4));

            // ── 2. JWT JIT warm-up ────────────────────────────────────────
            // JwtSecurityTokenHandler is a heavy class — first instantiation
            // triggers JIT compilation of the entire token pipeline (~1s).
            // Generating + validating a dummy token here absorbs that cost once
            // so every subsequent JWT test runs in <10ms.
            const string secret   = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
            const string issuer   = "OnlineQuizAPI-Test";
            const string audience = "OnlineQuizClient-Test";

            var warmToken = JwtTokenGenerator.GenerateToken(
                userId: 0, email: "warmup@test.com", roleId: 1, roleName: "Admin",
                secretKey: secret, issuer: issuer, audience: audience, expirationHours: 1);
            _ = JwtTokenGenerator.ValidateToken(warmToken, secret, issuer, audience);

            // ── 3. Mapster JIT warm-up ────────────────────────────────────
            // Register custom mappings then trigger one Adapt call per type pair
            // so Mapster compiles all mapping delegates before any test runs.
            MapsterConfig.RegisterMappings();

            _ = new Notification { UserId = 0, Type = "", Title = "", Message = "", CreatedAt = DateTime.UtcNow }
                    .Adapt<NotificationResponseDto>();
            _ = new ExportImportLog { UserId = 0, Type = "", FileName = "", Status = "", CreatedAt = DateTime.UtcNow }
                    .Adapt<ExportImportLogResponseDto>();
            _ = new Course { Code = "", Name = "", InstructorUserId = 0, Status = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = 0 }
                    .Adapt<CourseResponseDto>();
            _ = new CreateCourseDto { Code = "", Name = "", InstructorId = 0, CreatedBy = 0 }.Adapt<Course>();
            _ = new UpdateCourseDto().Adapt<Course>();
            _ = new Quiz { CourseId = 0, Title = "", Status = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = 0 }
                    .Adapt<QuizResponseDto>();
            _ = new Attempt { QuizId = 0, UserId = 0, StartedAt = DateTime.UtcNow }
                    .Adapt<AttemptResponseDto>();
            _ = new AttemptAnswer { AttemptId = 0, QuestionId = 0 }
                    .Adapt<AnswerResponseDto>();
            _ = new User { Email = "", PasswordHash = "", FullName = "", Status = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
                    .Adapt<UserResponseDto>();
        }

        public MapsterWarmupFixture() { }
        public void Dispose() { }
    }

    [CollectionDefinition("MapsterWarmup")]
    public class MapsterWarmupCollection : ICollectionFixture<MapsterWarmupFixture> { }
}
