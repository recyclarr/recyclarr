using FluentAssertions.Formatting;
using Trash.Sonarr.ReleaseProfile;

namespace Trash.Tests.ValueFormatters
{
    public class ProfileDataValueFormatter : IValueFormatter
    {
        public bool CanHandle(object value)
        {
            return value is ProfileData;
        }

        public string Format(object value, FormattingContext context, FormatChild formatChild)
        {
            var profileData = (ProfileData) value;
            return $"{profileData.Ignored}";
        }
    }
}
