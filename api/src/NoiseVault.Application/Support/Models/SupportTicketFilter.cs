using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Application.Support.Models
{
    public enum ESupportTicketSort
    {
        Activity,
        Newest,
        Priority,
    }

    public sealed record SupportTicketFilter(
        int? ReporterUserId = null,
        ESupportTicketStatus? Status = null,
        ESupportTag? Tag = null,
        ESupportArea? Area = null,
        ESupportTicketPriority? Priority = null,
        string? Search = null,
        int? ProtocolId = null,
        ESupportTicketSort Sort = ESupportTicketSort.Activity);
}
