using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Domain.Entities
{
    public class SupportTicketTag
    {
        public int SupportTicketId { get; set; }
        public ESupportTag Tag { get; set; }
    }
}
