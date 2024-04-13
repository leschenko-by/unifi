using Unifi.Gateway.Common.Interfaces;

namespace Unifi.Gateway.Common.Services
{
    public class FileReader : IFileReader
    {
        public async Task<string> ReadAsync(string file)
        {
            if (File.Exists(file))
            {
                return await File.ReadAllTextAsync(file);
            }
            else
            {
                return "";
            }
        }
    }
}
