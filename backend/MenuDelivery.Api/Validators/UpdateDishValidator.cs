
using FluentValidation;
using MenuDelivery.Api.DTOs;

namespace MenuDelivery.Api.Validators;

public class UpdateDishValidator : AbstractValidator<UpdateDishDto>
{
    public UpdateDishValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название обязательно.")
            .MaximumLength(100).WithMessage("Название не должно превышать 100 символов.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Цена должна быть больше нуля.");

        RuleFor(x => x.WeightGrams)
            .GreaterThan(0).WithMessage("Вес должен быть больше нуля.");

        RuleFor(x => x.Calories)
            .GreaterThanOrEqualTo(0).WithMessage("Калории не могут быть отрицательными.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Выберите категорию.");
    }
}
