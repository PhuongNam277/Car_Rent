using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Car_Rent.Helpers
{
    [HtmlTargetElement("pager")]
    public class PagerTagHelper : TagHelper
    {
        private readonly IUrlHelperFactory _urlHelperFactory;

        public PagerTagHelper(IUrlHelperFactory urlHelperFactory)
        {
            _urlHelperFactory = urlHelperFactory;
        }

        [ViewContext] public ViewContext ViewContext { get; set; }

        [HtmlAttributeName("page")] public int Page { get; set; }
        [HtmlAttributeName("total-pages")] public int TotalPages { get; set; }
        [HtmlAttributeName("page-size")] public int PageSize { get; set; } = 10;

        // ep action/controller khac neu muon
        [HtmlAttributeName("asp-action")] public string? Action { get; set; }
        [HtmlAttributeName("asp-controller")] public string? Controller { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (TotalPages <= 1)
            {
                output.SuppressOutput();
                return;
            }
            
            var urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);

            // Get all query parameters, except for page
            var routeValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in ViewContext.HttpContext.Request.Query)
            {
                if(!string.Equals(kv.Key, "page", StringComparison.OrdinalIgnoreCase))
                {
                    routeValues[kv.Key] = kv.Value.ToString();
                }
            }

            string BuildUrl(int page)
            {
                var rv = new RouteValueDictionary(routeValues)
                {
                    ["page"] = page,
                    ["pageSize"] = PageSize
                };
                var act = Action ?? ViewContext.RouteData.Values["action"]?.ToString();
                var ctl = Controller ?? ViewContext.RouteData.Values["controller"]?.ToString();
                return urlHelper.Action(act, ctl, rv) ?? "#";
            }

            // window: display 5 button
            int start = Math.Max(1, Page - 2);
            int end = Math.Min(TotalPages, Page + 2);
            if(end - start < 4)
            {
                if (start == 1) end = Math.Min(TotalPages, start + 4);
                else if (end == TotalPages) start = Math.Max(1, end - 4);
            }

            output.TagName = "nav";
            var ul = @"<ul class=""pagination"">";
            string li(string text, string url, bool disabled = false, bool active = false) =>
                $@"<li class=""page-item {(disabled ? "disabled" : "")} {(active ? "active" : "")}"">
                    <a class=""page-link"" href=""{(disabled ? "#" : url)}"">{text}</a>
                </li>";

            var html = ul
                + li("«", BuildUrl(Page - 1), Page <= 1)
            + (start > 1 ? li("1", BuildUrl(1)) : "")
            + (start > 2 ? @"<li class=""page-item disabled""><span class=""page-link"">…</span></li>" : "");

            for (int i = start; i <= end; i++)
                html += li(i.ToString(), BuildUrl(i), false, i == Page);

            html += (end < TotalPages - 1 ? @"<li class=""page-item disabled""><span class=""page-link"">…</span></li>" : "")
                 + (end < TotalPages ? li(TotalPages.ToString(), BuildUrl(TotalPages)) : "")
                 + li("»", BuildUrl(Page + 1), Page >= TotalPages)
                 + "</ul>";

            output.Content.SetHtmlContent(html);
        }
    }
}
