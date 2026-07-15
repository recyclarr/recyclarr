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

    [TestCase("hd-bluray-web")]
    [TestCase("anime_remux_1080p")]
    [TestCase("my-template-2")]
    public void Valid_id_is_accepted(string id)
    {
        var fs = CreateFs();
        var root = fs.DirectoryInfo.New("/provider/root");
        var entry = new TemplateEntry(id, "valid.yml");

        var result = TemplateMetadata.From(entry, root);

        result.Id.Should().Be(id);
    }

    [TestCase("templates/valid.yml")]
    [TestCase("valid.yml")]
    public void Valid_template_path_is_accepted(string template)
    {
        var fs = CreateFs();
        var root = fs.DirectoryInfo.New("/provider/root");
        var entry = new TemplateEntry("my-template", template);

        var result = TemplateMetadata.From(entry, root);

        result.TemplateFile.FullName.Should().StartWith(root.FullName);
    }

    [TestCase("../../etc/passwd")]
    [TestCase("../outside")]
    [TestCase("templates/../../outside")]
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

    [TestCase("../escape")]
    [TestCase("sub/dir")]
    [TestCase("back\\slash")]
    [TestCase("has spaces")]
    [TestCase("has.dots")]
    public void Id_with_invalid_characters_throws(string id)
    {
        var fs = CreateFs();
        var root = fs.DirectoryInfo.New("/provider/root");
        var entry = new TemplateEntry(id, "valid.yml");

        var act = () => TemplateMetadata.From(entry, root);

        act.Should().Throw<InvalidDataException>().WithMessage("*contains invalid characters*");
    }
}
