using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Text.Json;
using GroceryStoreApp.Models;
using GroceryStoreApp.Data;

namespace GroceryStoreApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private static readonly ProductSearchTrie _searchTrie = new();

        public HomeController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
            BuildTrieIndex();
        }

        private void BuildTrieIndex()
        {
            foreach (var p in _context.Products)
            {
                _searchTrie.Insert(p.Name, p.Id);
                _searchTrie.Insert(p.Category, p.Id);
            }
        }

        public IActionResult Index(string? category, string? search, string? sort)
        {
            try
            {
                _context.VisitorLogs.Add(new VisitorLog { IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1" });
                _context.SaveChanges();
            }
            catch { }

            var products = _context.Products.AsEnumerable();

            if (!string.IsNullOrEmpty(search))
            {
                var matchingIds = _searchTrie.SearchPrefix(search);
                products = products.Where(p => matchingIds.Contains(p.Id) || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                products = products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            products = sort switch
            {
                "price-low" => products.OrderBy(p => p.Price),
                "price-high" => products.OrderByDescending(p => p.Price),
                "rating" => products.OrderByDescending(p => p.Rating),
                _ => products.OrderBy(p => p.Id)
            };

            ViewBag.SelectedCategory = category ?? "All";
            ViewBag.SearchQuery = search ?? "";
            ViewBag.SelectedSort = sort ?? "";
            ViewBag.Categories = new List<string> { "All", "Fruits", "Vegetables", "Dairy", "Grocery", "Snacks" };
            ViewBag.Coupons = _context.Coupons.ToList();

            return View(products.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> Register(string name, string email, string phone, string password)
        {
            if (_context.Users.Any(u => u.Email == email || u.Phone == phone))
                return Json(new { success = false, message = "Email or phone already registered!" });

            var user = new User { Name = name, Email = email, Phone = phone, Password = password };
            _context.Users.Add(user);
            _context.SaveChanges();

            await SignInUserAsync(user.Id.ToString(), user.Name);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> LoginUser(string identifier, string password)
        {
            if ((identifier == "admin" || identifier == "admin@quickbite.com") && password == "admin123")
            {
                Response.Cookies.Append("AdminSession", "Authenticated", new CookieOptions { Expires = DateTimeOffset.UtcNow.AddHours(8), Path = "/" });
                await SignInUserAsync("99999", "Admin");
                return Json(new { success = true, isAdmin = true });
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == identifier || u.Phone == identifier);
            if (user == null || user.Password != password)
                return Json(new { success = false, message = "Invalid email/phone or password." });

            await SignInUserAsync(user.Id.ToString(), user.Name);
            return Json(new { success = true, isAdmin = false });
        }

        private async Task SignInUserAsync(string id, string name)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, id), new(ClaimTypes.Name, name) };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        }

        public async Task<IActionResult> LogoutUser()
        {
            Response.Cookies.Delete("AdminSession", new CookieOptions { Path = "/" });
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }

        public IActionResult CustomerDashboard()
        {
            if (!User.Identity?.IsAuthenticated ?? true) return RedirectToAction("Index");
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            ViewBag.User = _context.Users.Find(userId);
            ViewBag.Orders = _context.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.Id).ToList();
            ViewBag.Addresses = _context.SavedAddresses.Where(a => a.UserId == userId).ToList();
            return View();
        }

        [HttpPost]
        public IActionResult SaveAddress(string label, string fullAddress)
        {
            if (!User.Identity?.IsAuthenticated ?? true) return Json(new { success = false });
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            _context.SavedAddresses.Add(new SavedAddress { UserId = userId, Label = label, FullAddress = fullAddress });
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ValidateCoupon(string code, decimal cartTotal)
        {
            var coupon = _context.Coupons.FirstOrDefault(c => c.Code.ToUpper() == code.ToUpper());
            if (coupon == null) return Json(new { success = false, message = "Invalid coupon." });
            if (cartTotal < coupon.MinOrderAmount) return Json(new { success = false, message = $"Minimum order ₹{coupon.MinOrderAmount} required." });

            decimal discount = Math.Round(cartTotal * (coupon.DiscountPercent / 100m), 2);
            return Json(new { success = true, discount, message = $"{coupon.DiscountPercent}% discount applied!" });
        }

        [HttpGet]
        public IActionResult GetCustomerOrdersJson()
        {
            if (!User.Identity?.IsAuthenticated ?? true) return Json(new { success = false, orders = new List<object>() });
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var orders = _context.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.Id)
                .Select(o => new { id = o.Id, customerName = o.CustomerName, phone = o.Phone, address = o.Address, totalAmount = o.TotalAmount, status = o.Status, cartData = o.CartDataJson }).ToList();

            return Json(new { success = true, orders });
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrderAuthenticated([FromBody] OrderViewModel model)
        {
            if (!User.Identity?.IsAuthenticated ?? true) return Json(new { success = false, message = "Authentication required." });
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                var items = JsonSerializer.Deserialize<List<CartItemDto>>(model.CartDataJson) ?? new();
                foreach (var item in items)
                {
                    var product = _context.Products.Find(item.Id);
                    if (product != null) product.Stock = Math.Max(0, product.Stock - item.Qty);
                }
            }
            catch { }

            model.UserId = userId;
            var orderId = new Random().Next(100000, 999999);
            model.CustomerName = $"#{orderId} - " + model.CustomerName;
            model.Status = "Processing";

            _context.Orders.Add(model);
            _context.SaveChanges();

            // Safe PDF Invoice & Email dispatch wrapped in try-catch so it never blocks checkout
            try
            {
                var user = _context.Users.Find(userId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var pdfBytes = PdfInvoiceService.GenerateInvoice(model, user.Email);
                    _ = _emailService.SendOrderConfirmationEmailAsync(user.Email, model, pdfBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background PDF/Email generation warning: {ex.Message}");
            }

            return Json(new { success = true, orderId });
        }

        [HttpGet]
        public IActionResult DownloadInvoice(int id)
        {
            if (!User.Identity?.IsAuthenticated ?? true) return Unauthorized();
            var order = _context.Orders.Find(id);
            if (order == null) return NotFound();

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = _context.Users.Find(userId);
            string email = user?.Email ?? "customer@quickbite.com";

            var pdfBytes = PdfInvoiceService.GenerateInvoice(order, email);
            return File(pdfBytes, "application/pdf", $"QuickBite_Tax_Invoice_{order.Id}.pdf");
        }

        [HttpPost]
        public IActionResult CancelOrder(int id)
        {
            if (!User.Identity?.IsAuthenticated ?? true) return Json(new { success = false, message = "Login required." });
            var order = _context.Orders.Find(id);
            if (order == null) return Json(new { success = false, message = "Order not found." });

            if (order.Status != "Processing")
                return Json(new { success = false, message = "Orders cannot be cancelled once packed or out for delivery." });

            order.Status = "Cancelled";
            _context.SaveChanges();
            return Json(new { success = true, message = "Order successfully cancelled." });
        }
    }
}