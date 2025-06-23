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
    public class AuthZenClientOptions
    {
        public string AuthorizationUrl { get; set; }
    }
    
    public class AuthZenClient : IAuthZenClient
    {
        private const string AuthZenContentType = "application/json";
        internal const string UriBase = "access/v1";
        internal const string EvaluationUri = "evaluation";
        internal const string BoxcarUri = "evaluations";
        private const string RequestIdHeader = "X-Request-ID";
        
        private readonly HttpClient httpClient;

        private static JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public AuthZenClient(IHttpClientFactory httpClientFactory, IOptions<AuthZenClientOptions> options)
        {
            if (httpClientFactory == null) throw new ArgumentNullException(nameof(httpClientFactory));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(options.Value.AuthorizationUrl)) throw new ArgumentException("Authorization URL must be provided", nameof(options));
            
            httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(options.Value.AuthorizationUrl);
        }

        public async Task<AuthZenResponse> Evaluate(AuthZenEvaluationRequest request)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri($"{UriBase}/{EvaluationUri}", UriKind.Relative));
            if (request.CorrelationId != null)
            {
                requestMessage.Headers.Add(RequestIdHeader, request.CorrelationId);
            }

            string requestJson = JsonSerializer.Serialize(request.Body.ToDto(), serializerOptions);

            HttpContent content = new StringContent(requestJson, Encoding.UTF8, AuthZenContentType);
            requestMessage.Content = content;
            
            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);

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

        public async Task<AuthZenBoxcarResponse> Evaluate(AuthZenBoxcarEvaluationRequest request)
        {
            if (IsMultiEvaluationsMissing(request))
            {
                return await FallbackToSingleEvaluation(request);
            }
            
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri($"{UriBase}/{BoxcarUri}", UriKind.Relative));
            
            if (request.CorrelationId != null)
            {
                requestMessage.Headers.Add(RequestIdHeader, request.CorrelationId);
            }
            
            string requestJson = JsonSerializer.Serialize(request.Body.ToDto(), serializerOptions);
            
            HttpContent content = new StringContent(requestJson, Encoding.UTF8, AuthZenContentType);
            requestMessage.Content = content;
            
            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);
            
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