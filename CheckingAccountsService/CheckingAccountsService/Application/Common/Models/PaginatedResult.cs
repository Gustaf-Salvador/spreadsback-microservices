using System.Text.Json.Serialization;

namespace CheckingAccountsService.Application.Common.Models;

public class PaginatedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    
    [JsonIgnore]
    public bool HasNextPage => CurrentPage < TotalPages;
    
    [JsonIgnore]
    public bool HasPreviousPage => CurrentPage > 1;
}