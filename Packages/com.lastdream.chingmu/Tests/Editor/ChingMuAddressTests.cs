using ChingMU;
using NUnit.Framework;

public class ChingMuAddressTests
{
    [TestCase("MCServer@127.0.0.1", 3883, "MCServer@127.0.0.1:3883")]
    [TestCase("MCAvatar@127.0.0.1:1234", 3883, "MCAvatar@127.0.0.1:3883")]
    [TestCase("127.0.0.1", 3883, "127.0.0.1:3883")]
    public void BuildProducesOnePort(string address, int port, string expected)
    {
        Assert.That(ChingMuAddress.Build(address, port), Is.EqualTo(expected));
    }

    [Test]
    public void HostRemovesProtocolAndPort()
    {
        Assert.That(ChingMuAddress.Host("MCServer@192.168.1.5:3883"), Is.EqualTo("192.168.1.5"));
    }

    [Test]
    public void ServerTypeUsesAddressPrefix()
    {
        Assert.That(ChingMuAddress.ServerType("MCAvatar@127.0.0.1"), Is.EqualTo(CMPluginAPI.CMServerType.MCAvatar));
        Assert.That(ChingMuAddress.ServerType("MCServer@127.0.0.1"), Is.EqualTo(CMPluginAPI.CMServerType.MCServer));
    }

    [Test]
    public void ConfiguredHostKeepsCurrentServerPrefix()
    {
        Assert.That(
            ChingMuAddress.ApplyConfiguredHost("MCServer@127.0.0.1", "192.168.1.5"),
            Is.EqualTo("MCServer@192.168.1.5"));
        Assert.That(
            ChingMuAddress.ApplyConfiguredHost("MCServer@127.0.0.1", "MCAvatar@192.168.1.5"),
            Is.EqualTo("MCAvatar@192.168.1.5"));
    }

    [Test]
    public void LegacyBodyListMigratesWhenBodiesIsEmpty()
    {
        CMUTrackerPreset<int> preset = new CMUTrackerPreset<int>();
        preset.bodiesID.Add(12);

        preset.EnsureCollections();

        Assert.That(preset.Bodies, Is.EqualTo(new[] { 12 }));
    }
}
