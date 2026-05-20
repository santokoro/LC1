using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace LC1.SourceView
{
    internal static class SourceCodeFormatter
    {
        internal static string? FindLc1ProjectDirectory()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (var i = 0; i < 8 && dir != null; i++)
            {
                var csproj = Path.Combine(dir.FullName, "LC1.csproj");
                if (File.Exists(csproj))
                    return dir.FullName;
                dir = dir.Parent;
            }

            return null;
        }

        internal static string RemoveCSharpComments(string source)
        {
            try
            {
                var tree = CSharpSyntaxTree.ParseText(source);
                var root = tree.GetRoot();
                var stripped = new CommentTriviaRewriter().Visit(root);
                return stripped.ToFullString();
            }
            catch
            {
                return source;
            }
        }

        internal static string RemoveXmlComments(string xml) =>
            Regex.Replace(xml, @"<!--[\s\S]*?-->", string.Empty, RegexOptions.None);

        internal static string ToHtmlDocument(IReadOnlyList<(string Title, string Code)> sections)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"ru\"><head><meta charset=\"utf-8\"/>");
            sb.AppendLine("<title>Исходный код программы</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:'Segoe UI',Tahoma,sans-serif;margin:16px 20px;font-size:14px;color:#111;background:#fff;}");
            sb.AppendLine("h1{font-size:20px;text-align:center;margin-bottom:8px;}");
            sb.AppendLine("section{margin:28px 0;border-top:1px solid #d8dee4;padding-top:16px;}");
            sb.AppendLine("section:first-of-type{border-top:none;padding-top:0;}");
            sb.AppendLine("h2{font-size:15px;margin:0 0 10px 0;color:#24292f;font-family:Consolas,monospace;font-weight:600;}");
            sb.AppendLine("pre{margin:0;padding:14px 16px;background:#f6f8fa;border:1px solid #d0d7de;border-radius:6px;");
            sb.AppendLine("overflow:auto;max-height:70vh;font-family:Consolas,'Courier New',monospace;font-size:12px;line-height:1.45;white-space:pre;tab-size:4;}");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine("<h1>Исходный код программы</h1>");

            foreach (var (title, code) in sections)
            {
                sb.AppendLine("<section>");
                sb.AppendLine($"<h2>{System.Net.WebUtility.HtmlEncode(title)}</h2>");
                sb.AppendLine("<pre><code>");
                sb.Append(System.Net.WebUtility.HtmlEncode(code));
                sb.AppendLine("</code></pre></section>");
            }

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private sealed class CommentTriviaRewriter : CSharpSyntaxRewriter
        {
            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                var cleaned = token
                    .WithLeadingTrivia(token.LeadingTrivia.Where(t => !IsCommentTrivia(t)))
                    .WithTrailingTrivia(token.TrailingTrivia.Where(t => !IsCommentTrivia(t)));
                return base.VisitToken(cleaned);
            }

            private static bool IsCommentTrivia(SyntaxTrivia t) =>
                t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                t.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia) ||
                t.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia);
        }
    }
}
