# AuthZen TypeScript Client

A TypeScript client library for interacting with [AuthZen](https://openid.github.io/authzen/)-compliant Policy Decision Points (PDPs). This library implements the AuthZen Authorization API 1.0 specification.

## Features

- **Discovery** - Automatic PDP configuration via `/.well-known/authzen-configuration`
- **Access Evaluation API** - Single authorization decisions
- **Access Evaluations API** - Batch authorization decisions with multiple evaluation semantics
- **Type Safety** - Full TypeScript support with comprehensive type definitions
- **Validation** - Built-in request validation with descriptive error messages
- **Modern** - Uses global `fetch`; works with Node.js 18+ and modern browsers

## Installation

```bash
npm install @rocksolidknowledge/authzen-client
```

## Quick Start

The client uses AuthZen discovery to automatically resolve evaluation endpoints. On the first call to `evaluate()` or `evaluations()`, the client fetches `/.well-known/authzen-configuration` from the PDP URL and caches the result.

```typescript
import { AuthZenClient } from '@rocksolidknowledge/authzen-client';

// Create a client pointing at your PDP
const client = new AuthZenClient({
  pdpUrl: 'https://pdp.mycompany.com',
  token: 'your-bearer-token', // Optional
});

// Single access evaluation
const response = await client.evaluate({
  subject: { type: 'user', id: 'alice@example.com' },
  action: { name: 'can_read' },
  resource: { type: 'document', id: '123' },
});

if (response.decision) {
  console.log('Access granted');
} else {
  console.log('Access denied');
}
```

## API Reference

### Client Configuration

```typescript
import { AuthZenClientConfig } from '@rocksolidknowledge/authzen-client';

const client = new AuthZenClient({
  pdpUrl: 'https://pdp.mycompany.com',  // PDP base URL (required)
  token: 'your-bearer-token',           // Bearer token for authentication (optional)
  headers: { 'X-Custom': 'value' },     // Additional request headers (optional)
  timeout: 10000,                        // Request timeout in ms (default: 10000)
});
```

### Discovery

Fetch the PDP's AuthZen configuration from `/.well-known/authzen-configuration`. This is called automatically before the first evaluation, but you can also call it explicitly:

```typescript
const config = await client.discover();

console.log(config.policy_decision_point);
console.log(config.access_evaluation_endpoint);
console.log(config.access_evaluations_endpoint);
```

The returned `AuthZenConfiguration` object may contain:

| Property | Description |
|---|---|
| `policy_decision_point` | Base URL of the PDP (required) |
| `access_evaluation_endpoint` | Single evaluation endpoint |
| `access_evaluations_endpoint` | Batch evaluations endpoint |
| `search_subject_endpoint` | Subject search endpoint |
| `search_resource_endpoint` | Resource search endpoint |
| `search_action_endpoint` | Action search endpoint |

### Single Access Evaluation

Evaluate a single authorization request:

```typescript
const response = await client.evaluate({
  subject: {
    type: 'user',
    id: 'alice@example.com',
    properties: { department: 'Sales' }
  },
  action: {
    name: 'can_read',
    properties: { method: 'GET' }
  },
  resource: {
    type: 'document',
    id: '123',
    properties: { classification: 'confidential' }
  },
  context: {
    time: new Date().toISOString(),
    location: 'office'
  }
});

console.log(response.decision); // true or false
```

For single evaluations, `subject`, `action`, and `resource` are all required.

### Batch Access Evaluations

Evaluate multiple authorization requests in a single call using `evaluations()`:

```typescript
const response = await client.evaluations({
  // Default values applied to all evaluations
  subject: { type: 'user', id: 'alice@example.com' },
  context: { time: new Date().toISOString() },

  // Individual evaluations (can omit fields covered by defaults)
  // Values in individual evaluations will override a default if both are supplied
  evaluations: [
    {
      action: { name: 'can_read' },
      resource: { type: 'document', id: '123' }
    },
    {
      action: { name: 'can_write' },
      resource: { type: 'document', id: '456' }
    }
  ],

  options: {
    evaluations_semantic: 'execute_all'
  }
});

response.evaluations.forEach((result, i) => {
  console.log(`Evaluation ${i}: ${result.decision ? 'ALLOW' : 'DENY'}`);
});
```

### Evaluation Semantics

The batch evaluation API supports three evaluation semantics via `options.evaluations_semantic`:

| Semantic | Description |
|---|---|
| `execute_all` | Execute all evaluations and return all results (default) |
| `deny_on_first_deny` | Stop and return on the first denial (short-circuit AND) |
| `permit_on_first_permit` | Stop and return on the first permit (short-circuit OR) |

## Error Handling

The client provides specific error classes for different failure modes:

```typescript
import {
  AuthZenError,
  AuthZenValidationError,
  AuthZenRequestError,
  AuthZenResponseError,
  AuthZenNetworkError,
  AuthZenDiscoveryError,
} from '@rocksolidknowledge/authzen-client';

try {
  const response = await client.evaluate(request);
} catch (error) {
  if (error instanceof AuthZenValidationError) {
    // Request failed local validation (e.g. missing subject)
    console.error('Validation:', error.message);
  } else if (error instanceof AuthZenDiscoveryError) {
    // Discovery endpoint returned invalid configuration
    console.error('Discovery:', error.message);
  } else if (error instanceof AuthZenRequestError) {
    // PDP returned an HTTP error (4xx/5xx)
    console.error('HTTP error:', error.statusCode, error.message);
    console.error('Response data:', error.responseData);
  } else if (error instanceof AuthZenResponseError) {
    // Response was not valid JSON
    console.error('Response error:', error.statusCode, error.message);
  } else if (error instanceof AuthZenNetworkError) {
    // Network failure or timeout
    console.error('Network:', error.message);
  } else if (error instanceof AuthZenError) {
    // Base error class — catches all of the above
    console.error('AuthZen error:', error.message);
    console.error('Request ID:', error.requestId);
  }
}
```

All error classes extend `AuthZenError`, which includes an optional `requestId` for correlation.

## Built-in Constants

The library exports commonly used constant values:

```typescript
import { SubjectTypes, ResourceTypes, ActionNames, HttpStatusCodes } from '@rocksolidknowledge/authzen-client';

// Subject types
SubjectTypes.USER       // 'user'
SubjectTypes.SERVICE    // 'service'
SubjectTypes.GROUP      // 'group'
SubjectTypes.ROLE       // 'role'

// Resource types
ResourceTypes.DOCUMENT  // 'document'
ResourceTypes.API       // 'api'
ResourceTypes.FOLDER    // 'folder'
ResourceTypes.DATABASE  // 'database'
ResourceTypes.SERVICE   // 'service'

// Action names
ActionNames.READ        // 'can_read'
ActionNames.WRITE       // 'can_write'
ActionNames.DELETE      // 'can_delete'
ActionNames.EXECUTE     // 'can_execute'
ActionNames.CREATE      // 'can_create'
ActionNames.UPDATE      // 'can_update'
ActionNames.VIEW        // 'can_view'
ActionNames.EDIT        // 'can_edit'
```

## Advanced Examples

### Rich Context Evaluation

```typescript
const response = await client.evaluate({
  subject: {
    type: 'user',
    id: 'alice@example.com',
    properties: {
      department: 'Sales',
      role: 'Manager',
      clearance_level: 'confidential'
    }
  },
  action: {
    name: 'can_read',
    properties: {
      method: 'GET',
      api_endpoint: '/documents/123'
    }
  },
  resource: {
    type: 'document',
    id: '123',
    properties: {
      owner: 'bob@example.com',
      classification: 'confidential',
      project: 'Project Alpha'
    }
  },
  context: {
    location: 'office',
    device_type: 'laptop',
    ip_address: '192.168.1.100',
    time: new Date().toISOString()
  }
});
```

### Batch Evaluation with Short-Circuit Logic

```typescript
const response = await client.evaluations({
  subject: { type: 'user', id: 'alice@example.com' },
  evaluations: [
    { action: { name: 'can_read' }, resource: { type: 'document', id: '1' } },
    { action: { name: 'can_read' }, resource: { type: 'document', id: '2' } },
    { action: { name: 'can_read' }, resource: { type: 'document', id: '3' } }
  ],
  options: {
    evaluations_semantic: 'deny_on_first_deny' // Stop on first denial
  }
});

console.log(`Evaluated ${response.evaluations.length} requests`);
```

### Batch Evaluation with Mixed Defaults

Individual evaluations can omit fields that are provided as top-level defaults:

```typescript
const response = await client.evaluations({
  // These defaults apply to any evaluation that omits the field
  subject: { type: 'user', id: 'default-user@company.com' },
  resource: { type: 'document', id: 'shared-document' },
  action: { name: 'read' },
  context: { environment: 'production' },

  evaluations: [
    // Uses all defaults
    {},
    // Overrides subject only
    { subject: { type: 'user', id: 'alice@company.com' } },
    // Overrides resource and action
    {
      resource: { type: 'api', id: 'user-service' },
      action: { name: 'execute' }
    },
  ]
});
```

## Compatibility

The client uses the global `fetch` API, which is available natively in:

- **Node.js** 18+ (built-in global `fetch`)
- **Modern browsers** (Chrome, Firefox, Safari, Edge)

No additional fetch polyfill is required for these environments.

## Development

### Building

```bash
npm run build
```

### Testing

```bash
npm test
```

### Linting

```bash
npm run lint
```

## License

MIT License - see LICENSE file for details.

## Related

- [AuthZen Specification](https://openid.github.io/authzen/)
- [OpenID Foundation](https://openid.net/)
- [Policy Decision Points (PDPs)](https://en.wikipedia.org/wiki/XACML#Policy_Decision_Point_(PDP))

## Changelog

### 1.0.0

- Initial release
- Support endpoint discovery
- Support for Access Evaluation API
- Support for Access Evaluations API (batch)
- TypeScript support
- Documentation
