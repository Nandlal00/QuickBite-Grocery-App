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
        private static readonly ProductSearchTrie _searchTrie = new();

        public HomeController(AppDbContext context)
        {
            _context = context;
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
            var products = _context.Products.AsEnumerable();

            // Algorithmic Trie Prefix Search (Requirement 4 & 8)
            if (!string.IsNullOrEmpty(search))
            {
                var matchingIds = _searchTrie.SearchPrefix(search);
                products = products.Where(p => matchingIds.Contains(p.Id) || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                products = products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            // Advanced Sorting Algorithm (Requirement 8)
            products = sort switch
            {
                "price_low" => products.OrderBy(p => p.Price),
                "price_high" => products.OrderByDescending(p => p.Price),
                "rating" => products.OrderByDescending(p => p.Rating),
                "popularity" => products.OrderByDescending(p => p.SalesCount),
                _ => products.OrderBy(p => p.Id)
            };

            ViewBag.SelectedCategory = category ?? "All";
            ViewBag.SearchQuery = search ?? "";
            ViewBag.CurrentSort = sort ?? "default";
            ViewBag.Categories = new List<string> { "All", "Fruits", "Vegetables", "Dairy", "Grocery", "Snacks" };

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

        // Unified Login Screen Handler (Requirement 9)
        [HttpPost]
        public async Task<IActionResult> UnifiedLogin(string emailOrPhone, string password)
        {
            // Predefined Admin Credentials Check
            if (emailOrPhone.Trim().Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "admin123")
            {
                Response.Cookies.Append("AdminSession", "Authenticated", new CookieOptions { Expires = DateTimeOffset.UtcNow.AddHours(8), Path = "/" });
                return Json(new { success = true, isAdmin = true, redirectUrl = "/Admin/Dashboard" });
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == emailOrPhone || u.Phone == emailOrPhone);
            if (user == null || user.Password != password)
                return Json(new { success = false, message = "Invalid credentials or password." });

            await SignInUserAsync(user.Id.ToString(), user.Name);
            return Json(new { success = true, isAdmin = false, redirectUrl = "/Home/CustomerDashboard" });
        }

        private async Task SignInUserAsync(string userId, string userName)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId), new(ClaimTypes.Name, userName) };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        }

        public async Task<IActionResult> LogoutUser()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("AdminSession", new CookieOptions { Path = "/" });
            return RedirectToAction("Index");
        }

        public IActionResult CustomerDashboard()
        {
            if (!User.Identity?.IsAuthenticated ?? true) return RedirectToAction("Index");
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            ViewBag.User = _context.Users.Find(userId);
            ViewBag.SavedAddresses = _context.SavedAddresses.Where(a => a.UserId == userId).ToList();
            ViewBag.Wishlist = _context.Products.Where(p => _context.WishlistItems.Any(w => w.UserId == userId && w.ProductId == p.Id)).ToList();
            return View();
        }

        [HttpPost]
        public IActionResult SaveAddress(string label, string addressText)
        {
            if (!User.Identity?.IsAuthenticated ?? true) return Json(new { success = false });
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            _context.SavedAddresses.Add(new SavedAddress { UserId = userId, Label = label, AddressText = addressText });
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ValidateCoupon(string code, decimal cartTotal)
        {
            var coupon = _context.Coupons.FirstOrDefault(c => c.Code.ToUpper() == code.ToUpper());
            if (coupon == null) return Json(new { success = false, message = "Invalid coupon." });
            if (cartTotal < coupon.MinOrderAmount) return Json(new { success = false, message = $"Min order ₹{coupon.MinOrderAmount} required." });

            decimal discount = Math.Round(cartTotal * (coupon.DiscountPercent / 100m), 2);
            return Json(new { success = true, discount, message = $"{coupon.DiscountPercent}% discount applied!" });
        }

        [HttpGet]
        public IActionResult GetCustomerOrdersJson()
        {
            if (!User.Identity?.IsAuthenticated ?? true) return Json(new { success = false, orders = new List<object>() });
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var orders = _context.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.Id)
                .Select(o => new { id = o.Id, customerName = o.CustomerName, phone = o.Phone, address = o.Address, totalAmount = o.TotalAmount, status = o.Status, cartData = o.CartDataJson, createdAt = o.CreatedAt }).ToList();

            return Json(new { success = true, orders });
        }

        [HttpPost]
        public IActionResult PlaceOrderAuthenticated([FromBody] OrderViewModel model)
        {
            if (!User.Identity?.IsAuthenticated ?? true) return Json(new { success = false, message = "Authentication required." });
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                var cartItems = JsonSerializer.Deserialize<List<CartItemDto>>(model.CartDataJson) ?? new();
                foreach (var item in cartItems)
                {
                    var product = _context.Products.Find(item.Id);
                    if (product != null)
                    {
                        product.Stock = Math.Max(0, product.Stock - item.Qty);
                        product.SalesCount += item.Qty; // Increment for popularity sorting
                    }
                }
            }
            catch { }

            model.UserId = userId;
            var orderId = new Random().Next(100000, 999999);
            model.CustomerName = $"#{orderId} - " + model.CustomerName;
            model.Status = "Processing";
            model.CreatedAt = DateTime.UtcNow;

            _context.Orders.Add(model);
            _context.SaveChanges();

            return Json(new { success = true, orderId });
        }
    }

    public class CartItemDto
    {
        public int Id { get; set; }
        public int Qty { get; set; }
    }
}