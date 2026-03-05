using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinUiTemplate.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for making HTTP requests.
    /// </summary>
    public interface IHttpService
    {
        /// <summary>
        /// Sends an HTTP GET request asynchronously and deserializes the response.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response into.</typeparam>
        /// <param name="endpoint">The endpoint URL to send the request to.</param>
        /// <param name="token">A token to monitor for cancellation requests.</param>
        /// <returns>The deserialized response object, or null if the request failed or the response was empty.</returns>
        Task<T?> GetAsync<T>(string endpoint, CancellationToken token = default);

        /// <summary>
        /// Sends an HTTP POST request asynchronously with a JSON body and deserializes the response.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response into.</typeparam>
        /// <param name="endpoint">The endpoint URL to send the request to.</param>
        /// <param name="body">The object to serialize and send in the request body.</param>
        /// <param name="token">A token to monitor for cancellation requests.</param>
        /// <returns>The deserialized response object, or null if the request failed or the response was empty.</returns>
        Task<T?> PostAsync<T>(string endpoint, object body, CancellationToken token = default);

        /// <summary>
        /// Sends an HTTP PUT request asynchronously with a JSON body and deserializes the response.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response into.</typeparam>
        /// <param name="endpoint">The endpoint URL to send the request to.</param>
        /// <param name="body">The object to serialize and send in the request body.</param>
        /// <param name="token">A token to monitor for cancellation requests.</param>
        /// <returns>The deserialized response object, or null if the request failed or the response was empty.</returns>
        Task<T?> PutAsync<T>(string endpoint, object body, CancellationToken token = default);

        /// <summary>
        /// Sends an HTTP DELETE request asynchronously.
        /// </summary>
        /// <param name="endpoint">The endpoint URL to send the request to.</param>
        /// <param name="token">A token to monitor for cancellation requests.</param>
        Task DeleteAsync(string endpoint, CancellationToken token = default);
    }
}
