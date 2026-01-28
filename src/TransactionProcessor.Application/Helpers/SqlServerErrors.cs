using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TransactionProcessor.Application.Helpers;

public static class SqlServerErrors
{
    public static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sqlEx && (sqlEx.Number is 2601 or 2627);
    }
}