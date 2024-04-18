using Unifi.Gateway.Common.Models.SpeedTest;

namespace Unifi.Gateway.Common.Interfaces
{
    public interface ISpeedTestService
    {
        Task<Server> GetServerAsync();
        Task<int> TestServerLatencyAsync(Server server, int retryCount = 3);
        Task<double> TestDownloadSpeedAsync(Server server, int simultaneousDownloads = 2, int retryCount = 2);
        Task<double> TestUploadSpeedAsync(Server server, int simultaneousUploads = 2, int retryCount = 2);
    }
}
