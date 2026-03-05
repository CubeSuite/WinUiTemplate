using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.UserDataAccounts.SystemAccess;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.Services
{
    public class HttpService : IHttpService
    {
        // Services & Stores
        private readonly ILoggerService logger;
        private readonly IUserSettings userSettings;
        
        // Members
        private readonly HttpClient client;
        private const string baseUrl = "";

        // Constructors
        public HttpService(IServiceProvider serviceProvider) {
            logger = serviceProvider.GetRequiredService<ILoggerService>();
            userSettings = serviceProvider.GetRequiredService<IUserSettings>();
            
            client = new HttpClient();
        }

        // Public Functions

        public async Task<T?> GetAsync<T>(string endpoint, CancellationToken token = default) {
            return await SendAsync<T>(HttpMethod.Get, endpoint, null, token);
        }

        public async Task<T?> PostAsync<T>(string endpoint, object body, CancellationToken token = default) {
            return await SendAsync<T>(HttpMethod.Post, endpoint, body, token);
        }

        public async Task<T?> PutAsync<T>(string endpoint, object body, CancellationToken token = default) {
            return await SendAsync<T>(HttpMethod.Put, endpoint, body, token);
        }

        public async Task DeleteAsync(string endpoint, CancellationToken token = default) {
            await SendAsync<object>(HttpMethod.Delete, endpoint, null, token);
        }

        // Private Functions

        private async Task<T?> SendAsync<T>(HttpMethod method, string endpoint, object? body, CancellationToken token) {
            string url = baseUrl + endpoint;
            HttpRequestMessage request = new HttpRequestMessage(method, url);
            if (body != null) {
                string json = JsonConvert.SerializeObject(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            int attempts = 0;
            while(attempts <= userSettings.ApiMaxRetries) {
                attempts++;
                try {
                    CancellationTokenSource timeoutToken = new CancellationTokenSource(TimeSpan.FromSeconds(userSettings.ApiTimeout));
                    CancellationTokenSource linkedToken = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutToken.Token);

                    HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedToken.Token);
                    return await HandleResponseAsync<T>(response);
                }
                catch (TaskCanceledException e) {
                    if (token.IsCancellationRequested) return default;

                    logger.LogWarning($"Request to '{url}' timed out:");
                }
                catch (HttpRequestException e) {
                    logger.LogWarning($"Request to '{url}' caused exception: '{e.Message}'");
                }
            }

            logger.LogError($"Request failed and exceeded max retries");
            return default;
        }

        private async Task<T?> HandleResponseAsync<T>(HttpResponseMessage response) {
            string body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) {
                logger.LogError($"API error {response.StatusCode}: {body}");
                throw new ApiException(response.StatusCode, body);
            }

            if (typeof(T) == typeof(object)) return default;

            return JsonConvert.DeserializeObject<T>(body);
        }
    }

    public class ApiException : Exception 
    {
        public HttpStatusCode StatusCode { get; }
        public string? ResponseBody { get; }

        public ApiException(HttpStatusCode code, string? body)
                            : base($"HTTP Error: {(int)code} {code}") 
        {
            StatusCode = code;
            ResponseBody = body;
        }
    }
}
