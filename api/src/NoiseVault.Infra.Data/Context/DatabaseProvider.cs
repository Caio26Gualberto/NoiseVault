using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace NoiseVault.Infra.Data.Context
{
    public enum EDatabaseProvider
    {
        Postgres,
        SqlServer
    }

    public static class DatabaseProvider
    {
        public const string ConfigurationKey = "Database:Provider";

        public static EDatabaseProvider Resolve(IConfiguration configuration)
        {
            var value = configuration[ConfigurationKey];

            if (string.IsNullOrWhiteSpace(value) || value.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
                return EDatabaseProvider.Postgres;

            if (value.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                return EDatabaseProvider.SqlServer;

            throw new InvalidOperationException(
                $"Database:Provider '{value}' não é suportado. Use 'Postgres' ou 'SqlServer'.");
        }

        public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder builder, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection não configurado. Defina a variável de ambiente ConnectionStrings__DefaultConnection.");

            return Resolve(configuration) switch
            {
                EDatabaseProvider.SqlServer => builder.UseSqlServer(connectionString, b =>
                    b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                        .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)),

                _ => builder.UseNpgsql(connectionString, b =>
                    b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                        .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)),
            };
        }

        public static string SoftDeleteIndexFilter(DbContext context) =>
            context.Database.IsSqlServer() ? "[IsDeleted] = 0" : "\"IsDeleted\" = false";

        public static string NotNullIndexFilter(DbContext context, string column) =>
            context.Database.IsSqlServer()
                ? $"[{column}] IS NOT NULL AND [IsDeleted] = 0"
                : $"\"{column}\" IS NOT NULL AND \"IsDeleted\" = false";

        public static string ValueIndexFilter(DbContext context, string column, int value) =>
            context.Database.IsSqlServer()
                ? $"[{column}] = {value} AND [IsDeleted] = 0"
                : $"\"{column}\" = {value} AND \"IsDeleted\" = false";

        public static string ValuesIndexFilter(DbContext context, string column, params int[] values) =>
            context.Database.IsSqlServer()
                ? $"[{column}] IN ({string.Join(", ", values)}) AND [IsDeleted] = 0"
                : $"\"{column}\" IN ({string.Join(", ", values)}) AND \"IsDeleted\" = false";

        public static string JsonColumnType(DbContext context) =>
            context.Database.IsSqlServer() ? "nvarchar(max)" : "jsonb";
    }
}
