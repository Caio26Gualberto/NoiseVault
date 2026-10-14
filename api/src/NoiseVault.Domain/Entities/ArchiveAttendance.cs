namespace NoiseVault.Domain.Entities
{
    public class ArchiveAttendance
    {
        public int ArchiveEntryId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}
