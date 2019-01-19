using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace HaveIBeenPwnedPlugin
{
    internal class HIBP : IDisposable
    {
        private const string HIBP_RangeApiUrl = "https://api.pwnedpasswords.com/range/";
        private static readonly HttpClient s_httpClient = new HttpClient();

        public HIBP()
        {
            s_httpClient.DefaultRequestHeaders.Add("User-Agent", $"KeePass Plugin {nameof(HaveIBeenPwnedPlugin)}");
        }

        public async Task<(bool isBreached, int breachCount)> CheckRangeAsync(string sha1Prefix, string sha1Suffix)
        {
            bool isBreached = false;
            int breachedCount = 0;
            var response = await s_httpClient.GetStringAsync($"{HIBP_RangeApiUrl}{sha1Prefix}");

            if (response.Contains(sha1Suffix))
            {
                isBreached = true;

                int.TryParse(response.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    ?.FirstOrDefault(row => row.StartsWith(sha1Suffix, StringComparison.Ordinal))
                    ?.Split(new[] { ':' })?[1], out breachedCount);
            }

            return (isBreached, breachedCount);
        }

        public async Task<(bool isBreached, int breachCount)> CheckRangeAsync((string sha1Prefix, string sha1Suffix) sha1Values)
        {
            return await CheckRangeAsync(sha1Values.sha1Prefix, sha1Values.sha1Suffix);
        }

        public void Dispose()
        {
            s_httpClient.Dispose();
        }
    }
}
