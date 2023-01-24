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
    
    public PaginatedResponse(ICollection<T> data, int pageNumber, int pageSize, int totalItemCount)
    {
        Data = data;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalItemCount = totalItemCount;
        PageCount = (int)Math.Ceiling((double)totalItemCount/pageSize);
    }
}

