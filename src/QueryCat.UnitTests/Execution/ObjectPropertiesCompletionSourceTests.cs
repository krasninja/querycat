﻿using Xunit;
using QueryCat.Backend;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;

namespace QueryCat.UnitTests.Execution;

/// <summary>
/// Tests for <see cref="ObjectPropertiesCompletionSource" />.
/// </summary>
public sealed class ObjectPropertiesCompletionSourceTests : IDisposable
{
    private readonly IExecutionThread _executionThread;
    private readonly User _user = new()
    {
        Name = "Goblin",
        Age = 63,
        Addresses =
        [
            new()
            {
                City = "Saint-Petersburg",
                Street = "Rubenshteina",
            },
            new()
            {
                City = "Moscow",
                Street = "Kremlin",
            }
        ]
    };

    public ObjectPropertiesCompletionSourceTests()
    {
        _executionThread = new ExecutionThreadBootstrapper()
            .WithCompletionSource(new ObjectPropertiesCompletionSource())
            .Create();
    }

    private class User
    {
        public string Name { get; set; } = string.Empty;

        public int? Age { get; set; }

        public List<Address> Addresses { get; set; } = new();
    }

    private class Address
    {
        public string Street { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;
    }

    [Theory]
    [InlineData("user.", "Name")]
    [InlineData("user.a", "Age")]
    [InlineData("user.Addresses[0].", "Street")]
    [InlineData("user.Addresses[0].Cit", "City")]
    [InlineData("user.Addresses[2].Cit", "-")]
    [InlineData("user.Addresses[0].Q", "-")]
    public void GetCompletions_PartVariableName_ReturnsExpectedCompletions(string query, string expected)
    {
        // Arrange.
        _executionThread.TopScope.Variables["user"] = VariantValue.CreateFromObject(_user);

        // Act.
        var firstCompletion = _executionThread.GetCompletions(query).FirstOrDefault(CompletionResult.Empty);

        // Assert.
        Assert.Equal(expected, firstCompletion.Completion.Label);
    }

    [Theory]
    [InlineData("user.", "user.Name")]
    [InlineData("user.a", "user.Age")]
    [InlineData("'street' || user.Addresses[0].", "'street' || user.Addresses[0].Street")]
    [InlineData("user.Addresses[0].Cit", "user.Addresses[0].City")]
    public void ApplyCompletion_PartVariableName_ReturnsExpectedCompletions(string query, string expected)
    {
        // Arrange.
        _executionThread.TopScope.Variables["user"] = VariantValue.CreateFromObject(_user);

        // Act.
        var firstCompletion = _executionThread.GetCompletions(query).FirstOrDefault(CompletionResult.Empty);
        var replacedText = firstCompletion.Apply(query);

        // Assert.
        Assert.Equal(expected, replacedText);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _executionThread.Dispose();
    }
}
