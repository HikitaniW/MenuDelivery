using FluentValidation;
using MenuDelivery.Api.DTOs;

namespace MenuDelivery.Api.Validators;

public class DishValidator : AbstractValidator<CreateDishDto>
{
    public DishValidator()
    {
        RuleFor(d => d.Name)
            .NotEmpty()
            .WithMessage("Название блюда обязательно.")
            .MaximumLength(100)
            .WithMessage("Название не должно превышать 100 символов.");

        RuleFor(d => d.Price)
            .GreaterThan(0)
            .WithMessage("Цена должна быть больше 0.");

        RuleFor(d => d.WeightGrams)
            .GreaterThan(0)
            .WithMessage("Вес должен быть больше 0 граммов.");

        RuleFor(d => d.Calories)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Калории не могут быть отрицательными.");

        RuleFor(d => d.CategoryId)
            .GreaterThan(0)
            .WithMessage("Необходимо выбрать категорию.");
    }
}