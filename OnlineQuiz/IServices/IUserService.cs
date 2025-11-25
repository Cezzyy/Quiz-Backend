using OnlineQuiz.DTOs;

namespace OnlineQuiz.IServices
{
    public interface IUserService
    {
        Task<UserResponseDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<UserResponseDto?> GetUserByIdAsync(int userId);
        Task<List<UserResponseDto>> GetAllUsersAsync();
        Task<PagedResult<UserResponseDto>> GetAllUsersPagedAsync(PaginationParams paginationParams);
        Task<UserResponseDto> UpdateUserAsync(int userId, UpdateUserDto updateUserDto);
        Task<bool> DeleteUserAsync(int userId);
        Task<int> BulkDeleteAsync(List<int> userIds);
        Task ResetPasswordAsync(int userId, string newPassword);
        Task<BulkUserImportResultDto> BulkCreateUsersFromExcelAsync(Stream fileStream, string fileName, int createdByUserId);
    }
}
