using System;

namespace OMP.LSWTSS;

partial class Overlay1
{
    sealed class DirectX11StateBlock : IDisposable
    {
        readonly SharpDX.Direct3D11.DeviceContext _deviceContext;

        readonly SharpDX.Direct3D11.RasterizerState? _rasterizerState;

        readonly SharpDX.Mathematics.Interop.RawViewportF[] _viewports;

        readonly SharpDX.Direct3D11.RenderTargetView?[] _renderTargetViews;

        readonly SharpDX.Direct3D11.DepthStencilView? _depthStencilView;

        readonly SharpDX.Direct3D11.BlendState? _blendState;

        readonly SharpDX.Mathematics.Interop.RawColor4 _blendFactors;

        readonly int _blendSampleMask;

        readonly SharpDX.Direct3D11.InputLayout? _inputLayout;

        readonly SharpDX.Direct3D11.Buffer? _indexBuffer;

        readonly SharpDX.DXGI.Format _indexBufferFormat;

        readonly int _indexBufferOffset;

        readonly SharpDX.Direct3D.PrimitiveTopology _primitiveTopology;

        readonly SharpDX.Direct3D11.Buffer?[] _vertexBuffers = new SharpDX.Direct3D11.Buffer[SharpDX.Direct3D11.InputAssemblerStage.VertexInputResourceSlotCount];

        readonly int[] _vertexBuffersStride = new int[SharpDX.Direct3D11.InputAssemblerStage.VertexInputResourceSlotCount];

        readonly int[] _vertexBuffersOffset = new int[SharpDX.Direct3D11.InputAssemblerStage.VertexInputResourceSlotCount];

        bool _isDisposed;

        public DirectX11StateBlock(SharpDX.Direct3D11.DeviceContext deviceContext)
        {
            _deviceContext = deviceContext;
            _rasterizerState = deviceContext.Rasterizer.State;
            // SharpDX's parameterless GetViewports<T>() can throw
            // IndexOutOfRangeException in some device states; fall back to no saved
            // viewports (the game sets its own viewport each frame anyway).
            try
            {
                _viewports = deviceContext.Rasterizer.GetViewports<SharpDX.Mathematics.Interop.RawViewportF>();
            }
            catch
            {
                _viewports = System.Array.Empty<SharpDX.Mathematics.Interop.RawViewportF>();
            }
            _renderTargetViews = deviceContext.OutputMerger.GetRenderTargets(
                SharpDX.Direct3D11.OutputMergerStage.SimultaneousRenderTargetCount,
                out _depthStencilView
            );
            _blendState = deviceContext.OutputMerger.GetBlendState(out _blendFactors, out _blendSampleMask);
            _inputLayout = deviceContext.InputAssembler.InputLayout;
            deviceContext.InputAssembler.GetIndexBuffer(out _indexBuffer, out _indexBufferFormat, out _indexBufferOffset);
            _primitiveTopology = deviceContext.InputAssembler.PrimitiveTopology;
            deviceContext.InputAssembler.GetVertexBuffers(
                0,
                SharpDX.Direct3D11.InputAssemblerStage.VertexInputResourceSlotCount,
                _vertexBuffers,
                _vertexBuffersStride,
                _vertexBuffersOffset
            );
        }

        public void Apply()
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException();
            }

            _deviceContext.Rasterizer.State = _rasterizerState;
            if (_viewports.Length > 0)
            {
                _deviceContext.Rasterizer.SetViewports(_viewports);
            }
            _deviceContext.OutputMerger.SetRenderTargets(_depthStencilView, _renderTargetViews);
            _deviceContext.OutputMerger.SetBlendState(_blendState, _blendFactors, _blendSampleMask);
            _deviceContext.InputAssembler.InputLayout = _inputLayout;
            _deviceContext.InputAssembler.SetIndexBuffer(_indexBuffer, _indexBufferFormat, _indexBufferOffset);
            _deviceContext.InputAssembler.PrimitiveTopology = _primitiveTopology;
            _deviceContext.InputAssembler.SetVertexBuffers(0, _vertexBuffers, _vertexBuffersStride, _vertexBuffersOffset);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _rasterizerState?.Dispose();
                foreach (var renderTargetView in _renderTargetViews)
                {
                    renderTargetView?.Dispose();
                }
                _depthStencilView?.Dispose();
                _blendState?.Dispose();
                _inputLayout?.Dispose();
                _indexBuffer?.Dispose();
                foreach (var vertexBuffer in _vertexBuffers)
                {
                    vertexBuffer?.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}
