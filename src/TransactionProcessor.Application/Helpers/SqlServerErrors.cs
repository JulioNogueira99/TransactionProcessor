using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TransactionProcessor.Application.Helpers;

public static class SqlServerErrors
{
    public static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sqlEx && (sqlEx.Number is 2601 or 2627);
    }

    public static bool IsDeadlockOrTimeout(Exception ex)
    {
        if (ex is DbUpdateException dbu && dbu.InnerException is SqlException s1)
            return s1.Number is 1205 or -2;

        if (ex is TimeoutException) return true;

        if (ex is SqlException s2)
            return s2.Number is 1205 or -2;

        return false;
    }
}