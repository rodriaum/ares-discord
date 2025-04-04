using Ares.Core.Util;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Ares.Ares.Core.Monitor;

public class SystemMonitor
{
    private readonly PeriodicTimer _statsTimer;
    private readonly CancellationTokenSource _cts;

    private readonly Queue<float> _cpuUsageHistory = new();
    private readonly Queue<float> _ramUsageHistory = new();

    private const int MaxSamples = 6; // 6 samples for 30 minutes (5m each)

    public SystemMonitor()
    {
        _statsTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        _cts = new CancellationTokenSource();
    }

    public async Task Init()
    {
        await AresLogger.LogAsync("Monitor", "Monitoring started...");

        try
        {
            while (await _statsTimer.WaitForNextTickAsync(_cts.Token))
            {
                await SendStatsAsync();
            }
        }
        catch (OperationCanceledException)
        {
            await AresLogger.LogAsync("Monitor", "Monitoring cancelled.");
        }
        catch (Exception ex)
        {
            await AresLogger.LogAsync("Monitor", $"Error in monitoring: {ex.Message}");
        }
    }

    public void Stop()
    {
        _cts.Cancel();
        AresLogger.Log("Monitor", "Monitoring stopped...");
    }

    private async Task SendStatsAsync()
    {
        float cpuUsage = await GetCpuUsageAsync();
        float usedRam = GetUsedRam();

        UpdateHistory(_cpuUsageHistory, cpuUsage);
        UpdateHistory(_ramUsageHistory, usedRam);

        string statsMessage = $"\nStatistics\n* CPU (Last 30m, 15m, 5m): {GetAverage(_cpuUsageHistory, 6):F2}%, {GetAverage(_cpuUsageHistory, 3):F2}%, {GetAverage(_cpuUsageHistory, 1):F2}%\n" +
                              $"* RAM (Last 30m, 15m, 5m): {FormatterUtil.FormatRam(GetAverage(_ramUsageHistory, 6))}, {FormatterUtil.FormatRam(GetAverage(_ramUsageHistory, 3))}, {FormatterUtil.FormatRam(GetAverage(_ramUsageHistory, 1))}\n";

        await AresLogger.LogAsync("Monitor", statsMessage);
    }

    private async Task<float> GetCpuUsageAsync()
    {
        var startCpuTime = Process.GetCurrentProcess().TotalProcessorTime;
        await Task.Delay(1000);
        var endCpuTime = Process.GetCurrentProcess().TotalProcessorTime;

        int processorCount = Environment.ProcessorCount;
        return (float)((endCpuTime - startCpuTime).TotalMilliseconds / (1000 * processorCount));
    }

    private float GetUsedRam()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using (var proc = Process.GetCurrentProcess())
            {
                return proc.WorkingSet64 / 1024f / 1024f;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            string[] memInfo = File.ReadAllLines("/proc/meminfo");
            float totalMem = float.Parse(memInfo[0].Split(':')[1].Trim().Split(' ')[0]) / 1024f;
            float freeMem = float.Parse(memInfo[1].Split(':')[1].Trim().Split(' ')[0]) / 1024f;
            return totalMem - freeMem;
        }

        return 0;
    }

    private void UpdateHistory(Queue<float> history, float value)
    {
        if (history.Count >= MaxSamples)
        {
            history.Dequeue();
        }
        history.Enqueue(value);
    }

    private float GetAverage(Queue<float> history, int count)
    {
        if (history.Count == 0) return 0;
        count = Math.Min(count, history.Count);

        float sum = 0;

        int startIndex = history.Count - count;
        int index = 0;

        foreach (var value in history)
        {
            if (index++ >= startIndex)
            {
                sum += value;
            }
        }

        return sum / count;
    }
}
