using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Car_Rent.Helpers
{
    [HtmlTargetElement("admin-search-form")]
    public class SearchFormTagHelper : TagHelper
    {
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var controller = ViewContext.RouteData.Values["controller"]?.ToString();
            var action = ViewContext.RouteData.Values["action"]?.ToString();
            var currentSearch = ViewContext.HttpContext.Request.Query["search"].ToString();

            output.TagName = "form";
            output.Attributes.SetAttribute("method", "get");
            output.Attributes.SetAttribute("asp-controller", controller);
            output.Attributes.SetAttribute("asp-action", action);
            output.Attributes.SetAttribute("class", "px-2 py-1");

            output.Content.SetHtmlContent($@"
                <input type='search' name='search' value='{currentSearch}'
                       class='form-control !border-0 !shadow-none'
                       placeholder='Search here...' />
            ");
        }
    }
}
