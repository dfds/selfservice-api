using SelfService.Domain.Models;

namespace SelfService.Tests.Domain.Models;

public class TestUserRole
{
    [Theory]
    [MemberData(nameof(ValidInput))]
    public void try_parse_returns_expected_result_on_valid_input(string input)
    {
        var result = UserRole.TryParse(input, out _);
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(ValidInput))]
    public void try_parse_returns_expected_instance_on_valid_input(string input)
    {
        var _ = UserRole.TryParse(input, out var result);
        Assert.Equal(UserRole.NormalUser, result);
    }

    [Theory]
    [MemberData(nameof(ValidCloudEngineerInput))]
    public void try_parse_returns_expected_result_on_valid_cloud_engineer_input(string input)
    {
        var result = UserRole.TryParse(input, out _);
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(ValidCloudEngineerInput))]
    public void try_parse_returns_expected_instance_on_valid_cloud_engineer_input(string input)
    {
        var _ = UserRole.TryParse(input, out var result);
        Assert.Equal(UserRole.CloudEngineer, result);
    }

    [Theory]
    [MemberData(nameof(InvalidInput))]
    public void try_parse_returns_expected_result_on_invalid_input(string? input)
    {
        var result = UserRole.TryParse(input!, out _);
        Assert.False(result);
    }

    public static IEnumerable<object[]> ValidInput =>
        new[] { "foo", "bar", "baz", "qux", }.Select(value => new[] { value });

    public static IEnumerable<object[]> ValidCloudEngineerInput =>
        new[] { "Cloud.Engineer", "Cloud Engineer", "Cloud_Engineer", "Cloud-Engineer", }.Select(
            value => new[] { value }
        );

    public static IEnumerable<object[]> InvalidInput =>
        new[] { null!, "", " ", "     " }.Select(value => new[] { value });
}
