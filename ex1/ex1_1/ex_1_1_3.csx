#!/usr/bin/env dotnet-script
using System.Collections.Immutable;
using System.Threading;

public class ChopSticks
{
    private bool[] chopSticks;
    private int Total;
    private object _lockObject = new();

    public ChopSticks(int total)
    {
        chopSticks = Enumerable.Range(0, total).Select(i => true).ToArray();
        Total = total;
    }

    public bool Receive(int n)
    {
        var (right, left) = ChopSticksPairs(n);

        lock (_lockObject)
        {
            if (!chopSticks[left] || !chopSticks[right]) return false;

            chopSticks[left] = false;
            chopSticks[right] = false;
        }

        System.Console.WriteLine($"ThreadId: {n} received chopSticksPair(right: {right}, left: {left})");
        return true;
    }

    public void Release(int n)
    {
        var (right, left) = ChopSticksPairs(n);

        lock (_lockObject)
        {
            chopSticks[left] = true;
            chopSticks[right] = true;
        }

        System.Console.WriteLine($"ThreadId: {n} released chopSticksPair(right: {right}, left: {left})");
    }

    private (int right, int left) ChopSticksPairs(int n) => (n, (n + 1) % Total);
}

var rand = new Random();

var chopSticks = new ChopSticks(5);

var philosophers = Enumerable.Range(0, 5).Select(threadId => Task.Factory.StartNew(async (id) =>
{
    // 食事 => 考えるを 5回繰り返すとする。
    for (var i = 0; i < 5; i++)
    {
        var thId = (int)id;

        while (!chopSticks.Receive(thId))
            // 箸を確保できなかった場合は、箸をリリースするまで待つ。
            await Task.Delay(TimeSpan.FromSeconds(2));

        // 食事をとるのをシミュレート (1 ~ 10秒の間)
        await Task.Delay(TimeSpan.FromSeconds(1) * rand.Next(1, 10));

        // 箸をリリースする。
        chopSticks.Release(thId);

        // 考えをシミュレート (1 ~ 10秒の間)
        await Task.Delay(TimeSpan.FromSeconds(1) * rand.Next(1, 10));
    }
}, threadId).Unwrap()).ToArray();

await Task.WhenAll(philosophers);

System.Console.WriteLine("complete.");