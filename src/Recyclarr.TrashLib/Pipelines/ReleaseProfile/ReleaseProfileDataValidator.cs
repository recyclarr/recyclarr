using FluentValidation;

namespace Recyclarr.TrashLib.Pipelines.ReleaseProfile;

internal class TermDataValidator : AbstractValidator<TermData>
{
    public TermDataValidator()
    {
        RuleFor(x => x.Term).NotEmpty();
    }
}

internal class PreferredTermDataValidator : AbstractValidator<PreferredTermData>
{
    public PreferredTermDataValidator()
    {
        RuleFor(x => x.Terms).NotEmpty();
        RuleForEach(x => x.Terms).SetValidator(new TermDataValidator());
    }
}

internal class ReleaseProfileDataValidator : AbstractValidator<ReleaseProfileData>
{
    public ReleaseProfileDataValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.TrashId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.Required.Any() || x.Ignored.Any() || x.Preferred.Any())
            .WithMessage("Must have at least one of Required, Ignored, or Preferred terms");
    }
}
