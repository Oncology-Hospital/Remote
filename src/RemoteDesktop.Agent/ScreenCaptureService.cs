using System.Drawing.Imaging;

namespace RemoteDesktop.Agent;

internal static class ScreenCaptureService
{
    public static ScreenFrame Capture(
        string machineId,
        ScreenStreamOptions settings,
        string requestedQuality)
    {
        var bounds = SystemInformation.VirtualScreen;

        using var full = new Bitmap(bounds.Width, bounds.Height);
        using (var graphics = Graphics.FromImage(full))
        {
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }

        using var output = ResizeIfNeeded(full, settings.MaxWidth);
        using var stream = new MemoryStream();
        var encoder = ImageCodecInfo.GetImageEncoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
        using var encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(
            Encoder.Quality,
            Math.Clamp(settings.JpegQuality, 1L, 100L));
        output.Save(stream, encoder, encoderParams);
        var imageBytes = stream.ToArray();

        return new ScreenFrame
        {
            MachineId = machineId,
            Base64Jpeg = Convert.ToBase64String(imageBytes),
            Width = bounds.Width,
            Height = bounds.Height,
            FrameWidth = output.Width,
            FrameHeight = output.Height,
            EncodedBytes = imageBytes.Length,
            RequestedQuality = requestedQuality,
            QualityLevel = settings.Mode,
            SentAtUtc = DateTime.UtcNow
        };
    }

    private static Bitmap ResizeIfNeeded(Bitmap source, int maxFrameWidth)
    {
        maxFrameWidth = Math.Max(320, maxFrameWidth);
        if (source.Width <= maxFrameWidth)
        {
            return new Bitmap(source);
        }

        var scale = maxFrameWidth / (double)source.Width;
        var width = maxFrameWidth;
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var resized = new Bitmap(width, height);

        using var graphics = Graphics.FromImage(resized);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
        graphics.DrawImage(source, 0, 0, width, height);
        return resized;
    }
}
