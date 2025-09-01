using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rsk.AuthZen.Client.DTOs;

namespace Rsk.AuthZen.Client
{
    /// <summary>
    /// Provides configuration options for the <see cref="AuthZenClient"/>.
    /// </summary>
    public class AuthZenClientOptions
    {
        /// <summary>
        /// Gets or sets the base URL of the AuthZen authorization service.
        /// </summary>
        public string AuthorizationUrl { get; set; }
    }
    
    /// <inheritdoc cref="IAuthZenClient"/>
    public class AuthZenClient : IAuthZenClient
    {
        private const string AuthZenContentType = "application/json";
        internal const string UriBase = "access/v1";
        internal const string EvaluationUri = "evaluation";
        internal const string BoxcarUri = "evaluations";
        internal const string MetadataEndpointUri = ".well-known/authzen-configuration";
        private const string RequestIdHeader = "X-Request-ID";
        
        private readonly HttpClient httpClient;
        private AuthZenMetadataResponse _metadata;

        private static JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>
        /// Creates a new instance of <see cref="AuthZenClient"/>.
        /// </summary>
        /// <param name="httpClientFactory">The httpClientFactory</param>
        /// <param name="options">The AuthZenClientOptions</param>
        /// <exception cref="ArgumentNullException">Thrown if any argument is null</exception>
        /// <exception cref="ArgumentException">Thrown if the AuthZenClientOptions are invalid</exception>
        public AuthZenClient(IHttpClientFactory httpClientFactory, IOptions<AuthZenClientOptions> options)
        {
            if (httpClientFactory == null) throw new ArgumentNullException(nameof(httpClientFactory));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(options.Value.AuthorizationUrl)) throw new ArgumentException("Authorization URL must be provided", nameof(options));
            
            httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(options.Value.AuthorizationUrl);
        }

        /// <inheritdoc cref="IAuthZenClient"/>
        public async Task<AuthZenMetadataResponse> GetMetadata()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{MetadataEndpointUri}");

            HttpResponseMessage response = await httpClient.SendAsync(requestMessage).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new AuthZenRequestFailureException($"Metadata request failed with status code: {response.StatusCode}");
            }

            var responseContent  = await response.Content.ReadAsStringAsync();
            
            return JsonSerializer.Deserialize<AuthZenMetadataResponse>(responseContent, new JsonSerializerOptions(){ PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower});
        }

        /// <inheritdoc cref="IAuthZenClient"/>
        public async Task<AuthZenResponse> Evaluate(AuthZenEvaluationRequest request)
        {
            bool canRetry404 = true;
            if (_metadata == null)
            {
                _metadata = await GetMetadata();
                canRetry404 = false;
            }

            return await EvaluateInternal(request, canRetry404);
        }

        private async Task<AuthZenResponse> EvaluateInternal(AuthZenEvaluationRequest request, bool canRetry)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(_metadata.AccessEvaluationEndpoint, UriKind.Absolute));
            if (request.CorrelationId != null)
            {
                requestMessage.Headers.Add(RequestIdHeader, request.CorrelationId);
            }

            string requestJson = JsonSerializer.Serialize(request.Body.ToDto(), serializerOptions);

            HttpContent content = new StringContent(requestJson, Encoding.UTF8, AuthZenContentType);
            requestMessage.Content = content;
            
            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);

            if (canRetry && responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _metadata = null;
                return await Evaluate(request);
            }
            
            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new AuthZenRequestFailureException($"Evaluation request failed with status code: {responseMessage.StatusCode}");
            }
            
            string responseJson = await responseMessage.Content.ReadAsStringAsync();
            
            AuthZenResponseDto responseDto = JsonSerializer.Deserialize<AuthZenResponseDto>(responseJson, serializerOptions);

            var authZenResponse = new AuthZenResponse();

            if (responseMessage.Headers.TryGetValues(RequestIdHeader, out IEnumerable<string> requestIds))
            {
                authZenResponse.CorrelationId = requestIds.FirstOrDefault();
            }

            authZenResponse.Decision = responseDto.Decision ? Decision.Permit : Decision.Deny;
            authZenResponse.Context = responseDto.Context.ToString();
                
            return authZenResponse;
        }

        /// <inheritdoc cref="IAuthZenClient"/>
        public async Task<AuthZenBoxcarResponse> Evaluate(AuthZenBoxcarEvaluationRequest request)
        {
            bool canRetry404 = true;
            if (_metadata == null)
            {
                _metadata = await GetMetadata();
                canRetry404 = false;
            }

            return await BoxcarEvaluateInternal(request, canRetry404);
        }

        private async Task<AuthZenBoxcarResponse> BoxcarEvaluateInternal(AuthZenBoxcarEvaluationRequest request, bool canRetry)
        {
            if (IsMultiEvaluationsMissing(request))
            {
                return await FallbackToSingleEvaluation(request);
            }
            
            if (string.IsNullOrWhiteSpace(_metadata.AccessEvaluationsEndpoint))
            {
                throw new NotSupportedException("The AuthZen server does not support boxcar evaluation requests.");
            }
            
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(_metadata.AccessEvaluationsEndpoint, UriKind.Absolute));
            
            if (request.CorrelationId != null)
            {
                requestMessage.Headers.Add(RequestIdHeader, request.CorrelationId);
            }
            
            string requestJson = JsonSerializer.Serialize(request.Body.ToDto(), serializerOptions);
            
            HttpContent content = new StringContent(requestJson, Encoding.UTF8, AuthZenContentType);
            requestMessage.Content = content;
            
            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);
            
            if (canRetry && responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _metadata = null;
                return await Evaluate(request);
            }
            
            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new AuthZenRequestFailureException($"Evaluation request failed with status code: {responseMessage.StatusCode}");
            }
            
            string responseJson = await responseMessage.Content.ReadAsStringAsync();
            
            AuthZenBoxcarResponseDto responseDto = JsonSerializer.Deserialize<AuthZenBoxcarResponseDto>(responseJson, serializerOptions);
            
            var response = new AuthZenBoxcarResponse
            {
                Evaluations = responseDto.Evaluations.Select(e => new AuthZenResponse
                {
                    Decision = e.Decision ? Decision.Permit : Decision.Deny,
                    Context = e.Context.ToString(),
                }).ToList()
            };
            
            if (responseMessage.Headers.TryGetValues(RequestIdHeader, out IEnumerable<string> requestIds))
            {
                response.CorrelationId = requestIds.FirstOrDefault();
            }

            return response;
        }

        private static bool IsMultiEvaluationsMissing(AuthZenBoxcarEvaluationRequest evaluationRequest)
        {
            return evaluationRequest.Body.Evaluations == null || !evaluationRequest.Body.Evaluations.Any();
        }

        private async Task<AuthZenBoxcarResponse> FallbackToSingleEvaluation(AuthZenBoxcarEvaluationRequest evaluationRequest)
        {
            var singleResponse = await Evaluate(new AuthZenEvaluationRequest
            {
                CorrelationId = evaluationRequest.CorrelationId,
                Body = new AuthZenEvaluationBody
                {
                    Context = evaluationRequest.Body.DefaultValues.Context,
                    Subject = evaluationRequest.Body.DefaultValues.Subject,
                    Resource = evaluationRequest.Body.DefaultValues.Resource,
                    Action = evaluationRequest.Body.DefaultValues.Action,
                }
            });

            return new AuthZenBoxcarResponse
            {
                Evaluations = new List<AuthZenResponse>
                {
                    singleResponse
                },
                CorrelationId = singleResponse.CorrelationId
            };
        }
    }
}