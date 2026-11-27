using Recyclarr.ConfigTemplates;

namespace Recyclarr.Core.Tests.ConfigTemplates;

internal sealed class TemplateMetadataTest
{
    private static MockFileSystem CreateFs()
    {
        var fs = new MockFileSystem();
        fs.AddDirectory("/provider/root/templates");
        fs.AddEmptyFile("/provider/root/templates/valid.yml");
        fs.AddEmptyFile("/provider/root/valid.yml");
        return fs;
    }

    [Test]
    [Arguments("hd-bluray-web")]
    [Arguments("anime_remux_1080p")]
    [Arguments("my-template-2")]
    public void Valid_id_is_accepted(string id)
    {
        var fs = CreateFs();
        var root = fs.DirectoryInfo.New("/provider/root");
        var entry = new TemplateEntry(id, "valid.yml");

        var result = TemplateMetadata.From(entry, root);

        result.Id.Should().Be(id);
    }

    [Test]
    [Arguments("templates/valid.yml")]
    [Arguments("valid.yml")]
    public void Valid_template_path_is_accepted(string template)
    {
        var fs = CreateFs();
        var root = fs.DirectoryInfo.New("/provider/root");
        var entry = new TemplateEntry("my-template", template);

        var result = TemplateMetadata.From(entry, root);

        result.TemplateFile.FullName.Should().StartWith(root.FullName);
    }

    [Test]
    [Arguments("../../etc/passwd")]
    [Arguments("../outside")]
    [Arguments("templates/../../outside")]
    public void Template_path_traversal_throws(string template)
    {
        var fs = CreateFs();
        var root = fs.DirectoryInfo.New("/provider/root");
        var entry = new TemplateEntry("my-template", template);

        var act = () => TemplateMetadata.From(entry, root);

        act.Should()
            .Throw<InvalidDataException>()
            .WithMessage("*absolute or contains path traversal*");
    }

    [Test]
    public void Absolute_template_path_throws()
    {
        var fs = CreateFs();
        var root = fs.DirectoryInfo.New("/provider/root");
        var entry = new TemplateEntry("my-template", "/etc/passwd");

        var act = () => TemplateMetadata.From(entry, root);

        act.Should()
            .Throw<InvalidDataException>()
            .WithMessage("*absolute or contains path traversal*");
    }

    [Test]
    [Arguments("../escape")]
    [Arguments("sub/dir")]
    [Arguments("back\\slash")]
    [Arguments("has spaces")]
    [Arguments("has.dots")]
    public void Id_with_invalid_characters_throws(string id)
    {
        var fs = CreateFs();
        var root = fs.DirectoryInfo.New("/provider/root");
        var entry = new TemplateEntry(id, "valid.yml");

        var act = () => TemplateMetadata.From(entry, root);

        act.Should().Throw<InvalidDataException>().WithMessage("*contains invalid characters*");
    }
}
