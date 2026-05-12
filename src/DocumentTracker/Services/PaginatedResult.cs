public class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Total { get; set; }
}
