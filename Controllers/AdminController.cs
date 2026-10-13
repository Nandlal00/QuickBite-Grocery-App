using Microsoft.AspNetCore.Mvc;
using GroceryStoreApp.Models;
using GroceryStoreApp.Data;

namespace GroceryStoreApp.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context) => _context = context;

        public IActionResult Dashboard()
        {
            if (Request.Cookies["AdminSession"] != "Authenticated")
                return Redirect("/?openAdminLogin=true"); // Redirect to unified login

            var orders = _context.Orders.OrderByDescending(o => o.Id).ToList();
            var products = _context.Products.OrderByDescending(p => p.Id).ToList();

            // Business Analytics Calculations (Requirement 13)
            ViewBag.Orders = orders;
            ViewBag.Products = products;
            ViewBag.TotalRevenue = orders.Sum(o => o.TotalAmount);
            ViewBag.CompletedOrders = orders.Count(o => o.Status == "Completed");
            ViewBag.ActiveOrdersCount = orders.Count(o => o.Status != "Completed" && o.Status != "Cancelled");
            ViewBag.TopSellingProduct = products.OrderByDescending(p => p.SalesCount).FirstOrDefault()?.Name ?? "None";
            ViewBag.Coupons = _context.Coupons.ToList();

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
        public async Task<IActionResult> AddProduct(Product product, IFormFile? imageFile, string? externalImageUrl)
        {
            if (Request.Cookies["AdminSession"] != "Authenticated") return Unauthorized();

            // Support both Local Desktop File Upload & External URL (Requirement 11)
            if (imageFile != null && imageFile.Length > 0)
            {
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                var fileName = Guid.NewGuid() + "_" + imageFile.FileName;
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                    await imageFile.CopyToAsync(stream);
                product.ImageUrl = "/images/products/" + fileName;
            }
            else if (!string.IsNullOrEmpty(externalImageUrl))
            {
                product.ImageUrl = externalImageUrl;
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
        public IActionResult DeleteProduct(int id)
        {
            if (Request.Cookies["AdminSession"] != "Authenticated") return Unauthorized();
            var product = _context.Products.Find(id);
            if (product != null) { _context.Products.Remove(product); _context.SaveChanges(); }
            return RedirectToAction("Dashboard");
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("AdminSession", new CookieOptions { Path = "/" });
            return RedirectToAction("Index", "Home");
        }
    }
}