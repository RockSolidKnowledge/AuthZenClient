using System;
using System.Threading.Tasks;

namespace Rsk.AuthZen.Client
{
    public interface IAuthZenClient
    {
        Task<AuthZenResponse> Evaluate(
            AuthZenPayload<AuthZenEvaluationRequest> evaluationRequest);

        Task<AuthZenBoxcarResponse> Evaluate(
            AuthZenPayload<AuthZenBoxcarRequest> request);

        // Task<AuthZenBoxcarResponse> Evaluate(
        //     AuthZenBoxcarRequest request, 
        //     AuthZenBoxcarEvaluation defaults,
        //     AuthZenBoxcarOptions boxcarOptions);
    }
}