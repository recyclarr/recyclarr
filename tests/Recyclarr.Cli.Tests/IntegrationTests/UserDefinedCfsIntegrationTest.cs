using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Tests.Reusable;

namespace Recyclarr.Cli.Tests.IntegrationTests;

[TestFixture]
internal class UserDefinedCfsIntegrationTest : CliIntegrationFixture
{
    [Test]
    public async Task Sync_user_defined_custom_formats()
    {
        // language=yaml
        const string yaml = """
            sonarr:
              sonarr_test:
                custom_formats:
                  # Scores from TRaSH json
                  - user_defined:
                      - id: 5763d1b0ce84aff3b21038eea8e9b8ad
                        name: My User Defined CF
                        include_when_renaming: false
                        # We distinquish concrete implementations of various condition types based on key
                        # discrimination. Example: Presence of `release_title` indicates this is a
                        # ReleaseTitleCondition.
                        #
                        # Every condition type supports `name`, `negate` and `required`.
                        conditions:
                          - name: My Release Title Condition # Optional; defaults to some generated name based on condition type.
                            release_title: ^some regex$ # Required
                            negate: false # defaults to false if unspecified
                            required: false # defaults to false if unspecified
                          - edition: ^another regex$ # Required
                            negate: true
                          # may also be English, Japanese, etc. There's quite a few of them so we just treat
                          # this as a string field instead of enum.
                          - language: Original # Required
                            except_language: true # Unique to 'language' conditions; defaults to false.
                          - indexer_flag: Freeleech # Required. May also be: Halfleech, DoubleUpload, Internal, Scene, Freeleech75, Freeleech25, Nuked
                          - source: Television # Required. May also be: TelevisionRaw, Web, WebRip, DVD, Bluray, BlurayRaw
                          - resolution: R360p # Required. May also be: R480p, R540p, R576p, R720p, R1080p, R2160p
                          # One of the two size keys is required. Missing one defaults to 0
                          - minimum_size: 0
                            maximum_size: 0
            """;

        var context = new CustomFormatPipelineContext();
        var settings = Substitute.For<ISyncSettings>();

        var sut = Resolve<GenericSyncPipeline<CustomFormatPipelineContext>>();

        await sut.Execute(settings, CancellationToken.None);
    }
}
