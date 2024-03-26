namespace Unifi.Gateway.Common.Interfaces
{
    public interface ISystemInfoService
    {
        Task<double> GetCpuUsageAsync();
        Task<long> GetTotalMemoryAsync();
        Task<long> GetUptimeAsync();
        Task<long> GetUsedMemoryAsync();
    }
}
