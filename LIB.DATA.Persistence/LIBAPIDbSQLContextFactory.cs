using LIB.API.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace LIBPROPERTY.Persistence
{
    public static partial class PersistenceServiceRegistrtion
    {
        public class LIBAPIDbSQLContextFactory : IDesignTimeDbContextFactory<LIBAPIDbSQLContext>
        {
            public LIBAPIDbSQLContext CreateDbContext(string[] args)
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                var builder = new DbContextOptionsBuilder<LIBAPIDbSQLContext>();
                var connectionString = configuration.GetConnectionString("LIBAPISQLConnectionString");

                // Use Npgsql for PostgreSQL connection
                builder.UseNpgsql(connectionString);

                return new LIBAPIDbSQLContext(builder.Options);
            }
        }
    }
}
