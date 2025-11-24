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
            
            // For now, we rely on Mapster's convention-based mapping
            // which automatically maps properties with the same names
        }
    }
}
