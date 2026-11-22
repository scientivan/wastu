using System.Globalization;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Wastu.Domain;

namespace Wastu.Reporting;

public sealed class PdfReportBuilder
{
    private const double MarginLeft = 36;
    private const double MarginRight = 36;
    private const double MarginTop = 36;
    private const double MarginBottom = 42;
    private const double PageWidth = 595.28;  // A4
    private const double PageHeight = 841.89; // A4
    private const double ContentWidth = PageWidth - MarginLeft - MarginRight; // 523.28
    private const double MaxY = PageHeight - MarginBottom;

    // Palette
    private static readonly XColor PrimaryColor = XColor.FromArgb(24, 30, 42);       // #181E2A
    private static readonly XColor SecondaryColor = XColor.FromArgb(51, 65, 85);     // #334155
    private static readonly XColor MutedColor = XColor.FromArgb(100, 116, 139);      // #64748B
    private static readonly XColor AccentGreen = XColor.FromArgb(16, 120, 90);       // Brand green
    private static readonly XColor BorderColor = XColor.FromArgb(226, 232, 240);     // #E2E8F0
    private static readonly XColor HeaderBg = XColor.FromArgb(241, 245, 249);        // #F1F5F9
    private static readonly XColor CardBg = XColor.FromArgb(248, 250, 252);          // #F8FAFC
    private static readonly XColor CocokBg = XColor.FromArgb(220, 252, 231);         // #DCFCE7
    private static readonly XColor CocokFg = XColor.FromArgb(22, 101, 52);           // #166534
    private static readonly XColor PertimbanganBg = XColor.FromArgb(254, 243, 199);  // #FEF3C7
    private static readonly XColor PertimbanganFg = XColor.FromArgb(180, 83, 9);     // #B45309
    private static readonly XColor TidakDisarankanBg = XColor.FromArgb(254, 226, 226);// #FEE2E2
    private static readonly XColor TidakDisarankanFg = XColor.FromArgb(185, 28, 28);  // #B91C1C

    private readonly XFont _fontTitle;
    private readonly XFont _fontSubtitle;
    private readonly XFont _fontSection;
    private readonly XFont _fontSubSection;
    private readonly XFont _fontBody;
    private readonly XFont _fontBodyBold;
    private readonly XFont _fontSmall;
    private readonly XFont _fontSmallBold;
    private readonly XFont _fontFooter;

    public PdfReportBuilder()
    {
        InterFontResolver.EnsureRegistered();

        _fontTitle = new XFont("Inter", 16, XFontStyleEx.Bold);
        _fontSubtitle = new XFont("Inter", 9.5, XFontStyleEx.Regular);
        _fontSection = new XFont("Inter", 12, XFontStyleEx.Bold);
        _fontSubSection = new XFont("Inter", 9.5, XFontStyleEx.Bold);
        _fontBody = new XFont("Inter", 8.5, XFontStyleEx.Regular);
        _fontBodyBold = new XFont("Inter", 8.5, XFontStyleEx.Bold);
        _fontSmall = new XFont("Inter", 7.5, XFontStyleEx.Regular);
        _fontSmallBold = new XFont("Inter", 7.5, XFontStyleEx.Bold);
        _fontFooter = new XFont("Inter", 7.5, XFontStyleEx.Regular);
    }

    public PdfDocument Build(SnapshotLaporan snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var document = new PdfDocument();
        document.Info.Title = $"Laporan Rekomendasi Wastu - {snapshot.NamaProyek}";
        document.Info.Author = "Wastu Platform";
        document.Info.Subject = "Rekomendasi Material dan Jadwal Sensitif Cuaca";
        document.Info.CreationDate = snapshot.DibuatPadaUtc.UtcDateTime;

        var page = document.AddPage();
        page.Size = PageSize.A4;
        var gfx = XGraphics.FromPdfPage(page);
        var currentY = MarginTop;

        // Header
        DrawHeader(gfx, snapshot, ref currentY);

        // 1. Profil Proyek & Komponen
        DrawProfilProyek(ref page, ref gfx, document, snapshot, ref currentY);

        // 2. Profil Iklim
        DrawProfilIklim(ref page, ref gfx, document, snapshot, ref currentY);

        // 3. Rekomendasi Material Terpilih
        DrawMaterialSection(ref page, ref gfx, document, snapshot, ref currentY);

        // 4. Simulasi Horizon
        DrawSimulasiSection(ref page, ref gfx, document, snapshot, ref currentY);

        // 5. Jadwal Terkunci
        if (snapshot.SertakanJadwal)
        {
            DrawJadwalSection(ref page, ref gfx, document, snapshot, ref currentY);
        }

        // 6. Transparansi & Metodologi
        DrawTransparansiSection(ref page, ref gfx, document, snapshot, ref currentY);

        // Dispose content gfx before footer pass
        gfx.Dispose();

        // Final pass: draw footers on all pages
        DrawFooters(document, snapshot);

        return document;
    }

    private void DrawHeader(XGraphics gfx, SnapshotLaporan snapshot, ref double currentY)
    {
        // Brand & Title
        gfx.DrawString("WASTU", _fontTitle, new XSolidBrush(AccentGreen), new XPoint(MarginLeft, currentY + 14));
        gfx.DrawString("Laporan Rekomendasi Material & Penjadwalan Berkelanjutan", _fontSubSection, new XSolidBrush(PrimaryColor), new XPoint(MarginLeft + 68, currentY + 12));

        var subText = $"Proyek: {snapshot.NamaProyek} ({snapshot.Kota})  •  Dibuat: {snapshot.DibuatPadaWib:dd MMMM yyyy HH:mm} WIB";
        gfx.DrawString(subText, _fontSubtitle, new XSolidBrush(MutedColor), new XPoint(MarginLeft, currentY + 28));

        currentY += 36;
        gfx.DrawLine(new XPen(BorderColor, 1), MarginLeft, currentY, MarginLeft + ContentWidth, currentY);
        currentY += 12;
    }

    private void DrawProfilProyek(ref PdfPage page, ref XGraphics gfx, PdfDocument doc, SnapshotLaporan snapshot, ref double currentY)
    {
        var coordinateLines = WrapText(gfx, snapshot.KoordinatDisclaimer, _fontSmall, ContentWidth / 2 - 20);
        var areaLines = WrapText(gfx, "Luas komponen dihitung untuk analisis kebutuhan emisi awal.", _fontSmall, ContentWidth / 2 - 20);
        double boxHeight = 44 + Math.Max(coordinateLines.Count, areaLines.Count) * 10;
        EnsureSpace(ref page, ref gfx, doc, boxHeight + 32, ref currentY);

        gfx.DrawString("1. Profil Proyek & Komponen Bangunan", _fontSection, new XSolidBrush(PrimaryColor), new XPoint(MarginLeft, currentY + 12));
        currentY += 18;

        var boxRect = new XRect(MarginLeft, currentY, ContentWidth, boxHeight);
        gfx.DrawRoundedRectangle(new XPen(BorderColor, 0.8), new XSolidBrush(CardBg), boxRect, new XSize(4, 4));

        var yText = currentY + 14;

        // Left col: Location
        gfx.DrawString("Lokasi & Koordinat:", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(MarginLeft + 10, yText));
        var latStr = snapshot.Latitude.ToString("F4", CultureInfo.InvariantCulture);
        var lonStr = snapshot.Longitude.ToString("F4", CultureInfo.InvariantCulture);
        gfx.DrawString($"{snapshot.Kota} ({latStr}, {lonStr})", _fontBody, new XSolidBrush(PrimaryColor), new XPoint(MarginLeft + 10, yText + 12));
        for (int i = 0; i < coordinateLines.Count; i++)
            gfx.DrawString(coordinateLines[i], _fontSmall, new XSolidBrush(MutedColor), new XPoint(MarginLeft + 10, yText + 24 + i * 10));

        // Right col: Komponen & Luas
        gfx.DrawString("Komponen Bangunan Aktif:", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(MarginLeft + ContentWidth / 2 + 10, yText));
        var compSummary = snapshot.Komponen.Count > 0
            ? string.Join(", ", snapshot.Komponen.Select(k => $"{k.Jenis}: {k.LuasM2:F1} m²"))
            : "Belum ada komponen terdaftar.";
        gfx.DrawString(compSummary, _fontBodyBold, new XSolidBrush(PrimaryColor), new XPoint(MarginLeft + ContentWidth / 2 + 10, yText + 13));
        for (int i = 0; i < areaLines.Count; i++)
            gfx.DrawString(areaLines[i], _fontSmall, new XSolidBrush(MutedColor), new XPoint(MarginLeft + ContentWidth / 2 + 10, yText + 24 + i * 10));

        currentY += boxHeight + 14;
    }

    private void DrawProfilIklim(ref PdfPage page, ref XGraphics gfx, PdfDocument doc, SnapshotLaporan snapshot, ref double currentY)
    {
        var iklim = snapshot.Iklim;
        var diambilStr = iklim.DiambilPada.HasValue
            ? iklim.DiambilPada.Value.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy HH:mm") + " WIB"
            : "Tidak tercatat";
        var offlineTag = iklim.IsOffline ? " [MODE LURING / OFFLINE SNAPSHOT]" : "";
        var provText = $"Sumber: {iklim.Provider}  •  Periode: {iklim.Periode}  •  Status: {iklim.Provenance}{offlineTag}  •  Diambil: {diambilStr}";
        var provenanceLines = WrapText(gfx, provText, _fontSmallBold, ContentWidth - 20);
        var disclaimerLines = WrapText(gfx, iklim.PesisirDisclaimer, _fontSmall, ContentWidth - 20);
        var warningLines = string.IsNullOrWhiteSpace(iklim.CatatanOffline)
            ? new List<string>() : WrapText(gfx, iklim.CatatanOffline, _fontSmall, ContentWidth - 20);
        double boxHeight = 56 + (provenanceLines.Count + disclaimerLines.Count + warningLines.Count) * 10;
        EnsureSpace(ref page, ref gfx, doc, boxHeight + 32, ref currentY);

        gfx.DrawString("2. Profil Iklim Wilayah", _fontSection, new XSolidBrush(PrimaryColor), new XPoint(MarginLeft, currentY + 12));
        currentY += 18;

        var boxRect = new XRect(MarginLeft, currentY, ContentWidth, boxHeight);
        gfx.DrawRoundedRectangle(new XPen(BorderColor, 0.8), new XSolidBrush(CardBg), boxRect, new XSize(4, 4));

        var yText = currentY + 14;

        // Metric pills: 4 columns
        var cellW = ContentWidth / 4;
        DrawMetricCell(gfx, MarginLeft + cellW * 0 + 8, yText, "Curah Hujan", $"{iklim.CurahHujanTahunanMm:F0} mm/thn");
        DrawMetricCell(gfx, MarginLeft + cellW * 1 + 8, yText, "Kelembapan (RH)", $"{iklim.KelembapanRataPct:F1}%");
        DrawMetricCell(gfx, MarginLeft + cellW * 2 + 8, yText, "Suhu Rata-rata", $"{iklim.SuhuRataC:F1} °C");
        DrawMetricCell(gfx, MarginLeft + cellW * 3 + 8, yText, "Jarak Pesisir", $"{iklim.JarakPesisirKm:F1} km ({iklim.TingkatKorosi})");

        // Disclaimers & Provenance
        var provY = yText + 32;
        foreach (var line in provenanceLines)
        {
            gfx.DrawString(line, _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(MarginLeft + 10, provY));
            provY += 10;
        }
        foreach (var line in disclaimerLines.Concat(warningLines))
        {
            gfx.DrawString(line, _fontSmall, new XSolidBrush(MutedColor), new XPoint(MarginLeft + 10, provY));
            provY += 10;
        }

        currentY += boxHeight + 14;
    }

    private void DrawMetricCell(XGraphics gfx, double x, double y, string label, string val)
    {
        gfx.DrawString(label, _fontSmall, new XSolidBrush(MutedColor), new XPoint(x, y));
        gfx.DrawString(val, _fontBodyBold, new XSolidBrush(PrimaryColor), new XPoint(x, y + 13));
    }

    private void DrawMaterialSection(ref PdfPage page, ref XGraphics gfx, PdfDocument doc, SnapshotLaporan snapshot, ref double currentY)
    {
        EnsureSpace(ref page, ref gfx, doc, 60, ref currentY);

        gfx.DrawString("3. Rekomendasi Material Pilihan Pengguna", _fontSection, new XSolidBrush(PrimaryColor), new XPoint(MarginLeft, currentY + 12));
        currentY += 18;

        if (snapshot.BahanTerpilih.Count == 0)
        {
            gfx.DrawString("Tidak ada material yang dipilih untuk diekspor.", _fontBody, new XSolidBrush(MutedColor), new XPoint(MarginLeft, currentY + 12));
            currentY += 24;
            return;
        }

        // Table header widths: Rank(26), Komponen(44), Material(110), Skor(80), Emisi(60), Alasan(203.28)
        var wRank = 26.0;
        var wKomp = 44.0;
        var wNama = 110.0;
        var wSkor = 80.0;
        var wEmisi = 60.0;
        var wAlasan = ContentWidth - (wRank + wKomp + wNama + wSkor + wEmisi); // ~203.28

        DrawMaterialTableHeader(gfx, currentY, wRank, wKomp, wNama, wSkor, wEmisi, wAlasan, false);
        currentY += 20;

        foreach (var mat in snapshot.BahanTerpilih)
        {
            var alasanLines = WrapText(gfx, mat.Alasan, _fontSmall, wAlasan - 10);
            if (alasanLines.Count == 0) alasanLines.Add("-");

            // Add source summary if present
            var sumberSummary = mat.SumberProperti.Count > 0
                ? "Sumber: " + string.Join("; ", mat.SumberProperti.Select(s => $"{s.Properti} ({s.Sumber})"))
                : string.Empty;
            var sumberLines = string.IsNullOrEmpty(sumberSummary)
                ? []
                : WrapText(gfx, sumberSummary, _fontSmall, wAlasan - 10);

            var totalLines = alasanLines.Count + (sumberLines.Count > 0 ? (sumberLines.Count + 1) : 0);
            var rowHeight = Math.Max(26.0, totalLines * 10.5 + 8);

            // Page break check
            if (currentY + rowHeight > MaxY)
            {
                gfx.Dispose();
                page = doc.AddPage();
                page.Size = PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                currentY = MarginTop;

                DrawMaterialTableHeader(gfx, currentY, wRank, wKomp, wNama, wSkor, wEmisi, wAlasan, true);
                currentY += 20;
            }

            // Draw row background / border
            var rowRect = new XRect(MarginLeft, currentY, ContentWidth, rowHeight);
            gfx.DrawRectangle(new XPen(BorderColor, 0.5), rowRect);

            // Column 1: Rank
            var rCenter = MarginLeft + wRank / 2;
            var rankStr = mat.Peringkat > 0 ? $"#{mat.Peringkat}" : "-";
            gfx.DrawString(rankStr, _fontSmallBold, new XSolidBrush(PrimaryColor), new XPoint(MarginLeft + 4, currentY + 14));

            // Column 2: Komponen
            var kompStr = mat.Jenis == JenisKomponen.Dinding ? "Dinding" : "Atap";
            gfx.DrawString(kompStr, _fontSmall, new XSolidBrush(SecondaryColor), new XPoint(MarginLeft + wRank + 4, currentY + 14));

            // Column 3: Material Name
            var nameX = MarginLeft + wRank + wKomp + 4;
            var nameLines = WrapText(gfx, mat.Nama, _fontBodyBold, wNama - 8);
            var ny = currentY + 12;
            foreach (var nl in nameLines)
            {
                gfx.DrawString(nl, _fontBodyBold, new XSolidBrush(PrimaryColor), new XPoint(nameX, ny));
                ny += 11;
            }

            // Column 4: Skor + Badge
            var skorX = MarginLeft + wRank + wKomp + wNama + 4;
            var skorStr = $"{mat.SkorKesesuaian:F1}/100";
            gfx.DrawString(skorStr, _fontBodyBold, new XSolidBrush(PrimaryColor), new XPoint(skorX, currentY + 12));

            // Badge for Label: Cocok, Perlu Pertimbangan, Tidak Disarankan
            DrawBadge(gfx, skorX, currentY + 16, mat.LabelKesesuaian);

            // Sub-scores
            var subText = $"T:{mat.SubSkorTermal:F0} L:{mat.SubSkorLembap:F0} K:{mat.SubSkorKorosi:F0} U:{mat.SubSkorUsia:F0}";
            gfx.DrawString(subText, _fontSmall, new XSolidBrush(MutedColor), new XPoint(skorX, currentY + 31));

            // Column 5: Emisi Awal (kgCO₂e)
            var emisiX = MarginLeft + wRank + wKomp + wNama + wSkor + 4;
            gfx.DrawString($"{mat.EmisiAwalKg:F1}", _fontBodyBold, new XSolidBrush(PrimaryColor), new XPoint(emisiX, currentY + 12));
            gfx.DrawString("kgCO₂e", _fontSmall, new XSolidBrush(MutedColor), new XPoint(emisiX, currentY + 23));

            // Column 6: Alasan & Sumber
            var alasanX = MarginLeft + wRank + wKomp + wNama + wSkor + wEmisi + 6;
            var ay = currentY + 11;
            foreach (var al in alasanLines)
            {
                gfx.DrawString(al, _fontSmall, new XSolidBrush(PrimaryColor), new XPoint(alasanX, ay));
                ay += 10.5;
            }
            if (sumberLines.Count > 0)
            {
                ay += 2;
                foreach (var sl in sumberLines)
                {
                    gfx.DrawString(sl, _fontSmall, new XSolidBrush(MutedColor), new XPoint(alasanX, ay));
                    ay += 10;
                }
            }

            currentY += rowHeight;
        }

        currentY += 14;
    }

    private void DrawMaterialTableHeader(XGraphics gfx, double y, double wRank, double wKomp, double wNama, double wSkor, double wEmisi, double wAlasan, bool isContinuation)
    {
        var hRect = new XRect(MarginLeft, y, ContentWidth, 18);
        gfx.DrawRectangle(new XPen(BorderColor, 0.8), new XSolidBrush(HeaderBg), hRect);

        var titleSuffix = isContinuation ? " (Lanjutan)" : "";
        var colX = MarginLeft;

        gfx.DrawString("Rank", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX + 3, y + 12));
        colX += wRank;

        gfx.DrawString("Komponen", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX + 3, y + 12));
        colX += wKomp;

        gfx.DrawString($"Material{titleSuffix}", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX + 3, y + 12));
        colX += wNama;

        gfx.DrawString("Skor Kesesuaian", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX + 3, y + 12));
        colX += wSkor;

        gfx.DrawString("kgCO₂e Awal", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX + 3, y + 12));
        colX += wEmisi;

        gfx.DrawString("Alasan Penilaian & Rincian", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX + 5, y + 12));
    }

    private void DrawBadge(XGraphics gfx, double x, double y, string label)
    {
        var (bg, fg) = label.ToLowerInvariant() switch
        {
            var l when l.Contains("cocok") => (CocokBg, CocokFg),
            var l when l.Contains("pertimbangan") => (PertimbanganBg, PertimbanganFg),
            _ => (TidakDisarankanBg, TidakDisarankanFg)
        };

        var textWidth = gfx.MeasureString(label, _fontSmallBold).Width;
        var badgeRect = new XRect(x, y - 4, textWidth + 8, 12);
        gfx.DrawRoundedRectangle(new XSolidBrush(bg), badgeRect, new XSize(3, 3));
        gfx.DrawString(label, _fontSmallBold, new XSolidBrush(fg), new XPoint(x + 4, y + 5));
    }

    private void DrawSimulasiSection(ref PdfPage page, ref XGraphics gfx, PdfDocument doc, SnapshotLaporan snapshot, ref double currentY)
    {
        EnsureSpace(ref page, ref gfx, doc, 75, ref currentY);

        gfx.DrawString("4. Simulasi Emisi Siklus Hidup Jangka Panjang (Horizon)", _fontSection, new XSolidBrush(PrimaryColor), new XPoint(MarginLeft, currentY + 12));
        currentY += 18;

        if (snapshot.SimulasiHorizon.Count == 0)
        {
            gfx.DrawString("Tidak ada simulasi horizon yang dipilih.", _fontBody, new XSolidBrush(MutedColor), new XPoint(MarginLeft, currentY + 12));
            currentY += 24;
            return;
        }

        // Table header: Material(125), Komp(45), Horizon(48), Usia(85), N(45), Kumulatif(95), Model/Ket(80.28)
        var wMat = 125.0;
        var wKomp = 45.0;
        var wHor = 48.0;
        var wUsia = 85.0;
        var wN = 45.0;
        var wKum = 95.0;
        var wKet = ContentWidth - (wMat + wKomp + wHor + wUsia + wN + wKum); // ~80.28

        DrawSimulasiTableHeader(gfx, currentY, wMat, wKomp, wHor, wUsia, wN, wKum, wKet);
        currentY += 18;

        foreach (var sim in snapshot.SimulasiHorizon)
        {
            var rowH = 22.0;
            if (currentY + rowH > MaxY)
            {
                gfx.Dispose();
                page = doc.AddPage();
                page.Size = PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                currentY = MarginTop;

                DrawSimulasiTableHeader(gfx, currentY, wMat, wKomp, wHor, wUsia, wN, wKum, wKet);
                currentY += 18;
            }

            var rRect = new XRect(MarginLeft, currentY, ContentWidth, rowH);
            gfx.DrawRectangle(new XPen(BorderColor, 0.5), rRect);

            var cx = MarginLeft + 4;
            gfx.DrawString(sim.BahanNama, _fontBodyBold, new XSolidBrush(PrimaryColor), new XPoint(cx, currentY + 14));
            cx += wMat;

            var kStr = sim.Jenis == JenisKomponen.Dinding ? "Dinding" : "Atap";
            gfx.DrawString(kStr, _fontSmall, new XSolidBrush(SecondaryColor), new XPoint(cx, currentY + 14));
            cx += wKomp;

            gfx.DrawString($"{sim.HorizonTahun} thn", _fontBodyBold, new XSolidBrush(PrimaryColor), new XPoint(cx, currentY + 14));
            cx += wHor;

            gfx.DrawString($"{sim.UsiaPakaiNominal:F0} / {sim.UsiaPakaiEfektif:F1} thn", _fontSmall, new XSolidBrush(PrimaryColor), new XPoint(cx, currentY + 14));
            cx += wUsia;

            gfx.DrawString($"{sim.JumlahPenggantian}x", _fontBodyBold, new XSolidBrush(PrimaryColor), new XPoint(cx, currentY + 14));
            cx += wN;

            gfx.DrawString($"{sim.EmisiKumulatifKg:F1} kgCO₂e", _fontBodyBold, new XSolidBrush(AccentGreen), new XPoint(cx, currentY + 14));
            cx += wKum;

            gfx.DrawString($"a={sim.FaktorPenuaanA:F1} ({sim.VersiModel})", _fontSmall, new XSolidBrush(MutedColor), new XPoint(cx, currentY + 14));

            currentY += rowH;
        }

        currentY += 6;
        var disclaimer = snapshot.SimulasiHorizon.FirstOrDefault()?.EstimasiDisclaimer
            ?? "Hasil simulasi emisi adalah proyeksi estimasi model matematis, bukan hasil pengukuran langsung lapangan.";
        gfx.DrawString($"Catatan Estimasi: {disclaimer}", _fontSmall, new XSolidBrush(MutedColor), new XPoint(MarginLeft, currentY + 8));
        currentY += 18;
    }

    private void DrawSimulasiTableHeader(XGraphics gfx, double y, double wMat, double wKomp, double wHor, double wUsia, double wN, double wKum, double wKet)
    {
        var hRect = new XRect(MarginLeft, y, ContentWidth, 18);
        gfx.DrawRectangle(new XPen(BorderColor, 0.8), new XSolidBrush(HeaderBg), hRect);

        var colX = MarginLeft + 4;
        gfx.DrawString("Material", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX, y + 12));
        colX += wMat;

        gfx.DrawString("Komponen", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX, y + 12));
        colX += wKomp;

        gfx.DrawString("Horizon", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX, y + 12));
        colX += wHor;

        gfx.DrawString("Nominal / Efektif", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX, y + 12));
        colX += wUsia;

        gfx.DrawString("Ganti", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX, y + 12));
        colX += wN;

        gfx.DrawString("Emisi Kumulatif", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX, y + 12));
        colX += wKum;

        gfx.DrawString("Faktor a & Versi", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX, y + 12));
    }

    private void DrawJadwalSection(ref PdfPage page, ref XGraphics gfx, PdfDocument doc, SnapshotLaporan snapshot, ref double currentY)
    {
        EnsureSpace(ref page, ref gfx, doc, 65, ref currentY);

        gfx.DrawString("5. Jadwal Pelaksanaan Pekerjaan Lapangan (Terkunci)", _fontSection, new XSolidBrush(PrimaryColor), new XPoint(MarginLeft, currentY + 12));
        currentY += 18;

        if (snapshot.JadwalTerkunci.Count == 0)
        {
            gfx.DrawString("Belum ada jadwal terkunci.", _fontBodyBold, new XSolidBrush(MutedColor), new XPoint(MarginLeft, currentY + 12));
            currentY += 24;
            return;
        }

        // Columns: Pekerjaan(120), Jenis(65), Tanggal Terkunci(85), Status Cuaca(85), Alasan & Konfirmasi(168.28)
        var wPek = 120.0;
        var wJen = 65.0;
        var wTgl = 85.0;
        var wSts = 85.0;
        var wKet = ContentWidth - (wPek + wJen + wTgl + wSts); // ~168.28

        var hRect = new XRect(MarginLeft, currentY, ContentWidth, 18);
        gfx.DrawRectangle(new XPen(BorderColor, 0.8), new XSolidBrush(HeaderBg), hRect);

        var colX = MarginLeft + 4;
        gfx.DrawString("Nama Pekerjaan", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX, currentY + 12));
        colX += wPek;
        gfx.DrawString("Jenis", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX, currentY + 12));
        colX += wJen;
        gfx.DrawString("Tanggal Terkunci", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX, currentY + 12));
        colX += wTgl;
        gfx.DrawString("Status Cuaca", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX, currentY + 12));
        colX += wSts;
        gfx.DrawString("Alasan & Konfirmasi Risiko", _fontSmallBold, new XSolidBrush(SecondaryColor), new XPoint(colX, currentY + 12));

        currentY += 18;

        foreach (var j in snapshot.JadwalTerkunci)
        {
            var alasanStr = j.AlasanCuaca.Count > 0 ? string.Join("; ", j.AlasanCuaca) : "Kondisi cuaca dalam batas ambang aman.";
            var konfStr = string.Empty;
            if (j.KonfirmasiRisiko) konfStr += " [Persetujuan Risiko Pengguna]";
            if (j.KonfirmasiDataLama) konfStr += " [Konfirmasi Data Lama]";
            if (!string.IsNullOrEmpty(j.Catatan)) konfStr += $" ({j.Catatan})";

            var fullKet = alasanStr + konfStr;
            var ketLines = WrapText(gfx, fullKet, _fontSmall, wKet - 8);
            var rowH = Math.Max(22.0, ketLines.Count * 10.5 + 8);

            if (currentY + rowH > MaxY)
            {
                gfx.Dispose();
                page = doc.AddPage();
                page.Size = PageSize.A4;
                gfx = XGraphics.FromPdfPage(page);
                currentY = MarginTop;
            }

            var rRect = new XRect(MarginLeft, currentY, ContentWidth, rowH);
            gfx.DrawRectangle(new XPen(BorderColor, 0.5), rRect);

            var cx = MarginLeft + 4;
            gfx.DrawString(j.Nama, _fontBodyBold, new XSolidBrush(PrimaryColor), new XPoint(cx, currentY + 13));
            cx += wPek;

            gfx.DrawString(j.Jenis.ToString(), _fontSmall, new XSolidBrush(SecondaryColor), new XPoint(cx, currentY + 13));
            cx += wJen;

            var tglStr = j.TanggalDikunci.HasValue ? j.TanggalDikunci.Value.ToString("dd MMMM yyyy") : "-";
            gfx.DrawString(tglStr, _fontSmallBold, new XSolidBrush(PrimaryColor), new XPoint(cx, currentY + 13));
            cx += wTgl;

            gfx.DrawString(j.StatusCuaca, _fontSmallBold, new XSolidBrush(PrimaryColor), new XPoint(cx, currentY + 13));
            cx += wSts;

            var ky = currentY + 11;
            foreach (var kl in ketLines)
            {
                gfx.DrawString(kl, _fontSmall, new XSolidBrush(PrimaryColor), new XPoint(cx, ky));
                ky += 10.5;
            }

            currentY += rowH;
        }

        currentY += 14;
    }

    private void DrawTransparansiSection(ref PdfPage page, ref XGraphics gfx, PdfDocument doc, SnapshotLaporan snapshot, ref double currentY)
    {
        EnsureSpace(ref page, ref gfx, doc, 110, ref currentY);

        gfx.DrawString("6. Catatan Transparansi & Metodologi", _fontSection, new XSolidBrush(PrimaryColor), new XPoint(MarginLeft, currentY + 12));
        currentY += 18;

        var b = snapshot.Bobot;
        var bobot = FormattableString.Invariant(
            $"Bobot kriteria: Termal {b.Termal * 100:0.#########}%, Lembap {b.Lembap * 100:0.#########}%, Korosi {b.Korosi * 100:0.#########}%, Usia {b.Usia * 100:0.#########}%.");
        foreach (var line in WrapText(gfx, bobot, _fontSmall, ContentWidth))
        {
            gfx.DrawString(line, _fontSmall, new XSolidBrush(MutedColor), new XPoint(MarginLeft, currentY + 10));
            currentY += 12;
        }
        currentY += 6;

        var boxH = 58.0;
        var bRect = new XRect(MarginLeft, currentY, ContentWidth, boxH);
        gfx.DrawRoundedRectangle(new XPen(BorderColor, 0.8), new XSolidBrush(CardBg), bRect, new XSize(4, 4));

        var y = currentY + 12;
        var t1 = "• " + snapshot.AtribusiSumber;
        foreach (var l in WrapText(gfx, t1, _fontSmall, ContentWidth - 16))
        {
            gfx.DrawString(l, _fontSmall, new XSolidBrush(MutedColor), new XPoint(MarginLeft + 8, y));
            y += 10;
        }

        var t2 = "• Fungsi penyesuaian usia pakai efektif adalah model estimasi degradasi tropis sederhana (PRD §11.3, R2). " +
                 "Bahan bersumber dari literatur internasional dan disesuaikan untuk konteks Indonesia.";
        foreach (var l in WrapText(gfx, t2, _fontSmall, ContentWidth - 16))
        {
            gfx.DrawString(l, _fontSmall, new XSolidBrush(MutedColor), new XPoint(MarginLeft + 8, y));
            y += 10;
        }

        currentY += boxH + 10;
    }

    private void DrawFooters(PdfDocument doc, SnapshotLaporan snapshot)
    {
        var totalPages = doc.PageCount;
        var shortHash = string.IsNullOrEmpty(snapshot.SnapshotHash)
            ? snapshot.HitungHash()[..12]
            : snapshot.SnapshotHash[..Math.Min(12, snapshot.SnapshotHash.Length)];

        var footerY = PageHeight - MarginBottom + 18;

        for (int i = 0; i < totalPages; i++)
        {
            var p = doc.Pages[i];
            using var g = XGraphics.FromPdfPage(p);

            // Divider line above footer
            g.DrawLine(new XPen(BorderColor, 0.6), MarginLeft, PageHeight - MarginBottom + 6, MarginLeft + ContentWidth, PageHeight - MarginBottom + 6);

            // Left: Platform, Model version, Snapshot hash (zero DB passwords/credentials)
            var idShort = snapshot.Id.ToString("N")[..8];
            var leftText = $"Wastu {snapshot.VersiModel}  •  Hash: {shortHash}  •  ID: {idShort}";
            g.DrawString(leftText, _fontFooter, new XSolidBrush(MutedColor), new XPoint(MarginLeft, footerY));

            // Right: Page number and UTC + WIB timestamps
            var rightText = $"Halaman {i + 1} dari {totalPages}  •  {snapshot.DibuatPadaWib:dd/MM/yyyy HH:mm} WIB";
            var rightWidth = g.MeasureString(rightText, _fontFooter).Width;
            g.DrawString(rightText, _fontFooter, new XSolidBrush(MutedColor), new XPoint(MarginLeft + ContentWidth - rightWidth, footerY));
        }
    }

    private static void EnsureSpace(ref PdfPage page, ref XGraphics gfx, PdfDocument doc, double neededHeight, ref double currentY)
    {
        if (currentY + neededHeight > MaxY)
        {
            gfx.Dispose();
            page = doc.AddPage();
            page.Size = PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            currentY = MarginTop;
        }
    }

    private static List<string> WrapText(XGraphics gfx, string text, XFont font, double maxWidth)
    {
        var lines = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return lines;

        var paragraphs = text.Replace("\r\n", "\n").Split('\n');
        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                lines.Add(string.Empty);
                continue;
            }

            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var currentLine = new System.Text.StringBuilder();

            foreach (var word in words)
            {
                var testLine = currentLine.Length == 0 ? word : currentLine + " " + word;
                var width = gfx.MeasureString(testLine, font).Width;

                if (width <= maxWidth)
                {
                    currentLine.Clear();
                    currentLine.Append(testLine);
                }
                else
                {
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }

                    // Word wider than maxWidth -> split characters
                    if (gfx.MeasureString(word, font).Width > maxWidth)
                    {
                        var partial = new System.Text.StringBuilder();
                        foreach (var ch in word)
                        {
                            var testCh = partial.ToString() + ch;
                            if (gfx.MeasureString(testCh, font).Width > maxWidth)
                            {
                                lines.Add(partial.ToString());
                                partial.Clear();
                            }
                            partial.Append(ch);
                        }
                        if (partial.Length > 0)
                            currentLine.Append(partial);
                    }
                    else
                    {
                        currentLine.Append(word);
                    }
                }
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
            }
        }
        return lines;
    }
}
