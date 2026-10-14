using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Events.Models;
using NoiseVault.Domain.Entities;

namespace NoiseVault.Application.Events.Interfaces
{
    public interface IEventRepository : IRepository<Event>
    {
        Task<(List<EventReadModel> Items, int TotalCount)> PageAsync(EventFilter filter, int viewerUserId, int skip, int take);
        Task<EventReadModel?> FindAsync(int eventId, int viewerUserId);
        Task<List<EventReadModel>> ListByIdsAsync(IReadOnlyCollection<int> eventIds, int viewerUserId);
        Task<bool> AddInterestAsync(int eventId, int userId);
        Task RemoveInterestAsync(int eventId, int userId);
        Task<bool> TryCancelAsync(int eventId, DateTime now);
        Task<List<int>> InterestedUserIdsAsync(int eventId);
        Task<List<(int EventId, List<int> UserIds)>> InterestedStartingBetweenAsync(DateTime fromUtc, DateTime toUtc);
        Task<int> CountUpcomingByBandAsync(int bandId, DateTime now);
        Task<int> CountUpcomingByOrganizerAsync(int userId, DateTime now);
    }
}
