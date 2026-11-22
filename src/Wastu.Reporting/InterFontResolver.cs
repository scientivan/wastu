using PdfSharp.Fonts;

namespace Wastu.Reporting;

public sealed class InterFontResolver : IFontResolver
{
    private static readonly Lazy<byte[]> RegularFont = new(() => LoadFont("Wastu.Reporting.Fonts.Inter-400.ttf"));
    private static readonly Lazy<byte[]> BoldFont = new(() => LoadFont("Wastu.Reporting.Fonts.Inter-700.ttf"));

    private const string RegularFace = "Inter-Regular";
    private const string BoldFace = "Inter-Bold";

    private static bool _registered;
    private static readonly object Lock = new();

    public static void EnsureRegistered()
    {
        if (_registered) return;
        lock (Lock)
        {
            if (_registered) return;
            GlobalFontSettings.FontResolver = new InterFontResolver();
            _registered = true;
        }
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return isBold ? new FontResolverInfo(BoldFace) : new FontResolverInfo(RegularFace);
    }

    public byte[]? GetFont(string faceName)
    {
        return faceName switch
        {
            BoldFace => BoldFont.Value,
            _ => RegularFont.Value
        };
    }

    private static byte[] LoadFont(string resourceName)
    {
        var assembly = typeof(InterFontResolver).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' tidak ditemukan.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
