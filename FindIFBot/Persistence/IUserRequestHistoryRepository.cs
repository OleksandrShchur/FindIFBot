using FindIFBot.Domain;

namespace FindIFBot.Persistence
{
    public interface IUserRequestHistoryRepository
    {
        void Add(UserRequest request);
        void Update(UserRequest request);
        List<UserRequest> GetByUserId(long userId);
        UserRequest? GetById(Guid id);
    }
}