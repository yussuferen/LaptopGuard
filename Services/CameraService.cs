using OpenCvSharp;

namespace LaptopGuard.Services;

public static class CameraService
{
    private static readonly VideoCaptureAPIs[] Backends =
    [
        VideoCaptureAPIs.MSMF,
        VideoCaptureAPIs.DSHOW,
        VideoCaptureAPIs.ANY
    ];

    public static byte[]? CaptureFrame(int cameraIndex = 0, int warmupFrames = 8)
    {
        foreach (var backend in Backends)
        {
            try
            {
                byte[]? result = TryCaptureWithBackend(cameraIndex, backend, warmupFrames);
                if (result != null) return result;
            }
            catch
            {
            }
        }

        return null;
    }

    private static byte[]? TryCaptureWithBackend(int cameraIndex, VideoCaptureAPIs backend, int warmupFrames)
    {
        using var capture = new VideoCapture(cameraIndex, backend);

        if (!capture.IsOpened())
        {
            return null;
        }

        capture.Set(VideoCaptureProperties.FrameWidth, 1280);
        capture.Set(VideoCaptureProperties.FrameHeight, 720);

        using var frame = new Mat();

        for (int i = 0; i < warmupFrames; i++)
        {
            capture.Read(frame);
        }

        capture.Read(frame);

        if (frame.Empty())
        {
            return null;
        }

        bool encoded = Cv2.ImEncode(".jpg", frame, out byte[] buffer);

        return encoded && buffer.Length > 0 ? buffer : null;
    }

    public static MemoryStream? CaptureFrameAsStream(int cameraIndex = 0)
    {
        byte[]? data = CaptureFrame(cameraIndex);

        if (data == null) return null;

        return new MemoryStream(data);
    }
}
