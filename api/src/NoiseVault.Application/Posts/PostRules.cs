namespace NoiseVault.Application.Posts
{
    public static class PostRules
    {
        public const int ContentMaxLength = 5000;
        public const int CommentMaxLength = 2000;
        public const int MaxPhotosPerPost = 10;
        public const int MaxVideosPerPost = 1;
        public const int MaxMediaPerPost = MaxPhotosPerPost + MaxVideosPerPost;

        public static string NormalizeText(string? text) => (text ?? string.Empty).Trim();
    }
}
