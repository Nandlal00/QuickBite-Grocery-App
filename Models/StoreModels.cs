using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Net;
using System.Net.Mail;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
        public string Label { get; set; } = "Home";
        public string FullAddress { get; set; } = string.Empty;
    }

    public class OrderViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string CouponCode { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public string CartDataJson { get; set; } = string.Empty;
        public string Status { get; set; } = "Processing";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Coupon
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public decimal MinOrderAmount { get; set; }
    }

    public class VisitorLog
    {
        public int Id { get; set; }
        public DateTime VisitTime { get; set; } = DateTime.Now;
        public string IpAddress { get; set; } = string.Empty;
    }

    public class OperatingCity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class CartItemDto
    {
        public int Id { get; set; }
        public int Qty { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    // --- HIGH-PERFORMANCE TRIE SEARCH ALGORITHM ---
    public class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; set; } = new();
        public HashSet<int> ProductIds { get; set; } = new();
    }

    public class ProductSearchTrie
    {
        private readonly TrieNode _root = new();

        public void Insert(string text, int id)
        {
            var current = _root;
            foreach (var c in text.ToLower())
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

    // --- PDF INVOICE GENERATOR SERVICE ---
// --- PDF INVOICE GENERATOR SERVICE ---
    public static class PdfInvoiceService
    {
        public static byte[] GenerateInvoice(OrderViewModel order, string customerEmail)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseSystemFonts = true; // Enables fallback to system fonts

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Lato")); // Uses Lato font

                    page.Header().Element(header =>
                    {
                        header.Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("QuickBite Mart").Bold().FontSize(20).FontColor(Colors.Green.Medium);
                                col.Item().Text("10-Minute Instant Grocery Delivery").FontSize(9).FontColor(Colors.Grey.Medium);
                                col.Item().PaddingBottom(5);
                                col.Item().Text("GSTIN: 24AAAAA0000A1Z5 | FSSAI: 10020021000555").FontSize(8).FontColor(Colors.Grey.Darken1);
                                col.Item().Text("Headquarters: Bidasar, Churu, Rajasthan, India").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });

                            row.ConstantItem(160).AlignRight().Column(col =>
                            {
                                col.Item().Text("TAX INVOICE").Bold().FontSize(14).FontColor(Colors.Grey.Darken3);
                                col.Item().Text($"Invoice #: QB-INV-{order.Id}").FontSize(9);
                                col.Item().Text($"Date: {order.CreatedAt:dd MMM yyyy HH:mm}").FontSize(9);
                                col.Item().Text($"Status: {order.Status}").FontSize(9).Bold().FontColor(Colors.Green.Darken1);
                            });
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        col.Spacing(15);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                            {
                                c.Item().Text("Billed To:").Bold().FontSize(10).FontColor(Colors.Green.Darken2);
                                c.Item().Text($"Name: {order.CustomerName}").FontSize(9);
                                c.Item().Text($"Phone: {order.Phone}").FontSize(9);
                                c.Item().Text($"Email: {customerEmail}").FontSize(9);
                            });

                            row.ConstantItem(15);

                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                            {
                                c.Item().Text("Delivery Address:").Bold().FontSize(10).FontColor(Colors.Green.Darken2);
                                c.Item().Text(order.Address).FontSize(9);
                            });
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Green.Medium).Padding(5).Text("S.No").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Item Description").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Qty").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Price (₹)").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Green.Medium).Padding(5).Text("Total (₹)").Bold().FontColor(Colors.White);
                            });

                            try
                            {
                                var items = JsonSerializer.Deserialize<List<CartItemDto>>(order.CartDataJson) ?? new();
                                int sno = 1;
                                foreach (var item in items)
                                {
                                    var itemTotal = item.Qty * item.Price;
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(sno++.ToString()).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(item.Name).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(item.Qty.ToString()).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"₹{item.Price:0.00}").FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"₹{itemTotal:0.00}").FontSize(9);
                                }
                            }
                            catch { }
                        });

                        col.Item().AlignRight().Width(260).Column(c =>
                        {
                            c.Spacing(4);
                            var subtotal = order.TotalAmount + order.DiscountAmount;
                            c.Item().Row(r => { r.RelativeItem().Text("Subtotal:"); r.ConstantItem(80).AlignRight().Text($"₹{subtotal:0.00}"); });
                            if (order.DiscountAmount > 0)
                            {
                                c.Item().Row(r => { r.RelativeItem().Text("Promo Discount:"); r.ConstantItem(80).AlignRight().Text($"-₹{order.DiscountAmount:0.00}").FontColor(Colors.Red.Medium); });
                            }
                            c.Item().Row(r => { r.RelativeItem().Text("Delivery Charges:"); r.ConstantItem(80).AlignRight().Text("FREE").FontColor(Colors.Green.Darken1); });
                            c.Item().Row(r => { r.RelativeItem().Text("CGST (2.5%):").FontSize(8).FontColor(Colors.Grey.Darken1); r.ConstantItem(80).AlignRight().Text($"₹{(order.TotalAmount * 0.025m):0.00}").FontSize(8); });
                            c.Item().Row(r => { r.RelativeItem().Text("SGST (2.5%):").FontSize(8).FontColor(Colors.Grey.Darken1); r.ConstantItem(80).AlignRight().Text($"₹{(order.TotalAmount * 0.025m):0.00}").FontSize(8); });
                            c.Item().LineHorizontal(1).LineColor(Colors.Green.Medium);
                            c.Item().Row(r => { r.RelativeItem().Text("Grand Total:").Bold().FontSize(12); r.ConstantItem(90).AlignRight().Text($"₹{order.TotalAmount:0.00}").Bold().FontSize(12).FontColor(Colors.Green.Darken2); });
                        });
                    });

                    page.Footer().AlignCenter().Text(x => { x.Span("Page "); x.CurrentPageNumber(); x.Span(" of "); x.TotalPages(); });
                });
            });

            return document.GeneratePdf();
        }
    }
    // --- EMAIL DISPATCH SERVICE ---
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOrderConfirmationEmailAsync(string recipientEmail, OrderViewModel order, byte[] pdfInvoiceBytes)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "quickbitemart.official@gmail.com";
                var senderPassword = _configuration["EmailSettings:SenderPassword"] ?? "";

                if (string.IsNullOrEmpty(senderPassword)) return;

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "QuickBite Mart"),
                    Subject = $"Order Confirmation #{order.Id} - QuickBite Mart",
                    Body = $"Dear {order.CustomerName},\n\nThank you for shopping with QuickBite Mart! Your order has been placed successfully.\n\n" +
                           $"Order ID: #{order.Id}\nTotal Amount: ₹{order.TotalAmount:0.00}\n\nYour official Indian GST Tax Invoice is attached.",
                    IsBodyHtml = false
                };

                mailMessage.To.Add(recipientEmail);

                using var ms = new MemoryStream(pdfInvoiceBytes);
                mailMessage.Attachments.Add(new Attachment(ms, $"QuickBite_Invoice_{order.Id}.pdf", "application/pdf"));

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email Error: {ex.Message}");
            }
        }
    }
}