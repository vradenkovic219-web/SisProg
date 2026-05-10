using System.Collections.Concurrent;

public class FileSystem
{   
    private readonly LRUCache kes;
    private readonly string rootFolder;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> fileLocks;

    public FileSystem(string root, int capacity)
    {
        rootFolder = root;
        kes = new LRUCache(capacity);
        fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
    }

    public int GetWordCount(string filename)
    {

        var cached = kes.Get(filename);
        if(cached != null)
        {
            return cached.Value;
        }

        var semaphore = fileLocks.GetOrAdd(filename, _ => new SemaphoreSlim(1, 1));
        
        semaphore.Wait();
        try
        {
            // Druga provjera - sa lockom (double-checked locking)
            cached = kes.Get(filename);
            if(cached != null)
            {
                return cached.Value;
            }

            string? path = Directory.GetFiles(rootFolder, filename, SearchOption.AllDirectories).FirstOrDefault();
            if(path == null)
            {
                throw new FileNotFoundException($"File '{filename}' not found in '{rootFolder}'");
            }

            int count = CountWords(path);
            
            kes.Set(filename, count);
            
            return count;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public int CountWords(string path)
    {
        int count = 0;

        using StreamReader reader = new StreamReader(path);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            int duzina = 0;
            bool startsWithUpper = false;

            foreach (char c in line)
            {
                if (char.IsLetter(c))
                {
                    if (duzina == 0)
                        startsWithUpper = char.IsUpper(c);

                    duzina++;
                }
                else
                {
                    if (duzina > 5 && startsWithUpper)
                        count++;

                    duzina = 0;
                    startsWithUpper = false;
                }
            }

            if (duzina > 5 && startsWithUpper)
                count++;
        }

        return count;
    }

}