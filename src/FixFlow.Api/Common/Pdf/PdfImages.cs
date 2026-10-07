using QuestPDF.Drawing.Exceptions;
using QuestPDF.Infrastructure;

namespace FixFlow.Api.Common.Pdf;

public static class PdfImages
{
    public static bool CanEmbed(byte[] content)
    {
        try
        {
            using var image = Image.FromBinaryData(content);
            return true;
        }
        catch (DocumentComposeException)
        {
            return false;
        }
    }
}
