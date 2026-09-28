using FixFlow.Api.Common.Concurrency;

namespace FixFlow.Api.UnitTests.Common.Concurrency;

public sealed class EntityTagTests
{
    [Fact]
    public void Should_Format_Version_As_Quoted_Strong_Tag_When_Formatting()
    {
        EntityTag.Format(1234).ShouldBe("\"1234\"");
    }

    [Theory]
    [InlineData("\"42\"")]
    [InlineData("*")]
    [InlineData("\"7\", \"42\"")]
    [InlineData(" \"42\" ")]
    public void Should_Match_When_If_Match_Contains_Current_Tag_Or_Wildcard(string ifMatch)
    {
        EntityTag.Matches(ifMatch, 42).ShouldBeTrue();
    }

    [Theory]
    [InlineData("\"41\"")]
    [InlineData("W/\"42\"")]
    [InlineData("42")]
    [InlineData("")]
    public void Should_Not_Match_When_If_Match_Has_Other_Weak_Or_Unquoted_Tag(string ifMatch)
    {
        EntityTag.Matches(ifMatch, 42).ShouldBeFalse();
    }
}
