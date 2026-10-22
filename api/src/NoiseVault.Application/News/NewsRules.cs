namespace NoiseVault.Application.News
{
    public static class NewsRules
    {
        public const int RetentionDays = 30;
        public const int MaxArticlesPerRun = 40;
        public const int TitleMaxLength = 300;
        public const int UrlMaxLength = 500;
        public const int AuthorMaxLength = 120;
        public const string ScopeToday = "today";
        public const string ScopeEarlier = "earlier";
    }
}
