using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

internal static class ChingMuCallbackRegistry
{
    private static readonly object SyncRoot = new object();
    private static readonly Dictionary<long, WeakReference> Targets = new Dictionary<long, WeakReference>();
    private static long nextToken;

    internal static IntPtr Register(object target)
    {
        long token = Interlocked.Increment(ref nextToken);
        lock (SyncRoot)
        {
            Targets[token] = new WeakReference(target);
        }

        return new IntPtr(token);
    }

    internal static bool TryGet<T>(IntPtr token, out T target) where T : class
    {
        target = null;
        if (token == IntPtr.Zero)
        {
            return false;
        }

        lock (SyncRoot)
        {
            WeakReference reference;
            if (!Targets.TryGetValue(token.ToInt64(), out reference))
            {
                return false;
            }

            target = reference.Target as T;
            if (target != null)
            {
                return true;
            }

            Targets.Remove(token.ToInt64());
            return false;
        }
    }

    internal static void Unregister(IntPtr token)
    {
        if (token == IntPtr.Zero)
        {
            return;
        }

        lock (SyncRoot)
        {
            Targets.Remove(token.ToInt64());
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        lock (SyncRoot)
        {
            Targets.Clear();
        }
    }
}
