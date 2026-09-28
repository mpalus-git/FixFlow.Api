using FixFlow.Api.Common.Errors;

namespace FixFlow.Api.UnitTests.Common.Errors;

public sealed class ValidationErrorKeyTests
{
    [Theory]
    [InlineData("PageSize", "pageSize")]
    [InlineData("Address.PostalCode", "address.postalCode")]
    [InlineData("Parts[0].PartId", "parts[0].partId")]
    [InlineData("PhotoUrls[1]", "photoUrls[1]")]
    [InlineData("", "")]
    public void Should_Convert_Each_Segment_To_Camel_Case_When_Property_Path_Is_Converted(string propertyPath, string expectedKey)
    {
        ValidationErrorKey.FromPropertyPath(propertyPath).ShouldBe(expectedKey);
    }
}
