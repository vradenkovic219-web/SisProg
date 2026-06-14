public class SafeQueue
{
    private readonly Queue<HttpListenerContext> _queue = new();
    private readonly object _lock = new();
    private readonly int _maxSize;
    private bool _zavrsen = false;

    public SafeQueue(int maxSize)
    {
        _maxSize = maxSize;
    }

    public void Add(HttpListenerContext context)
    {
        lock (_lock)
        {
            while (_queue.Count == _maxSize)
                Monitor.Wait(_lock);

            _queue.Enqueue(context);
            Monitor.PulseAll(_lock);
        }
    }

    public HttpListenerContext? Take()
    {
        lock (_lock)
        {
            while (_queue.Count == 0 && !_zavrsen)
                Monitor.Wait(_lock);

            if (_queue.Count == 0)
                return null;

            var context = _queue.Dequeue();
            Monitor.PulseAll(_lock);
            return context;
        }
    }

    public void Zavrsi()
    {
        lock (_lock)
        {
            _zavrsen = true;
            Monitor.PulseAll(_lock);
        }
    }
}