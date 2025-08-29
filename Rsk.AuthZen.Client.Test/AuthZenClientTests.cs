using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Rsk.AuthZen.Client.DTOs;
using Xunit;

namespace Rsk.AuthZen.Client.Test;

public class AuthZenClientTests
{
    private Mock<IHttpClientFactory> httpClientFactory;
    Mock<HttpMessageHandler> httpMessageHandler;
    
    HttpClient httpClient;
    AuthZenClientOptions optionsValue;
    Mock<IOptions<AuthZenClientOptions>> options;

    private const string simpleEvaluationResponse =
        """
        {
            "decision": true
        }
        """;

    private const string simpleBoxcarResponse =
        """
        {
            "evaluations": [
                {
                    "decision": true
                }
            ],
            "correlationId": "12345"
        }
        """;
    
    public AuthZenClientTests()
    {
        httpClientFactory = new Mock<IHttpClientFactory>();
        httpMessageHandler = new Mock<HttpMessageHandler>();    
        httpClient = new HttpClient(httpMessageHandler.Object);
        options = new Mock<IOptions<AuthZenClientOptions>>();
        optionsValue = new AuthZenClientOptions{ AuthorizationUrl = "https://localhost:5001" };
        
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        options.Setup(o => o.Value).Returns(optionsValue);
    }
    
    private AuthZenClient CreateSut()
    {
        return new AuthZenClient(httpClientFactory?.Object, options?.Object);
    }
    
    private async Task VerifyMissingRequestPartOmitsElement(AuthZenEvaluationRequest singleEvaluationRequest, string expectedMissingElement)
    {
        HttpRequestMessage requestSent = null;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, c) => requestSent = r)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(simpleEvaluationResponse)
            });
        
        var sut = CreateSut();
        
        await sut.Evaluate(singleEvaluationRequest);
        
        string sentContent = await requestSent.Content.ReadAsStringAsync();
        
        var json = JsonDocument.Parse(sentContent);
        
        json.RootElement.TryGetProperty(expectedMissingElement, out JsonElement _).Should().BeFalse();
    }

    private void AdjustRequestSerialization(AuthZenRequestMessageDto request)
    {
        AdjustDeserializedDictionary(request.Subject?.Properties);
        AdjustDeserializedDictionary(request.Resource?.Properties);
        AdjustDeserializedDictionary(request.Action?.Properties);
        AdjustDeserializedDictionary(request.Context);
    }
    
    private void AdjustBoxcarRequestSerialization(AuthZenBoxcarRequestMessageDto request)
    {
        AdjustDeserializedDictionary(request.Subject?.Properties);
        AdjustDeserializedDictionary(request.Resource?.Properties);
        AdjustDeserializedDictionary(request.Action?.Properties);
        AdjustDeserializedDictionary(request.Context);
    }

    private void AdjustDeserializedDictionary(Dictionary<string,object> properties)
    {
        if (properties == null)
        {
            return;
        }
        
        foreach (string key in properties.Keys)
        {
            if (properties[key] is JsonElement e)
            {
                properties[key] = e.ValueKind switch
                {
                    JsonValueKind.String => e.GetString(),
                    _ => e
                };

            }
        }
    }
    
    [Fact]
    public void ctor_WhenPassedANullHttpClientFactory_ShouldThrowArgumentNullException()
    {
        httpClientFactory = null;

        Action act = () => CreateSut();

        act.Should().Throw<ArgumentNullException>();
    }
    
    [Fact]
    public void ctor_WhenPassedANullOptions_ShouldThrowArgumentNullException()
    {
        options = null;

        Action act = () => CreateSut();

        act.Should().Throw<ArgumentNullException>();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ctor_WhenPassedAnOptionsWithAnEmptyUrl_ShouldThrowArgumentException(string emptyUrl)
    {
        optionsValue.AuthorizationUrl = emptyUrl;

        Action act = () => CreateSut();

        act.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void ctor_WhenCalled_ShouldCreateHttpClientWithBaseUrlSetToThatFromOptions()
    {
        string expectedBaseUrl = "https://foo.bar.com";
        var uri = new Uri(expectedBaseUrl);
        
        optionsValue.AuthorizationUrl = expectedBaseUrl;

        var client = new HttpClient();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        
        var sut = CreateSut();

        client.BaseAddress.Should().Be(uri);
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithSingleEvaluation_ShouldPostToCorrectEndpoint()
    {
        HttpRequestMessage requestSent = null;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, c) => requestSent = r)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(simpleEvaluationResponse)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenEvaluationRequest
        {
            Body = new AuthZenEvaluationBody()
            {
                Subject = new AuthZenSubject
                {
                    Id = "dasfgthb",
                    Type = "aerfbqret"
                }
            }
        };
        
        await sut.Evaluate(evaluationRequest);
        
        requestSent.Should().NotBeNull();
        requestSent.Method.Should().Be(HttpMethod.Post);
        requestSent.RequestUri.Should().Be($"{optionsValue.AuthorizationUrl}/{AuthZenClient.UriBase}/{AuthZenClient.EvaluationUri}");
        requestSent.Content.Headers.ContentType.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Evaluate_WhenCalledWithSingleEvaluationWithCorrelationId_ShouldAddRequestIdHeaderToRequest()
    {
        HttpRequestMessage requestSent = null;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, c) => requestSent = r)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(simpleEvaluationResponse)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenEvaluationRequest
        {
            Body = new AuthZenEvaluationBody
            {
                Subject = new AuthZenSubject
                {
                    Id = "dasfgthb",
                    Type = "aerfbqret"
                },
            },
            CorrelationId = "pioxjhdfvbghdsfohiv"
        };
        
        await sut.Evaluate(evaluationRequest);
        
        requestSent.Headers.Should().ContainSingle(h => h.Key == "X-Request-ID"
                                                                && h.Value.Contains(evaluationRequest.CorrelationId));
    }

    [Fact]
    public async Task Evaluate_WhenCalledWithSingleEvaluation_ShouldPostSerializedRequestCorrectly()
    {
        HttpRequestMessage requestSent = null;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, c) => requestSent = r)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(simpleEvaluationResponse)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenEvaluationRequest
        {
            Body = new AuthZenEvaluationBody()
            {
                Subject = new AuthZenSubject
                {
                    Id = "dasfgthb",
                    Type = "aerfbqret",
                    Properties = new Dictionary<string, object>
                    {
                        { "dfgbfd", "sdfbf" },
                        { "sdfbfdsb", "sdfbsdf" }
                    }
                },
                Resource = new AuthZenResource
                {
                    Id = "fdgn",
                    Type = "bfda",
                    Properties = new Dictionary<string, object>
                    {
                        { "dgfnsg", "bnfa" },
                        { "bfea", "fba" }
                    }
                },
                Action = new AuthZenAction
                {
                    Name = "paioshd",
                    Properties = new Dictionary<string, object>
                    {
                        { "bdfaa", "aefdba" },
                        { "aedfb", "bfda" }
                    }
                },
                Context = new Dictionary<string, object>
                {
                    { "dabgn", "bfra" },
                    { "htearha", "erhaer" }
                }
            }
        };
        
        await sut.Evaluate(evaluationRequest);
        
        string sentContent = await requestSent.Content.ReadAsStringAsync();
        
        AuthZenRequestMessageDto deserializedRequest = JsonSerializer.Deserialize<AuthZenRequestMessageDto>(sentContent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
        
        AdjustRequestSerialization(deserializedRequest);
        
        deserializedRequest.Should().BeEquivalentTo(evaluationRequest.Body.ToDto());
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithSingleEvaluationAndNoSubject_ShouldNotSerializeSubject()
    {
        var evaluationRequest = new AuthZenEvaluationRequest
        {
            Body = new AuthZenEvaluationBody()
            {
                Resource = new AuthZenResource
                {
                    Id = "fdgn",
                    Type = "bfda",
                    Properties = new Dictionary<string, object>
                    {
                        { "dgfnsg", "bnfa" },
                        { "bfea", "fba" }
                    }
                },
                Action = new AuthZenAction
                {
                    Name = "paioshd",
                    Properties = new Dictionary<string, object>
                    {
                        { "bdfaa", "aefdba" },
                        { "aedfb", "bfda" }
                    }
                },
                Context = new Dictionary<string, object>
                {
                    { "dabgn", "bfra" },
                    { "htearha", "erhaer" }
                }
            }
        };
        
        await VerifyMissingRequestPartOmitsElement(evaluationRequest, "subject");
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithSingleEvaluationAndNoResource_ShouldNotSerializeResource()
    {
        var evaluationRequest = new AuthZenEvaluationRequest
        {
            Body = new AuthZenEvaluationBody()
            {
                Subject = new AuthZenSubject
                {
                    Id = "fdgn",
                    Type = "bfda",
                    Properties = new Dictionary<string, object>
                    {
                        { "dgfnsg", "bnfa" },
                        { "bfea", "fba" }
                    }
                },
                Action = new AuthZenAction
                {
                    Name = "paioshd",
                    Properties = new Dictionary<string, object>
                    {
                        { "bdfaa", "aefdba" },
                        { "aedfb", "bfda" }
                    }
                },
                Context = new Dictionary<string, object>
                {
                    { "dabgn", "bfra" },
                    { "htearha", "erhaer" }
                }
            }
        };

        await VerifyMissingRequestPartOmitsElement(evaluationRequest, "resource");
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithSingleEvaluationAndNoAction_ShouldNotSerializeAction()
    {
        var evaluationRequest = new AuthZenEvaluationRequest
        {
            Body = new AuthZenEvaluationBody()
            {
                Subject = new AuthZenSubject
                {
                    Id = "fdgn",
                    Type = "bfda",
                    Properties = new Dictionary<string, object>
                    {
                        { "dgfnsg", "bnfa" },
                        { "bfea", "fba" }
                    }
                },
                Resource = new AuthZenResource
                {
                    Id = "asdefhb",
                    Type = "aehb",
                    Properties = new Dictionary<string, object>
                    {
                        { "bre", "asreh" },
                    }
                },
                Context = new Dictionary<string, object>
                {
                    { "dabgn", "bfra" },
                    { "htearha", "erhaer" }
                }
            }
        };

        await VerifyMissingRequestPartOmitsElement(evaluationRequest, "action");
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithSingleEvaluationAndNoContext_ShouldNotSerializeContext()
    {
        var evaluationRequest = new AuthZenEvaluationRequest
        {
            Body = new AuthZenEvaluationBody()
            {
                Subject = new AuthZenSubject
                {
                    Id = "fdgn",
                    Type = "bfda",
                    Properties = new Dictionary<string, object>
                    {
                        { "dgfnsg", "bnfa" },
                        { "bfea", "fba" }
                    }
                },
                Resource = new AuthZenResource
                {
                    Id = "asdefhb",
                    Type = "aehb",
                    Properties = new Dictionary<string, object>
                    {
                        { "bre", "asreh" },
                    }
                },
                Action = new AuthZenAction
                {
                    Name = "paioshd",
                    Properties = new Dictionary<string, object>
                    {
                        { "dabgn", "bfra" },
                        { "htearha", "erhaer" }
                    }
                }
            }
        };

        await VerifyMissingRequestPartOmitsElement(evaluationRequest, "context");
    }
    
    [Theory]
    [InlineData("true", Decision.Permit)]
    [InlineData("false", Decision.Deny)]
    public async Task Evaluate_WhenCalledWithSingleEvaluation_ShouldParseDecisionCorrectly(string jsonDecision, Decision expectedDecision)
    {
        string response = $$"""
                            {
                                "decision": {{jsonDecision}}
                            }
                          """;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenEvaluationRequest
        {
            Body = new AuthZenEvaluationBody()
            {
                Subject = new AuthZenSubject
                {
                    Id = "dasfgthb",
                    Type = "aerfbqret"
                }
            }
        };
        
        AuthZenResponse authZenResponse = await sut.Evaluate(evaluationRequest);
        
        authZenResponse.Decision.Should().Be(expectedDecision);
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithSingleEvaluation_ShouldExtractContextCorrectly()
    {
        string context = """
                            {
                                "sjdo": "sdfb",
                                "sdjfgn": 73,
                                "sakjdhvuob":{
                                    "sdfbvbui":true,
                                    "hdfgouh": "iusdfvb"
                                }
                            }
                         """;
        
        string response = $$"""
                              {
                                  "decision": false,
                                  "context": {{context}}
                              }
                            """;
        
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenEvaluationRequest
        {
            Body = new AuthZenEvaluationBody()
            {
                Subject = new AuthZenSubject
                {
                    Id = "dasfgthb",
                    Type = "aerfbqret"
                }
            }
        };
        
        AuthZenResponse authZenResponse = await sut.Evaluate(evaluationRequest);
        
        authZenResponse.Context.Should().Be(context.Trim());
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithSingleEvaluationAndRequestFails_ShouldThrowAuthZenRequestFailureException()
    {
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var sut = CreateSut();

        var evaluationRequest = new AuthZenEvaluationRequest
        {
            Body = new AuthZenEvaluationBody()
            {
                Subject = new AuthZenSubject
                {
                    Id = "dasfgthb",
                    Type = "aerfbqret"
                }
            }
        };
        
        Func<Task> act = async () => await sut.Evaluate(evaluationRequest);
        
        await act.Should().ThrowAsync<AuthZenRequestFailureException>();
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithSingleEvaluationAndResponseContainsRequestId_ShouldAddValueToAuthZenResponse()
    {
        string expectedRequestId = "khsdfibsduvb";
        string response = """
                              {
                                  "decision": true
                              }
                          """;
        
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response),
                Headers = { { "X-Request-ID", expectedRequestId } }
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenEvaluationRequest
        {
            Body = new AuthZenEvaluationBody()
            {
                Subject = new AuthZenSubject
                {
                    Id = "dasfgthb",
                    Type = "aerfbqret"
                }
            }
        };
        
        AuthZenResponse authZenResponse = await sut.Evaluate(evaluationRequest);
        
        authZenResponse.CorrelationId.Should().Be(expectedRequestId);
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithMultipleEvaluationsNoDefaults_ShouldPostToCorrectEndpoint()
    {
        HttpRequestMessage requestSent = null;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, c) => requestSent = r)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(simpleBoxcarResponse)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody()
            {
                Evaluations = new List<AuthZenEvaluationBody>
                {
                    new()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                },
                DefaultValues = new AuthZenEvaluationBody()
            }
        };
        
        
        await sut.Evaluate(evaluationRequest);
        
        requestSent.Should().NotBeNull();
        requestSent.Method.Should().Be(HttpMethod.Post);
        requestSent.RequestUri.Should().Be($"{optionsValue.AuthorizationUrl}/{AuthZenClient.UriBase}/{AuthZenClient.BoxcarUri}");
        requestSent.Content.Headers.ContentType.MediaType.Should().Be("application/json");
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithMultipleEvaluationsNoDefaultsWithCorrelationId_ShouldAddRequestIdHeaderToRequest()
    {
        HttpRequestMessage requestSent = null;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, c) => requestSent = r)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(simpleBoxcarResponse)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            CorrelationId = Guid.NewGuid().ToString(),
            Body = new AuthZenBoxcarEvaluationBody()
            {
                Evaluations = new List<AuthZenEvaluationBody>
                {
                    new()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                },
                DefaultValues = new AuthZenEvaluationBody()
            },
        };
        
        await sut.Evaluate(evaluationRequest);

        requestSent.Headers
            .Should()
            .ContainSingle(h => h.Key == "X-Request-ID"
                                && h.Value.Contains(evaluationRequest.CorrelationId));
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithMultipleEvaluationsNoDefaults_ShouldPostSerializedRequestCorrectly()
    {
        HttpRequestMessage requestSent = null;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, c) => requestSent = r)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(simpleBoxcarResponse)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody()
            {
                Evaluations = new List<AuthZenEvaluationBody>()
                {
                    new()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                },
                DefaultValues = new AuthZenEvaluationBody()
            },
            CorrelationId = Guid.NewGuid().ToString(),
        };
        
        await sut.Evaluate(evaluationRequest);
        
        string sentContent = await requestSent.Content.ReadAsStringAsync();
        
        AuthZenBoxcarRequestMessageDto deserializedRequest = JsonSerializer.Deserialize<AuthZenBoxcarRequestMessageDto>(sentContent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
        
        AdjustBoxcarRequestSerialization(deserializedRequest);
        
        deserializedRequest.Should().BeEquivalentTo(evaluationRequest.Body.ToDto());
    }
    
    [Theory]
    [InlineData("true", Decision.Permit)]
    [InlineData("false", Decision.Deny)]
    public async Task Evaluate_WhenCalledWithMultipleEvaluationsNoDefaults_ShouldParseDecisionCorrectly(string jsonDecision, Decision expectedDecision)
    {
        string response =
            $$"""
                {
                    "evaluations": [
                        {
                            "decision": {{jsonDecision}}
                        },
                        {
                            "decision": {{jsonDecision}}
                        }
                    ]
                }
              """;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody()
            {
                Evaluations = new List<AuthZenEvaluationBody>()
                {
                    new AuthZenEvaluationBody()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                }
            }
        };
        
        AuthZenBoxcarResponse authZenResponse = await sut.Evaluate(evaluationRequest);
        
        authZenResponse.Evaluations.Should().BeEquivalentTo(new List<AuthZenResponse>()
        {
            new ()
            {
                Decision = expectedDecision,
                Context = ""
            },
            new ()
            {
                Decision = expectedDecision,
                Context = ""
            },
        });
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithMultipleEvaluationsNoDefaults_ShouldExtractContextCorrectly()
    {
        string context =
            """
               {
                   "sjdo": "sdfb",
                   "sdjfgn": 73,
                   "sakjdhvuob":{
                       "sdfbvbui":true,
                       "hdfgouh": "iusdfvb"
                   }
               }
            """;

        string response =
            $$"""
                {
                    "evaluations" :[
                        {        
                          "decision": false,
                          "context": {{context}}
                        }
                    ]
                }
              """;
        
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)                         
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody
            {
                Evaluations = new List<AuthZenEvaluationBody>()
                {
                    new AuthZenEvaluationBody()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                }
            }
        };
        
        AuthZenBoxcarResponse authZenResponse = await sut.Evaluate(evaluationRequest);
        
        authZenResponse.Evaluations.Single().Context.Should().Be(context.Trim());
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithMultipleEvaluationsNoDefaultsAndRequestFails_ShouldThrowAuthZenRequestFailureException()
    {
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody
            {
                Evaluations = new List<AuthZenEvaluationBody>()
                {
                    new AuthZenEvaluationBody()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                }
            }
        };
        
        Func<Task> act = async () => await sut.Evaluate(evaluationRequest);
        
        await act.Should().ThrowAsync<AuthZenRequestFailureException>();
    }
        
    [Fact]
    public async Task Evaluate_WhenCalledWithMultipleEvaluationsNoDefaultsAndResponseContainsRequestId_ShouldAddValueToAuthZenResponse()
    {
        string expectedRequestId = "khsdfibsduvb";
        string response = $$"""
                              {
                                  "evaluations" :[
                                      {        
                                        "decision": false
                                      }
                                  ]
                              }
                            """;
        
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response),
                Headers = { { "X-Request-ID", expectedRequestId } }
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody()
            {


                Evaluations = new List<AuthZenEvaluationBody>()
                {
                    new AuthZenEvaluationBody()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                }
            }
        };
        
        var authZenResponse = await sut.Evaluate(evaluationRequest);
        
        authZenResponse.CorrelationId.Should().Be(expectedRequestId);
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithMultipleEvaluationsWithDefaultValues_ShouldPostToCorrectEndpoint()
    {
        HttpRequestMessage requestSent = null;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, c) => requestSent = r)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(simpleBoxcarResponse)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody()
            {
                Evaluations = new List<AuthZenEvaluationBody>
                {
                    new()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                }, 
                DefaultValues = new AuthZenEvaluationBody()
                {
                    Subject = new AuthZenSubject()
                    {
                        Id = "jk;dfgn",
                        Type = "jk;dfbgn;"
                    },
                    Resource = new AuthZenResource()
                    {
                        Type = "hjldfbg",
                        Id = "jkldfbgns"
                    },
                    Action = new AuthZenAction()
                    {
                        Name = "hjkldfgb"
                    }
                }
            }
        };
        
        await sut.Evaluate(evaluationRequest);
        
        requestSent.Should().NotBeNull();
        requestSent.Method.Should().Be(HttpMethod.Post);
        requestSent.RequestUri.Should().Be($"{optionsValue.AuthorizationUrl}/{AuthZenClient.UriBase}/{AuthZenClient.BoxcarUri}");
        requestSent.Content.Headers.ContentType.MediaType.Should().Be("application/json");
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithMultipleEvaluationsWithDefaultValuesWithCorrelationId_ShouldAddRequestIdHeaderToRequest()
    {
        HttpRequestMessage requestSent = null;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, c) => requestSent = r)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(simpleBoxcarResponse)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody()
            {
                Evaluations = new List<AuthZenEvaluationBody>
                {
                    new()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                },
                DefaultValues = new AuthZenEvaluationBody()
                {
                    Subject = new AuthZenSubject()
                    {
                        Id = "jk;dfgn",
                        Type = "jk;dfbgn;"
                    },
                    Resource = new AuthZenResource()
                    {
                        Type = "hjldfbg",
                        Id = "jkldfbgns"
                    },
                    Action = new AuthZenAction()
                    {
                        Name = "hjkldfgb"
                    }
                }
            },
            CorrelationId = Guid.NewGuid().ToString(),
        };
        
        await sut.Evaluate(evaluationRequest);

        requestSent.Headers
            .Should()
            .ContainSingle(h => h.Key == "X-Request-ID"
                                && h.Value.Contains(evaluationRequest.CorrelationId));
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithMultipleEvaluationsWithDefaultValues_ShouldPostSerializedRequestCorrectly()
    {
        HttpRequestMessage requestSent = null;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, c) => requestSent = r)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(simpleBoxcarResponse)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody()
            {
                Evaluations = new List<AuthZenEvaluationBody>()
                {
                    new()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                },
                DefaultValues = new AuthZenEvaluationBody()
                {
                    Subject = new AuthZenSubject()
                    {
                        Id = "jk;dfgn",
                        Type = "jk;dfbgn;"
                    },
                    Resource = new AuthZenResource()
                    {
                        Type = "hjldfbg",
                        Id = "jkldfbgns"
                    },
                    Action = new AuthZenAction()
                    {
                        Name = "hjkldfgb"
                    }
                }
            },
            CorrelationId = Guid.NewGuid().ToString(),
        };
        
        await sut.Evaluate(evaluationRequest);
        
        string sentContent = await requestSent.Content.ReadAsStringAsync();
        
        AuthZenBoxcarRequestMessageDto deserializedRequest = JsonSerializer.Deserialize<AuthZenBoxcarRequestMessageDto>(sentContent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
        
        AdjustBoxcarRequestSerialization(deserializedRequest);
        
        deserializedRequest.Should().BeEquivalentTo(evaluationRequest.Body.ToDto());
    }
    
    [Theory]
    [InlineData("true", Decision.Permit)]
    [InlineData("false", Decision.Deny)]
    public async Task Evaluate_WhenCalledWithMultipleEvaluationsWithDefaultValues_ShouldParseDecisionCorrectly(string jsonDecision, Decision expectedDecision)
    {
        string response =
            $$"""
                {
                    "evaluations": [
                        {
                            "decision": {{jsonDecision}}
                        },
                        {
                            "decision": {{jsonDecision}}
                        }
                    ]
                }
              """;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody()
            {
                Evaluations = new List<AuthZenEvaluationBody>()
                {
                    new()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                },
                DefaultValues = new AuthZenEvaluationBody()
                {
                    Subject = new AuthZenSubject()
                    {
                        Id = "jk;dfgn",
                        Type = "jk;dfbgn;"
                    },
                    Resource = new AuthZenResource()
                    {
                        Type = "hjldfbg",
                        Id = "jkldfbgns"
                    },
                    Action = new AuthZenAction()
                    {
                        Name = "hjkldfgb"
                    }
                }
            }
        };
        
        AuthZenBoxcarResponse authZenResponse = await sut.Evaluate(evaluationRequest);
        
        authZenResponse.Evaluations.Should().BeEquivalentTo(new List<AuthZenResponse>()
        {
            new ()
            {
                Decision = expectedDecision,
                Context = ""
            },
            new ()
            {
                Decision = expectedDecision,
                Context = ""
            },
        });
    }
    
    [Fact]
    public async Task Evaluate_WhenCalledWithMultipleEvaluationsWithDefaultValues_ShouldExtractContextCorrectly()
    {
        string context =
            """
               {
                   "sjdo": "sdfb",
                   "sdjfgn": 73,
                   "sakjdhvuob":{
                       "sdfbvbui":true,
                       "hdfgouh": "iusdfvb"
                   }
               }
            """;

        string response =
            $$"""
                {
                    "evaluations" :[
                        {        
                          "decision": false,
                          "context": {{context}}
                        }
                    ]
                }
              """;
        
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)                         
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody()
            {
                Evaluations = new List<AuthZenEvaluationBody>()
                {
                    new()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                },
                DefaultValues = new AuthZenEvaluationBody()
                {
                    Subject = new AuthZenSubject()
                    {
                        Id = "jk;dfgn",
                        Type = "jk;dfbgn;"
                    },
                    Resource = new AuthZenResource()
                    {
                        Type = "hjldfbg",
                        Id = "jkldfbgns"
                    },
                    Action = new AuthZenAction()
                    {
                        Name = "hjkldfgb"
                    }
                }
            }
        };
        
        AuthZenBoxcarResponse authZenResponse = await sut.Evaluate(evaluationRequest);
        
        authZenResponse.Evaluations.Single().Context.Should().Be(context.Trim());
    }

    [Fact]
    public async Task
        Evaluate_WhenCalledWithMultipleEvaluationsWithDefaultValuesAndRequestFails_ShouldThrowAuthZenRequestFailureException()
    {
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody()
            {
                Evaluations = new List<AuthZenEvaluationBody>()
                {
                    new()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                },
                DefaultValues = new AuthZenEvaluationBody()
                {
                    Subject = new AuthZenSubject()
                    {
                        Id = "jk;dfgn",
                        Type = "jk;dfbgn;"
                    },
                    Resource = new AuthZenResource()
                    {
                        Type = "hjldfbg",
                        Id = "jkldfbgns"
                    },
                    Action = new AuthZenAction()
                    {
                        Name = "hjkldfgb"
                    }
                }
            }
        };
        
        Func<Task> act = async () => await sut.Evaluate(evaluationRequest);
        
        await act.Should().ThrowAsync<AuthZenRequestFailureException>();
    }
        
    [Fact]
    public async Task Evaluate_WhenCalledWithMultipleEvaluationsWithDefaultValuesAndResponseContainsRequestId_ShouldAddValueToAuthZenResponse()
    {
        string expectedRequestId = "khsdfibsduvb";
        string response = $$"""
                              {
                                  "evaluations" :[
                                      {        
                                        "decision": false
                                      }
                                  ]
                              }
                            """;
        
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response),
                Headers = { { "X-Request-ID", expectedRequestId } }
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody()
            {
                Evaluations = new List<AuthZenEvaluationBody>()
                {
                    new()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                },
                DefaultValues = new AuthZenEvaluationBody()
                {
                    Subject = new AuthZenSubject()
                    {
                        Id = "jk;dfgn",
                        Type = "jk;dfbgn;"
                    },
                    Resource = new AuthZenResource()
                    {
                        Type = "hjldfbg",
                        Id = "jkldfbgns"
                    },
                    Action = new AuthZenAction()
                    {
                        Name = "hjkldfgb"
                    }
                }
            }
        };
        
        var authZenResponse = await sut.Evaluate(evaluationRequest);
        
        authZenResponse.CorrelationId.Should().Be(expectedRequestId);
    }
    
    [Theory]
    [InlineData(BoxcarSemantics.DenyOnFirstDeny)]
    [InlineData(BoxcarSemantics.ExecuteAll)]
    [InlineData(BoxcarSemantics.PermitOnFirstPermit)]
    public async Task Evaluate_WhenBoxCarOptions_ShouldApplyOptionsCorrectly(BoxcarSemantics semantics)
    {
        HttpRequestMessage requestSent = null;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, c) => requestSent = r)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(simpleBoxcarResponse)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody()
            {
                Evaluations = new List<AuthZenEvaluationBody>()
                {
                    new()
                    {
                        Subject = new AuthZenSubject
                        {
                            Id = "dasfgthb",
                            Type = "aerfbqret"
                        }
                    }
                },
                DefaultValues = new AuthZenEvaluationBody()
                {
                    Subject = new AuthZenSubject()
                    {
                        Id = "jk;dfgn",
                        Type = "jk;dfbgn;"
                    },
                    Resource = new AuthZenResource()
                    {
                        Type = "hjldfbg",
                        Id = "jkldfbgns"
                    },
                    Action = new AuthZenAction()
                    {
                        Name = "hjkldfgb"
                    }
                },
                Options = new AuthZenBoxcarOptions
                {
                    Semantics = semantics
                }
            },
            CorrelationId = Guid.NewGuid().ToString(),
        };
        
        await sut.Evaluate(evaluationRequest);
        
        string sentContent = await requestSent.Content.ReadAsStringAsync();
        
        AuthZenBoxcarRequestMessageDto deserializedRequest = JsonSerializer.Deserialize<AuthZenBoxcarRequestMessageDto>(sentContent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
        
        AdjustBoxcarRequestSerialization(deserializedRequest);

        var expectation = evaluationRequest.Body.ToDto();
        deserializedRequest.Should().BeEquivalentTo(expectation);
    }

    [Fact]
    public async Task Evaluate_WhenBoxCarEvaluationsIsMissing_ShouldFallbackToSingleEvaluation()
    {
        HttpRequestMessage requestSent = null;
        httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((r, c) => requestSent = r)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(simpleEvaluationResponse)
            });
        
        var sut = CreateSut();

        var evaluationRequest = new AuthZenBoxcarEvaluationRequest()
        {
            Body = new AuthZenBoxcarEvaluationBody()
            {
                Evaluations = new List<AuthZenEvaluationBody>(),
                DefaultValues = new AuthZenEvaluationBody
                {
                    Subject = new AuthZenSubject
                    {
                        Id = "jk;dfgn",
                        Type = "jk;dfbgn;"
                    },
                    Resource = new AuthZenResource
                    {
                        Type = "hjldfbg",
                        Id = "jkldfbgns"
                    },
                    Action = new AuthZenAction
                    {
                        Name = "hjkldfgb"
                    }
                }
            }
        };
        
        await sut.Evaluate(evaluationRequest);
        
        requestSent.Should().NotBeNull();
        requestSent.Method.Should().Be(HttpMethod.Post);
        requestSent.RequestUri.Should().Be($"{optionsValue.AuthorizationUrl}/{AuthZenClient.UriBase}/{AuthZenClient.EvaluationUri}");
        requestSent.Content.Headers.ContentType.MediaType.Should().Be("application/json");
    }
}
