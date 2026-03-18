using Microsoft.EntityFrameworkCore;

namespace Merchant.Api.Infrastructure
{
    public class MerchantEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public decimal DailyLimit { get; set; }
    }

    public class MerchantDbContext : DbContext
    {
        public MerchantDbContext(DbContextOptions<MerchantDbContext> options) : base(options) { }
        public DbSet<MerchantEntity> Merchants { get; set; }
    }
}