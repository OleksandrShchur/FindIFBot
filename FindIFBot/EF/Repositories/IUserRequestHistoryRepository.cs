using FindIFBot.EF.Entities;

namespace FindIFBot.EF.Repositories
{
    public interface IUserRequestHistoryRepository
    {
        Task Add(UserRequest request);
        Task Update(UserRequest request);
        Task<List<UserRequest>> GetByUserId(long userId);
        Task<UserRequest?> GetById(Guid id);
        Task<bool> HasHistory(long userId);
    }
}
