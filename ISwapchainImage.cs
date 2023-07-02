namespace VolumeRenderer;

interface ISwapchainImage : IAsyncDisposable
{
    PixelSize Size { get; }
    Task? LastPresent { get; }
    void BeginDraw();
    void Present();
}
