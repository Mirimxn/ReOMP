using System;

namespace OMP.LSWTSS;

partial class Overlay1
{
    sealed class DirectX11Quad : IDisposable
    {
        readonly SharpDX.Direct3D11.DeviceContext _deviceContext;

        readonly SharpDX.Mathematics.Interop.RawViewportF _renderTargetViewport;

        readonly SharpDX.Direct3D11.BlendState _blendState;

        readonly SharpDX.Direct3D11.RasterizerState _rasterizerState;

        readonly SharpDX.Direct3D11.Buffer _vertexBuffer;

        readonly int _vertexBufferStride;

        readonly SharpDX.D3DCompiler.CompilationResult _vertexShaderBytecodeCompilationResult;

        readonly SharpDX.Direct3D11.VertexShader _vertexShader;

        readonly SharpDX.D3DCompiler.CompilationResult _pixelShaderBytecodeCompilationResult;

        readonly SharpDX.Direct3D11.PixelShader _pixelShader;

        readonly SharpDX.Direct3D11.ShaderResourceView _shaderTextureView;

        readonly SharpDX.Direct3D11.InputLayout _inputLayout;

        bool _isDisposed;

        public DirectX11Quad(
            SharpDX.Direct3D11.Device device,
            SharpDX.Direct3D11.Texture2D texture,
            SharpDX.Direct3D11.Texture2DDescription textureDesc,
            SharpDX.Mathematics.Interop.RawViewportF renderTargetViewport
        )
        {
            _deviceContext = device.ImmediateContext;

            _renderTargetViewport = renderTargetViewport;

            var blendStateDesc = new SharpDX.Direct3D11.BlendStateDescription
            {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = false,
            };

            blendStateDesc.RenderTarget[0] = new SharpDX.Direct3D11.RenderTargetBlendDescription
            {
                IsBlendEnabled = true,
                SourceBlend = SharpDX.Direct3D11.BlendOption.SourceAlpha,
                DestinationBlend = SharpDX.Direct3D11.BlendOption.InverseSourceAlpha,
                BlendOperation = SharpDX.Direct3D11.BlendOperation.Add,
                SourceAlphaBlend = SharpDX.Direct3D11.BlendOption.SourceAlpha,
                DestinationAlphaBlend = SharpDX.Direct3D11.BlendOption.DestinationAlpha,
                AlphaBlendOperation = SharpDX.Direct3D11.BlendOperation.Add,
                RenderTargetWriteMask = SharpDX.Direct3D11.ColorWriteMaskFlags.All,
            };

            _blendState = new SharpDX.Direct3D11.BlendState(device, blendStateDesc);

            var rasterizerStateDesc = new SharpDX.Direct3D11.RasterizerStateDescription
            {
                CullMode = SharpDX.Direct3D11.CullMode.None,
                FillMode = SharpDX.Direct3D11.FillMode.Solid,
                IsFrontCounterClockwise = true,
                DepthBias = 0,
                DepthBiasClamp = 0.0f,
                SlopeScaledDepthBias = 0.0f,
                IsDepthClipEnabled = true,
                IsScissorEnabled = false,
                IsMultisampleEnabled = false,
                IsAntialiasedLineEnabled = true,
            };

            _rasterizerState = new SharpDX.Direct3D11.RasterizerState(device, rasterizerStateDesc);

            var vertexBufferData = new float[]
            {
                // x, y, z, u, v
                -1.0f, 1.0f, 0.0f, 0.0f, 0.0f,
                -1.0f, -1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, -1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, -1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f, 0.0f,
                -1.0f, 1.0f, 0.0f, 0.0f, 0.0f,
            };

            var vertexBufferDesc = new SharpDX.Direct3D11.BufferDescription
            {
                SizeInBytes = vertexBufferData.Length * sizeof(float),
                Usage = SharpDX.Direct3D11.ResourceUsage.Default,
                BindFlags = SharpDX.Direct3D11.BindFlags.VertexBuffer,
                CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
                OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
                StructureByteStride = 0,
            };

            _vertexBuffer = SharpDX.Direct3D11.Buffer.Create(
                device,
                vertexBufferData,
                vertexBufferDesc
            );

            _vertexBufferStride = sizeof(float) * 5;

            string shaderCommonSrc = @"
                struct VShaderInput
                {
                    float4 position : POSITION;
                    float2 main_texture_coord : TEXCOORD;
                };

                struct PShaderInput
                {
                    float4 position : SV_POSITION;
                    float2 main_texture_coord : TEXCOORD0;
                };
            ";

            _vertexShaderBytecodeCompilationResult = SharpDX.D3DCompiler.ShaderBytecode.Compile(
                shaderCommonSrc
                +
                @"
                    PShaderInput VShader(VShaderInput v_shader_input)
                    {
                        PShaderInput p_shader_input = (PShaderInput)0;

                        p_shader_input.position = v_shader_input.position;
                        p_shader_input.main_texture_coord = v_shader_input.main_texture_coord;

                        return p_shader_input;
                    }
                ",
                "VShader",
                "vs_5_0"
            );

            if (_vertexShaderBytecodeCompilationResult.HasErrors)
            {
                throw new InvalidOperationException();
            }

            _vertexShader = new SharpDX.Direct3D11.VertexShader(
                device,
                _vertexShaderBytecodeCompilationResult.Bytecode
            );

            _pixelShaderBytecodeCompilationResult = SharpDX.D3DCompiler.ShaderBytecode.Compile(
                shaderCommonSrc
                +
                @"
                    Texture2D main_texture;
                    SamplerState main_texture_sampler
                    {
                        Filter = MIN_MAG_MIP_LINEAR;
                        AddressU = Wrap;
                        AddressV = Wrap;
                    };


                    float4 PShader(PShaderInput p_shader_input) : SV_Target
                    {
                        return main_texture.Sample(main_texture_sampler, p_shader_input.main_texture_coord).rgba;
                    }
                ",
                "PShader",
                "ps_5_0"
            );

            if (_pixelShaderBytecodeCompilationResult.HasErrors)
            {
                throw new InvalidOperationException();
            }

            _pixelShader = new SharpDX.Direct3D11.PixelShader(
                device,
                _pixelShaderBytecodeCompilationResult.Bytecode
            );

            var shaderTextureViewDesc = new SharpDX.Direct3D11.ShaderResourceViewDescription
            {
                Format = textureDesc.Format,
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                Texture2D = new SharpDX.Direct3D11.ShaderResourceViewDescription.Texture2DResource
                {
                    MipLevels = textureDesc.MipLevels,
                    MostDetailedMip = 0,
                },
            };

            _shaderTextureView = new SharpDX.Direct3D11.ShaderResourceView(
                device,
                texture,
                shaderTextureViewDesc
            );

            var inputElementsDesc = new SharpDX.Direct3D11.InputElement[]
            {
                new() {
                    SemanticName = "POSITION",
                    SemanticIndex = 0,
                    Format = SharpDX.DXGI.Format.R32G32B32_Float,
                    Slot = 0,
                    AlignedByteOffset = 0,
                    Classification = SharpDX.Direct3D11.InputClassification.PerVertexData,
                    InstanceDataStepRate = 0,
                },
                new() {
                    SemanticName = "TEXCOORD",
                    SemanticIndex = 0,
                    Format = SharpDX.DXGI.Format.R32G32_Float,
                    Slot = 0,
                    AlignedByteOffset = 12,
                    Classification = SharpDX.Direct3D11.InputClassification.PerVertexData,
                    InstanceDataStepRate = 0,
                },
            };

            _inputLayout = new SharpDX.Direct3D11.InputLayout(
                device,
                _vertexShaderBytecodeCompilationResult.Bytecode,
                inputElementsDesc
            );
        }

        public void Draw(SharpDX.Direct3D11.RenderTargetView renderTargetView)
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException();
            }

            using var retainingStateBlock = new DirectX11StateBlock(_deviceContext);

            _deviceContext.OutputMerger.SetRenderTargets(null, renderTargetView);

            _deviceContext.Rasterizer.SetViewport(_renderTargetViewport);

            _deviceContext.OutputMerger.SetBlendState(_blendState, null, 0xFFFFFFFF);

            _deviceContext.Rasterizer.State = _rasterizerState;

            _deviceContext.InputAssembler.SetVertexBuffers(
                0,
                new SharpDX.Direct3D11.VertexBufferBinding(
                    _vertexBuffer,
                    _vertexBufferStride,
                    0
                )
            );

            _deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;

            _deviceContext.GeometryShader.SetShader(null, null, 0);

            _deviceContext.VertexShader.SetShader(_vertexShader, null, 0);

            _deviceContext.PixelShader.SetShader(_pixelShader, null, 0);

            _deviceContext.PixelShader.SetShaderResource(0, _shaderTextureView);

            _deviceContext.InputAssembler.InputLayout = _inputLayout;

            _deviceContext.Draw(6, 0);

            _deviceContext.Flush();

            retainingStateBlock.Apply();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _inputLayout.Dispose();
                _shaderTextureView.Dispose();
                _pixelShader.Dispose();
                _vertexShader.Dispose();
                _vertexBuffer.Dispose();
                _rasterizerState.Dispose();
                _blendState.Dispose();
                _deviceContext.Dispose();

                _isDisposed = true;
            }
        }
    }
}