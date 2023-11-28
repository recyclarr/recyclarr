using Recyclarr.Common.Extensions;

namespace Recyclarr.Tests.Common.Extensions;

[TestFixture]
public class DictionaryExtensionsTest
{
    private sealed class MySampleValue;

    [Test]
    public void Create_item_if_none_exists()
    {
        var dict = new Dictionary<int, MySampleValue>();
        var theValue = dict.GetOrCreate(100);
        dict.Should().ContainSingle();
        dict.Should().Contain(100, theValue);
    }

    [Test]
    public void Return_default_if_no_item_exists()
    {
        var sample = new MySampleValue();
        var dict = new Dictionary<int, MySampleValue> {{100, sample}};

        var theValue = dict.GetValueOrDefault(200);

        dict.Should().HaveCount(1).And.Contain(100, sample);
        theValue.Should().BeNull();
    }

    [Test]
    public void Return_existing_item_if_exists_not_create()
    {
        var sample = new MySampleValue();
        var dict = new Dictionary<int, MySampleValue> {{100, sample}};

        var theValue = dict.GetOrCreate(100);
        dict.Should().ContainSingle();
        dict.Should().Contain(100, sample);
        dict.Should().ContainValue(theValue);
        theValue.Should().Be(sample);
    }

    [Test]
    public void Return_existing_item_if_it_exists_not_default()
    {
        var sample = new MySampleValue();
        var dict = new Dictionary<int, MySampleValue> {{100, sample}};

        var theValue = dict.GetValueOrDefault(100);

        // Ensure the container hasn't been mutated
        dict.Should().ContainSingle().And.Contain(100, sample);
        theValue.Should().Be(sample);
    }
}
