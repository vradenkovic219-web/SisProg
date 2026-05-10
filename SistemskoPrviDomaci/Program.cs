using System.Net;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        string rootFolder = @".\fajlovi"; 
        int cacheCapacity = 100;

        var fileSystem = new FileSystem(rootFolder, cacheCapacity);

        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5050/");
        
        listener.Start();
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("Server radi na http://localhost:5050/");
        Console.WriteLine(new string('-', 60));


        int totalRequests = 0;
        object statsLock = new object();

        while (true)
        {
            try
            {
                var context = listener.GetContext();
                
                ThreadPool.QueueUserWorkItem(_ => 
                {
                    lock(statsLock)
                    {
                        totalRequests++;
                    }
                    ProcessRequest(context, fileSystem);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri primanju zahteva: {ex.Message}");
            }
        }
    }

    static void ProcessRequest(HttpListenerContext context, FileSystem fileSystem)
    {
        var request = context.Request;
        var response = context.Response;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            string? filename = request.QueryString["file"];

            if (string.IsNullOrEmpty(filename))
            {
                SendResponse(response, 400, "Nije prosledjen naziv fajla u request-u");
                return;
            }

            int wordCount = fileSystem.GetWordCount(filename);
            
            stopwatch.Stop();

            string cacheStatus = stopwatch.ElapsedMilliseconds < 5 ? "CACHE HIT" : "CITANJE SA DISKA";
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {cacheStatus,-15} | File: {filename,-15} | Words: {wordCount,5} | Time: {stopwatch.ElapsedMilliseconds,4}ms");

            SendResponse(response, 200, $"File: {filename}\nBroj reci: {wordCount}\nVreme odziva: {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (FileNotFoundException ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] NOT FOUND | File: {request.QueryString["file"],-15} | Time: {stopwatch.ElapsedMilliseconds,4}ms");
            SendResponse(response, 404, ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Greska | {ex.Message}");
            SendResponse(response, 500, $"Greska: {ex.Message}");
        }
    }

    static void SendResponse(HttpListenerResponse response, int statusCode, string message)
    {
        response.StatusCode = statusCode;
        response.ContentType = "text/plain; charset=utf-8";
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }
}