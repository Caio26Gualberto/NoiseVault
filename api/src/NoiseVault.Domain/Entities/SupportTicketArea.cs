using NoiseVault.Domain.Enums.Support;

namespace NoiseVault.Domain.Entities
{
    public class SupportTicketArea
    {
        public int SupportTicketId { get; set; }
        public ESupportArea Area { get; set; }
    }
}
