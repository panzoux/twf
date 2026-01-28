using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        string path = @"C:\Users\user\Application Data"; // Likely restricted
        Console.WriteLine($"Checking path: {path}");
        Console.WriteLine($"Exists: {Directory.Exists(path)}");

        try
        {
            await foreach (var item in Enumerate(path))
            {
                Console.WriteLine($"Item: {item}");
            }
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("CAUGHT: UnauthorizedAccessException");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CAUGHT: {ex.GetType().Name}: {ex.Message}");
        }
    }

    static async IAsyncEnumerable<string> Enumerate(string path)
    {
        await Task.Yield();
        var di = new DirectoryInfo(path);
        
        IEnumerable<DirectoryInfo> dirs;
        try
        {
            dirs = di.EnumerateDirectories();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Inner catch (EnumerateDirectories): {ex.GetType().Name}");
            throw;
        }

        // Iteration starts here
        foreach (var d in dirs)
        {
            yield return d.Name;
        }
    }
}
