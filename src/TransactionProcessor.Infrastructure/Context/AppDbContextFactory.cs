using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TransactionProcessor.Infrastructure.Context;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        var apiDir = FindApiProjectDirectory();

        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(apiDir, "appsettings.json"), optional: true, reloadOnChange: false)
            .AddJsonFile(Path.Combine(apiDir, $"appsettings.{environment}.json"), optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var conn = config.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException(
                $"Connection string 'DefaultConnection' not found. Checked: {apiDir} (env: {environment}).");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(conn)
            .Options;

        return new AppDbContext(options);
    }

    private static string FindApiProjectDirectory()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (dir is not null)
        {
            var csprojHere = Path.Combine(dir.FullName, "TransactionProcessor.Api.csproj");
            if (File.Exists(csprojHere))
                return dir.FullName;

            var apiCandidate = Path.Combine(dir.FullName, "src", "TransactionProcessor.Api");
            var csprojCandidate = Path.Combine(apiCandidate, "TransactionProcessor.Api.csproj");
            if (File.Exists(csprojCandidate))
                return apiCandidate;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate TransactionProcessor.Api.csproj by walking up from the current directory.");
    }
}
