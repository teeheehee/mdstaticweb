using MarkdownStaticWebsite.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkdownStaticWebsite.Modules
{
    public static class HtmlHelpers
    {
        public static string GetListItemLinks(IEnumerable<TitleAndUrl> titleAndUrls)
        {
            var sb = new StringBuilder();

            foreach (var titleAndUrl in titleAndUrls)
            {
                sb.AppendLine($"<li><a href=\"{titleAndUrl.Url}\">{titleAndUrl.Title}</a></li>");
            }

            return sb.ToString();
        }
    }
}
