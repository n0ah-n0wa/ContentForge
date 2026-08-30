namespace ContentForge.UnitTests.Domain;

using ContentForge.Domain.Common;
using FluentAssertions;

public sealed class SlugTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  Mixed_Case Value ", "mixed-case-value")]
    [InlineData("Multiple---Hyphens", "multiple-hyphens")]
    public void Create_NormalizesValue(string input, string expected)
    {
        var slug = Slug.Create(input);

        slug.Value.Should().Be(expected);
    }

    [Fact]
    public void Create_WhenEmptyAfterNormalization_Throws()
    {
        var action = () => Slug.Create("!!!");

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void TryCreate_ReturnsFalseForInvalidInput()
    {
        var success = Slug.TryCreate(string.Empty, out var slug);

        success.Should().BeFalse();
        slug.Should().BeNull();
    }
}

public sealed class FieldNameTests
{
    [Fact]
    public void Create_AcceptsValidName()
    {
        var fieldName = FieldName.Create("title");

        fieldName.Value.Should().Be("title");
    }

    [Theory]
    [InlineData("1invalid")]
    [InlineData("has space")]
    public void Create_RejectsInvalidName(string value)
    {
        var action = () => FieldName.Create(value);

        action.Should().Throw<DomainValidationException>();
    }
}

public sealed class ConcurrencyTokenTests
{
    [Fact]
    public void Next_IncrementsValue()
    {
        var token = ConcurrencyToken.Initial;

        token.Next().Value.Should().Be(2);
    }

    [Fact]
    public void Previous_ReturnsPriorValue()
    {
        var token = ConcurrencyToken.Initial.Next();

        token.Previous().Should().Be(ConcurrencyToken.Initial);
    }

    [Fact]
    public void Create_RejectsZero()
    {
        var action = () => new ConcurrencyToken(0);

        action.Should().Throw<DomainValidationException>();
    }
}

public sealed class IdentifierTests
{
    [Fact]
    public void From_RejectsEmptyGuid()
    {
        var action = () => ContentEntryId.From(Guid.Empty);

        action.Should().Throw<DomainValidationException>();
    }
}
