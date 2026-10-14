using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Application.Support.Dtos
{
    public class SupportTicketUpdateInputDto
    {
        public ESupportTicketStatus Status { get; set; }
        public ESupportTicketPriority Priority { get; set; }
        public string? InternalNote { get; set; }
    }
}
