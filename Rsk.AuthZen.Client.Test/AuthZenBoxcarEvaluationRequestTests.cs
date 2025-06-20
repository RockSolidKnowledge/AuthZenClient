using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Rsk.AuthZen.Client.Test;

public class AuthZenBoxcarEvaluationRequestTests
{
    [Fact]
    public void ToDto_WhenDefaultSubjectIsSet_ShouldPopulateSubject()
    {
        var defaults = new AuthZenEvaluationRequest()
        {
            Subject = new AuthZenSubject
            {
                Id = "subject-id",
                Type = "subject-type",
                Properties = new Dictionary<string, object> { { "key", "value" } }
            }
        };
        
        
        var request = new AuthZenBoxcarEvaluationRequest
        {
            DefaultValues = defaults
        };

        var dto = request.ToDto();

        dto.Subject.Should().NotBeNull();
        dto.Subject.Id.Should().Be("subject-id");
        dto.Subject.Type.Should().Be("subject-type");
        dto.Subject.Properties.Keys.Should().Contain("key");
        dto.Subject.Properties["key"].Should().Be("value");
    }

    [Fact]
    public void ToDto_WhenDefaultResourceIsSet_ShouldPopulateResource()
    {
        var defaults = new AuthZenEvaluationRequest()
        {
            Resource = new AuthZenResource
            {
                Id = "resource-id",
                Type = "resource-type",
                Properties = new Dictionary<string, object> { { "key", "value" } }
            }
        };
        
        var request = new AuthZenBoxcarEvaluationRequest
        {
            DefaultValues = defaults
        };

        var dto = request.ToDto();

        dto.Resource.Should().NotBeNull();
        dto.Resource.Id.Should().Be("resource-id");
        dto.Resource.Type.Should().Be("resource-type");
        dto.Resource.Properties.Keys.Should().Contain("key");
        dto.Resource.Properties["key"].Should().Be("value");
    }

    [Fact]
    public void ToDto_WhenDefaultActionIsSet_ShouldPopulateAction()
    {
        var defaults = new AuthZenEvaluationRequest()
        {
            Action = new AuthZenAction
            {
                Name = "action-name",
                Properties = new Dictionary<string, object> { { "key", "value" } }
            }
        };
            
        var request = new AuthZenBoxcarEvaluationRequest
        {
            DefaultValues = defaults
        };

        var dto = request.ToDto();

        dto.Action.Should().NotBeNull();
        dto.Action.Name.Should().Be("action-name");
        dto.Action.Properties.Keys.Should().Contain("key");
        dto.Action.Properties["key"].Should().Be("value");
    }

    [Fact]
    public void ToDto_WhenDefaultContextIsSet_ShouldPopulateContext()
    {
        var defaults = new AuthZenEvaluationRequest()
        {
            Context = new Dictionary<string, object>
            {
                { "contextKey", "contextValue" }
            }
        };
        
        var request = new AuthZenBoxcarEvaluationRequest
        {
            DefaultValues = defaults
        };

        var dto = request.ToDto();

        dto.Context.Should().NotBeNull();
        dto.Context.Keys.Should().Contain("contextKey");
        dto.Context["contextKey"].Should().Be("contextValue");
    }

    [Fact]
    public void ToDto_WhenEvaluationsIsMissing_ShouldNotPopulateEvaluations()
    {
        var request = new AuthZenBoxcarEvaluationRequest
        {
            Evaluations = null
        };

        var dto = request.ToDto();

        dto.Evaluations.Should().BeNull();
    }

    [Fact]
    public void ToDto_WhenEvaluationsIsEmpty_ShouldNotPopulateEvaluations()
    {
        var request = new AuthZenBoxcarEvaluationRequest
        {
            Evaluations = new List<AuthZenEvaluationRequest>()
        };

        var dto = request.ToDto();

        dto.Evaluations.Should().BeNull();
    }

    [Fact]
    public void ToDto_WhenEvaluationsIsSet_ShouldPopulateEachEvaluation()
    {
        var request = new AuthZenBoxcarEvaluationRequest
        {
            Evaluations = new List<AuthZenEvaluationRequest>
            {
                new ()
                {
                    Subject = new AuthZenSubject { Id = "eval-subject-id1", Type = "eval-subject-type1" },
                    Resource = new AuthZenResource { Id = "eval-resource-id1", Type = "eval-resource-type1" },
                    Action = new AuthZenAction { Name = "eval-action-name1", }
                },

                new ()
                {
                    Subject = new AuthZenSubject { Id = "eval-subject-id2", Type = "eval-subject-type2" },
                    Resource = new AuthZenResource { Id = "eval-resource-id2", Type = "eval-resource-type2" },
                    Action = new AuthZenAction { Name = "eval-action-name2", }
                },

                new ()
                {
                    Subject = new AuthZenSubject { Id = "eval-subject-id3", Type = "eval-subject-type3" },
                    Resource = new AuthZenResource { Id = "eval-resource-id3", Type = "eval-resource-type3" },
                    Action = new AuthZenAction { Name = "eval-action-name3", }
                },
            }
        };

        var dto = request.ToDto();

        dto.Evaluations.Should().NotBeNull();
        dto.Evaluations.Length.Should().Be(3);

        dto.Evaluations[0].Subject.Id.Should().Be("eval-subject-id1");
        dto.Evaluations[0].Subject.Type.Should().Be("eval-subject-type1");
        dto.Evaluations[0].Resource.Id.Should().Be("eval-resource-id1");
        dto.Evaluations[0].Resource.Type.Should().Be("eval-resource-type1");
        dto.Evaluations[0].Action.Name.Should().Be("eval-action-name1");

        dto.Evaluations[1].Subject.Id.Should().Be("eval-subject-id2");
        dto.Evaluations[1].Subject.Type.Should().Be("eval-subject-type2");
        dto.Evaluations[1].Resource.Id.Should().Be("eval-resource-id2");
        dto.Evaluations[1].Resource.Type.Should().Be("eval-resource-type2");
        dto.Evaluations[1].Action.Name.Should().Be("eval-action-name2");

        dto.Evaluations[2].Subject.Id.Should().Be("eval-subject-id3");
        dto.Evaluations[2].Subject.Type.Should().Be("eval-subject-type3");
        dto.Evaluations[2].Resource.Id.Should().Be("eval-resource-id3");
        dto.Evaluations[2].Resource.Type.Should().Be("eval-resource-type3");
        dto.Evaluations[2].Action.Name.Should().Be("eval-action-name3");
    }

    [Fact]
    public void ToDto_WhenEvaluationSubjectIsSet_ShouldPopulateSubject()
    {
        var request = new AuthZenBoxcarEvaluationRequest
        {
            Evaluations = new List<AuthZenEvaluationRequest>()
            {
                new ()
                {
                    Subject = new AuthZenSubject
                    {
                        Id = "subject-id",
                        Type = "subject-type",
                        Properties = new Dictionary<string, object> { { "key", "value" } }
                    }
                }
            }
        };

        var dto = request.ToDto();

        dto.Evaluations[0].Subject.Should().NotBeNull();
        dto.Evaluations[0].Subject.Id.Should().Be("subject-id");
        dto.Evaluations[0].Subject.Type.Should().Be("subject-type");
        dto.Evaluations[0].Subject.Properties.Keys.Should().Contain("key");
        dto.Evaluations[0].Subject.Properties["key"].Should().Be("value");
    }

    [Fact]
    public void ToDto_WhenEvaluationResourceIsSet_ShouldPopulateResource()
    {
        var request = new AuthZenBoxcarEvaluationRequest
        {
            Evaluations = new List<AuthZenEvaluationRequest>()
            {
                new ()
                {
                    Resource = new AuthZenResource
                    {
                        Id = "resource-id",
                        Type = "resource-type",
                        Properties = new Dictionary<string, object> { { "key", "value" } }
                    }
                }
            }

        };

        var dto = request.ToDto();

        dto.Evaluations[0].Resource.Should().NotBeNull();
        dto.Evaluations[0].Resource.Id.Should().Be("resource-id");
        dto.Evaluations[0].Resource.Type.Should().Be("resource-type");
        dto.Evaluations[0].Resource.Properties.Keys.Should().Contain("key");
        dto.Evaluations[0].Resource.Properties["key"].Should().Be("value");
    }

    [Fact]
    public void ToDto_WhenEvaluationActionIsSet_ShouldPopulateAction()
    {
        var request = new AuthZenBoxcarEvaluationRequest
        {
            Evaluations = new List<AuthZenEvaluationRequest>()
            {
                new ()
                {
                    Action = new AuthZenAction
                    {
                        Name = "action-name",
                        Properties = new Dictionary<string, object> { { "key", "value" } }
                    }
                }
            }
        };

        var dto = request.ToDto();

        dto.Evaluations[0].Action.Should().NotBeNull();
        dto.Evaluations[0].Action.Name.Should().Be("action-name");
        dto.Evaluations[0].Action.Properties.Keys.Should().Contain("key");
        dto.Evaluations[0].Action.Properties["key"].Should().Be("value");
    }

    [Fact]
    public void ToDto_WhenEvaluationContextIsSet_ShouldPopulateContext()
    {
        var request = new AuthZenBoxcarEvaluationRequest
        {
            Evaluations = new List<AuthZenEvaluationRequest>()
            {
                new ()
                {
                    Context = new Dictionary<string, object>
                    {
                        { "contextKey", "contextValue" }
                    }
                }
            }
        };

        var dto = request.ToDto();

        dto.Evaluations[0].Context.Should().NotBeNull();
        dto.Evaluations[0].Context.Keys.Should().Contain("contextKey");
        dto.Evaluations[0].Context["contextKey"].Should().Be("contextValue");
    }

    [Theory]
    [InlineData(BoxcarSemantics.DenyOnFirstDeny)]
    [InlineData(BoxcarSemantics.PermitOnFirstPermit)]
    [InlineData(BoxcarSemantics.ExecuteAll)]
    public void ToDto_OptionsAreProvided_ShouldIncludeOptionsInRequestDto(BoxcarSemantics semantics)
    {
        var options = new AuthZenBoxcarOptions()
        {
            Semantics = semantics
        };
        
        var request = new AuthZenBoxcarEvaluationRequest
        {
            Evaluations = new List<AuthZenEvaluationRequest>
            {
                new ()
                {
                    Subject = new AuthZenSubject { Id = "eval-subject-id1", Type = "eval-subject-type1" },
                    Resource = new AuthZenResource { Id = "eval-resource-id1", Type = "eval-resource-type1" },
                    Action = new AuthZenAction { Name = "eval-action-name1", }
                },

                new ()
                {
                    Subject = new AuthZenSubject { Id = "eval-subject-id2", Type = "eval-subject-type2" },
                    Resource = new AuthZenResource { Id = "eval-resource-id2", Type = "eval-resource-type2" },
                    Action = new AuthZenAction { Name = "eval-action-name2", }
                },

                new ()
                {
                    Subject = new AuthZenSubject { Id = "eval-subject-id3", Type = "eval-subject-type3" },
                    Resource = new AuthZenResource { Id = "eval-resource-id3", Type = "eval-resource-type3" },
                    Action = new AuthZenAction { Name = "eval-action-name3", }
                },
            },
            Options = options
        };

        var dto = request.ToDto();
        
        dto.Options.Should().NotBeNull();
        dto.Options.Should().BeEquivalentTo(options.ToDto());
    }
}