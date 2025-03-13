
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
    public class LIBAPIDbSQLContext : DbContext
    {
        public readonly DbContextOptions<LIBAPIDbSQLContext> _context;


        public LIBAPIDbSQLContext(DbContextOptions<LIBAPIDbSQLContext> options) : base(options)
        {

            _context = options;

        }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(LIBAPIDbSQLContext).Assembly);
          


        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            foreach (var entity in ChangeTracker.Entries<BaseDomainEntity>())
            {
                // Perform some action for each entity being tracked by the DbContext


            }
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }



      
        public DbSet<Users> Users { get; set; }
        public DbSet<UpdateLog> UpdateLog { get; set; }
        public DbSet<Transaction> Transaction { get; set; }
        public DbSet<TransactionSimulation> TransactionSimulation { get; set; }
        public DbSet<ErrorLog> ErrorLog{ get; set; }
        
        public DbSet<AirlinesOrder> airlinesorder { get; set; }
        public DbSet<ConfirmOrders> confirmorders { get; set; }
        public DbSet<AirlinesError> airlineserror { get; set; }
        public DbSet<AirlinesTransfer> airlinestransfer { get; set; }
        public DbSet<Refund> refunds { get; set; }
        public DbSet<ConfirmRefund> confirmRefunds { get; set; }
        
    }

}
