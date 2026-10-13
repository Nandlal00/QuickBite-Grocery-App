using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using GroceryStoreApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services with Razor Runtime Compilation for instant hot reload
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

// Register SQLite Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=grocery.db"));

// Add Built-in Cookie Authentication (Production Standard)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "QuickBiteAuthCookie";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.Path = "/";
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// MUST be placed between UseRouting and UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure database and seeding
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new GroceryStoreApp.Models.Product { Name = "Fresh Organic Tomatoes", Category = "Vegetables", Price = 40.00m, Unit = "1 kg", ImageUrl = "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=500&auto=format&fit=crop&q=60", Rating = 4.8, Description = "Juicy, farm-fresh red tomatoes." },
            new GroceryStoreApp.Models.Product { Name = "Amul Fresh Milk", Category = "Dairy", Price = 32.00m, Unit = "500 ml", ImageUrl = "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=500&auto=format&fit=crop&q=60", Rating = 4.9, Description = "Pure and pasteurized cow milk." },
            new GroceryStoreApp.Models.Product { Name = "Cavendish Bananas", Category = "Fruits", Price = 60.00m, Unit = "1 dozen", ImageUrl = "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=500&auto=format&fit=crop&q=60", Rating = 4.7, Description = "Sweet and energy-boosting bananas." }
        );
        db.SaveChanges();
    }
}

app.Run();