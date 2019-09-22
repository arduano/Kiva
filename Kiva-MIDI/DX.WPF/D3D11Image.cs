using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace Kiva_MIDI
{
    /// <summary>
    //  An <see cref="D3DImage"/> that displays a user-created Direct3D 11 surface.
    /// </summary>
    public class D3D11Image : D3DImage, IDisposable
    {
        private Texture texture;
        private IntPtr textureSurfaceHandle;

        /// <summary>
        /// Creates new instance of <see cref="D3D11Image"/> Associates an D3D11 render target with the current instance.
        /// </summary>
        /// <param name="device">A valid D3D9 DeviceEx.</param>
        /// <param name="renderTarget">A valid D3D11 render target. It must be created with the "Shared" flag.</param>
        public D3D11Image(DeviceEx device, SharpDX.Direct3D11.Texture2D renderTarget)
        {
            using (var resource = renderTarget.QueryInterface<SharpDX.DXGI.Resource>())
            {
                var handle = resource.SharedHandle;
                texture = new Texture(device,
                                      renderTarget.Description.Width,
                                      renderTarget.Description.Height,
                                      1,
                                      Usage.RenderTarget,
                                      Format.A8R8G8B8,
                                      Pool.Default,
                                      ref handle);
            }
            using (var surface = texture.GetSurfaceLevel(0))
            {
                textureSurfaceHandle = surface.NativePointer;
                TrySetBackbufferPointer(textureSurfaceHandle);
            }

            this.IsFrontBufferAvailableChanged += HandleIsFrontBufferAvailableChanged;
        }

        /// <summary>
        /// Marks the surface of element as invalid and requests its presentation on screen.
        /// </summary>
        public void InvalidateRendering()
        {
            if (texture == null) return;

            this.Lock();
            this.AddDirtyRect(new Int32Rect(0, 0, this.PixelWidth, this.PixelHeight));
            this.Unlock();
        }

        /// <summary>
        /// Trys to set the backbuffer pointer.
        /// </summary>
        /// <param name="ptr"></param>
        public void TrySetBackbufferPointer(IntPtr ptr)
        {
            // TODO: use TryLock and check multithreading scenarios
            this.Lock();
            try
            {
                this.SetBackBuffer(D3DResourceType.IDirect3DSurface9, ptr);
            }
            finally
            {
                this.Unlock();
            }
        }

        /// <summary>
        /// Disposes associated D3D9 texture.
        /// </summary>
        public void Dispose()
        {
            if (texture != null)
            {
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    this.IsFrontBufferAvailableChanged -= HandleIsFrontBufferAvailableChanged;
                    texture.Dispose();
                    texture = null;
                    textureSurfaceHandle = IntPtr.Zero;

                    TrySetBackbufferPointer(IntPtr.Zero);
                }, System.Windows.Threading.DispatcherPriority.Send);
            }
        }

        private void HandleIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsFrontBufferAvailable)
                TrySetBackbufferPointer(textureSurfaceHandle);
        }

    }
}
