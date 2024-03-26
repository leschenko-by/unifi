using System.Diagnostics;
using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class SystemInfoService : ISystemInfoService
    {
        private long totalMemory = 0;

        public async Task<double> GetCpuUsageAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var startCpuUsage = Process.GetProcesses().Sum(a => a.TotalProcessorTime.TotalMilliseconds);
                await Task.Delay(500);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = Process.GetProcesses().Sum(a => a.TotalProcessorTime.TotalMilliseconds);
                var cpuUsedMs = endCpuUsage - startCpuUsage;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
                return cpuUsageTotal * 100;
            }
            catch
            {
                return 0;
            }
        }

        public Task<long> GetUptimeAsync()
        {
            var uptime = Environment.TickCount64 / 1000;
            return Task.FromResult(uptime);
        }

        public Task<long> GetUsedMemoryAsync()
        {
            return Task.FromResult(Process.GetProcesses().Sum(a => a.PrivateMemorySize64));
        }

        public async Task<long> GetTotalMemoryAsync()
        {
            // only parse the file once
            if (totalMemory > 0)
            {
                return totalMemory;
            }

            string path = "/proc/meminfo";
            if (File.Exists(path))
            {
                using var reader = new StreamReader(path);
                string? line = string.Empty;
                while (!string.IsNullOrWhiteSpace(line = await reader.ReadLineAsync()))
                {
                    if (line.Contains("MemTotal", StringComparison.OrdinalIgnoreCase))
                    {
                        // e.g. MemTotal:       16370152 kB
                        var parts = line.Split(':');
                        var valuePart = parts[1].Trim();
                        parts = valuePart.Split(' ');
                        var numberString = parts[0].Trim();

                        if (long.TryParse(numberString, out var totalMemoryInKb))
                        {
                            return totalMemory = totalMemoryInKb * 1024;
                        }
                    }
                }
            }

            return 0L;
        }
    }
}
