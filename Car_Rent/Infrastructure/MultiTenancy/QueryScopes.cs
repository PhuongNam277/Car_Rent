using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Infrastructure.MultiTenancy
{
    public static class QueryScopes // class này là class dùng chung để hiển thị các dữ liệu trong phần quản lý
                                    // Admin như Cars, Reservations, ... Chúng dùng để chỉ hiện thị các dữ liệu thuộc về các nhà phân phối
                                    // cho thuê xe đó thôi. 
    {
        // với entity có TenantId
        public static IQueryable<T> ForTenant<T>(this IQueryable<T> q, int tenantId) where T : class
            => q.Where(e => EF.Property<int>(e, "TenantId") == tenantId);

        // Với entity có cột BaseLocationId hoặc LocationId (tùy bảng)
        public static IQueryable<T> ForBranch<T>(this IQueryable<T> q, int? branchId, string colName)
            where T : class
            => branchId.HasValue
                ? q.Where(e => EF.Property<int>(e, colName) == branchId)
                : q;
    }
}
