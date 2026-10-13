namespace Headbanger.Application.Pagination
{
    public class PagedResultDto<T>
    {
        public IReadOnlyList<T> Items { get; init; } = [];

        public int Page { get; init; }

        public int PageSize { get; init; }

        public int TotalCount { get; init; }

        public int TotalPages => PageSize > 0
            ? (int)Math.Ceiling((double)TotalCount / PageSize)
            : 0;

        public bool HasPrevious => Page > 1;

        public bool HasNext => Page < TotalPages;

        public static PagedResultDto<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount) =>
            new()
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
            };
    }
}
