using Headbanger.Domain.Enums.Profiles;

namespace Headbanger.Domain.Entities
{
    public class UserGenre
    {
        public int UserId { get; set; }
        public EMusicGenre Genre { get; set; }
    }
}
