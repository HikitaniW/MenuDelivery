
using MenuDelivery.Api.Data;
using MenuDelivery.Api.DTOs;
using MenuDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FluentValidation;

namespace MenuDelivery.Api.Endpoints;

public static class DishEndpoints
{
    public static void MapDishEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dishes");

        // Загрузка фотографии
        group.MapPost("/upload", async (
            IFormFile file,
            IWebHostEnvironment env) =>
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest("Файл пустой.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return Results.BadRequest("Разрешены только JPG, PNG и WEBP.");

            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest("Размер файла не должен превышать 5 МБ.");

            var folder = Path.Combine(env.WebRootPath ?? "wwwroot", "images");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(folder, fileName);

            using var stream = File.Create(filePath);
            await file.CopyToAsync(stream);

            return Results.Ok(new { fileName });
        })
        .DisableAntiforgery()
        .RequireAuthorization();

        // GET: список блюд
        group.MapGet("/", async (
            ApplicationDbContext db,
            int? page,
            int? pageSize,
            int? categoryId,
            bool? isAvailable,
            decimal? maxPrice,
            string? sortBy) =>
        {
            int currentPage = Math.Max(page ?? 1, 1);
            int currentPageSize = Math.Clamp(pageSize ?? 5, 1, 50);

            var query = db.Dishes.AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(d => d.CategoryId == categoryId.Value);

            if (isAvailable.HasValue)
                query = query.Where(d => d.IsAvailable == isAvailable.Value);

            if (maxPrice.HasValue)
                query = query.Where(d => d.Price <= maxPrice.Value);

            var total = await query.CountAsync();

            if (sortBy == "price")
                query = query.OrderBy(d => (double)d.Price);
            else if (sortBy == "calories")
                query = query.OrderBy(d => d.Calories);
            else
                query = query.OrderBy(d => d.Id);

            var dishes = await query
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .Select(d => new DishDto(
                    d.Id,
                    d.Name,
                    d.Price,
                    d.WeightGrams,
                    d.Calories,
                    d.IsAvailable,
                    d.CategoryId,
                    d.ImageFileName,
                    d.AuthorId
                ))
                .ToListAsync();

            return Results.Ok(new
            {
                items = dishes,
                total,
                page = currentPage,
                pageSize = currentPageSize,
                totalPages = (int)Math.Ceiling(total / (double)currentPageSize)
            });
        });

        // GET: блюдо по ID
        group.MapGet("/{id:int}", async (
            int id,
            ApplicationDbContext db) =>
        {
            var dish = await db.Dishes
                .Where(d => d.Id == id)
                .Select(d => new DishDto(
                    d.Id,
                    d.Name,
                    d.Price,
                    d.WeightGrams,
                    d.Calories,
                    d.IsAvailable,
                    d.CategoryId,
                    d.ImageFileName,
                    d.AuthorId
                ))
                .FirstOrDefaultAsync();

            return dish is null
                ? Results.NotFound()
                : Results.Ok(dish);
        });

        // POST: создание блюда
        group.MapPost("/", async (
            CreateDishDto dto,
            ApplicationDbContext db,
            HttpContext context,
            IValidator<CreateDishDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);

            if (!validation.IsValid)
            {
                return Results.ValidationProblem(
                    validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        )
                );
            }

            var authorId = int.Parse(
                context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var categoryExists = await db.Categories
                .AnyAsync(c => c.Id == dto.CategoryId);

            if (!categoryExists)
                return Results.BadRequest("Категория не найдена.");

            var nameExists = await db.Dishes.AnyAsync(d =>
                d.AuthorId == authorId &&
                d.Name.ToLower() == dto.Name.Trim().ToLower());

            if (nameExists)
                return Results.Conflict("У вас уже есть блюдо с таким названием.");

            var dish = new Dish
            {
                Name = dto.Name.Trim(),
                Price = dto.Price,
                WeightGrams = dto.WeightGrams,
                Calories = dto.Calories,
                IsAvailable = dto.IsAvailable,
                CategoryId = dto.CategoryId,
                AuthorId = authorId,
                ImageFileName = dto.ImageFileName
            };

            db.Dishes.Add(dish);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is Microsoft.Data.Sqlite.SqliteException sqliteEx &&
                    sqliteEx.SqliteErrorCode == 19)
                    return Results.Conflict("У вас уже есть блюдо с таким названием.");

                throw;
            }

            var result = new DishDto(
                dish.Id,
                dish.Name,
                dish.Price,
                dish.WeightGrams,
                dish.Calories,
                dish.IsAvailable,
                dish.CategoryId,
                dish.ImageFileName,
                dish.AuthorId
            );

            return Results.Created($"/api/dishes/{dish.Id}", result);
        })
        .RequireAuthorization();

        // PUT: изменение блюда
        group.MapPut("/{id:int}", async (
            int id,
            UpdateDishDto dto,
            ApplicationDbContext db,
            HttpContext context,
            IValidator<UpdateDishDto> validator) =>
        {
            var validation = await validator.ValidateAsync(dto);

            if (!validation.IsValid)
            {
                return Results.ValidationProblem(
                    validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        )
                );
            }

            var dish = await db.Dishes.FindAsync(id);

            if (dish is null)
                return Results.NotFound();

            var authorId = int.Parse(
                context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (dish.AuthorId != authorId)
                return Results.Forbid();

            var categoryExists = await db.Categories
                .AnyAsync(c => c.Id == dto.CategoryId);

            if (!categoryExists)
                return Results.BadRequest("Категория не найдена.");

            var nameExists = await db.Dishes.AnyAsync(d =>
                d.AuthorId == authorId &&
                d.Id != id &&
                d.Name.ToLower() == dto.Name.Trim().ToLower());

            if (nameExists)
                return Results.Conflict("У вас уже есть блюдо с таким названием.");

            dish.Name = dto.Name.Trim();
            dish.Price = dto.Price;
            dish.WeightGrams = dto.WeightGrams;
            dish.Calories = dto.Calories;
            dish.IsAvailable = dto.IsAvailable;
            dish.CategoryId = dto.CategoryId;
            dish.ImageFileName = dto.ImageFileName;

            await db.SaveChangesAsync();

            return Results.Ok(new DishDto(
                dish.Id,
                dish.Name,
                dish.Price,
                dish.WeightGrams,
                dish.Calories,
                dish.IsAvailable,
                dish.CategoryId,
                dish.ImageFileName,
                dish.AuthorId
            ));
        })
        .RequireAuthorization();

        // DELETE: удаление блюда
        group.MapDelete("/{id:int}", async (
            int id,
            ApplicationDbContext db,
            HttpContext context) =>
        {
            var dish = await db.Dishes.FindAsync(id);

            if (dish is null)
                return Results.NotFound();

            var authorId = int.Parse(
                context.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (dish.AuthorId != authorId)
                return Results.Forbid();

            db.Dishes.Remove(dish);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization();
    }
}
