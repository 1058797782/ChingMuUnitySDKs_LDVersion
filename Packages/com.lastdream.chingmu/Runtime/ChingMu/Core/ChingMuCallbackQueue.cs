using System;
using System.Collections.Generic;

internal sealed class ChingMuCallbackQueue
{
    private readonly object syncRoot = new object();
    private readonly Queue<Action> actions = new Queue<Action>();

    internal void Enqueue(Action action)
    {
        if (action == null)
        {
            return;
        }

        lock (syncRoot)
        {
            actions.Enqueue(action);
        }
    }

    internal void Drain(int maximumActions = 256)
    {
        for (int count = 0; count < maximumActions; count++)
        {
            Action action;
            lock (syncRoot)
            {
                if (actions.Count == 0)
                {
                    return;
                }

                action = actions.Dequeue();
            }

            action();
        }
    }

    internal void Clear()
    {
        lock (syncRoot)
        {
            actions.Clear();
        }
    }
}
