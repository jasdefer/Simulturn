using FluentValidation;

namespace SimulturnApplication.Commands.Game.CreateGame;
public class CreateGameRequestValidator : AbstractValidator<CreateGameRequest>  
{
    public CreateGameRequestValidator()
    {
        RuleForEach(x => x.PlayerNames)
            .NotEmpty();
        RuleFor(x => x.PlayerNames)
            .Must(x => x.Distinct().Count() == x.Count());
    }
}
