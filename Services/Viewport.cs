namespace Wastu.Services;

// Lebar area klien jendela, padanan viewport CSS untuk breakpoint dan clamp(..vw..).
public static class Viewport
{
    public static double Width { get; private set; } = 1440;

    public static event Action? Changed;

    public static void Update(double width)
    {
        if (Math.Abs(width - Width) < 0.5) return;
        Width = width;
        Changed?.Invoke();
    }

    // clamp(min, vw%, max)
    public static double Clamp(double min, double vw, double max) => Math.Clamp(Width * vw / 100, min, max);
}
