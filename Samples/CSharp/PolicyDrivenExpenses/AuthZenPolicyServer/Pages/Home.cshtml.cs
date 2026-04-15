using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using System.Reflection;

namespace AuthZenPolicyServer.Pages
{
    public class HomeModel : PageModel
    {
        public string PolicyText { get; set; } = string.Empty;
        public List<(string Name, string Content)> PolicyFiles { get; set; } = new();

        public void OnGet()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames()
                .Where(r => r.Contains(".Policies.") && r.EndsWith(".alfa"));
            foreach (var resource in resources)
            {
                using var stream = assembly.GetManifestResourceStream(resource);
                using var reader = new StreamReader(stream!);
                var content = reader.ReadToEnd();
                var name = resource.Substring(resource.LastIndexOf(".Policies.") + 10);
                PolicyFiles.Add((name, content));
            }
        }

        public static string ColorizePolicy(string policy)
        {
            // Colorize comments first
            string result = Regex.Replace(policy, "//.*", "<span class='alfa-comment'>$0</span>", RegexOptions.None, TimeSpan.FromSeconds(1));
            // Colorize strings
            result = Regex.Replace(result, "\"([^\"]*)\"", "<span class='alfa-literal'>\"$1\"</span>", RegexOptions.None, TimeSpan.FromSeconds(1));
            // Keywords
            string[] keywords = new[] { "namespace", "import", "attribute", "policyset", "policy", "apply", "firstApplicable", "denyUnlessPermit", "permitUnlessDeny", "target", "clause", "rule", "condition", "permit", "deny", "on", "advice" , "money" , "int" , "double" , "time" , "obligation" , "string" , "date" , "let"};
            foreach (var keyword in keywords)
            {
                result = Regex.Replace(result, $@"\b{keyword}\b", $"<span class='alfa-keyword'>{keyword}</span>", RegexOptions.None, TimeSpan.FromSeconds(1));
            }
            // Numbers
            result = Regex.Replace(result, "(?<=\\s|^)([0-9]+(\\.[0-9]+)?)(?=\\s|$)", "<span class='alfa-number'>$1</span>", RegexOptions.None, TimeSpan.FromSeconds(1));
            // Braces
            result = result.Replace("{", "<span class='alfa-brace'>{</span>");
            result = result.Replace("}", "<span class='alfa-brace'>}</span>");
            // HTML encode everything except our tags
            result = Regex.Replace(result, "(<[^>]+>|[^<]+)", match =>
            {
                if (match.Value.StartsWith("<"))
                    return match.Value; // leave tags alone
                return System.Net.WebUtility.HtmlEncode(match.Value);
            });
            // Fix double-encoding of quotes inside attributes
            result = result.Replace("&#39;", "'").Replace("&quot;", "\"");
            return result;
        }

        public HtmlString PolicyHtml(string policy) => new HtmlString(ColorizePolicy(policy));
    }
}
