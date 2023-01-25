using System.Web;
using Microsoft.AspNetCore.WebUtilities;

namespace TodoApp.Pagination;

public class PaginatedResponse<T>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int PageCount { get; set; }
    public ICollection<T> Data { get; set; }
    public int TotalItemCount { get; set; }
    public Uri? NextPage { get; set; }
    public Uri? PreviousPage { get; set; }

    public PaginatedResponse()
    {
        Data = new List<T>();
        PageNumber = 0;
        PageSize = 0;
        TotalItemCount = 0;
        PageCount = 0;
    }

    public PaginatedResponse(ICollection<T> data, int pageNumber, int pageSize, int totalItemCount)
    {
        Data = data;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalItemCount = totalItemCount;
        PageCount = (int)Math.Ceiling((double)totalItemCount / pageSize);
    }

    public void CreateHelperURLs(Uri? uri)
    {
        if (uri == null)
        {
            return;
        }
        var qs = HttpUtility.ParseQueryString(uri.Query);
        NextPage = null;
        PreviousPage = null;
        if (PageNumber < PageCount)
        {
            qs.Set("PageNumber", (PageNumber + 1).ToString());
            var uriBuilder = new UriBuilder(uri);
            uriBuilder.Query = qs.ToString();
            NextPage = uriBuilder.Uri;
        }
        if (PageNumber > 1)
        {
            qs.Set("PageNumber", (PageNumber - 1).ToString());
            var uriBuilder = new UriBuilder(uri);
            uriBuilder.Query = qs.ToString();
            PreviousPage = uriBuilder.Uri;
        }
    }
}

