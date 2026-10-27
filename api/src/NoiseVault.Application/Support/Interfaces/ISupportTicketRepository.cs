using NoiseVault.Application.Abstractions.Repository;
using NoiseVault.Application.Support.Models;
using NoiseVault.Domain.Entities;
using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Application.Support.Interfaces
{
    public interface ISupportTicketRepository : IRepository<SupportTicket>
    {
        Task<(List<SupportTicketReadModel> Items, int TotalCount)> PageAsync(SupportTicketFilter filter, int skip, int take);
        Task<SupportTicketReadModel?> FindAsync(int ticketId, int? reporterUserId);
        Task<Dictionary<ESupportTicketStatus, int>> CountByStatusAsync();
        Task<int> CountOpenAsync(int reporterUserId);
        Task<(List<SupportTicketMessage> Items, int TotalCount)> PageMessagesAsync(int ticketId, int skip, int take);
        Task<List<int>> PurgeAsync(int ticketId);
    }
}
