using System;
using System.Threading.Tasks;

namespace Rsk.AuthZen.Client
{
    /// <summary>
    /// Defines methods for evaluating authorization requests using the AuthZen service.
    /// </summary>
    public interface IAuthZenClient
    {
        /// <summary>
        /// Gets the metadata information of the various endpoints from the AuthZen service.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the metadata response.
        /// </returns>
        Task<AuthZenMetadataResponse> GetMetadata();
        
        /// <summary>
        /// Evaluates a single authorization request.
        /// </summary>
        /// <param name="request">The authorization evaluation request.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the evaluation response.
        /// </returns>
        Task<AuthZenResponse> Evaluate(AuthZenEvaluationRequest request);
    
        /// <summary>
        /// Evaluates a boxcar (batch) authorization request.
        /// </summary>
        /// <param name="request">The boxcar evaluation request containing multiple evaluations.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the boxcar evaluation response.
        /// </returns>
        Task<AuthZenBoxcarResponse> Evaluate(AuthZenBoxcarEvaluationRequest request);
    }
}