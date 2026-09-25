using QuestPDF;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace FixFlow.Api.Common.Pdf;

public static class PdfGeneration
{
    public const string FontFamily = "Noto Sans";

    private const string FontResourcePrefix = "FixFlow.Api.Common.Pdf.Fonts.";

    public static void Configure()
    {
        Settings.License = LicenseType.Community;
        Settings.UseSystemFonts = false;

        var assembly = typeof(PdfGeneration).Assembly;
        foreach (var resourceName in assembly.GetManifestResourceNames().Where(name => name.StartsWith(FontResourcePrefix, StringComparison.Ordinal)))
        {
            FontManager.RegisterFontFromEmbeddedResource(assembly, resourceName);
        }
    }
}
