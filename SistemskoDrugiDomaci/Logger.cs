public class Logger
{
    private readonly object _lock = new();

    public void Log(string poruka)
    {
        lock (_lock)
        {
            Console.WriteLine(poruka);
        }
    }
}