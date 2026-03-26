using Gateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Gateway.Application.Sagas;

namespace Gateway.Infrastructure.Data
{
    public class GatewayDbContext : DbContext
    {
        public GatewayDbContext(DbContextOptions<GatewayDbContext> options) : base(options)
        {
        }

        public DbSet<PaymentTransaction> Transactions { get; set; }
        public DbSet<PaymentState> PaymentStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(x => x.IdempotencyKey)
                .IsUnique();

            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            modelBuilder.Entity<PaymentState>(entity =>
            {
                // Saga'nın zorunlu anahtarı (Primary Key)
                entity.HasKey(x => x.CorrelationId);

                // State ismini tutacağımız kolon (Max 64 karakter yeterli)
                entity.Property(x => x.CurrentState).HasMaxLength(64);
            });
        }
    }
}