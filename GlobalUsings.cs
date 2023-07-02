global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using Avalonia;
global using Avalonia.Rendering.Composition;
global using SharpDX;
global using SharpDX.Direct3D11;
global using SharpDX.DXGI;

global using DxgiFactory = SharpDX.DXGI.Factory1;
global using D3DDevice = SharpDX.Direct3D11.Device;
global using FeatureLevel = SharpDX.Direct3D.FeatureLevel;
global using Vector3 = SharpDX.Vector3;
global using Matrix4x4 = SharpDX.Matrix;
global using Vector2 = SharpDX.Vector2;

namespace VolumeRenderer;
