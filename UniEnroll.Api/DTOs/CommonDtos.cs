namespace UniEnroll.Api.DTOs;

public record PageResult<T>(IReadOnlyList<T> Items, int TotalCount);

