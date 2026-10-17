using System;
using System.Collections.Generic;
using CoffeeRuu.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeRuu.Data;

public partial class CoffeeRuuDbContext : DbContext
{
    public CoffeeRuuDbContext(DbContextOptions<CoffeeRuuDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<CafeTable> CafeTables { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<InventoryTransaction> InventoryTransactions { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderItemTopping> OrderItemToppings { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<OrderVoucher> OrderVouchers { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductRecipe> ProductRecipes { get; set; }

    public virtual DbSet<ProductSize> ProductSizes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<StockReceipt> StockReceipts { get; set; }

    public virtual DbSet<StockReceiptDetail> StockReceiptDetails { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<TableReservation> TableReservations { get; set; }

    public virtual DbSet<Topping> Toppings { get; set; }

    public virtual DbSet<ToppingRecipe> ToppingRecipes { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt, "IX_AuditLogs_CreatedAt");

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Details).HasMaxLength(1000);
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.EntityName).HasMaxLength(100);

            entity.HasOne(d => d.Employee).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_AuditLogs_Employees");
        });

        modelBuilder.Entity<CafeTable>(entity =>
        {
            entity.HasKey(e => e.TableId);

            entity.HasIndex(e => e.TableName, "UQ_CafeTables_TableName").IsUnique();

            entity.Property(e => e.Capacity).HasDefaultValue(4);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.TableName).HasMaxLength(50);
            entity.Property(e => e.TableStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Available");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.CategoryName, "UQ_Categories_CategoryName").IsUnique();

            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.Username, "UQ_Employees_Username").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(120);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Role).WithMany(p => p.Employees)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employees_Roles");
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasIndex(e => e.IngredientName, "UQ_Ingredients_IngredientName").IsUnique();

            entity.Property(e => e.CurrentStock).HasColumnType("decimal(12, 3)");
            entity.Property(e => e.IngredientName).HasMaxLength(120);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MinimumStock).HasColumnType("decimal(12, 3)");
            entity.Property(e => e.Unit).HasMaxLength(30);
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasIndex(e => new { e.IngredientId, e.CreatedAt }, "IX_InventoryTransactions_Ingredient_Date");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Note).HasMaxLength(300);
            entity.Property(e => e.QuantityChange).HasColumnType("decimal(12, 3)");
            entity.Property(e => e.ReferenceType).HasMaxLength(40);
            entity.Property(e => e.TransactionType).HasMaxLength(30);

            entity.HasOne(d => d.Employee).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_InventoryTransactions_Employees");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.InventoryTransactions)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InventoryTransactions_Ingredients");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => new { e.CreatedAt, e.OrderStatus }, "IX_Orders_CreatedAt_Status");

            entity.HasIndex(e => e.OrderCode, "UQ_Orders_OrderCode").IsUnique();

            entity.Property(e => e.CompletedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.OrderCode).HasMaxLength(30);
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(25)
                .HasDefaultValue("New");
            entity.Property(e => e.OrderType)
                .HasMaxLength(20)
                .HasDefaultValue("DineIn");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(14, 2)");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Employees");

            entity.HasOne(d => d.Table).WithMany(p => p.Orders)
                .HasForeignKey(d => d.TableId)
                .HasConstraintName("FK_Orders_CafeTables");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasIndex(e => e.OrderId, "IX_OrderItems_OrderId");

            entity.Property(e => e.LineTotal).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.Note).HasMaxLength(300);
            entity.Property(e => e.PreparationStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Queued");
            entity.Property(e => e.ProductNameSnapshot).HasMaxLength(150);
            entity.Property(e => e.SizeNameSnapshot).HasMaxLength(30);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Orders");

            entity.HasOne(d => d.ProductSize).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductSizeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_ProductSizes");
        });

        modelBuilder.Entity<OrderItemTopping>(entity =>
        {
            entity.Property(e => e.ExtraPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.ToppingNameSnapshot).HasMaxLength(100);

            entity.HasOne(d => d.OrderItem).WithMany(p => p.OrderItemToppings)
                .HasForeignKey(d => d.OrderItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItemToppings_OrderItems");

            entity.HasOne(d => d.Topping).WithMany(p => p.OrderItemToppings)
                .HasForeignKey(d => d.ToppingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItemToppings_Toppings");
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("OrderStatusHistory");

            entity.Property(e => e.ChangedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.NewStatus).HasMaxLength(25);
            entity.Property(e => e.Note).HasMaxLength(300);
            entity.Property(e => e.OldStatus).HasMaxLength(25);

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.ChangedBy)
                .HasConstraintName("FK_OrderStatusHistory_Employees");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderStatusHistories)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderStatusHistory_Orders");
        });

        modelBuilder.Entity<OrderVoucher>(entity =>
        {
            entity.HasKey(e => new { e.OrderId, e.VoucherId });

            entity.Property(e => e.DiscountApplied).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderVouchers)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderVouchers_Orders");

            entity.HasOne(d => d.Voucher).WithMany(p => p.OrderVouchers)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderVouchers_Vouchers");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => new { e.OrderId, e.PaymentStatus }, "IX_Payments_OrderId_Status");

            entity.Property(e => e.Amount).HasColumnType("decimal(14, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Note).HasMaxLength(300);
            entity.Property(e => e.PaidAt).HasPrecision(0);
            entity.Property(e => e.PaymentMethod).HasMaxLength(20);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TransactionRef).HasMaxLength(120);

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Orders");

            entity.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ProcessedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Employees");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_Products_CategoryId");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.ProductName).HasMaxLength(150);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Categories");
        });

        modelBuilder.Entity<ProductRecipe>(entity =>
        {
            entity.HasKey(e => new { e.ProductSizeId, e.IngredientId });

            entity.Property(e => e.QuantityUsed).HasColumnType("decimal(12, 3)");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.ProductRecipes)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductRecipes_Ingredients");

            entity.HasOne(d => d.ProductSize).WithMany(p => p.ProductRecipes)
                .HasForeignKey(d => d.ProductSizeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductRecipes_ProductSizes");
        });

        modelBuilder.Entity<ProductSize>(entity =>
        {
            entity.HasIndex(e => new { e.ProductId, e.SizeName }, "UQ_ProductSizes_Product_Size").IsUnique();

            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.SizeName).HasMaxLength(30);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductSizes)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductSizes_Products");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleName, "UQ_Roles_RoleName").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<StockReceipt>(entity =>
        {
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.ReceiptDate)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(14, 2)");

            entity.HasOne(d => d.ReceivedByNavigation).WithMany(p => p.StockReceipts)
                .HasForeignKey(d => d.ReceivedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockReceipts_Employees");

            entity.HasOne(d => d.Supplier).WithMany(p => p.StockReceipts)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockReceipts_Suppliers");
        });

        modelBuilder.Entity<StockReceiptDetail>(entity =>
        {
            entity.Property(e => e.Quantity).HasColumnType("decimal(12, 3)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.StockReceiptDetails)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockReceiptDetails_Ingredients");

            entity.HasOne(d => d.StockReceipt).WithMany(p => p.StockReceiptDetails)
                .HasForeignKey(d => d.StockReceiptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockReceiptDetails_Receipts");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.SupplierName).HasMaxLength(150);
        });

        modelBuilder.Entity<TableReservation>(entity =>
        {
            entity.HasKey(e => e.ReservationId);

            entity.Property(e => e.CustomerName).HasMaxLength(120);
            entity.Property(e => e.CustomerPhone).HasMaxLength(20);
            entity.Property(e => e.Note).HasMaxLength(300);
            entity.Property(e => e.ReservationAt).HasPrecision(0);
            entity.Property(e => e.ReservationStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Booked");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TableReservations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TableReservations_Employees");

            entity.HasOne(d => d.Table).WithMany(p => p.TableReservations)
                .HasForeignKey(d => d.TableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TableReservations_Tables");
        });

        modelBuilder.Entity<Topping>(entity =>
        {
            entity.HasIndex(e => e.ToppingName, "UQ_Toppings_ToppingName").IsUnique();

            entity.Property(e => e.ExtraPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.ToppingName).HasMaxLength(100);
        });

        modelBuilder.Entity<ToppingRecipe>(entity =>
        {
            entity.HasKey(e => new { e.ToppingId, e.IngredientId });

            entity.Property(e => e.QuantityUsed).HasColumnType("decimal(12, 3)");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.ToppingRecipes)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ToppingRecipes_Ingredients");

            entity.HasOne(d => d.Topping).WithMany(p => p.ToppingRecipes)
                .HasForeignKey(d => d.ToppingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ToppingRecipes_Toppings");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasIndex(e => e.VoucherCode, "UQ_Vouchers_VoucherCode").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.DiscountType).HasMaxLength(10);
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.EndAt).HasPrecision(0);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaximumDiscount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.MinimumOrder).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.StartAt).HasPrecision(0);
            entity.Property(e => e.VoucherCode).HasMaxLength(40);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
