using System.Runtime.Intrinsics.X86;
using Car_Rent.Interfaces;
using Car_Rent.Models;
using Car_Rent.ViewModels.Admin.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Services
{
    public class SalesService : ISalesService
    {
        private readonly CarRentalDbContext _context;
        public SalesService (CarRentalDbContext context)
        {
            _context = context;
        }

        public static readonly TimeZoneInfo Tz = TimeZoneInfo.FindSystemTimeZoneById(
#if WINDOWS
        "SE Asia Standard Time"
#else
        "Asia/Bangkok"
#endif
        );

        private static (DateTime utcStart, DateTime utcEnd) LocalRange(int year, int month, int? day = null)
        {
            DateTime startLocal = day.HasValue
                ? new DateTime(year, month, day.Value, 0, 0, 0, DateTimeKind.Unspecified)
                : new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            DateTime endLocal = day.HasValue
                ? startLocal.AddDays(1)
                : startLocal.AddMonths(1);

            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, Tz);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, Tz);
            return (startUtc, endUtc);
        }


        public async Task<DashboardSalesVm> GetDashboardSalesAsync()
        {
            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, Tz);
            int y = nowLocal.Year, m = nowLocal.Month, d = nowLocal.Day;

            // DAILY: hôm nay vs hôm qua
            var (todayStartUtc, todayEndUtc) = LocalRange(y, m, d);
            var (yestStartUtc, yestEndUtc) = (todayStartUtc.AddDays(-1), todayStartUtc);

            // MONTHLY: tháng này với tháng trước
            var (monthStartUtc, monthEndUtc) = LocalRange(y, m);
            var prevMonthLocalStart = new DateTime(y, m, 1).AddMonths(-1);
            var (prevMonthStartUtc, prevMonthEndUtc) = LocalRange(prevMonthLocalStart.Year, prevMonthLocalStart.Month);

            // YEARLY: năm nay với năm trước
            var thisYearLocalStart = new DateTime(y, 1, 1);
            var nextYearLocalStart = new DateTime(y + 1, 1, 1);
            var lastYearLocalStart = new DateTime(y - 1, 1, 1);
            var (yearStartUtc, yearEndUtc) = (
                TimeZoneInfo.ConvertTimeToUtc(thisYearLocalStart, Tz),
                TimeZoneInfo.ConvertTimeToUtc(nextYearLocalStart, Tz)
            );
            var (prevYearStartUtc, prevYearEndUtc) = (
                TimeZoneInfo.ConvertTimeToUtc(lastYearLocalStart, Tz),
                TimeZoneInfo.ConvertTimeToUtc(thisYearLocalStart, Tz)
            );

            // Helper: tính tổng an toàn
            static async Task<decimal> SumAsync(IQueryable<Payment> q)
            {
                return await q.SumAsync(p => (decimal?)p.Amount) ?? 0m;
            }

            // Base query: chỉ lọc trạng thái Paid
            var baseQuery = _context.Payments.AsNoTracking()
                .Where(p => p.Status == "Paid");

            // Daily
            var dailyCurrent = await SumAsync(baseQuery.Where(p => p.PaymentDate >= todayStartUtc && p.PaymentDate < todayEndUtc));
            var dailyPrev = await SumAsync(baseQuery.Where(p => p.PaymentDate >= yestStartUtc && p.PaymentDate < yestEndUtc));

            // Monthly
            var monthlyCurrent = await SumAsync(baseQuery.Where(p => p.PaymentDate >= monthStartUtc && p.PaymentDate < monthEndUtc));
            var monthlyPrev = await SumAsync(baseQuery.Where(p => p.PaymentDate >= prevMonthStartUtc && p.PaymentDate < prevMonthEndUtc));

            // Yearly
            var yearlyCurrent = await SumAsync(baseQuery.Where(p => p.PaymentDate >= yearStartUtc && p.PaymentDate < yearEndUtc));
            var yearlyPrev = await SumAsync(baseQuery.Where(p => p.PaymentDate >= prevYearStartUtc && p.PaymentDate < prevYearEndUtc));

            // Helper Pack
            static SalesStat Pack(decimal cur, decimal prev)
            {
                if (prev <= 0) return new(cur, 0, cur > 0 ? 100 : 0); // nếu kì trước không có dữ liệu
                var pct = Math.Round((cur - prev) / prev * 100m, 2);
                return new(cur, prev, pct);
            }

            // Kết quả trả về
            return new DashboardSalesVm
            {
                Daily = Pack(dailyCurrent, dailyPrev),
                Monthly = Pack(monthlyCurrent, monthlyPrev),
                Yearly = Pack(yearlyCurrent, yearlyPrev)
            };

        }
    }
}
