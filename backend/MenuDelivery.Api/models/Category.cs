
namespace MenuDelivery.Api.Models;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<Dish> Dishes { get; set; } = new();
}
