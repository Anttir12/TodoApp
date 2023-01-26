public class PaginationFilter
{
    public static int maxPageSize = 50;
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string SortOrder { get; set; }

    public PaginationFilter()
    {
        PageNumber = 1;
        PageSize = 10;
        SortOrder = "createDate";
    }
    public PaginationFilter(int pageNumber, int pageSize, string? sortOrder = null)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize > maxPageSize ? maxPageSize : pageSize;
        SortOrder = SortOrder ?? "createDate";

    }
}