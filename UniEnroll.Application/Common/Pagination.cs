using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace UniEnroll.Application.Common;

public static class Pagination
{
    public record PageRequest(int Page, int PageSize)
    {
        public static PageRequest From(HttpContext ctx, int defaultSize = 20, int maxSize = 100)
        {
            int.TryParse(ctx.Request.Query["page"], out var page);
            int.TryParse(ctx.Request.Query["pageSize"], out var size);
            page = page <= 0 ? 1 : page;
            size = size <= 0 ? defaultSize : Math.Min(size, maxSize);
            return new(page, size);
        }
    }

    public static void WriteLinkHeaders(HttpContext ctx, int page, int pageSize, int totalCount)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var baseUri = $"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.Path}";
        string Build(int p) => QueryHelpers.AddQueryString(baseUri, new Dictionary<string, string?>
        {
            ["page"] = p.ToString(),
            ["pageSize"] = pageSize.ToString()
        });

        var links = new List<string>();
        links.Add($"<{Build(Math.Max(1, page - 1))}>; rel=\"prev\"");
        links.Add($"<{Build(Math.Min(totalPages, page + 1))}>; rel=\"next\"");
        links.Add($"<{Build(1)}>; rel=\"first\"");
        links.Add($"<{Build(totalPages)}>; rel=\"last\"");
        ctx.Response.Headers["Link"] = string.Join(", ", links);
        ctx.Response.Headers["X-Total-Count"] = totalCount.ToString();
    }
}
