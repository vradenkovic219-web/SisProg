using System.IO;
public class FileSystem
{
    private readonly Cache _cache;
    private readonly Dictionary<string, SemaphoreSlim> _fileLocks = new();
    private readonly ReaderWriterLockSlim _entryLock = new(); 
    private readonly string _rootFolder;

    public FileSystem(int maxSize, string rootFolder)
    {
        _cache = new Cache(maxSize, TimeSpan.FromMinutes(10));
        _rootFolder = rootFolder;
    }

    private SemaphoreSlim GetFileLock(string filename)
    {
        _entryLock.EnterReadLock();
        try
        {
            if (_fileLocks.TryGetValue(filename, out var fileLock))
                return fileLock;
        }
        finally
        {
            _entryLock.ExitReadLock();
        }

        _entryLock.EnterWriteLock();
        try
        {
            if (_fileLocks.TryGetValue(filename, out var fileLock))
                return fileLock;

            var newFileLock = new SemaphoreSlim(1, 1);
            _fileLocks[filename] = newFileLock;
            return newFileLock;
        }
        finally
        {
            _entryLock.ExitWriteLock();
        }
    }

    private string? GetFile(string filename)
    {
        return Directory.GetFiles(_rootFolder, filename, SearchOption.AllDirectories).FirstOrDefault();
    }
    
    public async Task<(int, bool)> GetWordCount(string filename)
    {
        var count = _cache.Get(filename);
        if (count != -1)
            return (count, true);

        var fileLock = GetFileLock(filename);
        await fileLock.WaitAsync();
        try
        {
            count = _cache.Get(filename);
            if (count != -1)
                return (count, true);

            var putanja = GetFile(filename);
            if(putanja == null) throw new FileNotFoundException($"Fajl {filename} nije pronadjen");
            var text = await File.ReadAllTextAsync(putanja);
            count = BrojReciVelikimSlovom(text);

            _cache.Set(filename, count);
            return (count, false);
        }
        finally
        {
            fileLock.Release();
        }
    }

    private int BrojReciVelikimSlovom(string tekst)
    {
        int count = 0;
        int duzina = 0;
        bool velikoSlovo = false;

        for (int i = 0; i <= tekst.Length; i++)
        {
            bool krajReci = i == tekst.Length ||
                            tekst[i] == ' ' ||
                            tekst[i] == '\n' ||
                            tekst[i] == '\r' ||
                            tekst[i] == '\t';

            if (krajReci)
            {
                if (velikoSlovo && duzina > 5)
                    count++;
                duzina = 0;
                velikoSlovo = false;
            }
            else
            {
                if (duzina == 0)
                    velikoSlovo = char.IsUpper(tekst[i]);
                duzina++;
            }
        }

        return count;
    }

    public void Cleanup() => _cache.Cleanup();
}