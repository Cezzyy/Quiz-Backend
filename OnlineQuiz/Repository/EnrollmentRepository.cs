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
            return response.Model;
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
    }
}
