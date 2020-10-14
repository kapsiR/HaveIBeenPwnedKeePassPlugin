using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HaveIBeenPwnedPlugin
{
    internal class HIBP : IDisposable
    {
        private const string HIBP_RangeApiUrl = "https://api.pwnedpasswords.com/range/";
        private static readonly HttpClient s_httpClient = new HttpClient();

        public HIBP()
        {
            s_httpClient.DefaultRequestHeaders.Add("User-Agent", $"KeePass Plugin {nameof(HaveIBeenPwnedPlugin)} v{ThisAssembly.AssemblyFileVersion}");
            s_httpClient.DefaultRequestHeaders.Add("Add-Padding", bool.TrueString);
        }

        public async Task<(bool isBreached, int breachCount)> CheckRangeAsync(string sha1Prefix, string sha1Suffix, CancellationToken token = default)
        {
            bool isBreached = false;
            int breachedCount = 0;
            HttpResponseMessage responseMessage = await s_httpClient.GetAsync($"{HIBP_RangeApiUrl}{sha1Prefix}", token).ConfigureAwait(false);
            responseMessage.EnsureSuccessStatusCode();

            string response = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.Contains(sha1Suffix))
            {
                isBreached = true;

                int.TryParse(response.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    ?.FirstOrDefault(row => row.StartsWith(sha1Suffix, StringComparison.Ordinal))
                    ?.Split(new[] { ':' })?[1], out breachedCount);

                if (breachedCount == 0)
                {
                    // Padded entries always have a password count of 0 and can be discarded once received.
                    // https://haveibeenpwned.com/API/v3#PwnedPasswordsPadding
                    isBreached = false;
                }
            }

            return (isBreached, breachedCount);
        }

        public Task<(bool isBreached, int breachCount)> CheckRangeAsync((string sha1Prefix, string sha1Suffix) sha1Values, CancellationToken token = default)
        {
            return CheckRangeAsync(sha1Values.sha1Prefix, sha1Values.sha1Suffix, token);
        }

        public void Dispose()
        {
            s_httpClient.Dispose();
        }
    }
}
