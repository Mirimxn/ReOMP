using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OMP.LSWTSS;

partial class Overlay1
{
    DirectX11OverlayQuad? _directX11OverlayQuad;

    sealed class DirectX11OverlayQuad : IDisposable
    {
        readonly SharpDX.Direct3D11.Device1 _device1;

        readonly SharpDX.Direct3D11.DeviceContext _deviceContext;

        readonly SharpDX.Direct3D11.Texture2DDescription _textureDesc;

        readonly SharpDX.Direct3D11.Texture2D _texture;

        readonly DirectX11Quad _quad;

        // A FlipDiscard swapchain rotates its back buffer every frame, so the
        // render target view must come from the *current* back buffer each frame.
        // There are only a couple of distinct back-buffer resources, so cache a
        // view per resource instead of recreating one every frame.
        readonly Dictionary<IntPtr, SharpDX.Direct3D11.RenderTargetView> _renderTargetViewByBackBuffer = new();

        // CEF software paint: OnPaint (CEF thread) copies the BGRA pixel buffer
        // into this managed array under the lock; the GPU upload happens later on
        // the game's render thread in Draw, so the device context is only ever
        // used from one thread. This avoids the GPU shared-texture path entirely,
        // which sidesteps the cross-GPU and threading problems.
        byte[]? _queuedBgraTextureBytes;

        int _queuedBgraTextureWidth;

        int _queuedBgraTextureHeight;

        bool _isBgraTextureQueued;

        readonly object _lock;

        bool _isDisposed;

        public readonly SharpDX.DXGI.SwapChain SwapChain;

        public int TextureWidth => _textureDesc.Width;

        public int TextureHeight => _textureDesc.Height;

        public DirectX11OverlayQuad(
            SharpDX.DXGI.SwapChain swapChain
        )
        {
            SwapChain = swapChain;

            _device1 = swapChain.GetDevice<SharpDX.Direct3D11.Device1>();

            _deviceContext = _device1.ImmediateContext;

            var backBuffer = swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0);

            var backBufferDesc = backBuffer.Description;

            var backBufferViewport = new SharpDX.Mathematics.Interop.RawViewportF
            {
                X = 0,
                Y = 0,
                Width = backBufferDesc.Width,
                Height = backBufferDesc.Height,
                MinDepth = 0,
                MaxDepth = 1,
            };

            _textureDesc = new SharpDX.Direct3D11.Texture2DDescription
            {
                Width = backBufferDesc.Width,
                Height = backBufferDesc.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                SampleDescription = new SharpDX.DXGI.SampleDescription
                {
                    Count = 1,
                    Quality = 0,
                },
                Usage = SharpDX.Direct3D11.ResourceUsage.Default,
                BindFlags = SharpDX.Direct3D11.BindFlags.ShaderResource,
                CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
                OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
            };

            _texture = new SharpDX.Direct3D11.Texture2D(_device1, _textureDesc);

            _quad = new DirectX11Quad(_device1, _texture, _textureDesc, backBufferViewport);

            backBuffer.Dispose();

            _lock = new object();
        }

        SharpDX.Direct3D11.RenderTargetView? GetOrCreateCurrentRenderTargetView()
        {
            using var backBuffer = SwapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0);

            var key = backBuffer.NativePointer;

            if (!_renderTargetViewByBackBuffer.TryGetValue(key, out var renderTargetView))
            {
                renderTargetView = new SharpDX.Direct3D11.RenderTargetView(_device1, backBuffer);
                _renderTargetViewByBackBuffer[key] = renderTargetView;
            }

            return renderTargetView;
        }

        // Called from CEF's OnPaint thread. Copies the CPU pixel buffer into a
        // managed array only - no device-context work here.
        public void QueueBgraTextureUpdate(nint buffer, int width, int height)
        {
            if (buffer == nint.Zero)
            {
                return;
            }

            var byteCount = checked(width * height * 4);

            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }

                if (width != _textureDesc.Width || height != _textureDesc.Height)
                {
                    return;
                }

                if (_queuedBgraTextureBytes == null || _queuedBgraTextureBytes.Length != byteCount)
                {
                    _queuedBgraTextureBytes = new byte[byteCount];
                }

                Marshal.Copy(buffer, _queuedBgraTextureBytes, 0, byteCount);

                _queuedBgraTextureWidth = width;
                _queuedBgraTextureHeight = height;
                _isBgraTextureQueued = true;
            }
        }

        // Called from the game's render thread (in Draw). Uploads the queued CPU
        // buffer to the GPU texture on the immediate context.
        void UploadQueuedBgraTexture()
        {
            if (!_isBgraTextureQueued || _queuedBgraTextureBytes == null)
            {
                return;
            }

            var pinned = GCHandle.Alloc(_queuedBgraTextureBytes, GCHandleType.Pinned);

            try
            {
                var dataBox = new SharpDX.DataBox(
                    pinned.AddrOfPinnedObject(),
                    _queuedBgraTextureWidth * 4,
                    0
                );

                _deviceContext.UpdateSubresource(dataBox, _texture, 0);

                _isBgraTextureQueued = false;
            }
            finally
            {
                pinned.Free();
            }
        }

        public void Draw()
        {
            if (_isDisposed)
            {
                throw new InvalidOperationException();
            }

            lock (_lock)
            {
                UploadQueuedBgraTexture();

                var renderTargetView = GetOrCreateCurrentRenderTargetView();

                if (renderTargetView == null)
                    return; // swap chain is mid-transition, skip this frame

                _quad.Draw(renderTargetView);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_isDisposed)
                {
                    _quad.Dispose();
                    _texture.Dispose();
                    foreach (var renderTargetView in _renderTargetViewByBackBuffer.Values)
                    {
                        renderTargetView.Dispose();
                    }
                    _renderTargetViewByBackBuffer.Clear();
                    _deviceContext.Dispose();
                    _device1.Dispose();

                    _isDisposed = true;
                }
            }
        }
    }
}
