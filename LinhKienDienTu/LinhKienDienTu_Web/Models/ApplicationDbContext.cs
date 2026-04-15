using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using LinhKienDienTu_Web.Models;
using LinhKienDienTu_Web.Helpers;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) 
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<SubCategory> SubCategories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> Order_Details { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<Voucher> Vouchers { get; set; }
    public DbSet<NewsletterSubscriber> NewsletterSubscribers { get; set; }
    public DbSet<SearchHistory> SearchHistories { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<GiftRule> GiftRules { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<ComboDetail> ComboDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TABLE NAME MAPPING
        modelBuilder.Entity<Category>().ToTable("Category");
        modelBuilder.Entity<SubCategory>().ToTable("Sub_Category");
        modelBuilder.Entity<Product>().ToTable("Product");
        modelBuilder.Entity<Order>().ToTable("Order");
        modelBuilder.Entity<OrderDetail>().ToTable("Order_Detail", tb => tb.HasTrigger("Calculate_Thanh_Tien"));
        modelBuilder.Entity<User>().ToTable("User");
        modelBuilder.Entity<Promotion>().ToTable("Promotion");
        modelBuilder.Entity<Cart>().ToTable("Cart");
        modelBuilder.Entity<NewsletterSubscriber>().ToTable("Newsletter_Subscriber");
        modelBuilder.Entity<SearchHistory>().ToTable("SearchHistory");
        modelBuilder.Entity<Voucher>().ToTable("Voucher");
        modelBuilder.Entity<ChatSession>().ToTable("ChatSession");
        modelBuilder.Entity<ChatMessage>().ToTable("ChatMessage");
        modelBuilder.Entity<GiftRule>().ToTable("Gift_Rule");
        modelBuilder.Entity<Wishlist>().ToTable("Wishlist");
        modelBuilder.Entity<Review>().ToTable("Review");
        modelBuilder.Entity<ComboDetail>().ToTable("Combo_Detail");

        // PRIMARY KEY
        modelBuilder.Entity<Category>().HasKey(x => x.Category_ID);
        modelBuilder.Entity<SubCategory>().HasKey(x => x.SubCategory_ID);
        modelBuilder.Entity<Product>().HasKey(x => x.Product_ID);
        modelBuilder.Entity<Order>().HasKey(x => x.Order_ID);
        modelBuilder.Entity<OrderDetail>().HasKey(x => x.OrderDetail_ID);
        modelBuilder.Entity<User>().HasKey(x => x.User_ID);
        modelBuilder.Entity<Promotion>().HasKey(x => x.Promotion_ID);
        modelBuilder.Entity<Cart>().HasKey(x => x.Cart_ID);
        modelBuilder.Entity<GiftRule>().HasKey(x => x.GiftRule_ID);
        modelBuilder.Entity<Wishlist>().HasKey(x => x.Wishlist_ID);
        modelBuilder.Entity<Review>().HasKey(x => x.Review_ID);
        modelBuilder.Entity<ComboDetail>().HasKey(x => new { x.Combo_ID, x.Product_ID });

        // RELATIONSHIP
        modelBuilder.Entity<Wishlist>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.User_ID)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Wishlist>()
            .HasOne(w => w.Product)
            .WithMany()
            .HasForeignKey(w => w.Product_ID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.User_ID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.Product_ID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SubCategory>()
            .HasOne(s => s.Category)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(s => s.Category_ID);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.SubCategory)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SubCategory_ID);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Promotion)
            .WithMany()
            .HasForeignKey(p => p.Promotion_ID);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.User_ID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cart>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.User_ID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cart>()
            .HasOne(c => c.Product)
            .WithMany()
            .HasForeignKey(c => c.Product_ID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cart>()
            .HasIndex(c => new { c.User_ID, c.Product_ID })
            .IsUnique();

        modelBuilder.Entity<Voucher>().ToTable("Voucher");

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Voucher)
            .WithMany()
            .HasForeignKey(o => o.Voucher_ID)
            .OnDelete(DeleteBehavior.SetNull);

        // Cấu hình explicit FK cho OrderDetail để tránh shadow property Order_ID1 / Product_ID1
        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Order)
            .WithMany(o => o.OrderDetails)
            .HasForeignKey(od => od.Order_ID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Product)
            .WithMany()
            .HasForeignKey(od => od.Product_ID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SearchHistory>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.User_ID)
            .OnDelete(DeleteBehavior.Cascade);

        // GIFT RULE - Avoid multiple cascade paths
        modelBuilder.Entity<GiftRule>()
            .HasOne(g => g.MainProduct)
            .WithMany()
            .HasForeignKey(g => g.MainProduct_ID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GiftRule>()
            .HasOne(g => g.GiftProduct)
            .WithMany()
            .HasForeignKey(g => g.GiftProduct_ID)
            .OnDelete(DeleteBehavior.Restrict);

        // COMBO DETAIL RELATIONSHIPS
        modelBuilder.Entity<ComboDetail>()
            .HasOne(cd => cd.ComboProduct)
            .WithMany(p => p.ComboDetails)
            .HasForeignKey(cd => cd.Combo_ID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ComboDetail>()
            .HasOne(cd => cd.ComponentProduct)
            .WithMany()
            .HasForeignKey(cd => cd.Product_ID)
            .OnDelete(DeleteBehavior.Restrict);

        // DECIMAL PRECISION
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        // PII ENCRYPTION (AES-256)
        var encryptionConverter = new ValueConverter<string, string>(
            v => EncryptionHelper.Encrypt(v),
            v => EncryptionHelper.Decrypt(v));

        modelBuilder.Entity<User>()
            .Property(u => u.So_Dien_Thoai)
            .HasColumnType("nvarchar(max)")
            .HasConversion(encryptionConverter);

        modelBuilder.Entity<Order>()
            .Property(o => o.Dia_Chi_Giao_Hang)
            .HasColumnType("nvarchar(max)")
            .HasConversion(encryptionConverter);
    }
}
