using System;
using System.Collections.Generic;
using Rsk.AuthZen.Client.DTOs;

namespace Rsk.AuthZen.Client
{
    /// <summary>
    /// Represents a request body for a boxcar evaluation in AuthZen, containing multiple evaluations,
    /// default values, and evaluation options.
    /// </summary>
    public class AuthZenBoxcarEvaluationBody
    {
        /// <summary>
        /// Gets the list of individual evaluation bodies to be processed in the boxcar request.
        /// </summary>
        public List<AuthZenEvaluationBody> Evaluations { get; internal set; }

        /// <summary>
        /// Gets the default values to be applied to each evaluation if not explicitly set.
        /// </summary>
        public AuthZenEvaluationBody DefaultValues { get; internal set; }

        /// <summary>
        /// Gets the options that control the semantics of the boxcar evaluation.
        /// </summary>
        public AuthZenBoxcarOptions Options { get; internal set; }

        /// <summary>
        /// Converts this instance to a <see cref="AuthZenBoxcarRequestMessageDto"/> for transmission.
        /// </summary>
        /// <returns>The corresponding <see cref="AuthZenBoxcarRequestMessageDto"/>.</returns>
        internal AuthZenBoxcarRequestMessageDto ToDto()
        {
            var dto = new AuthZenBoxcarRequestMessageDto();

            if (DefaultValues?.Subject != null)
            {
                dto.Subject = new AuthZenSubjectDto
                {
                    Id = DefaultValues.Subject.Id,
                    Type = DefaultValues.Subject.Type,
                    Properties = DefaultValues.Subject.Properties
                };
            }

            if (DefaultValues?.Resource != null)
            {
                dto.Resource = new AuthZenResourceDto
                {
                    Id = DefaultValues.Resource.Id,
                    Type = DefaultValues.Resource.Type,
                    Properties = DefaultValues.Resource.Properties
                };
            }

            if (DefaultValues?.Action != null)
            {
                dto.Action = new AuthZenActionDto
                {
                    Name = DefaultValues.Action.Name,
                    Properties = DefaultValues.Action.Properties
                };
            }

            dto.Context = DefaultValues?.Context;

            if (Evaluations != null && Evaluations.Count > 0)
            {
                dto.Evaluations = new AuthZenRequestMessageDto[Evaluations.Count];
                for (int i = 0; i < Evaluations.Count; i++)
                {
                    dto.Evaluations[i] = Evaluations[i].ToDto();
                }
            }

            if (Options != null)
            {
                dto.Options = Options.ToDto();
            }

            return dto;
        }
    }

    /// <summary>
    /// Represents the response from a boxcar evaluation request in AuthZen,
    /// including a correlation identifier and the results of individual evaluations.
    /// </summary>
    public class AuthZenBoxcarResponse
    {
        /// <summary>
        /// Gets the correlation identifier for the boxcar evaluation request.
        /// </summary>
        public string CorrelationId { get; internal set; }
    
        /// <summary>
        /// Gets the list of evaluation results returned by the boxcar request.
        /// </summary>
        public List<AuthZenResponse> Evaluations { get; internal set; }
    }

    /// <summary>
    /// Defines the semantics for boxcar evaluations in AuthZen.
    /// </summary>
    public enum BoxcarSemantics
    {
        /// <summary>
        /// Indicates that all evaluations should be executed.
        /// </summary>
        ExecuteAll,

        /// <summary>
        /// Indicates that the evaluation should stop and deny on the first deny result.
        /// </summary>
        DenyOnFirstDeny,

        /// <summary>
        /// Indicates that the evaluation should stop and permit on the first permit result.
        /// </summary>
        PermitOnFirstPermit
    }

    /// <summary>
    /// Represents the options for a boxcar evaluation in AuthZen, including evaluation semantics.
    /// </summary>
    public class AuthZenBoxcarOptions
    {
        /// <summary>
        /// Gets the semantics that control the evaluation behavior in the boxcar request.
        /// </summary>
        public BoxcarSemantics Semantics { get; internal set; }

        /// <summary>
        /// Converts this instance to a <see cref="AuthZenBoxcarOptionsDto"/> for transmission.
        /// </summary>
        /// <returns>The corresponding <see cref="AuthZenBoxcarOptionsDto"/>.</returns>
        internal AuthZenBoxcarOptionsDto ToDto()
        {
            return new AuthZenBoxcarOptionsDto
            {
                Evaluations_semantic = ConvertSemantics(Semantics)
            };
        }

        /// <summary>
        /// Converts the <see cref="BoxcarSemantics"/> enumeration value to its string representation.
        /// </summary>
        /// <param name="semantics">The semantics to convert.</param>
        /// <returns>The string representation of the semantics.</returns>
        /// <exception cref="ArgumentException">Thrown if the semantics value is not supported.</exception>
        private static string ConvertSemantics(BoxcarSemantics semantics)
        {
            return semantics switch
            {
                BoxcarSemantics.ExecuteAll => "execute_all",
                BoxcarSemantics.DenyOnFirstDeny => "deny_on_first_deny",
                BoxcarSemantics.PermitOnFirstPermit => "permit_on_first_permit",
                _ => throw new ArgumentException($"Semantics value {semantics} is not supported ")
            };
        }
    }
}