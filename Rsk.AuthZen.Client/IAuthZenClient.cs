using System;
using System.Threading.Tasks;

namespace Rsk.AuthZen.Client
{
    public interface IAuthZenClient
    {
        Task<AuthZenResponse> Evaluate(
            AuthZenPayload<AuthZenSingleEvaluationRequest> request);

        Task<AuthZenBoxcarResponse> Evaluate(
            AuthZenPayload<AuthZenBoxcarEvaluationRequest> request);
    }
}