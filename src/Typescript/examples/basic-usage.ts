import {
  AuthZenClient,
  AuthZenError,
  AuthZenValidationError,
  AuthZenRequestError,
  AuthZenResponseError,
  AuthZenNetworkError,
  AuthZenDiscoveryError,
  AccessEvaluationRequest,
  AccessEvaluationsRequest,
  Subject,
  Resource,
  Action,
  Context,
} from '../src';

/**
 * Discovery endpoint example
 */
async function discoveryExample(): Promise<void> {
  console.log('\n=== Discovery Example ===');

  const client = new AuthZenClient({
    pdpUrl: 'https://pdp.mycompany.com',
    token: 'your-bearer-token-here',
  });

  try {
    const config = await client.discover();
    
    console.log('✅ Discovery successful!');
    console.log(`Policy Decision Point: ${config.policy_decision_point}`);
    
    if (config.access_evaluation_endpoint) {
      console.log(`Single Evaluation Endpoint: ${config.access_evaluation_endpoint}`);
    }
    
    if (config.access_evaluations_endpoint) {
      console.log(`Batch Evaluations Endpoint: ${config.access_evaluations_endpoint}`);
    }
    
    if (config.search_subject_endpoint) {
      console.log(`Subject Search Endpoint: ${config.search_subject_endpoint}`);
    }
    
    if (config.search_resource_endpoint) {
      console.log(`Resource Search Endpoint: ${config.search_resource_endpoint}`);
    }
    
    if (config.search_action_endpoint) {
      console.log(`Action Search Endpoint: ${config.search_action_endpoint}`);
    }
    
    console.log('\nFull Configuration:');
    console.log(JSON.stringify(config, null, 2));

  } catch (error) {
    if (error instanceof AuthZenDiscoveryError) {
      console.error('❌ Discovery configuration error:', error.message);
    } else if (error instanceof AuthZenRequestError) {
      console.error('❌ Discovery request failed:', error.message, `(Status: ${error.statusCode})`);
    } else if (error instanceof AuthZenNetworkError) {
      console.error('❌ Discovery network error:', error.message);
    } else {
      console.error('❌ Discovery failed:', error);
    }
  }
}

/**
 * Single evaluation example
 */
async function singleEvaluationExample(): Promise<void> {
  console.log('\n=== Single Evaluation Example ===');

  const client = new AuthZenClient({
    pdpUrl: 'https://pdp.mycompany.com',
    token: 'your-bearer-token-here',
    timeout: 5000,
  });

  try {
    const request: AccessEvaluationRequest = {
      subject: {
        type: 'user',
        id: 'alice@company.com',
        properties: { 
          role: 'employee', 
          department: 'engineering',
          clearance_level: 2
        }
      },
      resource: {
        type: 'document',
        id: 'design-doc-123',
        properties: { 
          classification: 'internal', 
          owner: 'alice@company.com',
          project: 'authzen-client'
        }
      },
      action: {
        name: 'read',
        properties: { 
          method: 'GET',
          requested_fields: ['title', 'content']
        }
      },
      context: {
        ip_address: '192.168.1.100',
        time: new Date().toISOString(),
        environment: 'production',
        user_agent: 'AuthZen-Client/1.0',
        request_id: 'req-12345'
      }
    };

    const response = await client.evaluate(request);
    
    console.log('✅ Single evaluation successful!');
    console.log(`Decision: ${response.decision ? 'ALLOW' : 'DENY'}`);
    
    if (response.context) {
      console.log('Response context:');
      console.log(JSON.stringify(response.context, null, 2));
    }

  } catch (error) {
    if (error instanceof AuthZenValidationError) {
      console.error('❌ Validation error:', error.message);
    } else if (error instanceof AuthZenRequestError) {
      console.error('❌ Request failed:', error.message, `(Status: ${error.statusCode})`);
    } else if (error instanceof AuthZenNetworkError) {
      console.error('❌ Network error:', error.message);
    } else {
      console.error('❌ Evaluation failed:', error);
    }
  }
}

/**
 * Batch evaluations example
 */
async function batchEvaluationsExample(): Promise<void> {
  console.log('\n=== Batch Evaluations Example ===');

  const client = new AuthZenClient({
    pdpUrl: 'https://pdp.mycompany.com',
    token: 'your-bearer-token-here',
  });

  try {
    const request: AccessEvaluationsRequest = {
      evaluations: [
        {
          subject: { type: 'user', id: 'alice@company.com' },
          resource: { type: 'document', id: 'doc-1' },
          action: { name: 'read' }
        },
        {
          // Missing subject - will use default
          resource: { type: 'document', id: 'doc-2' },
          action: { name: 'write' }
        },
        {
          subject: { type: 'user', id: 'charlie@company.com' },
          // Missing resource - will use default
          action: { name: 'delete' }
        },
        {
          subject: { type: 'user', id: 'david@company.com' },
          resource: { type: 'api', id: 'user-service' },
          // Missing action - will use default
        }
      ],
      // Default values applied when missing from individual evaluations
      subject: { 
        type: 'user', 
        id: 'default-user@company.com',
        properties: { role: 'guest' }
      },
      resource: { 
        type: 'document', 
        id: 'shared-document',
        properties: { classification: 'public' }
      },
      action: { 
        name: 'read',
        properties: { method: 'GET' }
      },
      context: {
        environment: 'production',
        ip_address: '10.0.0.1',
        time: new Date().toISOString(),
        batch_id: 'batch-789'
      },
      options: {
        evaluations_semantic: 'execute_all'
      }
    };

    const response = await client.evaluations(request);
    
    console.log('✅ Batch evaluations successful!');
    console.log(`Processed ${response.evaluations.length} evaluations:`);
    
    response.evaluations.forEach((evaluation, index) => {
      console.log(`  ${index + 1}. Decision: ${evaluation.decision ? 'ALLOW' : 'DENY'}`);
      if (evaluation.context?.reason_admin) {
        console.log(`     Reason: ${evaluation.context.reason_admin}`);
      }
    });

  } catch (error) {
    if (error instanceof AuthZenValidationError) {
      console.error('❌ Validation error:', error.message);
    } else if (error instanceof AuthZenRequestError) {
      console.error('❌ Request failed:', error.message, `(Status: ${error.statusCode})`);
    } else if (error instanceof AuthZenNetworkError) {
      console.error('❌ Network error:', error.message);
    } else {
      console.error('❌ Batch evaluations failed:', error);
    }
  }
}

/**
 * Run all basic examples
 */
async function runBasicExamples(): Promise<void> {
  console.log('🚀 AuthZen TypeScript Client - Basic Usage Examples');
  console.log('==================================================');

  await discoveryExample();
  await singleEvaluationExample();
  await batchEvaluationsExample();

  console.log('\n✅ All basic examples completed!');
}

// Export examples
export {
  discoveryExample,
  singleEvaluationExample,
  batchEvaluationsExample,
  runBasicExamples,
};

// Run examples if this file is executed directly
if (require.main === module) {
  runBasicExamples().catch(console.error);
}
