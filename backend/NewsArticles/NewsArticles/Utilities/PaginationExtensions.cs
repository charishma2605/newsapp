using NewsArticles.Models;

namespace NewsArticles.Utilities
{
    public static class PaginationExtensions
    {
        public static PagedSearchRequestDto Normalize(this PagedSearchRequestDto dto)
        {
            dto.PageNumber = dto.PageNumber <= 0 ? 1 : dto.PageNumber;
            dto.PageSize = dto.PageSize <= 0 ? 20 : dto.PageSize;
            dto.Search = string.IsNullOrWhiteSpace(dto.Search) ? null : dto.Search.Trim();
            return dto;
        }

        public static IEnumerable<T> Page<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
        {
            return source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }
    }
}
