using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;
using Postgrest;

namespace OnlineQuiz.Repository
{
    public class CourseRepository : ICourseRepository
    {
        private readonly SupabaseService _supabaseService;

        public CourseRepository(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        public async Task<Course> CreateAsync(Course course)
        {
            var response = await _supabaseService.GetClient().From<Course>().Insert(course);
            return response.Model;
        }

        public async Task<Course?> GetByIdAsync(int courseId)
        {
            var response = await _supabaseService.GetClient().From<Course>()
                .Where(c => c.CourseId == courseId)
                .Single();
            return response;
        }

        public async Task<List<Course>> GetAllAsync()
        {
            var response = await _supabaseService.GetClient().From<Course>().Get();
            return response.Models;
        }

        public async Task<Course> UpdateAsync(Course course)
        {
            var response = await _supabaseService.GetClient().From<Course>().Update(course);
            return response.Model;
        }

        public async Task<bool> DeleteAsync(int courseId)
        {
            await _supabaseService.GetClient().From<Course>()
                .Where(c => c.CourseId == courseId)
                .Delete();
            return true;
        }

        public async Task<List<Course>> GetByInstructorIdAsync(int instructorId)
        {
            var response = await _supabaseService.GetClient().From<Course>()
                .Where(c => c.InstructorId == instructorId)
                .Get();
            return response.Models;
        }

        public async Task<List<Course>> GetByStudentIdAsync(int studentId)
        {
            // This requires a join or a two-step query since Supabase C# SDK joins can be tricky
            // Step 1: Get Enrollment records for the student
            var enrollments = await _supabaseService.GetClient().From<Enrollment>()
                .Where(e => e.UserId == studentId)
                .Get();
            
            var courseIds = enrollments.Models.Select(e => e.CourseId).ToList();

            if (!courseIds.Any())
            {
                return new List<Course>();
            }

            // Step 2: Get Courses by IDs
            var response = await _supabaseService.GetClient().From<Course>()
                .Filter("CourseId", Postgrest.Constants.Operator.In, courseIds)
                .Get();
                
            return response.Models;
        }
    }
}
