using System;
using System.Threading.Tasks;

namespace Rsk.AuthZen.Client
{
    public interface IAuthZenClient
    {
        Task<AuthZenResponse> Evaluate(AuthZenEvaluationRequest request);

        Task<AuthZenBoxcarResponse> Evaluate(AuthZenBoxcarEvaluationRequest request);
    }
}