﻿// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.Forms.Auth
{
    internal class HttpManager : IHttpManager
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpManager(IHttpClientFactory httpClientFactory = null)
        {
            _httpClientFactory = httpClientFactory ?? new HttpClientFactory();
        }

        /// <inheritdoc />
        public async Task<HttpResponse> SendPostAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            IDictionary<string, string> bodyParameters,
            RequestContext requestContext,
            CancellationToken token)
        {
            HttpContent body = bodyParameters == null ? null : new FormUrlEncodedContent(bodyParameters);
            return await SendPostAsync(endpoint, headers, body, requestContext, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<HttpResponse> SendPostAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            RequestContext requestContext,
            CancellationToken token)
        {
            return await ExecuteWithRetryAsync(endpoint, headers, body, HttpMethod.Post, requestContext, token: token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<HttpResponse> SendGetAsync(
            Uri endpoint,
            Dictionary<string, string> headers,
            RequestContext requestContext,
            CancellationToken token)
        {
            return await ExecuteWithRetryAsync(endpoint, headers, null, HttpMethod.Get, requestContext, token: token).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs the POST request just like <see cref="SendPostAsync(Uri, IDictionary{string, string}, HttpContent, RequestContext, CancellationToken)"/>
        /// but does not throw a ServiceUnavailable service exception. Instead, it returns the <see cref="IHttpWebResponse"/> associated
        /// with the request.
        /// </summary>
        /// <param name="uri">The end point where to end the message.</param>
        /// <param name="headers">The headers to use.</param>
        /// <param name="body">The body to send..</param>
        /// <param name="requestContext">The context with information about the request.</param>
        /// <param name="token">A cancellation token for cancellation.</param>
        /// <returns>The response.</returns>
        public async Task<IHttpWebResponse> SendPostForceResponseAsync(
            Uri uri,
            Dictionary<string, string> headers,
            StringContent body,
            RequestContext requestContext,
            CancellationToken token)
        {
            return await ExecuteWithRetryAsync(uri, headers, body, HttpMethod.Post, requestContext, doNotThrow: true, token: token).ConfigureAwait(false);
        }

        internal /* internal for test only */ static async Task<HttpResponse> CreateResponseAsync(HttpResponseMessage response)
        {
            var body = response.Content == null
                ? null
                : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return new HttpResponse
            {
                Headers = response.Headers,
                Body = body,
                StatusCode = response.StatusCode
            };
        }

        protected virtual HttpClient GetHttpClient()
        {
            return _httpClientFactory.HttpClient;
        }

        private static async Task<HttpContent> CloneHttpContentAsync(HttpContent httpContent)
        {
            var temp = new MemoryStream();
            await httpContent.CopyToAsync(temp).ConfigureAwait(false);
            temp.Position = 0;

            var clone = new StreamContent(temp);
            if (httpContent.Headers != null)
            {
                foreach (var h in httpContent.Headers)
                {
                    clone.Headers.Add(h.Key, h.Value);
                }
            }

#if WINDOWS_APP
            // WORKAROUND 
            // On UWP there is a bug in the Http stack that causes an exception to be thrown when moving around a stream.
            // https://stackoverflow.com/questions/31774058/postasync-throwing-irandomaccessstream-error-when-targeting-windows-10-uwp
            // LoadIntoBufferAsync is necessary to buffer content for multiple reads - see https://stackoverflow.com/questions/26942514/multiple-calls-to-httpcontent-readasasync
            // Documentation is sparse, but it looks like loading the buffer into memory avoids the bug, without 
            // replacing the System.Net.HttpClient with Windows.Web.Http.HttpClient, which is not exactly a drop in replacement
            await clone.LoadIntoBufferAsync().ConfigureAwait(false);
#endif

            return clone;
        }

        private HttpRequestMessage CreateRequestMessage(Uri endpoint, IDictionary<string, string> headers)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage { RequestUri = endpoint };
            requestMessage.Headers.Accept.Clear();
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> kvp in headers)
                {
                    requestMessage.Headers.Add(kvp.Key, kvp.Value);
                }
            }

            return requestMessage;
        }

        private async Task<HttpResponse> ExecuteWithRetryAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            HttpMethod method,
            RequestContext requestContext,
            bool doNotThrow = false,
            bool retry = true,
            CancellationToken token = default)
        {
            Exception timeoutException = null;
            bool isRetryable = false;
            HttpResponse response = null;

            try
            {
                HttpContent clonedBody = body;
                if (body != null)
                {
                    // Since HttpContent would be disposed by underlying client.SendAsync(),
                    // we duplicate it so that we will have a copy in case we would need to retry
                    clonedBody = await CloneHttpContentAsync(body).ConfigureAwait(false);
                }

                response = await ExecuteAsync(endpoint, headers, clonedBody, method, token).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return response;
                }

                requestContext.Logger.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CoreErrorMessages.HttpRequestUnsuccessful,
                        (int)response.StatusCode,
                        response.StatusCode));

                if ((int)response.StatusCode >= 500 && (int)response.StatusCode < 600)
                {
                    isRetryable = true;
                }
            }
            catch (TaskCanceledException exception)
            {
                requestContext.Logger.Error(exception.Message);
                isRetryable = true;
                timeoutException = exception;
            }

            if (isRetryable)
            {
                if (retry)
                {
                    requestContext.Logger.Info("Retrying one more time..");
                    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    return await ExecuteWithRetryAsync(
                        endpoint,
                        headers,
                        body,
                        method,
                        requestContext,
                        doNotThrow,
                        retry: false,
                        token: token).ConfigureAwait(false);
                }

                requestContext.Logger.Info("Request retry failed.");
                if (timeoutException != null)
                {
                    throw ExceptionFactory.GetServiceException(
                        CoreErrorCodes.RequestTimeout,
                        "Request to the endpoint timed out.",
                        timeoutException,
                        (ExceptionDetail)null);
                }

                if (doNotThrow)
                {
                    return response;
                }

                throw ExceptionFactory.GetServiceException(
                        CoreErrorCodes.ServiceNotAvailable,
                        "Service is unavailable to process the request",
                        response);
            }

            return response;
        }

        private async Task<HttpResponse> ExecuteAsync(
            Uri endpoint,
            IDictionary<string, string> headers,
            HttpContent body,
            HttpMethod method,
            CancellationToken token)
        {
            HttpClient client = GetHttpClient();

            using (HttpRequestMessage requestMessage = CreateRequestMessage(endpoint, headers))
            {
                requestMessage.Method = method;
                requestMessage.Content = body;

                using (HttpResponseMessage responseMessage =
                    await client.SendAsync(requestMessage, token).ConfigureAwait(false))
                {
                    HttpResponse returnValue = await CreateResponseAsync(responseMessage).ConfigureAwait(false);
                    returnValue.UserAgent = client.DefaultRequestHeaders.UserAgent.ToString();
                    return returnValue;
                }
            }
        }
    }
}
