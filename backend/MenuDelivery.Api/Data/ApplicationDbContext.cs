
using Microsoft.EntityFrameworkCore;
using MenuDelivery.Api.Models;

namespace MenuDelivery.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Таблицы базы данных
    public DbSet<Dish> Dishes => Set<Dish>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Настройки пользователя
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.HasIndex(u => u.Email)
                .IsUnique();
        });

        // Настройки категории
        
        // Начальный тестовый пользователь
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Email = "demo@example.com",
                PasswordHash = "AQAAAAIAAYagAAAAEHnHE/oIHDdJmZ4aZep98X39aMa2TJPKRgFwprt7+iaggHc1THVj8IfTyfhk9aik6Q==",
                CreatedAt = new DateTime(2026, 1, 1)
            }
        );
    
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);
        });

        // Настройки блюда
        modelBuilder.Entity<Dish>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(d => d.Price)
            .HasConversion<double>();

            entity.Property(d => d.ImageFileName)
                .HasMaxLength(255);

            // Одно название не может повторяться
            // у одного и того же автора
            entity.HasIndex(d => new { d.AuthorId, d.Name })
                .IsUnique();

            // Связь: одна категория — много блюд
            entity.HasOne(d => d.Category)
                .WithMany(c => c.Dishes)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Связь: один пользователь — много блюд
            entity.HasOne<User>()
                .WithMany(u => u.Dishes)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
   
        // Начальные категории
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Пицца" },
            new Category { Id = 2, Name = "Бургеры" },
            new Category { Id = 3, Name = "Напитки" },
            new Category { Id = 4, Name = "Десерты" }
        );

        // Начальные блюда
        modelBuilder.Entity<Dish>().HasData(
            new Dish
            {
                Id = 1,
                Name = "Пицца Маргарита",
                Price = 45000,
                WeightGrams = 350,
                Calories = 700,
                IsAvailable = true,
                CategoryId = 1,
                AuthorId = 1
            },
            new Dish
            {
                Id = 2,
                Name = "Пицца Пепперони",
                Price = 55000,
                WeightGrams = 400,
                Calories = 850,
                IsAvailable = true,
                CategoryId = 1,
                AuthorId = 1
            },
            new Dish
            {
                Id = 3,
                Name = "Чизбургер",
                Price = 30000,
                WeightGrams = 250,
                Calories = 600,
                IsAvailable = true,
                CategoryId = 2,
                AuthorId = 1
            },
            new Dish
            {
                Id = 4,
                Name = "Кола",
                Price = 10000,
                WeightGrams = 500,
                Calories = 210,
                IsAvailable = true,
                CategoryId = 3,
                AuthorId = 1
            },
            new Dish
            {
                Id = 5,
                Name = "Чизкейк",
                Price = 25000,
                WeightGrams = 150,
                Calories = 400,
                IsAvailable = true,
                CategoryId = 4,
                AuthorId = 1
            }
        );
 }
}
