using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence.Context;

public sealed class PropostaDbContextFactory : IDesignTimeDbContextFactory<PropostaDbContext>
{
    public PropostaDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Server=localhost,1433;Database=PropostaCredito;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;Encrypt=False";

        var options = new DbContextOptionsBuilder<PropostaDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new PropostaDbContext(options);
    }
}
