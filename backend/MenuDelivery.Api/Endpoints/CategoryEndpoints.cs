using MenuDelivery.Api.Data;
using MenuDelivery.Api.DTOs;
using MenuDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MenuDelivery.Api.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/categories");

        // GET: получить все категории
        group.MapGet("/", async (ApplicationDbContext db) =>
        {
            var categories = await db.Categories
                .Select(c => new CategoryDto(c.Id, c.Name))
                .ToListAsync();

            return Results.Ok(categories);
        });

        // GET: получить категорию по ID
        group.MapGet("/{id:int}", async (int id, ApplicationDbContext db) =>
        {
            var category = await db.Categories.FindAsync(id);

            if (category is null)
                return Results.NotFound();

            return Results.Ok(new CategoryDto(category.Id, category.Name));
        });

        // POST: создать категорию
        group.MapPost("/", async (CreateCategoryDto dto, ApplicationDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest("Название категории обязательно.");

            var category = new Category
            {
                Name = dto.Name.Trim()
            };

            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var result = new CategoryDto(category.Id, category.Name);

            return Results.Created($"/api/categories/{category.Id}", result);
        });

        // PUT: обновить категорию
        group.MapPut("/{id:int}", async (int id, UpdateCategoryDto dto, ApplicationDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest("Название категории обязательно.");

            var category = await db.Categories.FindAsync(id);

            if (category is null)
                return Results.NotFound();

            category.Name = dto.Name.Trim();

            await db.SaveChangesAsync();

            return Results.Ok(new CategoryDto(category.Id, category.Name));
        });

        // DELETE: удалить категорию
        group.MapDelete("/{id:int}", async (int id, ApplicationDbContext db) =>
        {
            var category = await db.Categories
                .Include(c => c.Dishes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category is null)
                return Results.NotFound();

            if (category.Dishes.Any())
                return Results.Conflict("Нельзя удалить категорию, пока в ней есть блюда.");

            db.Categories.Remove(category);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}