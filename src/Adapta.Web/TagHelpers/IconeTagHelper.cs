using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Adapta.Web.TagHelpers;

[HtmlTargetElement("icone", TagStructure = TagStructure.WithoutEndTag)]
public sealed class IconeTagHelper : TagHelper
{
    public required string Nome { get; set; }
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "icone");
        output.Attributes.SetAttribute("aria-hidden", "true");
        output.Attributes.SetAttribute("focusable", "false");
        var use = new TagBuilder("use");
        use.Attributes["href"] = "/lib/lucide/icons.svg#" + Nome;
        output.Content.SetHtmlContent(use);
    }
}
