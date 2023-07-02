using SharpDX.D3DCompiler;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace VolumeRenderer;

sealed class Shader<TVertexConstantBuffer> : IDisposable
    where TVertexConstantBuffer : struct
{
    private bool _disposed;
    private readonly VertexShader _vertexShader;
    private readonly PixelShader _pixelShader;
    private readonly InputLayout _inputLayout;

    private readonly Buffer _vertexConstantBuffer;

    public Shader(D3DDevice device, string vertexShaderPath, string pixelShaderPath)
    {
        using var vertexShaderByteCode = ShaderBytecode.CompileFromFile(vertexShaderPath, "main", "vs_5_0", ShaderFlags.EnableStrictness | ShaderFlags.OptimizationLevel3, EffectFlags.None);
        _vertexShader = new VertexShader(device, vertexShaderByteCode);

        using var pixelShaderByteCode = ShaderBytecode.CompileFromFile(pixelShaderPath, "main", "ps_5_0", ShaderFlags.EnableStrictness | ShaderFlags.OptimizationLevel3, EffectFlags.None);
        _pixelShader = new PixelShader(device, pixelShaderByteCode);

        _inputLayout = new InputLayout(
            device,
            ShaderSignature.GetInputSignature(vertexShaderByteCode),
            new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("COLOR", 0, Format.R32G32B32_Float, 12, 0),
            });

        _vertexConstantBuffer = new Buffer(device, Utilities.SizeOf<TVertexConstantBuffer>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
    }

    public void Use(DeviceContext context)
    {
        context.VertexShader.Set(_vertexShader);
        context.PixelShader.Set(_pixelShader);
        context.InputAssembler.InputLayout = _inputLayout;
    }

    public void SetVertexConstantBuffer(DeviceContext context, TVertexConstantBuffer buffer)
    {
        context.VertexShader.SetConstantBuffer(0, _vertexConstantBuffer);
        var dataBox = context.MapSubresource(_vertexConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
        Utilities.Write(dataBox.DataPointer, ref buffer);
        context.UnmapSubresource(_vertexConstantBuffer, 0);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            GC.SuppressFinalize(this);
            _vertexShader?.Dispose();
            _pixelShader?.Dispose();
            _inputLayout?.Dispose();
        }
    }

    ~Shader()
    {
        Dispose();
    }
}

public sealed class Shader<TVertexConstantBuffer, TPixelConstantBuffer> : IDisposable
    where TVertexConstantBuffer : struct
    where TPixelConstantBuffer : struct
{
    private bool _disposed;
    private readonly VertexShader _vertexShader;
    private readonly PixelShader _pixelShader;
    private readonly InputLayout _inputLayout;

    private readonly Buffer _vertexConstantBuffer;
    private readonly Buffer _pixelConstantBuffer;

    public Shader(D3DDevice device, string vertexShaderPath, string pixelShaderPath)
    {
        using var vertexShaderByteCode = ShaderBytecode.CompileFromFile(vertexShaderPath, "main", "vs_5_0", ShaderFlags.EnableStrictness | ShaderFlags.Debug, EffectFlags.None);
        _vertexShader = new VertexShader(device, vertexShaderByteCode);

        using var pixelShaderByteCode = ShaderBytecode.CompileFromFile(pixelShaderPath, "main", "ps_5_0", ShaderFlags.EnableStrictness | ShaderFlags.Debug, EffectFlags.None);
        _pixelShader = new PixelShader(device, pixelShaderByteCode);

        _inputLayout = new InputLayout(
            device,
            ShaderSignature.GetInputSignature(vertexShaderByteCode),
            new[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0)
            });

        _vertexConstantBuffer = new Buffer(device, Utilities.SizeOf<TVertexConstantBuffer>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
        _pixelConstantBuffer = new Buffer(device, Utilities.SizeOf<TPixelConstantBuffer>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
    }

    public void Use(DeviceContext context)
    {
        context.VertexShader.Set(_vertexShader);
        context.PixelShader.Set(_pixelShader);
        context.InputAssembler.InputLayout = _inputLayout;
    }

    public void SetVertexConstantBuffer(DeviceContext context, TVertexConstantBuffer buffer)
    {
        context.VertexShader.SetConstantBuffer(0, _vertexConstantBuffer);
        var dataBox = context.MapSubresource(_vertexConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
        Utilities.Write(dataBox.DataPointer, ref buffer);
        context.UnmapSubresource(_vertexConstantBuffer, 0);
    }

    public void SetPixelConstantBuffer(DeviceContext context, TPixelConstantBuffer buffer)
    {
        context.PixelShader.SetConstantBuffer(1, _pixelConstantBuffer);
        var dataBox = context.MapSubresource(_pixelConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
        Utilities.Write(dataBox.DataPointer, ref buffer);
        context.UnmapSubresource(_pixelConstantBuffer, 0);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            GC.SuppressFinalize(this);
            _vertexShader?.Dispose();
            _pixelShader?.Dispose();
            _inputLayout?.Dispose();
        }
    }

    ~Shader()
    {
        Dispose();
    }
}
