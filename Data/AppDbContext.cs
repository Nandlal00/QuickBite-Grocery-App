using Microsoft.EntityFrameworkCore;
using GroceryStoreApp.Models;

namespace GroceryStoreApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<OrderViewModel> Orders { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<SavedAddress> SavedAddresses { get; set; } // Added this missing DbSet
    }
}