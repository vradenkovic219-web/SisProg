public class LRUCache
{
    private readonly Dictionary<string, LinkedListNode<(string key, int value)>> kes;
    private readonly LinkedList<(string key, int value)> LRU;
    private readonly int capacity;

    private readonly object lockObject = new object();

    public LRUCache(int Capacity)
    {
        capacity = Capacity;
        kes = new Dictionary<string, LinkedListNode<(string key, int value)>>(capacity);
        LRU = new LinkedList<(string key, int value)>();
    }

    public int? Get(string key)
    {
        lock (lockObject)
        {
            if (!kes.TryGetValue(key, out var node))
                return null;

            LRU.Remove(node);
            LRU.AddFirst(node);

            return node.Value.value;
        }
    }

    public void Set(string key, int value)
    {
        lock (lockObject)
        {
            if (kes.TryGetValue(key, out var existingNode))
            {
                LRU.Remove(existingNode);
                var updatedNode = new LinkedListNode<(string, int)>((key, value));
                LRU.AddFirst(updatedNode);
                kes[key] = updatedNode;
            }
            else
            {
                if (kes.Count >= capacity)
                {
                    var last = LRU.Last;
                    if (last != null)
                    {
                        kes.Remove(last.Value.key);
                        LRU.RemoveLast();
                    }
                }

                var newNode = new LinkedListNode<(string, int)>((key, value));
                LRU.AddFirst(newNode);
                kes[key] = newNode;
            }
        }
    }
}