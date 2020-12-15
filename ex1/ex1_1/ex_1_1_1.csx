#!/usr/bin/env dotnet-script
using System.Threading;

public class ChopStick
{
    private int picked = 0;
    public int Id;

    public ChopStick(int id)
    {
        Id = id;
    }

    public bool Use() => Interlocked.CompareExchange(ref picked, 1, 0) == 0;

    public bool Release() => Interlocked.CompareExchange(ref picked, 0, 1) == 1;
}

var rand = new Random();

// 5つ箸を生成
var chopSticks = Enumerable.Range(1, 5).Select(i => new ChopStick(i)).ToArray();

var philosophers = Enumerable.Range(1, 5).Select(threadId => Task.Factory.StartNew(async (id) =>
{
    // 食事 => 考えるを 5回繰り返すとする。
    for (var i = 0; i < 5; i++)
    {
        var held = new List<ChopStick>();

        // TODO: 全員が一気に１つずつ箸をとるのでDeadLockになる。
        while (true)
        {
            foreach (var chopStick in chopSticks)
            {
                // 箸をとる
                if (chopStick.Use())
                {
                    held.Add(chopStick);
                    Console.WriteLine($"ThreadId: {id} hold ChopStick: {chopStick.Id}");
                }
            }

            // 2つ確保できたら食事開始。
            if (held.Count >= 2) break;
        }

        // 食事をとるのをシミュレート (1 ~ 10秒の間)
        await Task.Delay(TimeSpan.FromSeconds(1) * rand.Next(1, 10));

        // 箸をリリースする。
        foreach (var chopStick in held)
        {
            chopStick.Release();
            Console.WriteLine($"ThreadId: {id} release ChopStick: {chopStick.Id}");
        }

        // 考えをシミュレート (1 ~ 10秒の間)
        await Task.Delay(TimeSpan.FromSeconds(1) * rand.Next(1, 10));
    }
}, threadId).Unwrap()).ToArray();

await Task.WhenAll(philosophers);

System.Console.WriteLine("complete.");