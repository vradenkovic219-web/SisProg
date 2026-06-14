global using System.Net;

class Program
{
    static async Task Main(string[] args)
    {
        var cache = new FileSystem(10, "./fajlovi");
        var queue = new SafeQueue(Environment.ProcessorCount * 2);
        var logger = new Logger();
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5050/");
        listener.Start();

        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Server radi na http://localhost:5050/");
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Pritisni ENTER za gasenje servera...");

        var prijemnaNit = new Thread(() =>
        {
            while (listener.IsListening)
            {
                try
                {
                    var context = listener.GetContext();
                    queue.Add(context);
                }
                catch (HttpListenerException)
                {
                    break;
                }
            }
            queue.Zavrsi();
        });
        prijemnaNit.Start();

        int brojWorkera = Environment.ProcessorCount;
        var workeri = Enumerable.Range(0, brojWorkera)
            .Select(_ => Task.Run(async () =>
            {
                while (true)
                {
                    var context = queue.Take();
                    if (context == null) break;

                    var filename = context.Request.Url!.AbsolutePath.TrimStart('/');

                    if (string.IsNullOrEmpty(filename))
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Nije prosledjen naziv fajla");
                        continue;
                    }

                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                   await Task.Run(async () => await cache.GetWordCount(filename))
                    .ContinueWith(t =>
                    {
                        stopwatch.Stop();
                        string vreme = stopwatch.Elapsed.TotalMilliseconds < 1
                            ? $"{stopwatch.Elapsed.TotalMicroseconds:F1}μs"
                            : $"{stopwatch.Elapsed.TotalMilliseconds:F1}ms";

                        if (t.IsFaulted)
                        {
                            logger.Log($"[{DateTime.Now:HH:mm:ss.fff}] NOT FOUND    | File: {filename,-15} | Time: {vreme,10} | Greska: {t.Exception?.InnerException?.Message}");
                            return;
                        }

                        var (count, fromCache) = t.Result;
                        string cacheStatus = fromCache ? "CACHE HIT" : "CACHE MISS";
                        logger.Log($"[{DateTime.Now:HH:mm:ss.fff}] FOUND | File: {filename,-15} | Time: {vreme,10} | Greska: {t.Exception?.InnerException?.Message}");
                    });
                }
            })).ToArray();

        _ = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                await Task.Delay(TimeSpan.FromMinutes(10));
                cache.Cleanup();
                logger.Log($"[{DateTime.Now:HH:mm:ss.fff}] {new string('-', 20)} KES OCISCEN {new string('-', 20)}");
            }
        });

        Console.ReadLine();
        listener.Stop();
        Task.WaitAll(workeri);
        Console.WriteLine("Server stopiran");
    }
}