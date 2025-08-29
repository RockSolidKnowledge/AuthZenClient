/**
 * AuthZen TypeScript Client Library
 * 
 * A TypeScript client library for interacting with AuthZen-compliant
 * Policy Decision Points (PDPs) according to the AuthZen Authorization API 1.0 specification.
 */

export { AuthZenClient } from './client';
export * from './types';

// Re-export commonly used types for convenience
export type {
  AuthZenClientConfig,
  AccessEvaluationRequest,
  AccessEvaluationResponse,
  AccessEvaluationsRequest,
  AccessEvaluationsResponse,
  BatchEvaluationOptions,
  AuthZenConfiguration,
  Subject,
  Resource,
  Action,
  Context,
  EvaluationSemantics,
  EvaluationOptions,
} from './types';
