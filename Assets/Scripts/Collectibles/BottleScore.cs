using System;

public static class BottleScore
{
    public static event Action<int> Changed;
    public static event Action<int> Finished;

    public static int Count { get; private set; }
    public static bool IsFinished { get; private set; }

    public static void Reset()
    {
        Count = 0;
        IsFinished = false;
        Changed?.Invoke(Count);
    }

    public static void AddOne()
    {
        Count++;
        Changed?.Invoke(Count);
    }

    public static void Finish()
    {
        if (IsFinished)
            return;

        IsFinished = true;
        Finished?.Invoke(Count);
    }
}
