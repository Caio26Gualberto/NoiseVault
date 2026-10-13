namespace Headbanger.Application.Pagination
{
    public class PagedRequestDto
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = DefaultPageSize;

        public int Skip => (Page - 1) * PageSize;

        public void Normalize()
        {
            if (Page < 1) Page = 1;
            if (PageSize < 1) PageSize = DefaultPageSize;
            if (PageSize > MaxPageSize) PageSize = MaxPageSize;
        }
    }
}
