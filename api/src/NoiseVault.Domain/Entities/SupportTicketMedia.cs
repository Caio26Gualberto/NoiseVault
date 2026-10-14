namespace NoiseVault.Domain.Entities
{
    public class SupportTicketMedia : EntityBase
    {
        public int SupportTicketId { get; set; }
        public int MediaUploadId { get; set; }
        public int Order { get; set; }

        public SupportTicket? SupportTicket { get; set; }
        public MediaUpload? MediaUpload { get; set; }
    }
}
