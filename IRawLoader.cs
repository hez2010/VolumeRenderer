namespace VolumeRenderer;

public interface IRawLoader : IDisposable
{
    public ShaderResourceView RawTextureView { get; }
    public SamplerState SamplerState { get; }
    public ICollection<double> HistogramX { get; }
    public ICollection<double> HistogramY { get; }
}
