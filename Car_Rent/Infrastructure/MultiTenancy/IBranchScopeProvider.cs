namespace Car_Rent.Infrastructure.MultiTenancy
{
    public interface IBranchScopeProvider
    {
        int? BranchId { get; } // null => không khóa theo chi nhánh
        bool IsBranchScoped { get; } // true nếu role là Staff (bắt buộc branch)
    }

    public class HttpBranchScopeProvider : IBranchScopeProvider
    {
        private readonly IHttpContextAccessor _http;
        public HttpBranchScopeProvider(IHttpContextAccessor http)
        {
            _http = http;
        }

        public int? BranchId
        {
            get
            {
                var val = _http.HttpContext?.User?.FindFirst("branch_id")?.Value;
                return int.TryParse(val, out var id) ? id : (int?)null;
            }
        }

        public bool IsBranchScoped
        {
            get
            {
                var u = _http.HttpContext?.User;
                return u?.IsInRole("Staff") == true; // Staff chỉ được xem 1 chi nhánh
            }
        }
    }
}
