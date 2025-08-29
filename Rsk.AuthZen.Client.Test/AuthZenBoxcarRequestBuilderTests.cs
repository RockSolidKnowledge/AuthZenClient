using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Rsk.AuthZen.Client.Test;

public class AuthZenBoxcarRequestBuilderTests
{
    private AuthZenBoxcarRequestBuilder CreateSut()
    {
        return new AuthZenBoxcarRequestBuilder();
    }

    [Fact]
    public void SetCorrelationId_WhenCalled_ShouldReturnSameBuilderInstance()
    {
        var sut = CreateSut();
        
        var result = sut.SetCorrelationId("test-correlation-id");

        result.Should().BeSameAs(sut);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SetCorrelationId_WhenCalledWithNullOrEmpty_ShouldThrowArgumentException(string correlationId)
    {
        var sut = CreateSut();

        Action act = () => sut.SetCorrelationId(correlationId);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Correlation ID must be provided*")
            .WithParameterName("id");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SetDefaultSubject_WhenCalledWithNullOrEmptyId_ShouldThrowArgumentException(string value)
    {
        var sut = CreateSut();

        Action act = () => sut.SetDefaultSubject(value, "hjdfgbhjdg");
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("ID must be provided*")
            .WithParameterName("id");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SetDefaultSubject_WhenCalledWithNullOrEmptyType_ShouldThrowArgumentException(string value)
    {
        var sut = CreateSut();

        Action act = () => sut.SetDefaultSubject("mhdfbghd",value);
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Type must be provided*")
            .WithParameterName("type");
    }

    [Fact]
    public void SetDefaultSubject_WhenCalled_ShouldReturnEmptyPropertyBag()
    {
        var sut = CreateSut();

        IAuthZenPropertyBag result = sut.SetDefaultSubject("subject-id", "subject-type");

        result.Should().NotBeNull();
        result.IsEmpty.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SetDefaultResource_WhenCalledWithNullOrEmptyId_ShouldThrowArgumentException(string value)
    {
        var sut = CreateSut();

        Action act = () => sut.SetDefaultResource(value, "resource-type");
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("ID must be provided*")
            .WithParameterName("id");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SetDefaultResource_WhenCalledWithNullOrEmptyType_ShouldThrowArgumentException(string value)
    {
        var sut = CreateSut();

        Action act = () => sut.SetDefaultResource("resource-id", value);
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Type must be provided*")
            .WithParameterName("type");
    }

    [Fact]
    public void SetDefaultResource_WhenCalled_ShouldReturnEmptyPropertyBag()
    {
        var sut = CreateSut();

        IAuthZenPropertyBag result = sut.SetDefaultResource("resource-id", "resource-type");

        result.Should().NotBeNull();
        result.IsEmpty.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SetDefaultAction_WhenCalledWithNullOrEmptyName_ShouldThrowArgumentException(string value)
    {
        var sut = CreateSut();

        Action act = () => sut.SetDefaultAction(value);
        
        act.Should().Throw<ArgumentException>()
            .WithMessage("Name must be provided*")
            .WithParameterName("name");
    }

    [Fact]
    public void SetDefaultAction_WhenCalled_ShouldReturnEmptyPropertyBag()
    {
        var sut = CreateSut();

        IAuthZenPropertyBag result = sut.SetDefaultAction("action-name");

        result.Should().NotBeNull();
        result.IsEmpty.Should().BeTrue();
    }
    
    [Fact]
    public void SetDefaultContext_WhenCalled_ShouldReturnPropertyBag()
    {
        var sut = CreateSut();

        IAuthZenPropertyBag result = sut.SetDefaultContext();

        result.Should().NotBeNull();
    }

    [Fact]
    public void Build_WhenCalledWithAllDefaults_ShouldCreateCorrectRequest()
    {
        var sut = CreateSut();
            
        sut.SetCorrelationId("test-correlation-id");
        sut.SetDefaultSubject("subject-id", "subject-type");
        sut.SetDefaultResource("resource-id", "resource-type");
        sut.SetDefaultAction("action-name");
        sut.SetDefaultContext()
            .Add("dfhjgbdfg", "dhjfbgdjhg");

        var request = sut.Build();

        request.Should().NotBeNull();
        request.CorrelationId.Should().Be("test-correlation-id");
        request.Body.DefaultValues.Subject.Id.Should().Be("subject-id");
        request.Body.DefaultValues.Subject.Type.Should().Be("subject-type");
        request.Body.DefaultValues.Resource.Id.Should().Be("resource-id");
        request.Body.DefaultValues.Resource.Type.Should().Be("resource-type");
        request.Body.DefaultValues.Action.Name.Should().Be("action-name");
        request.Body.DefaultValues.Context.Should().NotBeNull();
    }

    [Fact]
    public void Build_WhenCalledWithMissingDefaultSubject_ShouldBuildCorrectRequest()
    {
        var sut = CreateSut();
        
        sut.SetCorrelationId("test-correlation-id");
        sut.SetDefaultResource("resource-id", "resource-type");
        sut.SetDefaultAction("action-name");
        sut.SetDefaultContext()
            .Add("dfhjgbdfg", "dhjfbgdjhg");

        var request = sut.Build();

        request.Should().NotBeNull();
        request.CorrelationId.Should().Be("test-correlation-id");
        request.Body.DefaultValues.Subject.Should().BeNull();
        request.Body.DefaultValues.Resource.Id.Should().Be("resource-id");
        request.Body.DefaultValues.Resource.Type.Should().Be("resource-type");
        request.Body.DefaultValues.Action.Name.Should().Be("action-name");
        request.Body.DefaultValues.Context.Should().NotBeNull();
    }

    [Fact]
    public void Build_WhenCalledWithMissingDefaultResource_ShouldBuildCorrectRequest()
    {
        var sut = CreateSut();
        
        sut.SetCorrelationId("test-correlation-id");
        sut.SetDefaultSubject("subject-id", "subject-type");
        sut.SetDefaultAction("action-name");
        sut.SetDefaultContext()
            .Add("dfhjgbdfg", "dhjfbgdjhg");

        var request = sut.Build();

        request.Should().NotBeNull();
        request.CorrelationId.Should().Be("test-correlation-id");
        request.Body.DefaultValues.Subject.Id.Should().Be("subject-id");
        request.Body.DefaultValues.Subject.Type.Should().Be("subject-type");
        request.Body.DefaultValues.Resource.Should().BeNull();
        request.Body.DefaultValues.Action.Name.Should().Be("action-name");
        request.Body.DefaultValues.Context.Should().NotBeNull();
    }

    [Fact]
    public void Build_WhenCalledWithMissingDefaultAction_ShouldBuildCorrectRequest()
    {
        var sut = CreateSut();
        
        sut.SetCorrelationId("test-correlation-id");
        sut.SetDefaultSubject("subject-id", "subject-type");
        sut.SetDefaultResource("resource-id", "resource-type");
        sut.SetDefaultContext()
            .Add("dfhjgbdfg", "dhjfbgdjhg");

        var request = sut.Build();

        request.Should().NotBeNull();
        request.CorrelationId.Should().Be("test-correlation-id");
        request.Body.DefaultValues.Subject.Id.Should().Be("subject-id");
        request.Body.DefaultValues.Subject.Type.Should().Be("subject-type");
        request.Body.DefaultValues.Resource.Id.Should().Be("resource-id");
        request.Body.DefaultValues.Resource.Type.Should().Be("resource-type");
        request.Body.DefaultValues.Action.Should().BeNull();
        request.Body.DefaultValues.Context.Should().NotBeNull();
    }

    [Fact]
    public void Build_WhenCalledWithMissingDefaultContext_ShouldBuildCorrectRequest()
    {
        var sut = CreateSut();
        
        sut.SetCorrelationId("test-correlation-id");
        sut.SetDefaultSubject("subject-id", "subject-type");
        sut.SetDefaultResource("resource-id", "resource-type");
        sut.SetDefaultAction("action-name");

        var request = sut.Build();

        request.Should().NotBeNull();
        request.CorrelationId.Should().Be("test-correlation-id");
        request.Body.DefaultValues.Subject.Id.Should().Be("subject-id");
        request.Body.DefaultValues.Subject.Type.Should().Be("subject-type");
        request.Body.DefaultValues.Resource.Id.Should().Be("resource-id");
        request.Body.DefaultValues.Resource.Type.Should().Be("resource-type");
        request.Body.DefaultValues.Action.Name.Should().Be("action-name");
        request.Body.DefaultValues.Context.Should().BeNull();
    }
    [Fact]
    public void Build_WhenCalledWithEmptyDefaultContext_ShouldBuildCorrectRequest()
    {
        var sut = CreateSut();
        
        sut.SetCorrelationId("test-correlation-id");
        sut.SetDefaultSubject("subject-id", "subject-type");
        sut.SetDefaultResource("resource-id", "resource-type");
        sut.SetDefaultAction("action-name");
        sut.SetDefaultContext();

        var request = sut.Build();

        request.Should().NotBeNull();
        request.CorrelationId.Should().Be("test-correlation-id");
        request.Body.DefaultValues.Subject.Id.Should().Be("subject-id");
        request.Body.DefaultValues.Subject.Type.Should().Be("subject-type");
        request.Body.DefaultValues.Resource.Id.Should().Be("resource-id");
        request.Body.DefaultValues.Resource.Type.Should().Be("resource-type");
        request.Body.DefaultValues.Action.Name.Should().Be("action-name");
        request.Body.DefaultValues.Context.Should().BeNull();
    }

    [Fact]
    public void AddRequest_WhenCalled_ShouldShouldReturnRequestBuilder()
    {
        var sut = CreateSut();
        
        var result = sut.AddRequest();

        result.Should().NotBeSameAs(sut);
        result.Should().BeAssignableTo<IAuthZenRequestBuilder>();
    }
    
    [Fact]
    public void AddRequestThenSetSubject_WhenCalled_ShouldReturnAnIAuthZenPropertyBag()
    {
        var sut = CreateSut();

        IAuthZenPropertyBag result = sut.AddRequest().SetSubject("id", "type");

        result.Should().NotBeNull();
    }
    
    [Fact]
    public void AddRequestThenSetResource_WhenCalled_ShouldReturnAnIAuthZenPropertyBag()
    {
        var sut = CreateSut();

        IAuthZenPropertyBag result = sut.AddRequest().SetResource("id", "type");

        result.Should().NotBeNull();
    }
    
    [Fact]
    public void AddRequestThenSetAction_WhenCalled_ShouldReturnAnIAuthZenPropertyBag()
    {
        var sut = CreateSut();

        IAuthZenPropertyBag result = sut.AddRequest().SetAction("dfbgde");

        result.Should().NotBeNull();
    }
    
    [Fact]
    public void AddRequestThenSetContext_WhenCalled_ShouldReturnAnIAuthZenPropertyBag()
    {
        var sut = CreateSut();

        IAuthZenPropertyBag result = sut.AddRequest().SetContext();

        result.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("    ")]
    [InlineData("")]
    public void AddRequestThenSetSubject_WhenCalledWithInvalidId_ShouldThrowArgumentException(string invalidId)
    {
        var sut = CreateSut();

        Action act = () => sut.AddRequest().SetSubject(invalidId, "dihg");

        act.Should().Throw<ArgumentException>();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("    ")]
    [InlineData("")]
    public void AddRequestThenSetSubject_WhenCalledWithInvalidType_ShouldThrowArgumentException(string invalidType)
    {
        var sut = CreateSut();

        Action act = () => sut.AddRequest().SetSubject("iousdhgb", invalidType);

        act.Should().Throw<ArgumentException>();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("    ")]
    [InlineData("")]
    public void AddRequestThenSetResource_WhenCalledWithInvalidId_ShouldThrowArgumentException(string invalidId)
    {
        var sut = CreateSut();

        Action act = () => sut.AddRequest().SetResource(invalidId, "dihg");

        act.Should().Throw<ArgumentException>();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("    ")]
    [InlineData("")]
    public void AddRequestThenSetResource_WhenCalledWithInvalidType_ShouldThrowArgumentException(string invalidType)
    {
        var sut = CreateSut();

        Action act = () => sut.AddRequest().SetResource("iousdhgb", invalidType);

        act.Should().Throw<ArgumentException>();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("    ")]
    [InlineData("")]
    public void AddRequestThenSetAction_WhenCalledWithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        var sut = CreateSut();

        Action act = () => sut.AddRequest().SetAction(invalidName);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddRequestThenBuild_WhenCalled_ShouldConstructRequestCorrectly()
    {
        string subjectId = "jkisdhbfgv";
        string subjectType = "sdfg";
        string resourceId = "kjihsdfghui";
        string resourceType = "kijhi";
        string actionName = "kijh";
        
        string subjectProperty1 = "ijsbdgvu";
        int subjectProperty1Value = 8;
        string subjectProperty2 = "ksjhdfgo";
        bool subjectProperty2Value = true;
        
        string resourceProperty1 = "sfgnwse";
        DateTime resourceProperty1Value = new DateTime(2020, 12, 31);
        string resourceProperty2 = "bneafn";
        decimal resourceProperty2Value = 7826;

        string actionProperty1 = "fsgns";
        string actionProperty1Value = "xdsjhvb";
        string actionProperty2 = "hbeashb";
        int actionProperty2Value = 81;

        string contextProperty1 = "sfgnsfg";
        int contextProperty1Value = 87;
        string contextProperty2 = "bfesd";
        string contextProperty2Value = "khuibsdfv";
        string contextProperty3 = "saihdf90";
        string contextProperty3Value = "loihhio";
        
        var sut = CreateSut();

        var evaluation = sut.AddRequest();

        evaluation.SetSubject(subjectId, subjectType)
            .Add(subjectProperty1, subjectProperty1Value)
            .Add(subjectProperty2, subjectProperty2Value);

        evaluation.SetResource(resourceId, resourceType)
            .Add(resourceProperty1, resourceProperty1Value)
            .Add(resourceProperty2, resourceProperty2Value);
        
        evaluation.SetAction(actionName)
            .Add(actionProperty1, actionProperty1Value)
            .Add(actionProperty2, actionProperty2Value);
        
        evaluation.SetContext()
            .Add(contextProperty1, contextProperty1Value)
            .Add(contextProperty2, contextProperty2Value)
            .Add(contextProperty3, contextProperty3Value);
        
        var result = sut.Build();
        
        result.Should().NotBeNull();
        
        result.Body.Evaluations.Single().Subject.Id.Should().Be(subjectId);
        result.Body.Evaluations.Single().Subject.Type.Should().Be(subjectType);
        result.Body.Evaluations.Single().Subject.Properties.Should().HaveCount(2);
        result.Body.Evaluations.Single().Subject.Properties.Should().Contain(kv => kv.Key == subjectProperty1 && kv.Value.Equals(subjectProperty1Value));
        result.Body.Evaluations.Single().Subject.Properties.Should().Contain(kv => kv.Key == subjectProperty2 && kv.Value.Equals(subjectProperty2Value));

        result.Body.Evaluations.Single().Resource.Id.Should().Be(resourceId);
        result.Body.Evaluations.Single().Resource.Type.Should().Be(resourceType);
        result.Body.Evaluations.Single().Resource.Properties.Should().HaveCount(2);
        result.Body.Evaluations.Single().Resource.Properties.Should().Contain(kv => kv.Key == resourceProperty1 && kv.Value.Equals(resourceProperty1Value));
        result.Body.Evaluations.Single().Resource.Properties.Should().Contain(kv => kv.Key == resourceProperty2 && kv.Value.Equals(resourceProperty2Value));

        result.Body.Evaluations.Single().Action.Name.Should().Be(actionName);
        result.Body.Evaluations.Single().Action.Properties.Should().HaveCount(2);
        result.Body.Evaluations.Single().Action.Properties.Should().Contain(kv => kv.Key == actionProperty1 && kv.Value.Equals(actionProperty1Value));
        result.Body.Evaluations.Single().Action.Properties.Should().Contain(kv => kv.Key == actionProperty2 && kv.Value.Equals(actionProperty2Value));
        
        result.Body.Evaluations.Single().Context.Should().HaveCount(3);
        result.Body.Evaluations.Single().Context.Should().Contain(kv => kv.Key == contextProperty1 && kv.Value.Equals(contextProperty1Value));
        result.Body.Evaluations.Single().Context.Should().Contain(kv => kv.Key == contextProperty2 && kv.Value.Equals(contextProperty2Value));
        result.Body.Evaluations.Single().Context.Should().Contain(kv => kv.Key == contextProperty3 && kv.Value.Equals(contextProperty3Value));
    }
    
    [Fact]
    public void AddRequestThenBuild_WhenCalledWithNoSubject_ShouldNotSetSubject()
    {
        var sut = CreateSut();
        
        var evaluation = sut.AddRequest();

        evaluation.SetResource("lkjshdv", "loikjhv")
            .Add("lksiohv", "iuhgvi");
        
        evaluation.SetAction("kjhvbsdcbiu")
            .Add("ubsdfvubidxscfjib", 8);
        
        evaluation.SetContext()
            .Add("iuhdcv8", 76.5m);

        var result = sut.Build();
        
        result.Body.Evaluations.Single().Subject.Should().BeNull();
    }
    
    [Fact]
    public void AddRequestThenBuild_WhenCalledWithNoResource_ShouldNotSetResource()
    {
        var sut = CreateSut();

        var evaluation = sut.AddRequest();
        
        evaluation.SetSubject("lkjshdv", "loikjhv")
            .Add("lksiohv", "iuhgvi");
        
        evaluation.SetAction("kjhvbsdcbiu")
            .Add("ubsdfvubidxscfjib", 8);
        
        evaluation.SetContext()
            .Add("iuhdcv8", 76.5m);

        var result = sut.Build();
        
        result.Body.Evaluations.Single().Resource.Should().BeNull();
    }

    [Fact]
    public void AddRequestThenBuild_WhenCalledWithNoAction_ShouldNotSetAction()
    {
        var sut = CreateSut();
        
        var evaluation = sut.AddRequest();

        evaluation.SetSubject("lkjshdv", "loikjhv")
            .Add("lksiohv", "iuhgvi");
        
        evaluation.SetResource("jkdf", "okji0p")
            .Add("][-p0ik=", "oji-h9");
        
        evaluation.SetContext()
            .Add("iuhdcv8", 76.5m);

        var result = sut.Build();
        
        result.Body.Evaluations.Single().Action.Should().BeNull();
    }
    
    [Fact]
    public void AddRequestThenBuild_WhenCalledWithNoContext_ShouldNotSetContext()
    {
        var sut = CreateSut();
        
        var evaluation = sut.AddRequest();

        evaluation.SetSubject("lkjshdv", "loikjhv")
            .Add("lksiohv", "iuhgvi");
        
        evaluation.SetResource("jkdf", "okji0p")
            .Add("][-p0ik=", "oji-h9");
        
        evaluation.SetAction("oihsxdfbvh")
            .Add("iuhdcv8", 76.5m);

        var result = sut.Build();
        
        result.Body.Evaluations.Single().Context.Should().BeNull();
    }
    
    [Fact]
    public void AddRequestThenBuild_WhenCalledWithSubjectWithNoProperties_ShouldNotSetSubjectProperties()
    {
        var sut = CreateSut();

        sut.AddRequest().SetSubject("lkjshdv", "loikjhv");

        var result = sut.Build();
        
        result.Body.Evaluations.Single().Subject.Properties.Should().BeNull();
    }
    
    [Fact]
    public void AddRequestThenBuild_WhenCalledWithResourceWithNoProperties_ShouldNotSetResourceProperties()
    {
        var sut = CreateSut();

        sut.AddRequest().SetResource("lkjshdv", "loikjhv");

        var result = sut.Build();
        
        result.Body.Evaluations.Single().Resource.Properties.Should().BeNull();
    }
    
    [Fact]
    public void AddRequestThenBuild_WhenCalledWithActionWithNoProperties_ShouldNotSetActionProperties()
    {
        var sut = CreateSut();

        sut.AddRequest().SetAction("lkjshdv");

        var result = sut.Build();
        
        result.Body.Evaluations.Single().Action.Properties.Should().BeNull();
    }
    
    [Fact]
    public void AddRequestThenBuild_WhenCalledWithContextWithNoProperties_ShouldNotSetContext()
    {
        var sut = CreateSut();

        sut.AddRequest().SetContext();

        var result = sut.Build();

        result.Body.Evaluations.Should().BeEmpty();
    }

    [Theory]
    [InlineData(BoxcarSemantics.DenyOnFirstDeny)]
    [InlineData(BoxcarSemantics.PermitOnFirstPermit)]
    [InlineData(BoxcarSemantics.ExecuteAll)]
    public void SetEvaluationSemantics_WhenCalled_ShouldReturnSelf(BoxcarSemantics semantics)
    {
        var sut = CreateSut();
        
        var result = sut.SetEvaluationSemantics(semantics);
        
        result.Should().BeSameAs(sut);
    }

    [Theory]
    [InlineData(BoxcarSemantics.DenyOnFirstDeny)]
    [InlineData(BoxcarSemantics.PermitOnFirstPermit)]
    [InlineData(BoxcarSemantics.ExecuteAll)]
    public void Build_WhenSemanticsIsSet_ShouldShouldIncludeSemantics(BoxcarSemantics semantics)
    {
        var sut = CreateSut();
        
        sut.SetCorrelationId("test-correlation-id");
        sut.SetDefaultSubject("subject-id", "subject-type");
        sut.SetDefaultResource("resource-id", "resource-type");
        sut.SetDefaultAction("action-name");
        sut.SetDefaultContext()
            .Add("dfhjgbdfg", "dhjfbgdjhg");
        sut.SetEvaluationSemantics(semantics);

        var request = sut.Build();

        request.Should().NotBeNull();
        request.CorrelationId.Should().Be("test-correlation-id");
        request.Body.DefaultValues.Subject.Id.Should().Be("subject-id");
        request.Body.DefaultValues.Subject.Type.Should().Be("subject-type");
        request.Body.DefaultValues.Resource.Id.Should().Be("resource-id");
        request.Body.DefaultValues.Resource.Type.Should().Be("resource-type");
        request.Body.DefaultValues.Action.Name.Should().Be("action-name");
        request.Body.DefaultValues.Context.Should().NotBeNull();
        request.Body.Options.Semantics.Should().Be(semantics);
    }

    [Fact]
    public void Build_WhenBoxcarSemanticsIsNotSet_ShouldExcludeFromRequest()
    {
        var sut = CreateSut();
        
        sut.SetCorrelationId("test-correlation-id");
        sut.SetDefaultSubject("subject-id", "subject-type");
        sut.SetDefaultResource("resource-id", "resource-type");
        sut.SetDefaultAction("action-name");
        sut.SetDefaultContext()
            .Add("dfhjgbdfg", "dhjfbgdjhg");

        var request = sut.Build();
        
        request.Body.Options.Should().BeNull();
    }
}