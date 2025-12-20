using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// Usage: dotnet run --project PayloadLogQuery.csproj -- generate-data

namespace PayloadLogQuery.Utils
{
    public static class LogGenerator
    {
        public static async Task GenerateAsync(string outputDir, int count = 1000)
        {
            Directory.CreateDirectory(outputDir);
            var path = Path.Combine(outputDir, "demoService-demoSession1.log");
            Console.WriteLine($"Generating large log file at {path} with {count} entries...");

            await using var fs = File.Create(path);
            await using var sw = new StreamWriter(fs);

            var rnd = new Random();
            var baseTime = DateTimeOffset.UtcNow.AddDays(-1);

            for (int i = 0; i < count; i++)
            {
                var ts = baseTime.AddMilliseconds(i * 10);
                var version = "v1";
                // JSON-like content
                var status = rnd.Next(0, 10) < 8 ? 200 : (rnd.Next(0, 2) == 0 ? 500 : 404); // Mostly 200, some errors
                var payload = $"{{\"method\":\"GET\", \"url\":\"/api/resource/{i}\", \"status\":{status}, \"duration\":{rnd.Next(10, 500)}}}";

                var line = $"{ts:O} {version}:{payload} [status:{status}]";

                await sw.WriteLineAsync(line);
            }
            Console.WriteLine("Generation complete.");
        }
    }
}
