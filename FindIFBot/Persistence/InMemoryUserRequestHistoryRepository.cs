using System.Collections.Concurrent;
using FindIFBot.Domain;

namespace FindIFBot.Persistence
{
    public class InMemoryUserRequestHistoryRepository : IUserRequestHistoryRepository
    {
        private readonly ConcurrentDictionary<long, List<UserRequest>> _userRequests = new();
        private readonly ConcurrentDictionary<Guid, UserRequest> _requests = new();

        public void Add(UserRequest request)
        {
            _requests[request.Id] = request;
            var list = _userRequests.GetOrAdd(request.UserId, _ => new List<UserRequest>());
            list.Add(request);
        }

        public void Update(UserRequest request)
        {
            _requests[request.Id] = request;
            // Update in the list as well
            if (_userRequests.TryGetValue(request.UserId, out var list))
            {
                var index = list.FindIndex(r => r.Id == request.Id);
                if (index >= 0)
                {
                    list[index] = request;
                }
            }
        }

        public List<UserRequest> GetByUserId(long userId)
        {
            return _userRequests.GetOrAdd(userId, _ => new List<UserRequest>());
        }

        public UserRequest? GetById(Guid id)
        {
            _requests.TryGetValue(id, out var request);
            return request;
        }
    }
}