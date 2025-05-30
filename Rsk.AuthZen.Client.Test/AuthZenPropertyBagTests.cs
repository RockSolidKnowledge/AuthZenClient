using FluentAssertions;
using Xunit;

namespace Rsk.AuthZen.Client.Test;

public class AuthZenPropertyBagTests
{
    private AuthZenPropertyBag CreateSut()
    {
        return new AuthZenPropertyBag();
    }
    
    [Fact]
    public void Add_WhenCalledThenBuild_ShouldAddTheValueToTheReturnedDictionary()
    {
        var sut = CreateSut();
        var name1 = "dsfojov";
        var value1 = new object();
        var name2 = "sokfjgo0jd";
        var value2 = new object();
        
        sut.Add(name1, value1);
        sut.Add(name2, value2);
        
        var dictionary = sut.Build();

        dictionary.Should().HaveCount(2);
        dictionary.Should().Contain(kv => kv.Key == name1 && kv.Value == value1);
        dictionary.Should().Contain(kv => kv.Key == name2 && kv.Value == value2);
    }
    
    [Fact]
    public void Add_WhenCalledTwiceWithSameKey_ShouldOverwiteValue()
    {
        var sut = CreateSut();
        var name = "dsfojov";
        var value1 = new object();
        var value2 = new object();
        
        sut.Add(name, value1);
        sut.Add(name, value2);
        
        var dictionary = sut.Build();

        dictionary.Should().HaveCount(1);
        dictionary.Should().Contain(kv => kv.Key == name && kv.Value == value2);
    }
    
    [Fact]
    public void Add_WhenCalled_ShouldReturnSut()
    {
        var sut = CreateSut();
        
        var returnedValue = sut.Add("ijxshdfv", new object());
        
        returnedValue.Should().BeSameAs(sut);
    }
    
    [Fact]
    public void ctor_WhenCalled_ShouldSetIsEmptyToTrue()
    {
        var sut = CreateSut();
        
        sut.IsEmpty.Should().BeTrue();
    }
    
    [Fact]
    public void Add_WhenCalled_ShouldSetIsEmptyToFalse()
    {
        var sut = CreateSut();
        
        sut.Add("ijxshdfv", new object());
        
        sut.IsEmpty.Should().BeFalse();
    }
}