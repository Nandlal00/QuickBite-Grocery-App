using Microsoft.AspNetCore.Mvc;
using GroceryStoreApp.Models;
using GroceryStoreApp.Data;

namespace GroceryStoreApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context) => _context = context;

        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if ((username == "admin" || username == "admin@quickbite.com") && password == "admin123")
            {
                Response.Cookies.Append("AdminSession", "Authenticated", new CookieOptions { Expires = DateTimeOffset.UtcNow.AddHours(8), Path = "/" });
                return RedirectToAction("Dashboard");
            }
            ViewBag.Error = "Invalid admin credentials.";
            return View();
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("AdminSession", new CookieOptions { Path = "/" });
            return RedirectToAction("Login");
        }

        public IActionResult Dashboard()
        {
            if (Request.Cookies["AdminSession"] != "Authenticated") return RedirectToAction("Login");

            // Seed default cities if empty
            if (!_context.OperatingCities.Any())
            {
                _context.OperatingCities.AddRange(
                    new OperatingCity { Name = "Bidasar", IsActive = true },
                    new OperatingCity { Name = "Churu", IsActive = true },
                    new OperatingCity { Name = "Jaipur", IsActive = true },
                    new OperatingCity { Name = "Delhi", IsActive = true },
                    new OperatingCity { Name = "Mumbai", IsActive = true }
                );
                _context.SaveChanges();
            }

            ViewBag.Orders = _context.Orders.OrderByDescending(o => o.Id).ToList();
            ViewBag.Products = _context.Products.OrderByDescending(p => p.Id).ToList();
            ViewBag.Coupons = _context.Coupons.ToList();
            ViewBag.Cities = _context.OperatingCities.ToList();
            ViewBag.TotalVisitors = _context.VisitorLogs.Count();
            ViewBag.TotalCustomers = _context.Users.Count();
            ViewBag.TotalRevenue = _context.Orders.Sum(o => (decimal?)o.TotalAmount) ?? 0m;
            return View();
        }

        [HttpPost]
        public IActionResult UpdateOrderStatus(int id, string status)
        {
            if (Request.Cookies["AdminSession"] != "Authenticated") return Unauthorized();
            var order = _context.Orders.Find(id);
            if (order != null) { order.Status = status; _context.SaveChanges(); }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public IActionResult AddCity(string cityName)
        {
            if (Request.Cookies["AdminSession"] != "Authenticated") return Unauthorized();
            if (!string.IsNullOrWhiteSpace(cityName) && !_context.OperatingCities.Any(c => c.Name.ToLower() == cityName.ToLower()))
            {
                _context.OperatingCities.Add(new OperatingCity { Name = cityName.Trim(), IsActive = true });
                _context.SaveChanges();
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public IActionResult ToggleCityStatus(int id)
        {
            if (Request.Cookies["AdminSession"] != "Authenticated") return Unauthorized();
            var city = _context.OperatingCities.Find(id);
            if (city != null) { city.IsActive = !city.IsActive; _context.SaveChanges(); }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public IActionResult DeleteCity(int id)
        {
            if (Request.Cookies["AdminSession"] != "Authenticated") return Unauthorized();
            var city = _context.OperatingCities.Find(id);
            if (city != null) { _context.OperatingCities.Remove(city); _context.SaveChanges(); }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile? imageFile, string? imageUrlInput)
        {
            if (Request.Cookies["AdminSession"] != "Authenticated") return Unauthorized();

            if (imageFile != null && imageFile.Length > 0)
            {
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                var fileName = Guid.NewGuid() + "_" + imageFile.FileName;
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                    await imageFile.CopyToAsync(stream);
                product.ImageUrl = "/images/products/" + fileName;
            }
            else if (!string.IsNullOrEmpty(imageUrlInput))
            {
                product.ImageUrl = imageUrlInput;
            }
            else if (string.IsNullOrEmpty(product.ImageUrl))
            {
                product.ImageUrl = "https://images.unsplash.com/photo-1542838132-92c53300491e?w=500&auto=format&fit=crop&q=60";
            }

            _context.Products.Add(product);
            _context.SaveChanges();
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public IActionResult AddCoupon(Coupon coupon)
        {
            if (Request.Cookies["AdminSession"] != "Authenticated") return Unauthorized();
            _context.Coupons.Add(coupon);
            _context.SaveChanges();
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public IActionResult DeleteCoupon(int id)
        {
            if (Request.Cookies["AdminSession"] != "Authenticated") return Unauthorized();
            var coupon = _context.Coupons.Find(id);
            if (coupon != null) { _context.Coupons.Remove(coupon); _context.SaveChanges(); }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public IActionResult DeleteProduct(int id)
        {
            if (Request.Cookies["AdminSession"] != "Authenticated") return Unauthorized();
            var product = _context.Products.Find(id);
            if (product != null) { _context.Products.Remove(product); _context.SaveChanges(); }
            return RedirectToAction("Dashboard");
        }
    }
}