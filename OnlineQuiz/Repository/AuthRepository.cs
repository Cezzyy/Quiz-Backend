using OnlineQuiz.IRepository;
using OnlineQuiz.Models;
using OnlineQuiz.Services;

namespace OnlineQuiz.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly SupabaseService _supabaseService;
        private readonly IUserRepository _userRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ITeacherRepository _teacherRepository;

        public AuthRepository(
            SupabaseService supabaseService,
            IUserRepository userRepository,
            IUserRoleRepository userRoleRepository,
            IStudentRepository studentRepository,
            ITeacherRepository teacherRepository)
        {
            _supabaseService = supabaseService;
            _userRepository = userRepository;
            _userRoleRepository = userRoleRepository;
            _studentRepository = studentRepository;
            _teacherRepository = teacherRepository;
        }

        public async Task<User?> VerifyUserCredentialsAsync(string email, string password)
        {
            try
            {
                var client = _supabaseService.GetClient();
                
                // Call the Supabase RPC function to verify credentials
                var result = await client.Rpc("verify_user_credentials", new Dictionary<string, object>
                {
                    { "user_email", email },
                    { "user_password", password }
                });

                // The RPC function returns user data if credentials are valid
                // Parse the result and return User object
                if (result != null && result.Content != null)
                {
                    // The RPC function returns a single user record
                    var users = Newtonsoft.Json.JsonConvert.DeserializeObject<List<User>>(result.Content);
                    return users?.FirstOrDefault();
                }

                return null;
            }
            catch (Exception)
            {
                // Invalid credentials or other error
                return null;
            }
        }

        public async Task<(User user, UserRole userRole, Student? student, Teacher? teacher)?> GetUserWithRolesAsync(int userId)
        {
            try
            {
                // Fetch user, user roles, student, and teacher data in parallel
                var userTask = _userRepository.GetByIdAsync(userId);
                var userRolesTask = _userRoleRepository.GetByUserIdAsync(userId);
                var studentTask = _studentRepository.GetByUserIdAsync(userId);
                var teacherTask = _teacherRepository.GetByUserIdAsync(userId);

                await Task.WhenAll(userTask, userRolesTask, studentTask, teacherTask);

                var user = await userTask;
                if (user == null)
                    return null;

                var userRoles = await userRolesTask;
                var userRole = userRoles.FirstOrDefault();
                if (userRole == null)
                    return null;

                // Get the appropriate role-specific data based on roleId
                Student? student = null;
                Teacher? teacher = null;

                if (userRole.RoleId == 3) // Student
                {
                    student = await studentTask;
                }
                else if (userRole.RoleId == 2) // Teacher
                {
                    teacher = await teacherTask;
                }

                return (user, userRole, student, teacher);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task UpdatePasswordAsync(int userId, string newPasswordHash)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException($"User with ID {userId} not found");
            }

            var client = _supabaseService.GetClient();
            user.PasswordHash = newPasswordHash;
            user.UpdatedAt = DateTime.UtcNow;
            
            await client.From<User>()
                .Where(u => u.UserId == userId)
                .Update(user);
        }
    }
}
