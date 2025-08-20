namespace Car_Rent.Application.Common
{
    public sealed class PaginationQuery
    {
        private const int MaxPageSize = 100;
        public int Page { get; init; } = 1;

        private int _pageSize = 10;

        public int PageSize
        {
            get => _pageSize;
            init => _pageSize = value <= 0 ? 10 : Math.Min(value, MaxPageSize);
        }
    }
}
