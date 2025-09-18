using Car_Rent.ViewModels.Admin.Dashboard;

namespace Car_Rent.Interfaces
{
    public interface ISalesService
    {
        Task<DashboardSalesVm> GetDashboardSalesAsync();
    }
}
