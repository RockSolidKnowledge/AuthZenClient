using System;
using System.Threading.Tasks;

namespace Rsk.AuthZen.Client
{
    public interface IAuthZenClient
    {
        Task<AuthZenResponse> Evaluate(
            AuthZenPayload<AuthZenEvaluationRequest> request);

        Task<AuthZenBoxcarResponse> Evaluate(
            AuthZenPayload<AuthZenBoxcarEvaluationRequest> request);
    }
}