using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Application.Common
{
    public sealed class PaginatedList<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int Page { get; }
        public int PageSize { get; }
        public int Total { get; }
        public int TotalPages { get; }

        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        private PaginatedList(IReadOnlyList<T> items, int total, int page, int pageSize)
        {
            Items = items;
            Total = total;
            Page = page;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int page, int pageSize, CancellationToken ct = default)
        {
            var total = await source.CountAsync(ct);
            if(total == 0) return new PaginatedList<T>(Array.Empty<T>(), 0, 1, pageSize);

            var skip = (page - 1) * pageSize;
            var items = await source.Skip(skip).Take(pageSize).ToListAsync(ct);
            return new PaginatedList<T>(items, total, page, pageSize);

        }
    }
}
