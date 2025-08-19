using Microsoft.Data.SqlClient;

namespace UniEnroll.Api.Errors;

public static class SqlErrorClassifier
{
    // Return (status, code, title)
    public static (int Status, string Code, string Title) Classify(SqlException ex)
        => ex.Number switch
        {
            2627 or 2601 => (409, ErrorCodes.DuplicateResource, "Duplicate resource"),
            1205 => (503, ErrorCodes.TransientDbError, "Deadlock detected"),
            -2 => (503, ErrorCodes.DatabaseUnavailable, "SQL timeout"),
            4060 => (503, ErrorCodes.DatabaseUnavailable, "Cannot open database"),
            18456 => (401, ErrorCodes.DatabaseUnavailable, "Login failed"),
            50000 or 51000 => (409, ErrorCodes.DuplicateResource, ex.Message), // THROW ... user-defined
            _ => (500, ErrorCodes.Unknown, "Database error")
        };
}
