
using LIB.API.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace LIBPROPERTY.Persistence
{
    public static partial class PersistenceServiceRegistrtion
    {
        public class LIBDATADbContextFactory : IDesignTimeDbContextFactory<LIBAPIDbContext>
        {
            public LIBAPIDbContext CreateDbContext(string[] args)
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();

                var builder = new DbContextOptionsBuilder<LIBAPIDbContext>();
                var connectionString = configuration.GetConnectionString("LIBAPIConnectionString");

                // Replace UseSqlServer with UseMySQL
                builder.UseOracle(connectionString);

                return new LIBAPIDbContext(builder.Options);
            }
        }
    }
}
