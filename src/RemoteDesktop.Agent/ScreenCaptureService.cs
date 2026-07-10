using System.Drawing.Imaging;

namespace RemoteDesktop.Agent;

internal static class ScreenCaptureService
{
    private const int MaxFrameWidth = 1280;
    private const long JpegQuality = 45L;

    public static ScreenFrame Capture(string machineId)
    {
        var bounds = SystemInformation.VirtualScreen;

        using var full = new Bitmap(bounds.Width, bounds.Height);
        using (var graphics = Graphics.FromImage(full))
        {
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }

        using var output = ResizeIfNeeded(full);
        using var stream = new MemoryStream();
        var encoder = ImageCodecInfo.GetImageEncoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
        using var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, JpegQuality);
        output.Save(stream, encoder, encoderParams);

        return new ScreenFrame
        {
            MachineId = machineId,
            Base64Jpeg = Convert.ToBase64String(stream.ToArray()),
            Width = bounds.Width,
            Height = bounds.Height,
            SentAtUtc = DateTime.UtcNow
        };
    }

    private static Bitmap ResizeIfNeeded(Bitmap source)
    {
        if (source.Width <= MaxFrameWidth)
        {
            return new Bitmap(source);
        }

        var scale = MaxFrameWidth / (double)source.Width;
        var width = MaxFrameWidth;
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var resized = new Bitmap(width, height);

        using var graphics = Graphics.FromImage(resized);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
        graphics.DrawImage(source, 0, 0, width, height);
        return resized;
    }
}
