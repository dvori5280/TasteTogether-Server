namespace TasteTogether.Core.DTOs;

public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    // סך כל הפריטים שמתאימים לחיפוש, בכל העמודים
    public int TotalCount { get; set; }
}