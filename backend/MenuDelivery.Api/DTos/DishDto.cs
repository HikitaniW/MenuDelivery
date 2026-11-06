
namespace MenuDelivery.Api.DTOs;

public record DishDto(
    int Id,
    string Name,
    decimal Price,
    int WeightGrams,
    int Calories,
    bool IsAvailable,
    int CategoryId,
    string? ImageFileName,
    int AuthorId
);

public record CreateDishDto(
    string Name,
    decimal Price,
    int WeightGrams,
    int Calories,
    bool IsAvailable,
    int CategoryId,
    string? ImageFileName
);

public record UpdateDishDto(
    string Name,
    decimal Price,
    int WeightGrams,
    int Calories,
    bool IsAvailable,
    int CategoryId,
    string? ImageFileName
);
