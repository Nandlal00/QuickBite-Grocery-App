using GroceryStoreApp.Models;

namespace GroceryStoreApp.Data
{
    public static class ProductStore
    {
        public static List<Product> Products { get; set; } = new()
        {
            new Product { Id = 1, Name = "Fresh Organic Bananas", Category = "Fruits", Price = 45.00m, Unit = "1 kg", ImageUrl = "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=500&auto=format&fit=crop&q=60", Rating = 4.8, Description = "Sweet, energy-rich farm-fresh bananas handpicked daily." },
            new Product { Id = 2, Name = "Alphonso Mangoes", Category = "Fruits", Price = 250.00m, Unit = "1 kg", ImageUrl = "https://images.unsplash.com/photo-1553279768-865429fa0078?w=500&auto=format&fit=crop&q=60", Rating = 4.9, Description = "King of mangoes, rich in flavor and aroma." },
            new Product { Id = 3, Name = "Farm Fresh Tomatoes", Category = "Vegetables", Price = 30.00m, Unit = "500 g", ImageUrl = "https://images.unsplash.com/photo-1592924357228-91a4daadcfea?w=500&auto=format&fit=crop&q=60", Rating = 4.7, Description = "Juicy and fresh red tomatoes straight from local farms." },
            new Product { Id = 4, Name = "Fresh Green Spinach", Category = "Vegetables", Price = 20.00m, Unit = "250 g", ImageUrl = "https://images.unsplash.com/photo-1576045057995-568f588f82fb?w=500&auto=format&fit=crop&q=60", Rating = 4.6, Description = "Nutrient-packed leafy green vegetables." },
            new Product { Id = 5, Name = "Amul Butter - 500g", Category = "Dairy", Price = 275.00m, Unit = "Pack", ImageUrl = "https://images.unsplash.com/photo-1589985270826-4b7bb135bc9d?w=500&auto=format&fit=crop&q=60", Rating = 4.9, Description = "Creamy, delicious butter made from pure milk fat." },
            new Product { Id = 6, Name = "Aashirvaad Shudh Chakki Atta", Category = "Grocery", Price = 240.00m, Unit = "5 kg", ImageUrl = "https://images.unsplash.com/photo-1586201375761-83865001e31c?w=500&auto=format&fit=crop&q=60", Rating = 4.8, Description = "100% whole wheat flour for soft, fluffy rotis." },
            new Product { Id = 7, Name = "Tata Salt - Iodised", Category = "Grocery", Price = 28.00m, Unit = "1 kg", ImageUrl = "https://images.unsplash.com/photo-1518117715867-c3aed61f2f89?w=500&auto=format&fit=crop&q=60", Rating = 4.9, Description = "Essential vacuum evaporated iodised salt." },
            new Product { Id = 8, Name = "Roasted Peanuts (High Protein)", Category = "Snacks", Price = 90.00m, Unit = "400 g", ImageUrl = "https://images.unsplash.com/photo-1526406915894-7bcd65f60845?w=500&auto=format&fit=crop&q=60", Rating = 4.7, Description = "Crunchy, protein-dense roasted peanuts for clean snacking." }
        };

        public static List<OrderViewModel> Orders { get; set; } = new();
    }
}