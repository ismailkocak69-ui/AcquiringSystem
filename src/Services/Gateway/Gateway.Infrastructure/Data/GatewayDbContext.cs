using Gateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MassTransit;

namespace Gateway.Infrastructure.Data
{
    public class GatewayDbContext : DbContext
    {
        public GatewayDbContext(DbContextOptions<GatewayDbContext> options) : base(options)
        {
        }

        public DbSet<PaymentTransaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(x => x.IdempotencyKey)
                .IsUnique();

            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();
        }
    }
}