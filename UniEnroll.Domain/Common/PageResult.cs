namespace UniEnroll.Domain.Common;

public record PageResult<T>(IReadOnlyList<T> Items, int TotalCount);

