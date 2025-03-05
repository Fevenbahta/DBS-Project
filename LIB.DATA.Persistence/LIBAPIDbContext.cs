
using LIB.API.Domain;
using LIB.API.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIB.API.Persistence
{
    public class LIBAPIDbContext : DbContext
    {
        public readonly DbContextOptions<LIBAPIDbContext> _context;


        public LIBAPIDbContext(DbContextOptions<LIBAPIDbContext> options) : base(options)
        {

            _context = options;

        }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            foreach (var entity in ChangeTracker.Entries<BaseDomainEntity>())
            {
                // Perform some action for each entity being tracked by the DbContext


            }
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }





        
        public DbSet<UserData> userDatas { get; set; }
    }

}
