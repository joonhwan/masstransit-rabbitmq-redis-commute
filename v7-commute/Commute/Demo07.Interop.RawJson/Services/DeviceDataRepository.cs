using System.Collections.Generic;
using System.Threading.Tasks;

namespace Demo07.Interop.RawJson.Services
{
    public interface IDeviceDataRepository
    {
        Task<bool> GetLocation(string deviceId, out (int, int) location);
        Task SetLocation(string deviceId, (int, int) location);
    }

    public class InMemoryDeviceDataRepository : IDeviceDataRepository
    {
        private static readonly Dictionary<string, (int, int)> Store = new Dictionary<string, (int, int)>();

        public Task<bool> GetLocation(string deviceId, out (int, int) location)
        {
            lock (Store)
            {
                return Task.FromResult(Store.TryGetValue(deviceId, out location));
            }
        }

        public async Task SetLocation(string deviceId, (int, int) location)
        {
            await Task.Delay(1000);

            lock (Store)
            {
                Store[deviceId] = location;
            }
        }
    }
}