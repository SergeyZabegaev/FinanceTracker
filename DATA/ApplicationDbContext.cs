using FinanceTracker.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Budget> Budgets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);   // <-- ОБЯЗАТЕЛЬНО: настройка Identity-таблиц

        // Начальные данные (Сидинг) категорий
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Зарплата", Type = TransactionType.Income },
            new Category { Id = 2, Name = "Фриланс", Type = TransactionType.Income },
            new Category { Id = 3, Name = "Еда", Type = TransactionType.Expense },
            new Category { Id = 4, Name = "Транспорт", Type = TransactionType.Expense },
            new Category { Id = 5, Name = "Развлечения", Type = TransactionType.Expense },
            new Category { Id = 6, Name = "Коммунальные услуги", Type = TransactionType.Expense }
        );

        // === Бюджеты ===
        modelBuilder.Entity<Budget>()
            .HasIndex(b => new { b.CategoryId, b.Year, b.Month })
            .IsUnique();

        modelBuilder.Entity<Budget>()
            .HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}