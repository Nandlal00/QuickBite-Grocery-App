using System.ComponentModel.DataAnnotations;

namespace GroceryStoreApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; } = 50;
        public string Unit { get; set; } = "1 kg";
        public string ImageUrl { get; set; } = string.Empty;
        public double Rating { get; set; } = 4.8;
        public int SalesCount { get; set; } = 0; // For Popularity sorting algorithm
        public string Description { get; set; } = string.Empty;
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class SavedAddress
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Label { get; set; } = "Home"; // Home, Office, Other
        public string AddressText { get; set; } = string.Empty;
    }

    public class OrderViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string CartDataJson { get; set; } = string.Empty;
        public string Status { get; set; } = "Processing"; // Processing -> Out for Delivery -> Completed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Coupon
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public decimal MinOrderAmount { get; set; }
    }

    public class WishlistItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
    }

    // --- ALGORITHMIC TRIE FOR INSTANT PREFIX SEARCH ---
    public class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; set; } = new();
        public HashSet<int> ProductIds { get; set; } = new();
    }

    public class ProductSearchTrie
    {
        private readonly TrieNode _root = new();

        public void Insert(string name, int id)
        {
            var current = _root;
            foreach (var c in name.ToLower())
            {
                if (!current.Children.ContainsKey(c))
                    current.Children[c] = new TrieNode();
                current = current.Children[c];
                current.ProductIds.Add(id);
            }
        }

        public HashSet<int> SearchPrefix(string prefix)
        {
            var current = _root;
            foreach (var c in prefix.ToLower())
            {
                if (!current.Children.ContainsKey(c))
                    return new HashSet<int>();
                current = current.Children[c];
            }
            return current.ProductIds;
        }
    }
}