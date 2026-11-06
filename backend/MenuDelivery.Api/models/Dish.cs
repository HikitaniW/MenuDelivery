
namespace MenuDelivery.Api.Models;

public class Dish
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int WeightGrams { get; set; }

    public int Calories { get; set; }

    public bool IsAvailable { get; set; }

    // Связь с категорией
    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    // Автор блюда
    public int AuthorId { get; set; }

    // Имя загруженного файла фотографии
    public string? ImageFileName { get; set; }
}
