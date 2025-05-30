using FluentAssertions;
using Rsk.AuthZen.Client.DTOs;
using Xunit;

namespace Rsk.AuthZen.Client.Test;

public class AuthZenBoxCarOptionsTests
{
    private AuthZenBoxcarOptions CreateSut()
    {
        return new AuthZenBoxcarOptions();
    }

    [Theory]
    [InlineData(BoxcarSemantics.ExecuteAll, "execute_all")]
    [InlineData(BoxcarSemantics.DenyOnFirstDeny, "deny_on_first_deny")]
    [InlineData(BoxcarSemantics.PermitOnFirstPermit, "permit_on_first_permit")]
    public void ToDto_WhenCalled_ShouldTranslateSemanticsCorrectly(BoxcarSemantics semantics, string expectedDtoValue)
    {
        var sut = CreateSut();
        sut.Semantics = semantics;
        
        AuthZenBoxcarOptionsDto dto = sut.ToDto();
        
        dto.Evaluation_semantics.Should().Be(expectedDtoValue);
    }
}