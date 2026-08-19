using System;
using NUnit.Framework;

public class ChingMuCallbackRegistryTests
{
    [Test]
    public void RegisterLookupAndUnregisterDoNotRetainAStaleTarget()
    {
        object expected = new object();
        IntPtr token = ChingMuCallbackRegistry.Register(expected);

        object actual;
        Assert.That(ChingMuCallbackRegistry.TryGet(token, out actual), Is.True);
        Assert.That(actual, Is.SameAs(expected));

        ChingMuCallbackRegistry.Unregister(token);
        Assert.That(ChingMuCallbackRegistry.TryGet(token, out actual), Is.False);
        Assert.That(actual, Is.Null);
    }
}
