/**
 * AuthZen TypeScript Client Types
 * 
 * Type definitions for the AuthZen Authorization API 1.0 specification
 */

// ============================================================================
// Core AuthZen Types
// ============================================================================

/**
 * Subject represents the user or machine principal requesting access
 */
export interface Subject {
  type: string;
  id: string;
  properties?: Record<string, any>;
}

/**
 * Resource represents the target of the access request
 */
export interface Resource {
  type: string;
  id: string;
  properties?: Record<string, any>;
}

/**
 * Action represents the operation being attempted on the resource
 */
export interface Action {
  name: string;
  properties?: Record<string, any>;
}

/**
 * Context provides environmental and contextual data for the authorization decision
 */
export interface Context {
  [key: string]: any;
}

// ============================================================================
// Request/Response Types
// ============================================================================

/**
 * Single access evaluation request
 */
export interface AccessEvaluationRequest {
  subject?: Subject;  // Make optional - can use defaults
  resource?: Resource; // Make optional - can use defaults  
  action?: Action;    // Make optional - can use defaults
  context?: Context;
}

/**
 * Single access evaluation response
 */
export interface AccessEvaluationResponse {
  decision: boolean;
  context?: Context;
}

/**
 * Evaluation semantics for batch requests
 */
export type EvaluationSemantics = 'execute_all' | 'deny_on_first_deny' | 'permit_on_first_permit';

/**
 * Options for batch evaluation requests
 */
export interface BatchEvaluationOptions {
  /** How to handle multiple evaluations */
  evaluations_semantic?: EvaluationSemantics;
}

/**
 * Batch access evaluations request
 */
export interface AccessEvaluationsRequest {
  evaluations?: AccessEvaluationRequest[]; // Make optional - can omit for defaults-only
  /** Optional evaluation options */
  options?: BatchEvaluationOptions;
  /** Default subject values applied to all evaluations */
  subject?: Partial<Subject>;
  /** Default resource values applied to all evaluations */
  resource?: Partial<Resource>;
  /** Default action values applied to all evaluations */
  action?: Partial<Action>;
  /** Default context values applied to all evaluations */
  context?: Context;
}

/**
 * Individual evaluation result in batch response
 */
export interface EvaluationResult {
  decision: boolean;
  context?: Context;
}

/**
 * Batch access evaluations response
 */
export interface AccessEvaluationsResponse {
  evaluations: EvaluationResult[];
}

// ============================================================================
// Client Configuration
// ============================================================================

/**
 * Configuration options for the AuthZen client
 */
export interface AuthZenClientConfig {
  /** Base URL of the Policy Decision Point (PDP) */
  pdpUrl: string;
  /** Optional authentication token (will be sent as Bearer token) */
  token?: string;
  /** Additional headers to include with requests */
  headers?: Record<string, string>;
  /** Request timeout in milliseconds (default: 10000) */
  timeout?: number;
}

/**
 * Options for evaluation requests
 */
export interface EvaluationOptions {
  /** Custom headers for this request */
  headers?: Record<string, string>;
  /** Override default timeout for this request */
  timeout?: number;
}

// ============================================================================
// Error Types
// ============================================================================

/**
 * Base AuthZen error class
 */
export class AuthZenError extends Error {
  public name = 'AuthZenError';
  public readonly requestId?: string;

  constructor(message: string, requestId?: string) {
    super(message);
    this.requestId = requestId;
    
    // Ensure proper prototype chain for instanceof checks
    Object.setPrototypeOf(this, AuthZenError.prototype);
  }
}

/**
 * Network-related errors (timeouts, connection failures, etc.)
 */
export class AuthZenNetworkError extends AuthZenError {
  public readonly name = 'AuthZenNetworkError';

  constructor(message: string, requestId?: string) {
    super(message, requestId);
    Object.setPrototypeOf(this, AuthZenNetworkError.prototype);
  }
}

/**
 * HTTP request errors (4xx, 5xx status codes)
 */
export class AuthZenRequestError extends AuthZenError {
  public readonly name = 'AuthZenRequestError';
  public readonly statusCode: number;
  public readonly responseData?: any;

  constructor(message: string, statusCode: number, requestId?: string, responseData?: any) {
    super(message, requestId);
    this.statusCode = statusCode;
    this.responseData = responseData;
    Object.setPrototypeOf(this, AuthZenRequestError.prototype);
  }
}

/**
 * Response parsing/format errors
 */
export class AuthZenResponseError extends AuthZenError {
  public readonly name = 'AuthZenResponseError';
  public readonly statusCode: number;

  constructor(message: string, statusCode: number, requestId?: string) {
    super(message, requestId);
    this.statusCode = statusCode;
    Object.setPrototypeOf(this, AuthZenResponseError.prototype);
  }
}

/**
 * Request validation errors
 */
export class AuthZenValidationError extends AuthZenError {
  public readonly name = 'AuthZenValidationError';

  constructor(message: string) {
    super(message);
    Object.setPrototypeOf(this, AuthZenValidationError.prototype);
  }
}

// ============================================================================
// Utility Types and Constants
// ============================================================================

/**
 * Common subject types
 */
export const SubjectTypes = {
  USER: 'user',
  SERVICE: 'service',
  GROUP: 'group',
  ROLE: 'role',
} as const;

/**
 * Common resource types
 */
export const ResourceTypes = {
  DOCUMENT: 'document',
  API: 'api',
  FOLDER: 'folder',
  DATABASE: 'database',
  SERVICE: 'service',
} as const;

/**
 * Common action names
 */
export const ActionNames = {
  READ: 'can_read',
  WRITE: 'can_write',
  DELETE: 'can_delete',
  EXECUTE: 'can_execute',
  CREATE: 'can_create',
  UPDATE: 'can_update',
  VIEW: 'can_view',
  EDIT: 'can_edit',
} as const;

/**
 * HTTP status codes commonly used in AuthZen responses
 */
export const HttpStatusCodes = {
  OK: 200,
  BAD_REQUEST: 400,
  UNAUTHORIZED: 401,
  FORBIDDEN: 403,
  NOT_FOUND: 404,
  INTERNAL_SERVER_ERROR: 500,
  SERVICE_UNAVAILABLE: 503,
} as const;

// ============================================================================
// Discovery Types
// ============================================================================

/**
 * AuthZen Configuration returned by the discovery endpoint
 * As defined in the AuthZen specification
 */
export interface AuthZenConfiguration {
  /** The base URL of the Policy Decision Point (required) */
  policy_decision_point: string;
  /** Access evaluation endpoint URL (optional) */
  access_evaluation_endpoint?: string;
  /** Access evaluations (batch) endpoint URL (optional) */
  access_evaluations_endpoint?: string;
  /** Subject search endpoint URL (optional) */
  search_subject_endpoint?: string;
  /** Resource search endpoint URL (optional) */
  search_resource_endpoint?: string;
  /** Action search endpoint URL (optional) */
  search_action_endpoint?: string;
  /** Additional custom endpoints (optional) */
  [key: string]: string | undefined;
}

/**
 * Error thrown when discovery configuration is invalid
 */
export class AuthZenDiscoveryError extends AuthZenError {
  public readonly name = 'AuthZenDiscoveryError';

  constructor(message: string, requestId?: string) {
    super(message, requestId);
    Object.setPrototypeOf(this, AuthZenDiscoveryError.prototype);
  }
}
