namespace NoiseVault.Application.Users.Dtos
{
    public class UserSignupSummaryDto
    {
        public int Today { get; set; }
        public int Last7Days { get; set; }
        public int Last30Days { get; set; }
        public int Total { get; set; }
        public int Unconfirmed { get; set; }
    }
}
