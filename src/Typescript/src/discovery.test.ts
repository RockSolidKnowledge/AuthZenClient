import { AuthZenClient } from './client';
import {
  AuthZenConfiguration,
  AuthZenDiscoveryError,
  AuthZenRequestError,
  AuthZenResponseError,
  AuthZenNetworkError,
} from './types';

// Mock global fetch
const mockFetch = jest.fn();
global.fetch = mockFetch;

describe('AuthZenClient - Discovery', () => {
  beforeEach(() => {
    mockFetch.mockClear();
    jest.clearAllTimers();
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  describe('discover()', () => {
    let client: AuthZenClient;

    beforeEach(() => {
      client = new AuthZenClient({
        pdpUrl: 'https://pdp.example.com',
        token: 'test-token',
      });
    });

    describe('successful discovery', () => {
      it('should discover minimal valid configuration', async () => {
        const mockConfig: AuthZenConfiguration = {
          policy_decision_point: 'https://pdp.example.com',
        };

        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockResolvedValue(mockConfig),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        const result = await client.discover();

        expect(mockFetch).toHaveBeenCalledWith(
          'https://pdp.example.com/.well-known/authzen-configuration',
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

        expect(result).toEqual(mockConfig);
      });

      it('should discover full configuration with all optional endpoints', async () => {
        const mockConfig: AuthZenConfiguration = {
          policy_decision_point: 'https://pdp.example.com',
          access_evaluation_endpoint: 'https://pdp.example.com/access/v1/evaluation',
          access_evaluations_endpoint: 'https://pdp.example.com/access/v1/evaluations',
          search_subject_endpoint: 'https://pdp.example.com/access/v1/search/subject',
          search_resource_endpoint: 'https://pdp.example.com/access/v1/search/resource',
          search_action_endpoint: 'https://pdp.example.com/access/v1/search/action',
          custom_endpoint: 'https://pdp.example.com/custom',
        };

        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockResolvedValue(mockConfig),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        const result = await client.discover();

        expect(result).toEqual(mockConfig);
      });

      it('should discover configuration with relative paths', async () => {
        const mockConfig: AuthZenConfiguration = {
          policy_decision_point: 'https://pdp.example.com',
          access_evaluation_endpoint: '/access/v1/evaluation',
          access_evaluations_endpoint: '/access/v1/evaluations',
          search_subject_endpoint: '/access/v1/search/subject',
        };

        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockResolvedValue(mockConfig),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        const result = await client.discover();

        expect(result).toEqual(mockConfig);
      });

      it('should work without authentication token', async () => {
        const clientWithoutToken = new AuthZenClient({
          pdpUrl: 'https://pdp.example.com',
        });

        const mockConfig: AuthZenConfiguration = {
          policy_decision_point: 'https://pdp.example.com',
        };

        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockResolvedValue(mockConfig),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        const result = await clientWithoutToken.discover();

        expect(mockFetch).toHaveBeenCalledWith(
          'https://pdp.example.com/.well-known/authzen-configuration',
          expect.objectContaining({
            headers: expect.not.objectContaining({
              'Authorization': expect.anything(),
            }),
          })
        );

        expect(result).toEqual(mockConfig);
      });
    });

    describe('validation errors', () => {
      it('should throw AuthZenDiscoveryError for non-object response', async () => {
        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockResolvedValue('not an object'),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await expect(client.discover()).rejects.toThrow(AuthZenDiscoveryError);
        await expect(client.discover()).rejects.toThrow('Discovery configuration must be an object');
      });

      it('should throw AuthZenDiscoveryError for missing policy_decision_point', async () => {
        const mockConfig = {
          access_evaluation_endpoint: '/access/v1/evaluation',
        };

        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockResolvedValue(mockConfig),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await expect(client.discover()).rejects.toThrow(AuthZenDiscoveryError);
        await expect(client.discover()).rejects.toThrow('Discovery configuration must have a policy_decision_point string property');
      });

      it('should throw AuthZenDiscoveryError for non-string policy_decision_point', async () => {
        const mockConfig = {
          policy_decision_point: 123,
        };

        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockResolvedValue(mockConfig),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await expect(client.discover()).rejects.toThrow(AuthZenDiscoveryError);
        await expect(client.discover()).rejects.toThrow('Discovery configuration must have a policy_decision_point string property');
      });

      it('should throw AuthZenDiscoveryError for invalid policy_decision_point URL', async () => {
        const mockConfig = {
          policy_decision_point: 'not-a-valid-url',
        };

        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockResolvedValue(mockConfig),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await expect(client.discover()).rejects.toThrow(AuthZenDiscoveryError);
        await expect(client.discover()).rejects.toThrow('policy_decision_point must be a valid URL');
      });

      it('should throw AuthZenDiscoveryError for non-string optional endpoints', async () => {
        const mockConfig = {
          policy_decision_point: 'https://pdp.example.com',
          access_evaluation_endpoint: 123,
        };

        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockResolvedValue(mockConfig),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await expect(client.discover()).rejects.toThrow(AuthZenDiscoveryError);
        await expect(client.discover()).rejects.toThrow('access_evaluation_endpoint must be a string if provided');
      });

      it('should throw AuthZenDiscoveryError for empty string endpoints', async () => {
        const mockConfig = {
          policy_decision_point: 'https://pdp.example.com',
          access_evaluations_endpoint: '',
        };

        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockResolvedValue(mockConfig),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await expect(client.discover()).rejects.toThrow(AuthZenDiscoveryError);
        await expect(client.discover()).rejects.toThrow('access_evaluations_endpoint cannot be an empty string');
      });

      it('should throw AuthZenDiscoveryError for invalid endpoint paths', async () => {
        const mockConfig = {
          policy_decision_point: 'https://pdp.example.com',
          search_subject_endpoint: 'invalid-path',
        };

        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockResolvedValue(mockConfig),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await expect(client.discover()).rejects.toThrow(AuthZenDiscoveryError);
        await expect(client.discover()).rejects.toThrow('search_subject_endpoint must be either a relative path starting with \'/\' or a full URL');
      });

      it('should validate all optional endpoint properties', async () => {
        const endpointProperties = [
          'access_evaluation_endpoint',
          'access_evaluations_endpoint',
          'search_subject_endpoint',
          'search_resource_endpoint',
          'search_action_endpoint',
        ];

        for (const prop of endpointProperties) {
          mockFetch.mockClear();
          
          const mockConfig = {
            policy_decision_point: 'https://pdp.example.com',
            [prop]: 123, // Invalid type
          };

          const mockResponse = {
            ok: true,
            status: 200,
            json: jest.fn().mockResolvedValue(mockConfig),
            headers: {
              get: jest.fn().mockReturnValue('application/json'),
            },
          };
          mockFetch.mockResolvedValue(mockResponse);

          await expect(client.discover()).rejects.toThrow(AuthZenDiscoveryError);
          await expect(client.discover()).rejects.toThrow(`${prop} must be a string if provided`);
        }
      });
    });

    describe('HTTP errors', () => {
      it('should handle 404 Not Found', async () => {
        const mockResponse = {
          ok: false,
          status: 404,
          statusText: 'Not Found',
          json: jest.fn().mockResolvedValue({ message: 'Discovery endpoint not found' }),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await expect(client.discover()).rejects.toThrow(AuthZenRequestError);
        await expect(client.discover()).rejects.toThrow('Discovery endpoint not found');
      });

      it('should handle 500 Internal Server Error', async () => {
        const mockResponse = {
          ok: false,
          status: 500,
          statusText: 'Internal Server Error',
          json: jest.fn().mockResolvedValue({}),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await expect(client.discover()).rejects.toThrow(AuthZenRequestError);
        await expect(client.discover()).rejects.toThrow('HTTP 500: Internal Server Error');
      });

      it('should handle 403 Forbidden', async () => {
        const mockResponse = {
          ok: false,
          status: 403,
          statusText: 'Forbidden',
          json: jest.fn().mockResolvedValue({ message: 'Access denied' }),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await expect(client.discover()).rejects.toThrow(AuthZenRequestError);
        
        try {
          await client.discover();
          expect(true).toBe(false); // Should not reach here
        } catch (error: unknown) {
          expect(error).toBeInstanceOf(AuthZenRequestError);
          if (error instanceof AuthZenRequestError) {
            expect(error.statusCode).toBe(403);
            expect(error.responseData).toEqual({ message: 'Access denied' });
          }
        }
      });
    });

    describe('network errors', () => {
      it('should handle network timeouts', async () => {
        // Mock fetch to simulate hanging request
        mockFetch.mockImplementation((url, options) => {
          return new Promise((resolve, reject) => {
            // Set up abort signal listener
            if (options?.signal) {
              const abortHandler = () => {
                const error = new Error('The operation was aborted');
                error.name = 'AbortError';
                reject(error);
              };
              
              options.signal.addEventListener('abort', abortHandler);
            }
          });
        });

        const promise = client.discover();
        jest.advanceTimersByTime(10000);

        await expect(promise).rejects.toThrow(AuthZenNetworkError);
        await expect(promise).rejects.toThrow('Request timeout after 10000ms');
      });

      it('should handle connection errors', async () => {
        mockFetch.mockRejectedValue(new Error('Connection refused'));

        await expect(client.discover()).rejects.toThrow(AuthZenNetworkError);
        await expect(client.discover()).rejects.toThrow('Network error: Connection refused');
      });

      it('should handle DNS resolution errors', async () => {
        mockFetch.mockRejectedValue(new Error('getaddrinfo ENOTFOUND'));

        await expect(client.discover()).rejects.toThrow(AuthZenNetworkError);
        await expect(client.discover()).rejects.toThrow('Network error: getaddrinfo ENOTFOUND');
      });
    });

    describe('response parsing errors', () => {
      it('should handle invalid JSON responses', async () => {
        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockRejectedValue(new Error('Invalid JSON')),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await expect(client.discover()).rejects.toThrow(AuthZenResponseError);
        await expect(client.discover()).rejects.toThrow('Invalid JSON in response');
      });

      it('should handle non-JSON content type', async () => {
        const mockResponse = {
          ok: true,
          status: 200,
          text: jest.fn().mockResolvedValue('<html>Not JSON</html>'),
          headers: {
            get: jest.fn().mockReturnValue('text/html'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await expect(client.discover()).rejects.toThrow(AuthZenResponseError);
        await expect(client.discover()).rejects.toThrow('Expected JSON response, got: text/html');
      });

      it('should handle missing content type', async () => {
        const mockResponse = {
          ok: true,
          status: 200,
          text: jest.fn().mockResolvedValue('some text'),
          headers: {
            get: jest.fn().mockReturnValue(null),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await expect(client.discover()).rejects.toThrow(AuthZenResponseError);
        await expect(client.discover()).rejects.toThrow('Expected JSON response, got: null');
      });
    });

    describe('request correlation', () => {
      it('should include request ID in headers', async () => {
        const mockConfig: AuthZenConfiguration = {
          policy_decision_point: 'https://pdp.example.com',
        };

        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockResolvedValue(mockConfig),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await client.discover();

        expect(mockFetch).toHaveBeenCalledWith(
          expect.any(String),
          expect.objectContaining({
            headers: expect.objectContaining({
              'X-Request-ID': expect.stringMatching(/^authzen-\d+-[a-z0-9]+$/),
            }),
          })
        );
      });

      it('should generate unique request IDs for multiple calls', async () => {
        const mockConfig: AuthZenConfiguration = {
          policy_decision_point: 'https://pdp.example.com',
        };

        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockResolvedValue(mockConfig),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        await client.discover();
        await client.discover();

        const calls = mockFetch.mock.calls;
        const requestId1 = calls[0][1].headers['X-Request-ID'];
        const requestId2 = calls[1][1].headers['X-Request-ID'];

        expect(requestId1).toMatch(/^authzen-\d+-[a-z0-9]+$/);
        expect(requestId2).toMatch(/^authzen-\d+-[a-z0-9]+$/);
        expect(requestId1).not.toBe(requestId2);
      });
    });

    describe('error context', () => {
      it('should not include request ID in discovery validation errors', async () => {
        const mockConfig = {
          policy_decision_point: 'invalid-url',
        };

        const mockResponse = {
          ok: true,
          status: 200,
          json: jest.fn().mockResolvedValue(mockConfig),
          headers: {
            get: jest.fn().mockReturnValue('application/json'),
          },
        };
        mockFetch.mockResolvedValue(mockResponse);

        try {
          await client.discover();
          expect(true).toBe(false); // Should not reach here
        } catch (error: unknown) {
          expect(error).toBeInstanceOf(AuthZenDiscoveryError);
          if (error instanceof AuthZenDiscoveryError) {
            expect(error.requestId).toBeUndefined(); // Discovery validation errors don't include request ID
            expect(error.message).toBe('policy_decision_point must be a valid URL');
          }
        }
      });

      it('should include request ID in network errors', async () => {
        mockFetch.mockRejectedValue(new Error('Network error'));

        try {
          await client.discover();
          expect(true).toBe(false); // Should not reach here
        } catch (error: unknown) {
          expect(error).toBeInstanceOf(AuthZenNetworkError);
          if (error instanceof AuthZenNetworkError) {
            expect(error.requestId).toMatch(/^authzen-\d+-[a-z0-9]+$/);
          }
        }
      });
    });
  });
});