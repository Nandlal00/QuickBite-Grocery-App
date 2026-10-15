using Microsoft.EntityFrameworkCore;
using GroceryStoreApp.Models;

namespace GroceryStoreApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<SavedAddress> SavedAddresses { get; set; }
        public DbSet<OrderViewModel> Orders { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<VisitorLog> VisitorLogs { get; set; }
        public DbSet<OperatingCity> OperatingCities { get; set; } //new DbSet for OperatingCity
    }
}