using System;
using FluentAssertions;
using Xunit;

namespace Rsk.AuthZen.Client.Test;

public class AuthZenSingleRequestBuilderTests
{
    [Fact]
    public void SetSubject_WhenCalled_ShouldReturnAnIAuthZenPropertyBag()
    {
        var sut = CreateSut();

        IAuthZenPropertyBag result = sut.SetSubject("id", "type");

        result.Should().NotBeNull();
    }
    
    [Fact]
    public void SetResource_WhenCalled_ShouldReturnAnIAuthZenPropertyBag()
    {
        var sut = CreateSut();

        IAuthZenPropertyBag result = sut.SetResource("id", "type");

        result.Should().NotBeNull();
    }
    
    [Fact]
    public void SetAction_WhenCalled_ShouldReturnAnIAuthZenPropertyBag()
    {
        var sut = CreateSut();

        IAuthZenPropertyBag result = sut.SetAction("dfbgde");

        result.Should().NotBeNull();
    }
    
    [Fact]
    public void SetContext_WhenCalled_ShouldReturnAnIAuthZenPropertyBag()
    {
        var sut = CreateSut();

        IAuthZenPropertyBag result = sut.SetContext();

        result.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("    ")]
    [InlineData("")]
    public void SetSubject_WhenCalledWithInvalidId_ShouldThrowArgumentException(string invalidId)
    {
        var sut = CreateSut();

        Action act = () => sut.SetSubject(invalidId, "dihg");

        act.Should().Throw<ArgumentException>();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("    ")]
    [InlineData("")]
    public void SetSubject_WhenCalledWithInvalidType_ShouldThrowArgumentException(string invalidType)
    {
        var sut = CreateSut();

        Action act = () => sut.SetSubject("iousdhgb", invalidType);

        act.Should().Throw<ArgumentException>();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("    ")]
    [InlineData("")]
    public void SetResource_WhenCalledWithInvalidId_ShouldThrowArgumentException(string invalidId)
    {
        var sut = CreateSut();

        Action act = () => sut.SetResource(invalidId, "dihg");

        act.Should().Throw<ArgumentException>();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("    ")]
    [InlineData("")]
    public void SetResource_WhenCalledWithInvalidType_ShouldThrowArgumentException(string invalidType)
    {
        var sut = CreateSut();

        Action act = () => sut.SetResource("iousdhgb", invalidType);

        act.Should().Throw<ArgumentException>();
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("    ")]
    [InlineData("")]
    public void SetAction_WhenCalledWithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        var sut = CreateSut();

        Action act = () => sut.SetAction(invalidName);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Build_WhenCalled_ShouldConstructRequestCorrectly()
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

        sut.SetSubject(subjectId, subjectType)
            .Add(subjectProperty1, subjectProperty1Value)
            .Add(subjectProperty2, subjectProperty2Value);

        sut.SetResource(resourceId, resourceType)
            .Add(resourceProperty1, resourceProperty1Value)
            .Add(resourceProperty2, resourceProperty2Value);
        
        sut.SetAction(actionName)
            .Add(actionProperty1, actionProperty1Value)
            .Add(actionProperty2, actionProperty2Value);
        
        sut.SetContext()
            .Add(contextProperty1, contextProperty1Value)
            .Add(contextProperty2, contextProperty2Value)
            .Add(contextProperty3, contextProperty3Value);
        
        var result = sut.Build();
        
        result.Should().NotBeNull();
        
        result.Body.Subject.Id.Should().Be(subjectId);
        result.Body.Subject.Type.Should().Be(subjectType);
        result.Body.Subject.Properties.Should().HaveCount(2);
        result.Body.Subject.Properties.Should().Contain(kv => kv.Key == subjectProperty1 && kv.Value.Equals(subjectProperty1Value));
        result.Body.Subject.Properties.Should().Contain(kv => kv.Key == subjectProperty2 && kv.Value.Equals(subjectProperty2Value));

        result.Body.Resource.Id.Should().Be(resourceId);
        result.Body.Resource.Type.Should().Be(resourceType);
        result.Body.Resource.Properties.Should().HaveCount(2);
        result.Body.Resource.Properties.Should().Contain(kv => kv.Key == resourceProperty1 && kv.Value.Equals(resourceProperty1Value));
        result.Body.Resource.Properties.Should().Contain(kv => kv.Key == resourceProperty2 && kv.Value.Equals(resourceProperty2Value));

        result.Body.Action.Name.Should().Be(actionName);
        result.Body.Action.Properties.Should().HaveCount(2);
        result.Body.Action.Properties.Should().Contain(kv => kv.Key == actionProperty1 && kv.Value.Equals(actionProperty1Value));
        result.Body.Action.Properties.Should().Contain(kv => kv.Key == actionProperty2 && kv.Value.Equals(actionProperty2Value));
        
        result.Body.Context.Should().HaveCount(3);
        result.Body.Context.Should().Contain(kv => kv.Key == contextProperty1 && kv.Value.Equals(contextProperty1Value));
        result.Body.Context.Should().Contain(kv => kv.Key == contextProperty2 && kv.Value.Equals(contextProperty2Value));
        result.Body.Context.Should().Contain(kv => kv.Key == contextProperty3 && kv.Value.Equals(contextProperty3Value));
    }
    
    [Fact]
    public void Build_WhenCalledWithNoSubject_ShouldNotSetSubject()
    {
        var sut = CreateSut();

        sut.SetResource("lkjshdv", "loikjhv")
            .Add("lksiohv", "iuhgvi");
        
        sut.SetAction("kjhvbsdcbiu")
            .Add("ubsdfvubidxscfjib", 8);
        
        sut.SetContext()
            .Add("iuhdcv8", 76.5m);

        var result = sut.Build();
        
        result.Body.Subject.Should().BeNull();
    }
    
    [Fact]
    public void Build_WhenCalledWithNoResource_ShouldNotSetResource()
    {
        var sut = CreateSut();

        sut.SetSubject("lkjshdv", "loikjhv")
            .Add("lksiohv", "iuhgvi");
        
        sut.SetAction("kjhvbsdcbiu")
            .Add("ubsdfvubidxscfjib", 8);
        
        sut.SetContext()
            .Add("iuhdcv8", 76.5m);

        var result = sut.Build();
        
        result.Body.Resource.Should().BeNull();
    }

    [Fact]
    public void Build_WhenCalledWithNoAction_ShouldNotSetAction()
    {
        var sut = CreateSut();

        sut.SetSubject("lkjshdv", "loikjhv")
            .Add("lksiohv", "iuhgvi");
        
        sut.SetResource("jkdf", "okji0p")
            .Add("][-p0ik=", "oji-h9");
        
        sut.SetContext()
            .Add("iuhdcv8", 76.5m);

        var result = sut.Build();
        
        result.Body.Action.Should().BeNull();
    }
    
    [Fact]
    public void Build_WhenCalledWithNoContext_ShouldNotSetContext()
    {
        var sut = CreateSut();

        sut.SetSubject("lkjshdv", "loikjhv")
            .Add("lksiohv", "iuhgvi");
        
        sut.SetResource("jkdf", "okji0p")
            .Add("][-p0ik=", "oji-h9");
        
        sut.SetAction("oihsxdfbvh")
            .Add("iuhdcv8", 76.5m);

        var result = sut.Build();
        
        result.Body.Context.Should().BeNull();
    }
    
    [Fact]
    public void Build_WhenCalledWithSubjectWithNoProperties_ShouldNotSetSubjectProperties()
    {
        var sut = CreateSut();

        sut.SetSubject("lkjshdv", "loikjhv");

        var result = sut.Build();
        
        result.Body.Subject.Properties.Should().BeNull();
    }
    
    [Fact]
    public void Build_WhenCalledWithResourceWithNoProperties_ShouldNotSetResourceProperties()
    {
        var sut = CreateSut();

        sut.SetResource("lkjshdv", "loikjhv");

        var result = sut.Build();
        
        result.Body.Resource.Properties.Should().BeNull();
    }
    
    [Fact]
    public void Build_WhenCalledWithActionWithNoProperties_ShouldNotSetActionProperties()
    {
        var sut = CreateSut();

        sut.SetAction("lkjshdv");

        var result = sut.Build();
        
        result.Body.Action.Properties.Should().BeNull();
    }
    
    [Fact]
    public void Build_WhenCalledWithContextWithNoProperties_ShouldNotSetContext()
    {
        var sut = CreateSut();

        sut.SetContext();

        var result = sut.Build();
        
        result.Body.Context.Should().BeNull();
    }
    
    [Fact]
    public void SetCorrelationId_WhenCalled_ShouldReturnSelf()
    {
        string expectedCorrelationId = "ihjubvsdlfvchusdiufvbidusb";
        var sut = CreateSut();

        IAuthZenSingleRequestBuilder result = sut.SetCorrelationId(expectedCorrelationId);
        
        result.Should().BeSameAs(sut);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("    ")]
    [InlineData("")]
    public void SetCorrelationId_WhenCalledWithInvalidCorrelationId_ShouldThrowArgumentException(string invalidCorrelationId)
    {
        var sut = CreateSut();

        Action act = () => sut.SetCorrelationId(invalidCorrelationId);
        
        act.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void Build_WhenCalledWithCorrelationId_ShouldSetCorrelationId()
    {
        string expectedCorrelationId = "ihjubvsdlfvchusdiufvbidusb";
        var sut = CreateSut();

        sut.SetCorrelationId(expectedCorrelationId);

        var result = sut.Build();
        
        result.CorrelationId.Should().Be(expectedCorrelationId);
    }

    private AuthZenSingleRequestBuilder CreateSut()
    {
        return new AuthZenSingleRequestBuilder();
    }
}