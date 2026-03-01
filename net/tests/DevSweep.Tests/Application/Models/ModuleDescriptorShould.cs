using DevSweep.Application.Models;
using DevSweep.Domain.Enums;

namespace DevSweep.Tests.Application.Models;

public class ModuleDescriptorShould
{
    [Fact]
    public void SucceedWithValidParameters()
    {
        var result = ModuleDescriptor.Create(CleanupModuleName.Docker, "Cleans Docker caches", false);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FailWhenDescriptionIsMissingOrEmpty(string description)
    {
        var result = ModuleDescriptor.Create(CleanupModuleName.Docker, description, false);

        result.IsFailure.Should().BeTrue();
        result.Error.IsValidationError().Should().BeTrue();
    }

    [Fact]
    public void ReportDestructiveWhenMarkedAsDestructive()
    {
        var descriptorResult = ModuleDescriptor.Create(CleanupModuleName.Docker, "Cleans Docker caches", true);
        var descriptor = descriptorResult.Value;

        descriptorResult.IsSuccess.Should().BeTrue();
        descriptor.IsDestructive().Should().BeTrue();
    }

    [Fact]
    public void ReportNonDestructiveWhenNotMarkedAsDestructive()
    {
        var descriptorResult = ModuleDescriptor.Create(CleanupModuleName.Homebrew, "Cleans Homebrew caches", false);
        var descriptor = descriptorResult.Value;

        descriptorResult.IsSuccess.Should().BeTrue();
        descriptor.IsDestructive().Should().BeFalse();
    }
}
