using FluentValidation;

namespace Recyclarr.TrashGuide.ReleaseProfile;

public class TermDataValidator : AbstractValidator<TermData>
{
    public TermDataValidator()
    {
        RuleFor(x => x.Term).NotEmpty();
    }
}

public class PreferredTermDataValidator : AbstractValidator<PreferredTermData>
{
    public PreferredTermDataValidator()
    {
        RuleFor(x => x.Terms).NotEmpty();
        RuleForEach(x => x.Terms).SetValidator(new TermDataValidator());
    }
}

public class ReleaseProfileDataValidator : AbstractValidator<ReleaseProfileData>
{
    public ReleaseProfileDataValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.TrashId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.Required.Count != 0 || x.Ignored.Count != 0 || x.Preferred.Count != 0)
            .WithMessage("Must have at least one of Required, Ignored, or Preferred terms");
    }
}
