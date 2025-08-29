# AuthZen TypeScript Client

A comprehensive TypeScript client library for interacting with [AuthZen](https://openid.github.io/authzen/)-compliant Policy Decision Points (PDPs). This library implements the AuthZen Authorization API 1.0 specification.

## Features

- ✅ **Access Evaluation API** - Single authorization decisions
- ✅ **Access Evaluations API** - Batch authorization decisions with multiple evaluation semantics
- 🔄 **Search APIs** - Subject, Resource, and Action search (coming soon)
- 🛡️ **Type Safety** - Full TypeScript support with comprehensive type definitions
- 🚀 **Modern** - Built with ES2020, supports both Node.js and browser environments
- 🔧 **Flexible** - Configurable fetch implementation, timeouts, and custom headers
- 📝 **Well Documented** - Comprehensive JSDoc comments and examples

## Installation

```bash
npm install authzen-client
```

For Node.js environments, you'll also need to install node-fetch:

```bash
npm install node-fetch
npm install --save-dev @types/node-fetch
```

## Quick Start

```typescript
import { AuthZenClient, createSubject, createAction, createResource } from 'authzen-client';

// Create a client
const client = new AuthZenClient({
  baseUrl: 'https://pdp.mycompany.com',
  token: 'your-bearer-token', // Optional
});

// Simple access evaluation
const response = await client.evaluate({
  subject: createSubject('user', 'alice@example.com'),
  action: createAction('can_read'),
  resource: createResource('document', '123'),
});

if (response.decision) {
  console.log('✅ Access granted');
} else {
  console.log('❌ Access denied');
}
```

## API Reference

### Client Configuration

```typescript
interface AuthZenClientConfig {
  baseUrl: string;           // PDP base URL (required)
  apiVersion?: string;       // API version (default: 'v1')
  token?: string;           // Bearer token for authentication
  headers?: Record<string, string>; // Custom headers
  timeout?: number;         // Request timeout in ms (default: 30000)
  fetch?: Function;         // Custom fetch implementation
}
```

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
    time: '2024-01-01T12:00:00Z',
    location: 'office'
  }
});
```

### Batch Access Evaluations

Evaluate multiple authorization requests in a single call:

```typescript
const response = await client.evaluateBatch({
  // Default values applied to all evaluations
  subject: createSubject('user', 'alice@example.com'),
  context: { time: new Date().toISOString() },
  
  // Individual evaluations
  evaluations: [
    {
      action: createAction('can_read'),
      resource: createResource('document', '123')
    },
    {
      action: createAction('can_write'),
      resource: createResource('document', '456')
    }
  ],
  
  options: {
    evaluations_semantic: 'execute_all' // or 'deny_on_first_deny' or 'permit_on_first_permit'
  }
});
```

### Evaluation Semantics

The batch evaluation API supports three evaluation semantics:

- **`execute_all`** (default) - Execute all evaluations and return all results
- **`deny_on_first_deny`** - Stop and return on the first denial (short-circuit AND)
- **`permit_on_first_permit`** - Stop and return on the first permit (short-circuit OR)

## Utility Functions

The library provides helpful utility functions for creating AuthZen objects:

```typescript
import { 
  createSubject, 
  createResource, 
  createAction, 
  createContext,
  createContextWithTime,
  SubjectTypes,
  ResourceTypes,
  ActionNames
} from 'authzen-client';

// Create objects with utilities
const user = createSubject(SubjectTypes.USER, 'alice@example.com');
const document = createResource(ResourceTypes.DOCUMENT, '123');
const readAction = createAction(ActionNames.READ);
const context = createContextWithTime({ location: 'office' });
```

### Built-in Constants

```typescript
// Subject types
SubjectTypes.USER      // 'user'
SubjectTypes.SERVICE   // 'service'
SubjectTypes.GROUP     // 'group'

// Resource types  
ResourceTypes.DOCUMENT // 'document'
ResourceTypes.API      // 'api'
ResourceTypes.FOLDER   // 'folder'

// Action names
ActionNames.READ       // 'can_read'
ActionNames.WRITE      // 'can_write'
ActionNames.DELETE     // 'can_delete'
```

## Error Handling

The client throws `AuthZenError` for API-related errors:

```typescript
import { AuthZenError } from 'authzen-client';

try {
  const response = await client.evaluate(request);
} catch (error) {
  if (error instanceof AuthZenError) {
    console.error('Status:', error.status);
    console.error('Message:', error.message);
    console.error('Request ID:', error.requestId);
  }
}
```

## Node.js Usage

For Node.js environments, provide a fetch implementation:

```typescript
import fetch from 'node-fetch';
import { AuthZenClient } from 'authzen-client';

const client = new AuthZenClient({
  baseUrl: 'https://pdp.mycompany.com',
  fetch: fetch as any, // Provide fetch implementation
});
```

## Browser Usage

In browser environments, the global `fetch` API is used automatically:

```typescript
import { AuthZenClient } from 'authzen-client';

const client = new AuthZenClient({
  baseUrl: 'https://pdp.mycompany.com',
  // No fetch needed - uses browser's global fetch
});
```

## Advanced Examples

### Complex Authorization with Rich Context

```typescript
const response = await client.evaluate({
  subject: createSubject('user', 'alice@example.com', {
    department: 'Sales',
    role: 'Manager',
    clearance_level: 'confidential'
  }),
  action: createAction('can_read', {
    method: 'GET',
    api_endpoint: '/documents/123'
  }),
  resource: createResource('document', '123', {
    owner: 'bob@example.com',
    classification: 'confidential',
    project: 'Project Alpha'
  }),
  context: createContextWithTime({
    location: 'office',
    device_type: 'laptop',
    ip_address: '192.168.1.100'
  })
});
```

### Batch Evaluation with Short-Circuit Logic

```typescript
const response = await client.evaluateBatch({
  subject: createSubject('user', 'alice@example.com'),
  evaluations: [
    { action: createAction('can_read'), resource: createResource('document', '1') },
    { action: createAction('can_read'), resource: createResource('document', '2') },
    { action: createAction('can_read'), resource: createResource('document', '3') }
  ],
  options: {
    evaluations_semantic: 'deny_on_first_deny' // Stop on first denial
  }
});

console.log(`Evaluated ${response.evaluations.length} requests`);
```

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

## Contributing

Contributions are welcome! Please read our contributing guidelines and submit pull requests for any improvements.

## License

MIT License - see LICENSE file for details.

## Related

- [AuthZen Specification](https://openid.github.io/authzen/)
- [OpenID Foundation](https://openid.net/)
- [Policy Decision Points (PDPs)](https://en.wikipedia.org/wiki/XACML#Policy_Decision_Point_(PDP))

## Changelog

### 1.0.0

- Initial release
- Support for Access Evaluation API
- Support for Access Evaluations API (batch)
- TypeScript support
- Utility functions
- Comprehensive documentation
