namespace NoiseVault.Domain.Entities
{
    public class SupportTicketMessage : EntityBase
    {
        public int SupportTicketId { get; set; }
        public int AuthorUserId { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool FromStaff { get; set; }

        public SupportTicket? SupportTicket { get; set; }
        public User? Author { get; set; }
    }
}
