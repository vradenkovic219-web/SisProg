using System.ComponentModel;

public class Cache
{
    private readonly int _capacity;
    private readonly Dictionary<string, (int value, DateTime lastAccess)> _dict;
    private Queue<string> _queue;

    private readonly TimeSpan _expires;

    private readonly ReaderWriterLockSlim _lock = new();
    public Cache(int max, TimeSpan expires)
    {
        _expires = expires;
        _capacity = max;
        _dict = new Dictionary<string, (int,DateTime)>();
        _queue = new Queue<string>(_capacity);
    }   

    public int Get(string filename)
    {
        _lock.EnterReadLock();
        try
        {
            if(_dict.TryGetValue(filename, out var count)) 
                return count.value; 
            return -1;
        }
        finally
        {
            _lock.ExitReadLock();
        }  
    }

    public void Set(string filename, int count)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_queue.Count == _capacity)
            {
                string key = _queue.Dequeue();
                _dict.Remove(key);
            }
            _queue.Enqueue(filename);
            _dict[filename] = (count, DateTime.Now);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Cleanup()
    {
        _lock.EnterReadLock();
        try
        {
            var expired = _dict.Where(kvp=> DateTime.Now - kvp.Value.lastAccess > _expires).Select(kvp=>kvp.Key).ToList();
            expired.ForEach(key =>
            {
                _dict.Remove(key);
            });
            _queue = new Queue<string>(_queue.Where(node => _dict.ContainsKey(node)));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

}