using OnlineQuiz.DTOs;
using OnlineQuiz.IRepository;
using OnlineQuiz.IServices;
using OnlineQuiz.Models;
using OnlineQuiz.Utilities;

namespace OnlineQuiz.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ITeacherRepository _teacherRepository;
        private readonly IUserRoleRepository _userRoleRepository;

        public UserService(
            IUserRepository userRepository,
            IStudentRepository studentRepository,
            ITeacherRepository teacherRepository,
            IUserRoleRepository userRoleRepository)
        {
            _userRepository = userRepository;
            _studentRepository = studentRepository;
            _teacherRepository = teacherRepository;
            _userRoleRepository = userRoleRepository;
        }

        public async Task<UserResponseDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            // Validate role-specific requirements
            if (createUserDto.RoleId == RoleConstants.Student && string.IsNullOrEmpty(createUserDto.StudentId))
            {
                throw new ArgumentException("StudentId is required for students");
            }

            // Check if email already exists
            var existingUser = await _userRepository.GetByEmailAsync(createUserDto.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException($"User with email {createUserDto.Email} already exists");
            }

            // Create User entity
            var user = new User
            {
                Email = createUserDto.Email,
                PasswordHash = PasswordHasher.HashPassword(createUserDto.Password),
                FullName = createUserDto.FullName,
                Status = "Active",
                ContactNumber = createUserDto.ContactNumber ?? string.Empty,
                EmergencyContactNumber = createUserDto.EmergencyContactNumber ?? string.Empty,
                CreatedBy = createUserDto.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Insert user
            var createdUser = await _userRepository.CreateAsync(user);

            try
            {
                // Create UserRole entry
                var userRole = new UserRole
                {
                    UserId = createdUser.UserId,
                    RoleId = createUserDto.RoleId
                };
                await _userRoleRepository.CreateAsync(userRole);

                // Create role-specific entity based on RoleId
                switch (createUserDto.RoleId)
                {
                    case RoleConstants.Student: // Student
                        var student = new Student
                        {
                            UserId = createdUser.UserId,
                            StudentId = createUserDto.StudentId!,
                            YearLevel = createUserDto.YearLevel,
                            Section = createUserDto.Section,
                            Course = createUserDto.Course
                        };
                        await _studentRepository.CreateAsync(student);
                        break;

                    case RoleConstants.Teacher: // Teacher
                        var teacher = new Teacher
                        {
                            UserId = createdUser.UserId,
                            Department = createUserDto.Department
                        };
                        await _teacherRepository.CreateAsync(teacher);
                        break;

                    case RoleConstants.Admin: // Admin - no additional table needed
                        break;
                }

                // Return the created user
                return await GetUserByIdAsync(createdUser.UserId) 
                    ?? throw new InvalidOperationException("Failed to retrieve created user");
            }
            catch (Exception)
            {
                // Manual Rollback: Delete the partially created user
                await _userRepository.DeleteAsync(createdUser.UserId);
                throw; // Re-throw the original exception
            }
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            var userRoles = await _userRoleRepository.GetByUserIdAsync(userId);
            var userRole = userRoles.FirstOrDefault();
            
            if (userRole == null)
            {
                throw new InvalidOperationException($"User {userId} has no role assigned");
            }

            var response = new UserResponseDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                Status = user.Status,
                ContactNumber = user.ContactNumber,
                EmergencyContactNumber = user.EmergencyContactNumber,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                CreatedBy = user.CreatedBy,
                RoleId = userRole.RoleId,
                RoleName = GetRoleName(userRole.RoleId)
            };

            // Populate role-specific data
            switch (userRole.RoleId)
            {
                case RoleConstants.Student: // Student
                    var student = await _studentRepository.GetByUserIdAsync(userId);
                    if (student != null)
                    {
                        response.Student = new StudentData
                        {
                            StudentId = student.StudentId,
                            YearLevel = student.YearLevel,
                            Section = student.Section,
                            Course = student.Course
                        };
                    }
                    break;

                case RoleConstants.Teacher: // Teacher
                    var teacher = await _teacherRepository.GetByUserIdAsync(userId);
                    if (teacher != null)
                    {
                        response.Teacher = new TeacherData
                        {
                            Department = teacher.Department
                        };
                    }
                    break;
            }

            return response;
        }

        public async Task<List<UserResponseDto>> GetAllUsersAsync()
        {
            // Fetch all data in parallel
            var usersTask = _userRepository.GetAllAsync();
            var userRolesTask = _userRoleRepository.GetAllAsync();
            var studentsTask = _studentRepository.GetAllAsync();
            var teachersTask = _teacherRepository.GetAllAsync();

            await Task.WhenAll(usersTask, userRolesTask, studentsTask, teachersTask);

            var users = await usersTask;
            var userRoles = await userRolesTask;
            var students = await studentsTask;
            var teachers = await teachersTask;

            // Create dictionaries for fast lookup
            var userRoleMap = userRoles.GroupBy(ur => ur.UserId).ToDictionary(g => g.Key, g => g.First());
            var studentMap = students.ToDictionary(s => s.UserId);
            var teacherMap = teachers.ToDictionary(t => t.UserId);

            var userResponses = new List<UserResponseDto>();

            foreach (var user in users)
            {
                // Skip users without roles (data integrity issue, but handle gracefully)
                if (!userRoleMap.TryGetValue(user.UserId, out var userRole))
                {
                    continue;
                }

                var response = new UserResponseDto
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    FullName = user.FullName,
                    Status = user.Status,
                    ContactNumber = user.ContactNumber,
                    EmergencyContactNumber = user.EmergencyContactNumber,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    CreatedBy = user.CreatedBy,
                    RoleId = userRole.RoleId,
                    RoleName = GetRoleName(userRole.RoleId)
                };

                // Populate role-specific data
                switch (userRole.RoleId)
                {
                    case RoleConstants.Student: // Student
                        if (studentMap.TryGetValue(user.UserId, out var student))
                        {
                            response.Student = new StudentData
                            {
                                StudentId = student.StudentId,
                                YearLevel = student.YearLevel,
                                Section = student.Section,
                                Course = student.Course
                            };
                        }
                        break;

                    case RoleConstants.Teacher: // Teacher
                        if (teacherMap.TryGetValue(user.UserId, out var teacher))
                        {
                            response.Teacher = new TeacherData
                            {
                                Department = teacher.Department
                            };
                        }
                        break;
                }

                userResponses.Add(response);
            }

            return userResponses;
        }

        public async Task<UserResponseDto> UpdateUserAsync(int userId, UpdateUserDto updateUserDto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }

            // Get user role to determine which entity to update
            var userRoles = await _userRoleRepository.GetByUserIdAsync(userId);
            var userRole = userRoles.FirstOrDefault();
            
            if (userRole == null)
            {
                throw new InvalidOperationException($"User {userId} has no role assigned");
            }

            // Update User entity
            if (!string.IsNullOrEmpty(updateUserDto.Email))
                user.Email = updateUserDto.Email;
            
            if (!string.IsNullOrEmpty(updateUserDto.FullName))
                user.FullName = updateUserDto.FullName;
            
            if (!string.IsNullOrEmpty(updateUserDto.Status))
                user.Status = updateUserDto.Status;
            
            if (updateUserDto.ContactNumber != null)
                user.ContactNumber = updateUserDto.ContactNumber;
            
            if (updateUserDto.EmergencyContactNumber != null)
                user.EmergencyContactNumber = updateUserDto.EmergencyContactNumber;

            await _userRepository.UpdateAsync(user);

            // Update role-specific data
            switch (userRole.RoleId)
            {
                case RoleConstants.Student: // Student
                    var student = await _studentRepository.GetByUserIdAsync(userId);
                    if (student != null)
                    {
                        if (updateUserDto.YearLevel.HasValue)
                            student.YearLevel = updateUserDto.YearLevel;
                        
                        if (updateUserDto.Section != null)
                            student.Section = updateUserDto.Section;
                        
                        if (updateUserDto.Course != null)
                            student.Course = updateUserDto.Course;

                        await _studentRepository.UpdateAsync(student);
                    }
                    break;

                case RoleConstants.Teacher: // Teacher
                    var teacher = await _teacherRepository.GetByUserIdAsync(userId);
                    if (teacher != null && updateUserDto.Department != null)
                    {
                        teacher.Department = updateUserDto.Department;
                        await _teacherRepository.UpdateAsync(teacher);
                    }
                    break;
            }

            // Return updated user
            return await GetUserByIdAsync(userId) 
                ?? throw new InvalidOperationException("Failed to retrieve updated user");
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }

            // Delete user (cascades to Student/Teacher/UserRole due to database constraints)
            return await _userRepository.DeleteAsync(userId);
        }

        private string GetRoleName(int roleId)
        {
            return roleId switch
            {
                RoleConstants.Admin => "Admin",
                RoleConstants.Teacher => "Teacher",
                RoleConstants.Student => "Student",
                _ => "Unknown"
            };
        }
    }
}
