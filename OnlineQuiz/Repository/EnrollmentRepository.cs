using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;

namespace OnlineQuiz.Repository
{
    public class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly SupabaseService _supabaseService;

        public EnrollmentRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<Enrollment> CreateAsync(Enrollment enrollment)
        {
            var response = await _supabaseService.GetClient().From<Enrollment>().Insert(enrollment);
            return response.Model ?? throw new InvalidOperationException("Failed to create enrollment");
        }

        public async Task<bool> ExistsAsync(int studentId, int courseId)
        {
            var response = await _supabaseService.GetClient().From<Enrollment>()
                .Where(e => e.UserId == studentId && e.CourseId == courseId)
                .Get();
            return response.Models.Any();
        }

        public async Task<List<Enrollment>> GetByCourseIdAsync(int courseId)
        {
            var response = await _supabaseService.GetClient().From<Enrollment>()
                .Where(e => e.CourseId == courseId)
                .Get();
            return response.Models;
        }

        public async Task<bool> DeleteAsync(int enrollmentId)
        {
            await _supabaseService.GetClient().From<Enrollment>()
                .Where(e => e.EnrollmentId == enrollmentId)
                .Delete();
            return true;
        }

        public async Task<int> CountByCourseIdAsync(int courseId)
        {
            var count = await _supabaseService.GetClient().From<Enrollment>()
                .Where(e => e.CourseId == courseId)
                .Count(Postgrest.Constants.CountType.Exact);
            return count;
        }

        public async Task<Dictionary<int, int>> CountByCourseIdsAsync(List<int> courseIds)
        {
            if (!courseIds.Any()) return new Dictionary<int, int>();

            var response = await _supabaseService.GetClient().From<Enrollment>()
                .Filter("CourseId", Postgrest.Constants.Operator.In, courseIds)
                .Get();

            return response.Models
                .GroupBy(e => e.CourseId)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}
