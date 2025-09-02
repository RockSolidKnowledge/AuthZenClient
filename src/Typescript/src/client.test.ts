import { AuthZenClient } from './client';
import {
  AuthZenError,
  AuthZenRequestError,
  AuthZenResponseError,
  AuthZenNetworkError,
  AuthZenValidationError,
  AccessEvaluationRequest,
  AccessEvaluationsRequest,
} from './types';

// Mock global fetch
const mockFetch = jest.fn();
global.fetch = mockFetch;

describe('AuthZenClient', () => {
  beforeEach(() => {
    mockFetch.mockReset();
    jest.clearAllTimers();
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  // Helper to mock discovery before each test that expects an API call
  const setupDiscovery = (overrides = {}) => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: jest.fn().mockResolvedValue({
        policy_decision_point: 'https://example.com',
        access_evaluation_endpoint: 'https://example.com/setupDiscovery/evaluation',
        access_evaluations_endpoint: 'https://example.com/setupDiscovery/evaluations',
        ...overrides,
      }),
      headers: {
        get: jest.fn().mockReturnValue('application/json')
      },
    }); 
  };

  describe('constructor', () => {
    it('should create a client with required config', () => {
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      expect(client).toBeInstanceOf(AuthZenClient);
      expect(client.pdpUrl).toBe('https://example.com');
    });

    it('should normalize PDP URL by removing trailing slash', () => {
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com/',
      });

      expect(client.pdpUrl).toBe('https://example.com');
    });
  });

  describe('evaluate', () => {
    const validRequest: AccessEvaluationRequest = {
      subject: { type: 'user', id: 'alice@example.com' },
      action: { name: 'can_read' },
      resource: { type: 'document', id: '123' },
    };

    it('should call discover and use the absolute evaluation endpoint from discovery result on first evaluate', async () => {
      const mockDiscoveryResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          policy_decision_point: 'https://example.com',
          access_evaluation_endpoint: 'https://example.com/custom/evaluate', // absolute URI
        }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mockEvaluateResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({ decision: true }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      // First call is discovery, second is evaluate
      mockFetch
        .mockResolvedValueOnce(mockDiscoveryResponse)
        .mockResolvedValueOnce(mockEvaluateResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
        token: 'test-token',
      });

      const response = await client.evaluate(validRequest);

      // First call: discovery
      expect(mockFetch).toHaveBeenNthCalledWith(
        1,
        'https://example.com/.well-known/authzen-configuration',
        expect.objectContaining({
          method: 'GET',
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
            'Accept': 'application/json',
            'Authorization': 'Bearer test-token',
            'X-Request-ID': expect.stringMatching(/^authzen-\d+-[a-z0-9]+$/),
          }),
          signal: expect.any(AbortSignal),
        })
      );

      // Second call: evaluate using absolute endpoint from discovery
      expect(mockFetch).toHaveBeenNthCalledWith(
        2,
        'https://example.com/custom/evaluate',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify(validRequest),
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
            'Accept': 'application/json',
            'Authorization': 'Bearer test-token',
            'X-Request-ID': expect.stringMatching(/^authzen-\d+-[a-z0-9]+$/),
          }),
          signal: expect.any(AbortSignal),
        })
      );

      expect(response).toEqual({ decision: true });
    });

    it('should use cached discovery result for subsequent evaluate calls', async () => {
      const mockDiscoveryResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          policy_decision_point: 'https://example.com',
          access_evaluation_endpoint: 'https://example.com/custom/evaluate', // absolute URI
        }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mockEvaluateResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({ decision: true }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      // First call is discovery, then two evaluate calls
      mockFetch
        .mockResolvedValueOnce(mockDiscoveryResponse)
        .mockResolvedValue(mockEvaluateResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
        token: 'test-token',
      });

      const response1 = await client.evaluate(validRequest);
      const response2 = await client.evaluate(validRequest);

      // Only one discovery call
      expect(mockFetch).toHaveBeenNthCalledWith(
        1,
        'https://example.com/.well-known/authzen-configuration',
        expect.anything()
      );

      // Both evaluate calls use the absolute endpoint from discovery
      expect(mockFetch).toHaveBeenNthCalledWith(
        2,
        'https://example.com/custom/evaluate',
        expect.anything()
      );
      expect(mockFetch).toHaveBeenNthCalledWith(
        3,
        'https://example.com/custom/evaluate',
        expect.anything()
      );

      expect(response1).toEqual({ decision: true });
      expect(response2).toEqual({ decision: true });
    });

    it('should retry discovery and evaluation once if evaluation endpoint returns 404', async () => {
      const mockDiscoveryResponse1 = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          policy_decision_point: 'https://example.com',
          access_evaluation_endpoint: 'https://example.com/custom/evaluate',
        }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mockDiscoveryResponse2 = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          policy_decision_point: 'https://example.com',
          access_evaluation_endpoint: 'https://example.com/custom/evaluate2',
        }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mock404Response = {
        ok: false,
        status: 404,
        statusText: 'Not Found',
        json: jest.fn().mockResolvedValue({ message: 'Not Found' }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mockEvaluateResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({ decision: true }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      // Sequence: discover, 404, discover, success
      mockFetch
        .mockResolvedValueOnce(mockDiscoveryResponse1) // initial discover
        .mockResolvedValueOnce(mock404Response)        // first evaluate (404)
        .mockResolvedValueOnce(mockDiscoveryResponse2) // retry discover
        .mockResolvedValueOnce(mockEvaluateResponse);  // retry evaluate (success)

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
        token: 'test-token',
      });

      const response = await client.evaluate(validRequest);

      // First call: initial discovery
      expect(mockFetch).toHaveBeenNthCalledWith(
        1,
        'https://example.com/.well-known/authzen-configuration',
        expect.anything()
      );

      // Second call: first evaluate (404)
      expect(mockFetch).toHaveBeenNthCalledWith(
        2,
        'https://example.com/custom/evaluate',
        expect.anything()
      );

      // Third call: retry discovery
      expect(mockFetch).toHaveBeenNthCalledWith(
        3,
        'https://example.com/.well-known/authzen-configuration',
        expect.anything()
      );

      // Fourth call: retry evaluate (success)
      expect(mockFetch).toHaveBeenNthCalledWith(
        4,
        'https://example.com/custom/evaluate2',
        expect.anything()
      );

      expect(response).toEqual({ decision: true });
    });

    it('should return error if evaluation endpoint returns 404 twice', async () => {
      const mockDiscoveryResponse1 = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          policy_decision_point: 'https://example.com',
          access_evaluation_endpoint: 'https://example.com/custom/evaluate',
        }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mockDiscoveryResponse2 = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          policy_decision_point: 'https://example.com',
          access_evaluation_endpoint: 'https://example.com/custom/evaluate2',
        }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mock404Response = {
        ok: false,
        status: 404,
        statusText: 'Not Found',
        json: jest.fn().mockResolvedValue({ message: 'Not Found' }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      // Sequence: discover, 404, discover, 404
      mockFetch
        .mockResolvedValueOnce(mockDiscoveryResponse1) // initial discover
        .mockResolvedValueOnce(mock404Response)        // first evaluate (404)
        .mockResolvedValueOnce(mockDiscoveryResponse2) // retry discover
        .mockResolvedValueOnce(mock404Response);       // retry evaluate (404)

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
        token: 'test-token',
      });

      await expect(client.evaluate(validRequest)).rejects.toThrow(AuthZenRequestError);

      // First call: initial discovery
      expect(mockFetch).toHaveBeenNthCalledWith(
        1,
        'https://example.com/.well-known/authzen-configuration',
        expect.anything()
      );

      // Second call: first evaluate (404)
      expect(mockFetch).toHaveBeenNthCalledWith(
        2,
        'https://example.com/custom/evaluate',
        expect.anything()
      );

      // Third call: retry discovery
      expect(mockFetch).toHaveBeenNthCalledWith(
        3,
        'https://example.com/.well-known/authzen-configuration',
        expect.anything()
      );

      // Fourth call: retry evaluate (404)
      expect(mockFetch).toHaveBeenNthCalledWith(
        4,
        'https://example.com/custom/evaluate2',
        expect.anything()
      );
    });

    it('should make correct API call for access evaluation', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({ decision: true }),
        headers: { 
          get: jest.fn().mockReturnValue('application/json') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
        token: 'test-token',
      });

      const response = await client.evaluate(validRequest);

      expect(mockFetch).toHaveBeenNthCalledWith(
        1,
        'https://example.com/.well-known/authzen-configuration',
        expect.anything()
      );
      expect(mockFetch).toHaveBeenNthCalledWith(
        2,
        'https://example.com/setupDiscovery/evaluation',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify(validRequest),
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
            'Accept': 'application/json',
            'Authorization': 'Bearer test-token',
            'X-Request-ID': expect.stringMatching(/^authzen-\d+-[a-z0-9]+$/),
          }),
          signal: expect.any(AbortSignal),
        })
      );

      expect(response).toEqual({ decision: true });
    });

    it('should handle HTTP errors', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: false,
        status: 403,
        statusText: 'Forbidden',
        json: jest.fn().mockResolvedValue({ message: 'Access denied' }),
        headers: { 
          get: jest.fn().mockReturnValue('application/json') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      await expect(client.evaluate(validRequest)).rejects.toThrow(AuthZenRequestError);
    });

    it('should handle network errors', async () => {
      setupDiscovery();
      mockFetch.mockRejectedValue(new Error('Network error'));

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      await expect(client.evaluate(validRequest)).rejects.toThrow(AuthZenNetworkError);
    });

    it('should handle invalid JSON responses', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockRejectedValue(new Error('Invalid JSON')),
        headers: { 
          get: jest.fn().mockReturnValue('application/json') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      await expect(client.evaluate(validRequest)).rejects.toThrow(AuthZenResponseError);
    });

    it('should handle non-JSON responses', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: true,
        status: 200,
        text: jest.fn().mockResolvedValue('Not JSON'),
        headers: { 
          get: jest.fn().mockReturnValue('text/plain') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      await expect(client.evaluate(validRequest)).rejects.toThrow(AuthZenResponseError);
    });

    it('should validate request before sending', async () => {
      setupDiscovery();
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const invalidRequest = {
        subject: { type: 'user' }, // Missing id
        action: { name: 'can_read' },
        resource: { type: 'document', id: '123' },
      } as AccessEvaluationRequest;

      await expect(client.evaluate(invalidRequest)).rejects.toThrow(AuthZenValidationError);
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should include Authorization header with bearer token when provided', async () => {
    const mockDiscoveryResponse = {
      ok: true,
      status: 200,
      json: jest.fn().mockResolvedValue({
        policy_decision_point: 'https://example.com',
        access_evaluation_endpoint: 'https://example.com/custom/evaluate',
      }),
      headers: {
        get: jest.fn().mockReturnValue('application/json')
      },
    };

    const mockEvaluateResponse = {
      ok: true,
      status: 200,
      json: jest.fn().mockResolvedValue({ decision: true }),
      headers: {
        get: jest.fn().mockReturnValue('application/json')
      },
    };

    mockFetch
      .mockResolvedValueOnce(mockDiscoveryResponse)
      .mockResolvedValueOnce(mockEvaluateResponse);

    const client = new AuthZenClient({
      pdpUrl: 'https://example.com',
      token: 'my-secret-token',
    });

    await client.evaluate(validRequest);

    // Second call: evaluate should include Authorization header
    expect(mockFetch).toHaveBeenNthCalledWith(
      2,
      'https://example.com/custom/evaluate',
      expect.objectContaining({
        method: 'POST',
        headers: expect.objectContaining({
          'Authorization': 'Bearer my-secret-token',
        }),
        body: JSON.stringify(validRequest),
        signal: expect.any(AbortSignal),
      })
    );
  });
  });

  describe('evaluations', () => {
    const validRequest: AccessEvaluationsRequest = {
      evaluations: [
        {
          subject: { type: 'user', id: 'alice@example.com' },
          action: { name: 'can_read' },
          resource: { type: 'document', id: '123' },
        },
        {
          subject: { type: 'user', id: 'bob@example.com' },
          action: { name: 'can_write' },
          resource: { type: 'document', id: '456' },
        },
      ],
    };

    it('should call discover and use the absolute evaluations endpoint from discovery result on first evaluations', async () => {
      const mockDiscoveryResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          policy_decision_point: 'https://example.com',
          access_evaluations_endpoint: 'https://example.com/custom/evaluations', // absolute URI
        }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mockEvaluationsResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({ evaluations: [{ decision: true }, { decision: false }] }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      mockFetch
        .mockResolvedValueOnce(mockDiscoveryResponse)
        .mockResolvedValueOnce(mockEvaluationsResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
        token: 'test-token',
      });

      const response = await client.evaluations(validRequest);

      expect(mockFetch).toHaveBeenNthCalledWith(
        1,
        'https://example.com/.well-known/authzen-configuration',
        expect.anything()
      );

      expect(mockFetch).toHaveBeenNthCalledWith(
        2,
        'https://example.com/custom/evaluations',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify(validRequest),
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
            'Accept': 'application/json',
            'Authorization': 'Bearer test-token',
            'X-Request-ID': expect.stringMatching(/^authzen-\d+-[a-z0-9]+$/),
          }),
          signal: expect.any(AbortSignal),
        })
      );

      expect(response).toEqual({ evaluations: [{ decision: true }, { decision: false }] });
    });

    it('should use cached discovery result for subsequent evaluations calls', async () => {
      const mockDiscoveryResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          policy_decision_point: 'https://example.com',
          access_evaluations_endpoint: 'https://example.com/custom/evaluations', // absolute URI
        }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mockEvaluationsResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({ evaluations: [{ decision: true }, { decision: false }] }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      // First call is discovery, then two evaluate calls
      mockFetch
        .mockResolvedValueOnce(mockDiscoveryResponse)
        .mockResolvedValue(mockEvaluationsResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
        token: 'test-token',
      });

      const response1 = await client.evaluations(validRequest);
      const response2 = await client.evaluations(validRequest);

      // Only one discovery call
      expect(mockFetch).toHaveBeenNthCalledWith(
        1,
        'https://example.com/.well-known/authzen-configuration',
        expect.anything()
      );

      // Both evaluate calls use the absolute endpoint from discovery
      expect(mockFetch).toHaveBeenNthCalledWith(
        2,
        'https://example.com/custom/evaluations',
        expect.anything()
      );
      expect(mockFetch).toHaveBeenNthCalledWith(
        3,
        'https://example.com/custom/evaluations',
        expect.anything()
      );

      expect(response1).toEqual({ evaluations: [{ decision: true }, { decision: false }] });
      expect(response2).toEqual({ evaluations: [{ decision: true }, { decision: false }] });
    });

    it('should retry discovery and evaluations once if evaluations endpoint returns 404', async () => {
      const mockDiscoveryResponse1 = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          policy_decision_point: 'https://example.com',
          access_evaluations_endpoint: 'https://example.com/custom/evaluations',
        }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mockDiscoveryResponse2 = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          policy_decision_point: 'https://example.com',
          access_evaluations_endpoint: 'https://example.com/custom/evaluations2',
        }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mock404Response = {
        ok: false,
        status: 404,
        statusText: 'Not Found',
        json: jest.fn().mockResolvedValue({ message: 'Not Found' }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mockEvaluationsResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({ evaluations: [{ decision: true }, { decision: false }] }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      // Sequence: discover, 404, discover, success
      mockFetch
        .mockResolvedValueOnce(mockDiscoveryResponse1) // initial discover
        .mockResolvedValueOnce(mock404Response)        // first evaluations (404)
        .mockResolvedValueOnce(mockDiscoveryResponse2) // retry discover
        .mockResolvedValueOnce(mockEvaluationsResponse); // retry evaluations (success)

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
        token: 'test-token',
      });

      const response = await client.evaluations(validRequest);

      expect(mockFetch).toHaveBeenNthCalledWith(
        1,
        'https://example.com/.well-known/authzen-configuration',
        expect.anything()
      );

      expect(mockFetch).toHaveBeenNthCalledWith(
        2,
        'https://example.com/custom/evaluations',
        expect.anything()
      );

      expect(mockFetch).toHaveBeenNthCalledWith(
        3,
        'https://example.com/.well-known/authzen-configuration',
        expect.anything()
      );

      expect(mockFetch).toHaveBeenNthCalledWith(
        4,
        'https://example.com/custom/evaluations2',
        expect.anything()
      );

      expect(response).toEqual({ evaluations: [{ decision: true }, { decision: false }] });
    });

    it('should return error if evaluations endpoint returns 404 twice', async () => {
      const mockDiscoveryResponse1 = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          policy_decision_point: 'https://example.com',
          access_evaluations_endpoint: 'https://example.com/custom/evaluations',
        }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mockDiscoveryResponse2 = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          policy_decision_point: 'https://example.com',
          access_evaluations_endpoint: 'https://example.com/custom/evaluations2',
        }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mock404Response = {
        ok: false,
        status: 404,
        statusText: 'Not Found',
        json: jest.fn().mockResolvedValue({ message: 'Not Found' }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      // Sequence: discover, 404, discover, 404
      mockFetch
        .mockResolvedValueOnce(mockDiscoveryResponse1) // initial discover
        .mockResolvedValueOnce(mock404Response)        // first evaluations (404)
        .mockResolvedValueOnce(mockDiscoveryResponse2) // retry discover
        .mockResolvedValueOnce(mock404Response);       // retry evaluations (404)

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
        token: 'test-token',
      });

      await expect(client.evaluations(validRequest)).rejects.toThrow(AuthZenRequestError);

      expect(mockFetch).toHaveBeenNthCalledWith(
        1,
        'https://example.com/.well-known/authzen-configuration',
        expect.anything()
      );

      expect(mockFetch).toHaveBeenNthCalledWith(
        2,
        'https://example.com/custom/evaluations',
        expect.anything()
      );

      expect(mockFetch).toHaveBeenNthCalledWith(
        3,
        'https://example.com/.well-known/authzen-configuration',
        expect.anything()
      );

      expect(mockFetch).toHaveBeenNthCalledWith(
        4,
        'https://example.com/custom/evaluations2',
        expect.anything()
      );
    });

    it('should make correct API call for batch evaluation', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          evaluations: [{ decision: true }, { decision: false }],
        }),
        headers: { 
          get: jest.fn().mockReturnValue('application/json') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const response = await client.evaluations(validRequest);

      expect(mockFetch).toHaveBeenNthCalledWith(
        1,
        'https://example.com/.well-known/authzen-configuration',
        expect.anything()
      );
      expect(mockFetch).toHaveBeenNthCalledWith(
        2,
        'https://example.com/setupDiscovery/evaluations',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify(validRequest),
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
            'Accept': 'application/json',
            'X-Request-ID': expect.stringMatching(/^authzen-\d+-[a-z0-9]+$/),
          }),
          signal: expect.any(AbortSignal),
        })
      );

      expect(response).toEqual({
        evaluations: [{ decision: true }, { decision: false }],
      });
    });

    it('should validate evaluations_semantic in options', async () => {
      setupDiscovery();
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const invalidRequest = {
        evaluations: validRequest.evaluations,
        options: {
          evaluations_semantic: 'invalid_semantic' as any,
        },
      };

      await expect(client.evaluations(invalidRequest)).rejects.toThrow(AuthZenValidationError);
      await expect(client.evaluations(invalidRequest)).rejects.toThrow('Invalid evaluations_semantic');
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should accept valid evaluations_semantic values', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          evaluations: [{ decision: true }, { decision: false }],
        }),
        headers: { 
          get: jest.fn().mockReturnValue('application/json') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const validSemantics = ['execute_all', 'deny_on_first_deny', 'permit_on_first_permit'];
      
      for (const semantic of validSemantics) {
        mockFetch.mockClear();
        
        const requestWithOptions = {
          evaluations: validRequest.evaluations,
          options: {
            evaluations_semantic: semantic as any,
          },
        };

        await expect(client.evaluations(requestWithOptions)).resolves.toBeDefined();
        expect(mockFetch).toHaveBeenCalled();
      }
    });

    it('should allow request without options', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          evaluations: [{ decision: true }, { decision: false }],
        }),
        headers: { 
          get: jest.fn().mockReturnValue('application/json') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithoutOptions = {
        evaluations: validRequest.evaluations,
        // No options property
      };

      await expect(client.evaluations(requestWithoutOptions)).resolves.toBeDefined();
      expect(mockFetch).toHaveBeenCalled();
    });

    it('should allow empty options object', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          evaluations: [{ decision: true }, { decision: false }],
        }),
        headers: { 
          get: jest.fn().mockReturnValue('application/json') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithEmptyOptions = {
        evaluations: validRequest.evaluations,
        options: {},
      };

      await expect(client.evaluations(requestWithEmptyOptions)).resolves.toBeDefined();
      expect(mockFetch).toHaveBeenCalled();
    });

    it('should validate that options is an object if provided', async () => {
      setupDiscovery();
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const invalidRequest = {
        evaluations: validRequest.evaluations,
        options: 'not an object' as any,
      };

      await expect(client.evaluations(invalidRequest)).rejects.toThrow(AuthZenValidationError);
      await expect(client.evaluations(invalidRequest)).rejects.toThrow('Options must be an object');
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should validate empty evaluations array', async () => {
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const invalidRequest = {
        evaluations: [],
      };

      await expect(client.evaluations(invalidRequest)).rejects.toThrow(AuthZenValidationError);
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should validate individual evaluations in batch', async () => {
      setupDiscovery();
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const invalidRequest = {
        evaluations: [
          {
            subject: { type: 'user', id: 'alice@example.com' },
            action: { name: 'can_read' },
            resource: { type: 'document', id: '123' },
          },
          {
            subject: { type: 'user' }, // Missing id
            action: { name: 'can_read' },
            resource: { type: 'document', id: '456' },
          },
        ],
      } as AccessEvaluationsRequest;

      await expect(client.evaluations(invalidRequest)).rejects.toThrow(AuthZenValidationError);
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should validate default values structure', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          evaluations: [{ decision: true }],
        }),
        headers: { 
          get: jest.fn().mockReturnValue('application/json') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithDefaults = {
        evaluations: [
          {
            subject: { type: 'user', id: 'alice@example.com' },
            action: { name: 'can_read' },
            resource: { type: 'document', id: '123' },
          },
        ],
        subject: { type: 'user' },
        resource: { type: 'document' },
        action: { name: 'can_read' },
        context: { environment: 'test' },
      };

      await expect(client.evaluations(requestWithDefaults)).resolves.toBeDefined();
      expect(mockFetch).toHaveBeenCalled();
    });

    it('should include Authorization header with bearer token when provided', async () => {
      const mockDiscoveryResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          policy_decision_point: 'https://example.com',
          access_evaluations_endpoint: 'https://example.com/custom/evaluations',
        }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      const mockEvaluationsResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({ evaluations: [{ decision: true }, { decision: false }] }),
        headers: {
          get: jest.fn().mockReturnValue('application/json')
        },
      };

      mockFetch
        .mockResolvedValueOnce(mockDiscoveryResponse)
        .mockResolvedValueOnce(mockEvaluationsResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
        token: 'my-evaluations-token',
      });

      await client.evaluations(validRequest);

      expect(mockFetch).toHaveBeenNthCalledWith(
        2,
        'https://example.com/custom/evaluations',
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Authorization': 'Bearer my-evaluations-token',
          }),
          body: JSON.stringify(validRequest),
          signal: expect.any(AbortSignal),
        })
      );
    });
  });

  describe('default value handling', () => {
    it('should allow missing subject in evaluations when default subject is provided', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          evaluations: [{ decision: true }, { decision: false }],
        }),
        headers: { 
          get: jest.fn().mockReturnValue('application/json') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithDefaultSubject = {
        evaluations: [
          {
            // No subject - should use default
            action: { name: 'can_read' },
            resource: { type: 'document', id: '123' },
          },
          {
            // No subject - should use default
            action: { name: 'can_write' },
            resource: { type: 'document', id: '456' },
          },
        ],
        subject: { type: 'user', id: 'alice@example.com' }, // Default subject
      };

      await expect(client.evaluations(requestWithDefaultSubject)).resolves.toBeDefined();
      expect(mockFetch).toHaveBeenCalled();
    });

    it('should allow missing resource in evaluations when default resource is provided', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          evaluations: [{ decision: true }, { decision: false }],
        }),
        headers: { 
          get: jest.fn().mockReturnValue('application/json') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithDefaultResource = {
        evaluations: [
          {
            subject: { type: 'user', id: 'alice@example.com' },
            action: { name: 'can_read' },
            // No resource - should use default
          },
          {
            subject: { type: 'user', id: 'bob@example.com' },
            action: { name: 'can_write' },
            // No resource - should use default
          },
        ],
        resource: { type: 'document', id: 'shared-doc' }, // Default resource
      };

      await expect(client.evaluations(requestWithDefaultResource)).resolves.toBeDefined();
      expect(mockFetch).toHaveBeenCalled();
    });

    it('should allow missing action in evaluations when default action is provided', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          evaluations: [{ decision: true }, { decision: false }],
        }),
        headers: { 
          get: jest.fn().mockReturnValue('application/json') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithDefaultAction = {
        evaluations: [
          {
            subject: { type: 'user', id: 'alice@example.com' },
            resource: { type: 'document', id: '123' },
            // No action - should use default
          },
          {
            subject: { type: 'user', id: 'bob@example.com' },
            resource: { type: 'document', id: '456' },
            // No action - should use default
          },
        ],
        action: { name: 'can_read' }, // Default action
      };

      await expect(client.evaluations(requestWithDefaultAction)).resolves.toBeDefined();
      expect(mockFetch).toHaveBeenCalled();
    });

    it('should allow completely empty evaluations when all defaults are provided', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          evaluations: [{ decision: true }],
        }),
        headers: { 
          get: jest.fn().mockReturnValue('application/json') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithAllDefaults = {
        evaluations: [
          {
            // Completely empty evaluation - should use all defaults
          },
        ],
        subject: { type: 'user', id: 'alice@example.com' },
        resource: { type: 'document', id: 'shared-doc' },
        action: { name: 'can_read' },
        context: { environment: 'test' }
      };

      await expect(client.evaluations(requestWithAllDefaults)).resolves.toBeDefined();
      expect(mockFetch).toHaveBeenCalled();
    });

    it('should still require subject when no default subject is provided', async () => {
      setupDiscovery();
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestMissingSubject = {
        evaluations: [
          {
            // No subject and no default subject
            action: { name: 'can_read' },
            resource: { type: 'document', id: '123' },
          },
        ],
        // No default subject provided
        resource: { type: 'document', id: 'shared-doc' },
        action: { name: 'can_read' },
      };

      await expect(client.evaluations(requestMissingSubject)).rejects.toThrow(AuthZenValidationError);
      await expect(client.evaluations(requestMissingSubject)).rejects.toThrow('Subject is required');
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should still require resource when no default resource is provided', async () => {
      setupDiscovery();
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestMissingResource = {
        evaluations: [
          {
            subject: { type: 'user', id: 'alice@example.com' },
            action: { name: 'can_read' },
            // No resource and no default resource
          },
        ],
        subject: { type: 'user', id: 'bob@example.com' },
        action: { name: 'can_write' },
        // No default resource provided
      };

      await expect(client.evaluations(requestMissingResource)).rejects.toThrow(AuthZenValidationError);
      await expect(client.evaluations(requestMissingResource)).rejects.toThrow('Resource is required');
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should still require action when no default action is provided', async () => {
      setupDiscovery();
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestMissingAction = {
        evaluations: [
          {
            subject: { type: 'user', id: 'alice@example.com' },
            resource: { type: 'document', id: '123' },
            // No action and no default action
          },
        ],
        subject: { type: 'user', id: 'bob@example.com' },
        resource: { type: 'document', id: 'shared-doc' },
        // No default action provided
      };

      await expect(client.evaluations(requestMissingAction)).rejects.toThrow(AuthZenValidationError);
      await expect(client.evaluations(requestMissingAction)).rejects.toThrow('Action is required');
      expect(mockFetch).not.toHaveBeenCalled();
    });
  });

  describe('partial element validation', () => {
    it('should reject partial subject (missing type) in evaluations', async () => {
      setupDiscovery();
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithPartialSubject = {
        evaluations: [
          {
            subject: { id: 'alice@example.com' }, // Missing type - partial subject not allowed
            action: { name: 'can_read' },
            resource: { type: 'document', id: '123' },
          },
        ],
      } as AccessEvaluationsRequest;

      await expect(client.evaluations(requestWithPartialSubject)).rejects.toThrow(AuthZenValidationError);
      await expect(client.evaluations(requestWithPartialSubject)).rejects.toThrow('Subject type is required');
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should reject partial subject (missing id) in evaluations', async () => {
      setupDiscovery();
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithPartialSubject = {
        evaluations: [
          {
            subject: { type: 'user' }, // Missing id - partial subject not allowed
            action: { name: 'can_read' },
            resource: { type: 'document', id: '123' },
          },
        ],
      } as AccessEvaluationsRequest;

      await expect(client.evaluations(requestWithPartialSubject)).rejects.toThrow(AuthZenValidationError);
      await expect(client.evaluations(requestWithPartialSubject)).rejects.toThrow('Subject id is required');
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should reject partial resource (missing type) in evaluations', async () => {
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithPartialResource = {
        evaluations: [
          {
            subject: { type: 'user', id: 'alice@example.com' },
            action: { name: 'can_read' },
            resource: { id: '123' }, // Missing type - partial resource not allowed
          },
        ],
      } as AccessEvaluationsRequest;

      await expect(client.evaluations(requestWithPartialResource)).rejects.toThrow(AuthZenValidationError);
      await expect(client.evaluations(requestWithPartialResource)).rejects.toThrow('Resource type is required');
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should reject partial resource (missing id) in evaluations', async () => {
      setupDiscovery();
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithPartialResource = {
        evaluations: [
          {
            subject: { type: 'user', id: 'alice@example.com' },
            action: { name: 'can_read' },
            resource: { type: 'document' }, // Missing id - partial resource not allowed
          },
        ],
      } as AccessEvaluationsRequest;

      await expect(client.evaluations(requestWithPartialResource)).rejects.toThrow(AuthZenValidationError);
      await expect(client.evaluations(requestWithPartialResource)).rejects.toThrow('Resource id is required');
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should reject partial action (missing name) in evaluations', async () => {
      setupDiscovery();
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithPartialAction = {
        evaluations: [
          {
            subject: { type: 'user', id: 'alice@example.com' },
            action: { properties: { method: 'GET' } }, // Missing name - partial action not allowed
            resource: { type: 'document', id: '123' },
          },
        ],
      } as unknown as AccessEvaluationsRequest;

      await expect(client.evaluations(requestWithPartialAction)).rejects.toThrow(AuthZenValidationError);
      await expect(client.evaluations(requestWithPartialAction)).rejects.toThrow('Action name is required');
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should reject partial subject in defaults when used without evaluations', async () => {
      setupDiscovery();
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithPartialDefaultSubject = {
        // No evaluations array
        subject: { type: 'user' }, // Missing id - partial default not allowed
        resource: { type: 'document', id: 'doc123' },
        action: { name: 'read' }
      };

      await expect(client.evaluations(requestWithPartialDefaultSubject)).rejects.toThrow(AuthZenValidationError);
      await expect(client.evaluations(requestWithPartialDefaultSubject)).rejects.toThrow(
        'When no evaluations array is provided, default subject with type and id is required'
      );
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should reject partial resource in defaults when used without evaluations', async () => {
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithPartialDefaultResource = {
        // No evaluations array
        subject: { type: 'user', id: 'alice@example.com' },
        resource: { type: 'document' }, // Missing id - partial default not allowed
        action: { name: 'read' }
      };

      await expect(client.evaluations(requestWithPartialDefaultResource)).rejects.toThrow(AuthZenValidationError);
      await expect(client.evaluations(requestWithPartialDefaultResource)).rejects.toThrow(
        'When no evaluations array is provided, default resource with type and id is required'
      );
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should reject partial action in defaults when used without evaluations', async () => {
      setupDiscovery();
      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithPartialDefaultAction = {
        // No evaluations array
        subject: { type: 'user', id: 'alice@example.com' },
        resource: { type: 'document', id: 'doc123' },
        action: { properties: { method: 'GET' } } // Missing name - partial default not allowed
      };

      await expect(client.evaluations(requestWithPartialDefaultAction)).rejects.toThrow(AuthZenValidationError);
      await expect(client.evaluations(requestWithPartialDefaultAction)).rejects.toThrow(
        'When no evaluations array is provided, default action with name is required'
      );
      expect(mockFetch).not.toHaveBeenCalled();
    });

    it('should allow complete elements with properties', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({
          evaluations: [{ decision: true }],
        }),
        headers: { 
          get: jest.fn().mockReturnValue('application/json') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const requestWithCompleteElements = {
        evaluations: [
          {
            subject: { 
              type: 'user', 
              id: 'alice@example.com',
              properties: { role: 'admin', department: 'engineering' }
            },
            action: { 
              name: 'can_read',
              properties: { method: 'GET', scope: 'full' }
            },
            resource: { 
              type: 'document', 
              id: '123',
              properties: { classification: 'confidential', owner: 'alice' }
            },
          },
        ],
      };

      await expect(client.evaluations(requestWithCompleteElements)).resolves.toBeDefined();
      expect(mockFetch).toHaveBeenCalled();
    });
  });

  describe('request validation', () => {
    let client: AuthZenClient;

    beforeEach(() => {
      client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });
    });

    it('should validate subject type is required', async () => {
      setupDiscovery();
      const invalidRequest = {
        subject: { id: 'alice@example.com' }, // Missing type
        action: { name: 'can_read' },
        resource: { type: 'document', id: '123' },
      } as AccessEvaluationRequest;

      await expect(client.evaluate(invalidRequest)).rejects.toThrow(AuthZenValidationError);
    });

    it('should validate subject id is required', async () => {
      setupDiscovery();
      const invalidRequest = {
        subject: { type: 'user' }, // Missing id
        action: { name: 'can_read' },
        resource: { type: 'document', id: '123' },
      } as AccessEvaluationRequest;

      await expect(client.evaluate(invalidRequest)).rejects.toThrow(AuthZenValidationError);
    });

    it('should validate resource type is required', async () => {
      setupDiscovery();
      const invalidRequest = {
        subject: { type: 'user', id: 'alice@example.com' },
        action: { name: 'can_read' },
        resource: { id: '123' }, // Missing type
      } as AccessEvaluationRequest;

      await expect(client.evaluate(invalidRequest)).rejects.toThrow(AuthZenValidationError);
    });

    it('should validate resource id is required', async () => {
      setupDiscovery();
      const invalidRequest = {
        subject: { type: 'user', id: 'alice@example.com' },
        action: { name: 'can_read' },
        resource: { type: 'document' }, // Missing id
      } as AccessEvaluationRequest;

      await expect(client.evaluate(invalidRequest)).rejects.toThrow(AuthZenValidationError);
    });

    it('should validate action name is required', async () => {
      setupDiscovery();
      const invalidRequest = {
        subject: { type: 'user', id: 'alice@example.com' },
        action: {}, // Missing name
        resource: { type: 'document', id: '123' },
      } as AccessEvaluationRequest;

      await expect(client.evaluate(invalidRequest)).rejects.toThrow(AuthZenValidationError);
    });
  });

  describe('error handling', () => {
    let client: AuthZenClient;

    beforeEach(() => {
      client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });
    });

    it('should wrap unknown errors as AuthZenError', async () => {
      mockFetch.mockRejectedValue(new Error('Unknown error'));

      const validRequest: AccessEvaluationRequest = {
        subject: { type: 'user', id: 'alice@example.com' },
        action: { name: 'can_read' },
        resource: { type: 'document', id: '123' },
      };

      await expect(client.evaluate(validRequest)).rejects.toThrow(AuthZenError);
    });

    it('should preserve AuthZen errors', async () => {
      setupDiscovery();
      const customError = new AuthZenValidationError('Custom validation error');
      
      // Mock the validation to throw our custom error
      jest.spyOn(client as any, 'validateEvaluationRequest').mockImplementation(() => {
        throw customError;
      });

      const validRequest: AccessEvaluationRequest = {
        subject: { type: 'user', id: 'alice@example.com' },
        action: { name: 'can_read' },
        resource: { type: 'document', id: '123' },
      };

      await expect(client.evaluate(validRequest)).rejects.toBe(customError);
    });
  });

  describe('request ID generation', () => {
    it('should generate unique request IDs', async () => {
      setupDiscovery();
      const mockResponse = {
        ok: true,
        status: 200,
        json: jest.fn().mockResolvedValue({ decision: true }),
        headers: { 
          get: jest.fn().mockReturnValue('application/json') 
        },
      };
      mockFetch.mockResolvedValue(mockResponse);

      const client = new AuthZenClient({
        pdpUrl: 'https://example.com',
      });

      const validRequest: AccessEvaluationRequest = {
        subject: { type: 'user', id: 'alice@example.com' },
        action: { name: 'can_read' },
        resource: { type: 'document', id: '123' },
      };

      await client.evaluate(validRequest);
      await client.evaluate(validRequest);

      const calls = mockFetch.mock.calls;
      const requestId1 = calls[0][1].headers['X-Request-ID'];
      const requestId2 = calls[1][1].headers['X-Request-ID'];

      expect(requestId1).toMatch(/^authzen-\d+-[a-z0-9]+$/);
      expect(requestId2).toMatch(/^authzen-\d+-[a-z0-9]+$/);
      expect(requestId1).not.toBe(requestId2);
    });
  });
});
