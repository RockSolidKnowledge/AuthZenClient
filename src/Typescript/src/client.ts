import {
  AuthZenClientConfig,
  AccessEvaluationRequest,
  AccessEvaluationResponse,
  AccessEvaluationsRequest,
  AccessEvaluationsResponse,
  AuthZenConfiguration,
  AuthZenError,
  AuthZenRequestError,
  AuthZenResponseError,
  AuthZenNetworkError,
  AuthZenValidationError,
  AuthZenDiscoveryError,
  Subject,
  Resource,
  Action,
  Context,
} from './types';

interface IAuthZenClient {
  discover(): Promise<AuthZenConfiguration>;
  evaluate(request: AccessEvaluationRequest): Promise<AccessEvaluationResponse>;
  evaluations(request: AccessEvaluationsRequest): Promise<AccessEvaluationsResponse>;
}

/**
 * AuthZen Client for making authorization requests to a Policy Decision Point (PDP)
 */
export class AuthZenClient implements IAuthZenClient {
  readonly pdpUrl: string;
  private readonly headers: Record<string, string>;
  private readonly timeout: number;

  constructor(config: AuthZenClientConfig) {
    this.pdpUrl = config.pdpUrl.replace(/\/$/, ''); // Remove trailing slash
    this.timeout = config.timeout || 10000; // Default 10 seconds
    
    this.headers = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      ...config.headers,
    };

    // Add Authorization header if token provided
    if (config.token) {
      this.headers['Authorization'] = `Bearer ${config.token}`;
    }
  }

  /**
   * Discover AuthZen configuration from the well-known endpoint
   * 
   * @returns Promise<AuthZenConfiguration> The discovered configuration
   * @throws {AuthZenDiscoveryError} When discovery fails or configuration is invalid
   * @throws {AuthZenNetworkError} When network request fails
   * @throws {AuthZenRequestError} When HTTP request returns an error status
   */
  async discover(): Promise<AuthZenConfiguration> {
    try {
      const response = await this.makeRequest('/.well-known/authzen-configuration', {
        method: 'GET',
      });

      const config = response as AuthZenConfiguration;
      this.validateDiscoveryConfiguration(config);
      
      return config;
    } catch (error) {
      if (error instanceof AuthZenError) {
        throw error;
      }
      throw this.handleError(error, 'discover');
    }
  }

  /**
   * Make a single access evaluation request
   */
  async evaluate(request: AccessEvaluationRequest): Promise<AccessEvaluationResponse> {
    this.validateEvaluationRequest(request);

    try {
      const response = await this.makeRequest('/access/v1/evaluation', {
        method: 'POST',
        body: JSON.stringify(request),
      });

      return response as AccessEvaluationResponse;
    } catch (error) {
      throw this.handleError(error, 'evaluate');
    }
  }

  /**
   * Make multiple access evaluation requests in a single call
   */
  async evaluations(request: AccessEvaluationsRequest): Promise<AccessEvaluationsResponse> {
    this.validateEvaluationsRequest(request);

    try {
      const response = await this.makeRequest('/access/v1/evaluations', {
        method: 'POST',
        body: JSON.stringify(request),
      });

      return response as AccessEvaluationsResponse;
    } catch (error) {
      throw this.handleError(error, 'evaluations');
    }
  }

  /**
   * Make an HTTP request to the PDP
   */
  private async makeRequest(endpoint: string, options: RequestInit): Promise<any> {
    const url = `${this.pdpUrl}${endpoint}`;
    const requestId = this.generateRequestId();

    // Create abort controller for timeout handling
    const abortController = new AbortController();
    const timeoutId = setTimeout(() => {
      abortController.abort();
    }, this.timeout);

    const requestOptions: RequestInit = {
      ...options,
      headers: {
        ...this.headers,
        'X-Request-ID': requestId,
        ...options.headers,
      },
      signal: abortController.signal,
    };

    let response: Response;
    
    try {
      response = await fetch(url, requestOptions);
    } catch (error: any) {
      clearTimeout(timeoutId);
      
      if (error.name === 'AbortError') {
        throw new AuthZenNetworkError(`Request timeout after ${this.timeout}ms`, requestId);
      }
      
      throw new AuthZenNetworkError(`Network error: ${error.message}`, requestId);
    } finally {
      clearTimeout(timeoutId);
    }

    // Handle non-JSON responses
    let responseData: any;
    const contentType = response.headers.get('content-type');
    
    if (contentType && contentType.includes('application/json')) {
      try {
        responseData = await response.json();
      } catch (error) {
        throw new AuthZenResponseError(
          'Invalid JSON in response',
          response.status,
          requestId
        );
      }
    } else {
      const text = await response.text();
      throw new AuthZenResponseError(
        `Expected JSON response, got: ${contentType}. Body: ${text}`,
        response.status,
        requestId
      );
    }

    if (!response.ok) {
      throw new AuthZenRequestError(
        responseData?.message || `HTTP ${response.status}: ${response.statusText}`,
        response.status,
        requestId,
        responseData
      );
    }

    return responseData;
  }

  /**
   * Validate discovery configuration response
   */
  private validateDiscoveryConfiguration(config: AuthZenConfiguration): void {
    if (!config || typeof config !== 'object') {
      throw new AuthZenDiscoveryError('Discovery configuration must be an object');
    }

    if (!config.policy_decision_point || typeof config.policy_decision_point !== 'string') {
      throw new AuthZenDiscoveryError('Discovery configuration must have a policy_decision_point string property');
    }

    // Validate that policy_decision_point is a valid URL
    try {
      new URL(config.policy_decision_point);
    } catch {
      throw new AuthZenDiscoveryError('policy_decision_point must be a valid URL');
    }

    // Validate optional endpoint URLs if present
    const endpointProperties = [
      'access_evaluation_endpoint',
      'access_evaluations_endpoint',
      'search_subject_endpoint',
      'search_resource_endpoint',
      'search_action_endpoint',
    ];

    for (const prop of endpointProperties) {
      const value = config[prop];
      if (value !== undefined) {
        if (typeof value !== 'string') {
          throw new AuthZenDiscoveryError(`${prop} must be a string if provided`);
        }
        if (value.length === 0) {
          throw new AuthZenDiscoveryError(`${prop} cannot be an empty string`);
        }
        // Validate that it's either a full URL or a relative path
        if (!value.startsWith('/') && !value.startsWith('http')) {
          throw new AuthZenDiscoveryError(`${prop} must be either a relative path starting with '/' or a full URL`);
        }
      }
    }
  }

  /**
   * Validate an evaluation request, considering default values
   */
  private validateEvaluationRequestWithDefaults(
    evaluation: AccessEvaluationRequest, 
    defaults?: {
      subject?: Partial<Subject>;
      resource?: Partial<Resource>;
      action?: Partial<Action>;
      context?: Context;
    }
  ): void {
    if (!evaluation || typeof evaluation !== 'object') {
      throw new AuthZenValidationError('Evaluation must be an object');
    }

    // Check if subject is provided or can use default
    const hasSubject = evaluation.subject && Object.keys(evaluation.subject).length > 0;
    const hasDefaultSubject = defaults?.subject && Object.keys(defaults.subject).length > 0;
    
    if (hasSubject) {
      // If subject is provided, it must be complete
      if (!evaluation.subject || typeof evaluation.subject !== 'object') {
        throw new AuthZenValidationError('Subject is required and must be an object');
      }
      if (!evaluation.subject.type || typeof evaluation.subject.type !== 'string') {
        throw new AuthZenValidationError('Subject type is required and must be a string');
      }
      if (!evaluation.subject.id || typeof evaluation.subject.id !== 'string') {
        throw new AuthZenValidationError('Subject id is required and must be a string');
      }
    } else if (!hasDefaultSubject) {
      // No subject provided and no default available
      throw new AuthZenValidationError('Subject is required');
    }

    // Check if resource is provided or can use default
    const hasResource = evaluation.resource && Object.keys(evaluation.resource).length > 0;
    const hasDefaultResource = defaults?.resource && Object.keys(defaults.resource).length > 0;
    
    if (hasResource) {
      // If resource is provided, it must be complete
      if (!evaluation.resource || typeof evaluation.resource !== 'object') {
        throw new AuthZenValidationError('Resource is required and must be an object');
      }
      if (!evaluation.resource.type || typeof evaluation.resource.type !== 'string') {
        throw new AuthZenValidationError('Resource type is required and must be a string');
      }
      if (!evaluation.resource.id || typeof evaluation.resource.id !== 'string') {
        throw new AuthZenValidationError('Resource id is required and must be a string');
      }
    } else if (!hasDefaultResource) {
      // No resource provided and no default available
      throw new AuthZenValidationError('Resource is required');
    }

    // Check if action is provided or can use default
    const hasAction = evaluation.action && Object.keys(evaluation.action).length > 0;
    const hasDefaultAction = defaults?.action && Object.keys(defaults.action).length > 0;
    
    if (hasAction) {
      // If action is provided, it must be complete
      if (!evaluation.action || typeof evaluation.action !== 'object' || !evaluation.action.name || typeof evaluation.action.name !== 'string') {
        throw new AuthZenValidationError('Action name is required and must be a string');
      }
    } else if (!hasDefaultAction) {
      // No action provided and no default available
      throw new AuthZenValidationError('Action is required');
    }

    // Validate context if present
    if (evaluation.context) {
      this.validateContext(evaluation.context);
    }
  }

  /**
   * Validate context - can be any JSON object
   */
  private validateContext(context: Context): void {
    if (context === null || context === undefined) {
      throw new AuthZenValidationError('Context cannot be null or undefined');
    }

    if (typeof context !== 'object') {
      throw new AuthZenValidationError('Context must be an object');
    }

    if (Array.isArray(context)) {
      throw new AuthZenValidationError('Context cannot be an array');
    }

    // Validate that context contains only JSON-serializable values
    try {
      JSON.stringify(context);
    } catch (error) {
      throw new AuthZenValidationError('Context must contain only JSON-serializable values');
    }
  }

  /**
   * Validate a single evaluation request (original method for single evaluations)
   */
  private validateEvaluationRequest(request: AccessEvaluationRequest): void {
    if (!request || typeof request !== 'object') {
      throw new AuthZenValidationError('Request must be an object');
    }

    // For single evaluations, all fields are required
    if (!request.subject || typeof request.subject !== 'object') {
      throw new AuthZenValidationError('Subject is required');
    }

    if (!request.subject.type || typeof request.subject.type !== 'string') {
      throw new AuthZenValidationError('Subject type is required and must be a string');
    }

    if (!request.subject.id || typeof request.subject.id !== 'string') {
      throw new AuthZenValidationError('Subject id is required and must be a string');
    }

    if (!request.resource || typeof request.resource !== 'object') {
      throw new AuthZenValidationError('Resource is required');
    }

    if (!request.resource.type || typeof request.resource.type !== 'string') {
      throw new AuthZenValidationError('Resource type is required and must be a string');
    }

    if (!request.resource.id || typeof request.resource.id !== 'string') {
      throw new AuthZenValidationError('Resource id is required and must be a string');
    }

    if (!request.action || typeof request.action !== 'object') {
      throw new AuthZenValidationError('Action is required');
    }

    if (!request.action.name || typeof request.action.name !== 'string') {
      throw new AuthZenValidationError('Action name is required and must be a string');
    }

    if (request.context) {
      this.validateContext(request.context);
    }
  }

  /**
   * Validate a batch evaluations request
   */
  private validateEvaluationsRequest(request: AccessEvaluationsRequest): void {
    if (!request || typeof request !== 'object') {
      throw new AuthZenValidationError('Request must be an object');
    }

    // Extract defaults for validation
    const defaults = {
      subject: request.subject,
      resource: request.resource,
      action: request.action,
      context: request.context,
    };

    // If evaluations array is provided, validate it
    if (request.evaluations !== undefined) {
      if (!Array.isArray(request.evaluations)) {
        throw new AuthZenValidationError('Evaluations must be an array if provided');
      }

      if (request.evaluations.length === 0) {
        throw new AuthZenValidationError('Evaluations array cannot be empty if provided');
      }

      // Validate each evaluation with defaults
      request.evaluations.forEach((evaluation, index) => {
        try {
          this.validateEvaluationRequestWithDefaults(evaluation, defaults);
        } catch (error) {
          if (error instanceof AuthZenValidationError) {
            throw new AuthZenValidationError(
              `Evaluation at index ${index}: ${error.message}`
            );
          }
          throw error;
        }
      });
    } else {
      // If no evaluations array, validate that required defaults are present and complete
      if (!request.subject || !request.subject.type || !request.subject.id) {
        throw new AuthZenValidationError(
          'When no evaluations array is provided, default subject with type and id is required'
        );
      }

      if (!request.resource || !request.resource.type || !request.resource.id) {
        throw new AuthZenValidationError(
          'When no evaluations array is provided, default resource with type and id is required'
        );
      }

      if (!request.action || !request.action.name) {
        throw new AuthZenValidationError(
          'When no evaluations array is provided, default action with name is required'
        );
      }
    }

    // Validate options if provided
    if (request.options) {
      if (typeof request.options !== 'object') {
        throw new AuthZenValidationError('Options must be an object');
      }

      if (request.options.evaluations_semantic) {
        const validSemantics = ['execute_all', 'deny_on_first_deny', 'permit_on_first_permit'];
        if (!validSemantics.includes(request.options.evaluations_semantic)) {
          throw new AuthZenValidationError(
            `Invalid evaluations_semantic. Must be one of: ${validSemantics.join(', ')}`
          );
        }
      }
    }

    // Validate default values structure if provided
    if (request.subject) {
      if (request.subject.type !== undefined && typeof request.subject.type !== 'string') {
        throw new AuthZenValidationError('Default subject type must be a string');
      }
      if (request.subject.id !== undefined && typeof request.subject.id !== 'string') {
        throw new AuthZenValidationError('Default subject id must be a string');
      }
    }

    if (request.resource) {
      if (request.resource.type !== undefined && typeof request.resource.type !== 'string') {
        throw new AuthZenValidationError('Default resource type must be a string');
      }
      if (request.resource.id !== undefined && typeof request.resource.id !== 'string') {
        throw new AuthZenValidationError('Default resource id must be a string');
      }
    }

    if (request.action) {
      if (request.action.name !== undefined && typeof request.action.name !== 'string') {
        throw new AuthZenValidationError('Default action name must be a string');
      }
    }

    if (request.context) {
      this.validateContext(request.context);
    }
  }

  /**
   * Handle and wrap errors appropriately
   */
  private handleError(error: any, operation: string): AuthZenError {
    if (error instanceof AuthZenError) {
      return error;
    }

    return new AuthZenError(`Error during ${operation}: ${error.message}`);
  }

  /**
   * Generate a unique request ID for correlation
   */
  private generateRequestId(): string {
    return `authzen-${Date.now()}-${Math.random().toString(36).substring(2, 11)}`;
  }
}
