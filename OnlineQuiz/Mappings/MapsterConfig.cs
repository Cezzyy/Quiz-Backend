using Mapster;
using OnlineQuiz.DTOs;
using OnlineQuiz.Models;

namespace OnlineQuiz.Mappings
{
    /// <summary>
    /// Configures Mapster mappings for the application
    /// </summary>
    public static class MapsterConfig
    {
        public static void RegisterMappings()
        {
            // User mappings
            // Mapster works with convention-based mapping by default
            // Custom mappings only needed for complex scenarios
            
            // Example: Custom mapping if property names don't match
            // TypeAdapterConfig<User, UserResponseDto>
            //     .NewConfig()
            //     .Map(dest => dest.UserId, src => src.UserId);
            
            // Course mappings
            TypeAdapterConfig<Course, CourseResponseDto>
                .NewConfig()
                .Map(dest => dest.InstructorId, src => src.InstructorUserId);

            TypeAdapterConfig<CreateCourseDto, Course>
                .NewConfig()
                .Map(dest => dest.InstructorUserId, src => src.InstructorId);
                
            TypeAdapterConfig<UpdateCourseDto, Course>
                .NewConfig()
                .Map(dest => dest.InstructorUserId, src => src.InstructorId);
        }
    }
}
