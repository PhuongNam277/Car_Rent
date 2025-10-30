namespace Car_Rent.Infrastructure.MultiTenancy
{
    public interface ITenantProvider
    {
        int TenantId { get; }
        bool IsEndUser { get; set; }
    }

    public class HttpTenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _http;

        public HttpTenantProvider (IHttpContextAccessor http)
        {
            _http = http;
        }

        // bổ sung phần này để bật/tắt filter cho tenant cho end-user
        public bool IsEndUser { get; set; } = false;

        public int TenantId
        {
            get
            {
                var ctx = _http.HttpContext;
                if (ctx?.User?.Identity?.IsAuthenticated == true)
                {
                    var claim = ctx.User.FindFirst("tenant_id")?.Value;
                    if (int.TryParse(claim, out var t)) return t;
                }

                // Fallback (lưu tạm ở Session/Items khi user chọn tenant)
                if (_http.HttpContext?.Items.TryGetValue("TenantId", out var v) == true && v is int i)
                    return i;

                return 0;
            }
        }
    }
}
