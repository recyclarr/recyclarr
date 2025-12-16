using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;

namespace Recyclarr.Cli.Tests.Pipelines.MediaNaming.Config;

internal sealed class NamingFormatLookupTest
{
    private static readonly Dictionary<string, string> GuideFormats = new()
    {
        { "default", "format_default" },
        { "plex", "format_plex" },
        { "emby", "format_emby" },
        { "default:4", "format_default_v4" },
    };

    [Test]
    public void Return_null_when_config_key_null()
    {
        var sut = new NamingFormatLookup();

        var result = sut.ObtainFormat(GuideFormats, null, "Test Format");

        result.Should().BeNull();
        sut.Errors.Should().BeEmpty();
    }

    [Test]
    public void Return_format_for_valid_key()
    {
        var sut = new NamingFormatLookup();

        var result = sut.ObtainFormat(GuideFormats, "plex", "Test Format");

        result.Should().Be("format_plex");
        sut.Errors.Should().BeEmpty();
    }

    [Test]
    public void Lookup_is_case_insensitive()
    {
        var sut = new NamingFormatLookup();

        var result = sut.ObtainFormat(GuideFormats, "PLEX", "Test Format");

        result.Should().Be("format_plex");
        sut.Errors.Should().BeEmpty();
    }

    [Test]
    public void Collect_error_for_invalid_key()
    {
        var sut = new NamingFormatLookup();

        var result = sut.ObtainFormat(GuideFormats, "invalid_key", "Test Format");

        result.Should().BeNull();
        sut.Errors.Should().BeEquivalentTo([new InvalidNamingEntry("Test Format", "invalid_key")]);
    }

    [Test]
    public void Prefer_suffixed_key_when_suffix_provided()
    {
        var sut = new NamingFormatLookup();

        var result = sut.ObtainFormat(GuideFormats, "default", ":4", "Test Format");

        result.Should().Be("format_default_v4");
        sut.Errors.Should().BeEmpty();
    }

    [Test]
    public void Fall_back_to_base_key_when_suffixed_key_not_found()
    {
        var sut = new NamingFormatLookup();

        var result = sut.ObtainFormat(GuideFormats, "plex", ":4", "Test Format");

        result.Should().Be("format_plex");
        sut.Errors.Should().BeEmpty();
    }

    [Test]
    public void Collect_multiple_errors()
    {
        var sut = new NamingFormatLookup();

        sut.ObtainFormat(GuideFormats, "bad1", "Format A");
        sut.ObtainFormat(GuideFormats, "bad2", "Format B");
        sut.ObtainFormat(GuideFormats, "bad3", "Format C");

        sut.Errors.Should()
            .BeEquivalentTo([
                new InvalidNamingEntry("Format A", "bad1"),
                new InvalidNamingEntry("Format B", "bad2"),
                new InvalidNamingEntry("Format C", "bad3"),
            ]);
    }
}
