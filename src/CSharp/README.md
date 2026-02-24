# AuthZen C# Client

A C# client library for interacting with [AuthZen](https://openid.github.io/authzen/)-compliant Policy Decision Points (PDPs). This library implements the AuthZen Authorization API 1.0 specification.

## Features

- **Discovery** - Automatic PDP configuration via `/.well-known/authzen-configuration`
- **Access Evaluation API** - Single authorization decisions
- **Access Evaluations API** - Batch (boxcar) authorization decisions with multiple evaluation semantics
- **Fluent Builders** - Builder pattern for constructing evaluation requests
- **Property Bags** - Flexible key-value properties on subjects, resources, actions, and context
- **Compatibility** - Targets .NET Standard 2.0 for broad runtime support

## Installation

```bash
dotnet add package Rsk.AuthZen.Client
```

Or via the NuGet Package Manager:

```
Install-Package Rsk.AuthZen.Client
```

## Quick Start

The client uses AuthZen discovery to automatically resolve evaluation endpoints. On the first call to `Evaluate()`, the client fetches `/.well-known/authzen-configuration` from the authorization URL and caches the result.

### Registration

Register the client in your dependency injection container:

```csharp
services.AddHttpClient();
services.Configure<AuthZenClientOptions>(options =>
{
    options.AuthorizationUrl = "https://pdp.mycompany.com";
});
services.AddTransient<IAuthZenClient, AuthZenClient>();
```

### Basic Evaluation

```csharp
// Build a single evaluation request using the fluent builder
var request = new AuthZenSingleRequestBuilder()
    .SetSubject("alice@example.com", "user")
    .SetAction("can_read")
    .SetResource("123", "document")
    .Build();

// Evaluate
AuthZenResponse response = await authZenClient.Evaluate(request);

if (response.Decision == Decision.Permit)
{
    Console.WriteLine("Access granted");
}
else
{
    Console.WriteLine("Access denied");
}
```

## API Reference

### Client Configuration

The client is configured via `AuthZenClientOptions` and uses `IHttpClientFactory` for HTTP requests:

```csharp
public class AuthZenClientOptions
{
    /// <summary>
    /// Base URL of the AuthZen authorization service (required).
    /// </summary>
    public string AuthorizationUrl { get; set; }
}
```

The `AuthZenClient` constructor requires:

| Parameter | Type | Description |
|---|---|---|
| `httpClientFactory` | `IHttpClientFactory` | Factory for creating `HttpClient` instances |
| `options` | `IOptions<AuthZenClientOptions>` | Configuration options |

### Discovery

Fetch the PDP's AuthZen configuration from `/.well-known/authzen-configuration`. This is called automatically before the first evaluation, but you can also call it explicitly:

```csharp
AuthZenMetadataResponse metadata = await authZenClient.GetMetadata();

Console.WriteLine(metadata.PolicyDecisionPoint);
Console.WriteLine(metadata.AccessEvaluationEndpoint);
Console.WriteLine(metadata.AccessEvaluationsEndpoint);
```

The returned `AuthZenMetadataResponse` contains:

| Property | Type | Description |
|---|---|---|
| `PolicyDecisionPoint` | `string` | Base URL of the PDP (required) |
| `AccessEvaluationEndpoint` | `string` | Single evaluation endpoint (required) |
| `AccessEvaluationsEndpoint` | `string` | Batch evaluations endpoint |
| `SearchSubjectEndpoint` | `string` | Subject search endpoint |
| `SearchResourceEndpoint` | `string` | Resource search endpoint |
| `SearchActionEndpoint` | `string` | Action search endpoint |

### Single Access Evaluation

Build and evaluate a single authorization request using `AuthZenSingleRequestBuilder`:

```csharp
var request = new AuthZenSingleRequestBuilder()
    .SetCorrelationId("req-12345")
    .SetSubject("alice@example.com", "user")
        .Add("department", "Sales")
        .Add("role", "Manager")
    .SetAction("can_read")
        .Add("method", "GET")
    .SetResource("123", "document")
        .Add("classification", "confidential")
    .SetContext()
        .Add("location", "office")
        .Add("time", DateTime.UtcNow.ToString("o"))
    .Build();

AuthZenResponse response = await authZenClient.Evaluate(request);

Console.WriteLine($"Decision: {response.Decision}");     // Permit or Deny
Console.WriteLine($"Correlation: {response.CorrelationId}");
Console.WriteLine($"Context: {response.Context}");
```

The builder methods `SetSubject()`, `SetAction()`, `SetResource()`, and `SetContext()` each return an `IAuthZenPropertyBag`, allowing you to chain `.Add(name, value)` calls to attach additional properties.

### Response

The `AuthZenResponse` contains:

| Property | Type | Description |
|---|---|---|
| `Decision` | `Decision` | `Decision.Permit` or `Decision.Deny` |
| `Context` | `string` | Context information from the PDP response |
| `CorrelationId` | `string` | Request correlation ID from the `X-Request-ID` header |

### Batch (Boxcar) Access Evaluations

Evaluate multiple authorization requests in a single call using `AuthZenBoxcarRequestBuilder`:

```csharp
var request = new AuthZenBoxcarRequestBuilder()
    .SetCorrelationId("batch-001")
    // Default values applied to any evaluation that omits the field
    .SetDefaultSubject("alice@example.com", "user")
    .SetDefaultAction("can_read")
    // Individual evaluations
    .AddRequest()
        .SetResource("doc-1", "document")
    .AddRequest()
        .SetResource("doc-2", "document")
        .SetAction("can_write")  // Overrides the default action
    .AddRequest()
        .SetSubject("bob@example.com", "user")  // Overrides the default subject
        .SetResource("doc-3", "document")
    .Build();

AuthZenBoxcarResponse response = await authZenClient.Evaluate(request);

foreach (var evaluation in response.Evaluations)
{
    Console.WriteLine($"Decision: {evaluation.Decision}");
}
```

#### Default Values

The boxcar builder supports setting default values for subject, resource, action, and context via `SetDefaultSubject()`, `SetDefaultResource()`, `SetDefaultAction()`, and `SetDefaultContext()`. These defaults are applied to any individual evaluation that does not specify that field. Individual evaluations override defaults when both are supplied.

#### Fallback Behaviour

If a boxcar request contains no individual evaluations (only defaults), the client automatically falls back to a single evaluation using the default values.

If the PDP does not advertise a batch evaluations endpoint (`AccessEvaluationsEndpoint` is missing from discovery), the client throws a `NotSupportedException`.

### Evaluation Semantics

The batch evaluation API supports three evaluation semantics via `SetEvaluationSemantics()`:

```csharp
var request = new AuthZenBoxcarRequestBuilder()
    .SetDefaultSubject("alice@example.com", "user")
    .SetEvaluationSemantics(BoxcarSemantics.DenyOnFirstDeny)
    .AddRequest()
        .SetAction("can_read")
        .SetResource("doc-1", "document")
    .AddRequest()
        .SetAction("can_read")
        .SetResource("doc-2", "document")
    .Build();
```

| Semantic | Description |
|---|---|
| `BoxcarSemantics.ExecuteAll` | Execute all evaluations and return all results (default) |
| `BoxcarSemantics.DenyOnFirstDeny` | Stop and return on the first denial (short-circuit AND) |
| `BoxcarSemantics.PermitOnFirstPermit` | Stop and return on the first permit (short-circuit OR) |

## Error Handling

The client throws `AuthZenRequestFailureException` when HTTP requests to the PDP fail:

```csharp
try
{
    var response = await authZenClient.Evaluate(request);
}
catch (AuthZenRequestFailureException ex)
{
    // HTTP error from the PDP (e.g. 400, 401, 500)
    Console.Error.WriteLine($"AuthZen request failed: {ex.Message}");
}
catch (NotSupportedException ex)
{
    // Batch endpoint not supported by this PDP
    Console.Error.WriteLine($"Not supported: {ex.Message}");
}
```

The client also implements automatic retry on 404 responses — if an evaluation endpoint returns 404, the client re-fetches the discovery metadata and retries once.

## Advanced Examples

### Rich Context Evaluation

```csharp
var request = new AuthZenSingleRequestBuilder()
    .SetSubject("alice@example.com", "user")
        .Add("department", "Sales")
        .Add("role", "Manager")
        .Add("clearance_level", "confidential")
    .SetAction("can_read")
        .Add("method", "GET")
        .Add("api_endpoint", "/documents/123")
    .SetResource("123", "document")
        .Add("owner", "bob@example.com")
        .Add("classification", "confidential")
        .Add("project", "Project Alpha")
    .SetContext()
        .Add("location", "office")
        .Add("device_type", "laptop")
        .Add("ip_address", "192.168.1.100")
        .Add("time", DateTime.UtcNow.ToString("o"))
    .Build();

AuthZenResponse response = await authZenClient.Evaluate(request);
```

### Batch Evaluation with Short-Circuit Logic

```csharp
var request = new AuthZenBoxcarRequestBuilder()
    .SetDefaultSubject("alice@example.com", "user")
    .SetEvaluationSemantics(BoxcarSemantics.DenyOnFirstDeny)
    .AddRequest()
        .SetAction("can_read")
        .SetResource("1", "document")
    .AddRequest()
        .SetAction("can_read")
        .SetResource("2", "document")
    .AddRequest()
        .SetAction("can_read")
        .SetResource("3", "document")
    .Build();

AuthZenBoxcarResponse response = await authZenClient.Evaluate(request);
Console.WriteLine($"Evaluated {response.Evaluations.Count} requests");
```

### Batch Evaluation with Mixed Defaults

```csharp
var request = new AuthZenBoxcarRequestBuilder()
    // Defaults applied to evaluations that omit the field
    .SetDefaultSubject("default-user@company.com", "user")
    .SetDefaultResource("shared-document", "document")
    .SetDefaultAction("read")
    .SetDefaultContext()
        .Add("environment", "production")
    // Override subject only
    .AddRequest()
        .SetSubject("alice@company.com", "user")
    // Override resource and action
    .AddRequest()
        .SetResource("user-service", "api")
        .SetAction("execute")
    .Build();

AuthZenBoxcarResponse response = await authZenClient.Evaluate(request);
```

## Compatibility

The library targets **.NET Standard 2.0**, which is supported by:

- .NET Core 2.0+
- .NET 5+
- .NET Framework 4.6.1+

### Dependencies

| Package | Version |
|---|---|
| `Microsoft.Extensions.Http` | 9.0.3 |
| `Microsoft.Extensions.Options` | 9.0.3 |
| `System.Text.Json` | 9.0.3 |

## Development

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

## License

See LICENSE file for details.

## Related

- [AuthZen Specification](https://openid.github.io/authzen/)
- [OpenID Foundation](https://openid.net/)