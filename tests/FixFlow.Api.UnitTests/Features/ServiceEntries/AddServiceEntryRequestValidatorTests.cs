using FixFlow.Api.Features.ServiceEntries.AddServiceEntry;

namespace FixFlow.Api.UnitTests.Features.ServiceEntries;

public sealed class AddServiceEntryRequestValidatorTests
{
    private static readonly DateTimeOffset WorkStartedAt = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly AddServiceEntryRequestValidator _validator = new();

    [Fact]
    public void Should_Accept_Work_Entry_When_Work_Time_Is_Given_Without_Location_And_Parts()
    {
        var result = _validator.Validate(WorkEntry() with { Latitude = null, Longitude = null, Parts = null });

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Accept_Work_Entry_When_Location_Photos_And_Parts_Are_Given()
    {
        var result = _validator.Validate(WorkEntry());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Accept_Work_Entry_When_Client_Identifier_Is_Given()
    {
        var result = _validator.Validate(WorkEntry() with { Id = Guid.CreateVersion7() });

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Entry_When_Client_Identifier_Is_Empty()
    {
        var result = _validator.Validate(WorkEntry() with { Id = Guid.Empty });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(AddServiceEntryRequest.Id));
    }

    [Fact]
    public void Should_Reject_Work_Entry_When_Work_Time_Is_Missing()
    {
        var result = _validator.Validate(WorkEntry() with { WorkStartedAt = null, WorkFinishedAt = null });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(AddServiceEntryRequest.WorkStartedAt));
        result.Errors.ShouldContain(error => error.PropertyName == nameof(AddServiceEntryRequest.WorkFinishedAt));
    }

    [Fact]
    public void Should_Reject_Work_Entry_When_Only_Latitude_Is_Given()
    {
        var result = _validator.Validate(WorkEntry() with { Longitude = null });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(AddServiceEntryRequest.Longitude));
    }

    [Theory]
    [InlineData(90.1, 21.0)]
    [InlineData(52.2, -180.1)]
    public void Should_Reject_Work_Entry_When_Location_Is_Out_Of_Range(double latitude, double longitude)
    {
        var result = _validator.Validate(WorkEntry() with { Latitude = latitude, Longitude = longitude });

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("photos/1.jpg")]
    [InlineData("ftp://photos.test/1.jpg")]
    public void Should_Reject_Entry_When_Photo_Url_Is_Not_Absolute_Http_Address(string photoUrl)
    {
        var result = _validator.Validate(WorkEntry() with { PhotoUrls = [photoUrl] });

        result.Errors.ShouldContain(error => error.PropertyName.StartsWith(nameof(AddServiceEntryRequest.PhotoUrls), StringComparison.Ordinal));
    }

    [Fact]
    public void Should_Reject_Entry_When_More_Than_Ten_Photos_Are_Attached()
    {
        var photoUrls = Enumerable.Range(1, AddServiceEntryRequestValidator.MaxPhotoCount + 1).Select(number => $"https://photos.test/{number}.jpg").ToList();

        var result = _validator.Validate(WorkEntry() with { PhotoUrls = photoUrls });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(AddServiceEntryRequest.PhotoUrls));
    }

    [Fact]
    public void Should_Reject_Entry_When_Part_Is_Listed_Twice()
    {
        var partId = Guid.CreateVersion7();

        var result = _validator.Validate(WorkEntry() with { Parts = [new(partId, 1), new(partId, 2)] });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(AddServiceEntryRequest.Parts));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(AddServiceEntryRequestValidator.MaxPartQuantity + 1)]
    public void Should_Reject_Entry_When_Part_Quantity_Is_Out_Of_Range(int quantity)
    {
        var result = _validator.Validate(WorkEntry() with { Parts = [new(Guid.CreateVersion7(), quantity)] });

        result.Errors.ShouldContain(error => error.PropertyName.EndsWith(nameof(ServiceEntryPartRequest.Quantity), StringComparison.Ordinal));
    }

    [Fact]
    public void Should_Accept_Correction_When_It_Returns_Parts_Without_Work_Time_And_Location()
    {
        var result = _validator.Validate(Correction());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Reject_Correction_When_No_Part_Is_Returned()
    {
        var result = _validator.Validate(Correction() with { Parts = [] });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(AddServiceEntryRequest.Parts));
    }

    [Fact]
    public void Should_Reject_Correction_When_Work_Time_Or_Location_Is_Given()
    {
        var result = _validator.Validate(Correction() with { WorkStartedAt = WorkStartedAt, Latitude = 52.2 });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(AddServiceEntryRequest.WorkStartedAt));
        result.Errors.ShouldContain(error => error.PropertyName == nameof(AddServiceEntryRequest.Latitude));
    }

    [Fact]
    public void Should_Reject_Entry_When_Note_Is_Empty()
    {
        var result = _validator.Validate(WorkEntry() with { Note = " " });

        result.Errors.ShouldContain(error => error.PropertyName == nameof(AddServiceEntryRequest.Note));
    }

    private static AddServiceEntryRequest WorkEntry() => new(
        "Replaced filters",
        PhotoUrls: ["https://photos.test/1.jpg"],
        WorkStartedAt: WorkStartedAt,
        WorkFinishedAt: WorkStartedAt.AddHours(1),
        Latitude: 52.2297,
        Longitude: 21.0122,
        Parts: [new(Guid.CreateVersion7(), 2)]);

    private static AddServiceEntryRequest Correction() =>
        new("Two filters were not used", IsCorrection: true, Parts: [new(Guid.CreateVersion7(), 2)]);
}
