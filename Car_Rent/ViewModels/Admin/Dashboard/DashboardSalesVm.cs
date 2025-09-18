namespace Car_Rent.ViewModels.Admin.Dashboard
{
    public record SalesStat(decimal current, decimal previous, decimal changePct);
    public class DashboardSalesVm
    {
        public SalesStat Daily {  get; set; }
        public SalesStat Monthly { get; set; }
        public SalesStat Yearly { get; set; }
    }
}
