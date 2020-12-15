#!/usr/bin/env dotnet-script
using System.Threading;

public class ChopStick
{
    private int pickedBy = int.MaxValue;
    public int Id;

    public ChopStick(int id)
    {
        Id = id;
    }

    public bool Use(int num) => Interlocked.CompareExchange(ref pickedBy, num, int.MaxValue) == int.MaxValue;

    public void Release(int num) => Interlocked.Exchange(ref pickedBy, int.MaxValue);
}

var rand = new Random();

// 5つ箸を生成
var chopSticks = Enumerable.Range(0, 5).Select(i => new ChopStick(i)).ToList();

var philosophers = Enumerable.Range(0, 5).Select(threadId => Task.Factory.StartNew(async (id) =>
{
    // 食事 => 考えるを 5回繰り返すとする。
    for (var i = 0; i < 5; i++)
    {
        var thId = (int)id;
        var held = new List<ChopStick>();

        while (true)
        {
            // n, n + 1 の箸を取りに行く。 (modulo 5 でindexをオーバーする場合は丸め込める)
            foreach (var chopStick in chopSticks.Where(x => x.Id == thId || x.Id == (thId + 1) % 5))
            {
                if (!chopStick.Use(thId))
                {
                    // 箸取れなくて、自身が奇数番目の場合は、偶数番目に譲る。
                    if (thId % 2 != 0)
                    {
                        held.ForEach(x =>
                        {
                            x.Release(thId);
                            Console.WriteLine($"ThreadId: {thId} give up ChopStick: {x.Id}");
                        });

                        held = new List<ChopStick>();
                        break;
                    }

                    // 偶数の場合は、次の物を取りに行く。
                    continue;
                }

                held.Add(chopStick);
                Console.WriteLine($"ThreadId: {thId} hold ChopStick: {chopStick.Id}");
            }

            // 2つ確保できたら食事開始。
            if (held.Count >= 2) break;

            // 箸を確保できなかった場合は、箸をリリースするまで待つ。
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // 食事をとるのをシミュレート (1 ~ 10秒の間)
        await Task.Delay(TimeSpan.FromSeconds(1) * rand.Next(1, 10));

        // 箸をリリースする。
        foreach (var chopStick in held)
        {
            chopStick.Release(thId);
            Console.WriteLine($"ThreadId: {thId} release ChopStick: {chopStick.Id}");
        }

        // 考えをシミュレート (1 ~ 10秒の間)
        await Task.Delay(TimeSpan.FromSeconds(1) * rand.Next(1, 10));
    }
}, threadId).Unwrap()).ToArray();

await Task.WhenAll(philosophers);

System.Console.WriteLine("complete.");