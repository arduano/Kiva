using System;
using SharpDX.Direct3D9;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Runtime.InteropServices;

namespace SharpDX.WPF
{
	public static class DXSharing
	{
		#region ToD3D9Format()

		public static SharpDX.Direct3D9.Format ToD3D9(this SharpDX.DXGI.Format dxgiformat)
		{
			switch (dxgiformat)
			{
				case SharpDX.DXGI.Format.R10G10B10A2_UNorm:
					return SharpDX.Direct3D9.Format.A2B10G10R10;
				case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
					return SharpDX.Direct3D9.Format.A8R8G8B8;
				case SharpDX.DXGI.Format.R16G16B16A16_Float:
					return SharpDX.Direct3D9.Format.A16B16G16R16F;

					// not sure those one below will work...

				case SharpDX.DXGI.Format.R32G32B32A32_Float:
					return SharpDX.Direct3D9.Format.A32B32G32R32F;

				case SharpDX.DXGI.Format.R16G16B16A16_UNorm:
					return SharpDX.Direct3D9.Format.A16B16G16R16;
				case SharpDX.DXGI.Format.R32G32_Float:
					return SharpDX.Direct3D9.Format.G32R32F;

				case SharpDX.DXGI.Format.R8G8B8A8_UNorm:
					return SharpDX.Direct3D9.Format.A8R8G8B8;

				case SharpDX.DXGI.Format.R16G16_UNorm:
					return SharpDX.Direct3D9.Format.G16R16;

				case SharpDX.DXGI.Format.R16G16_Float:
					return SharpDX.Direct3D9.Format.G16R16F;
				case SharpDX.DXGI.Format.R32_Float:
					return SharpDX.Direct3D9.Format.R32F;

				case SharpDX.DXGI.Format.R16_Float:
					return SharpDX.Direct3D9.Format.R16F;

				case SharpDX.DXGI.Format.A8_UNorm:
					return SharpDX.Direct3D9.Format.A8;
				case SharpDX.DXGI.Format.R8_UNorm:
					return SharpDX.Direct3D9.Format.L8;

				case SharpDX.DXGI.Format.BC1_UNorm:
					return SharpDX.Direct3D9.Format.MtDxt1;
				case SharpDX.DXGI.Format.BC2_UNorm:
					return SharpDX.Direct3D9.Format.MtDxt3;
				case SharpDX.DXGI.Format.BC3_UNorm:
					return SharpDX.Direct3D9.Format.MtDxt5;

				default:
					return SharpDX.Direct3D9.Format.Unknown;
			}
		}

		#endregion

		#region GetD3D9(Direct3D10.Texture2D)

		public static Texture GetSharedD3D9(this DeviceEx device, SharpDX.Direct3D10.Texture2D renderTarget)
		{
			if (renderTarget == null)
				return null;

			if ((renderTarget.Description.OptionFlags & SharpDX.Direct3D10.ResourceOptionFlags.Shared) == 0)
				throw new ArgumentException("Texture must be created with ResourceOptionFlags.Shared");

			Format format = ToD3D9(renderTarget.Description.Format);
			if (format == Format.Unknown)
				throw new ArgumentException("Texture format is not compatible with OpenSharedResource");

			using (var resource = renderTarget.QueryInterface<SharpDX.DXGI.Resource>())
			{
				IntPtr handle = resource.SharedHandle;
				if (handle == IntPtr.Zero)
					throw new ArgumentNullException("Handle");
				return new Texture(device, renderTarget.Description.Width, renderTarget.Description.Height, 1, Usage.RenderTarget, format, Pool.Default, ref handle);
			}
		}

		#endregion

		#region GetD3D9(Direct3D11.Texture2D)

		public static Texture GetSharedD3D9(this DeviceEx device, SharpDX.Direct3D11.Texture2D renderTarget)
		{
			if (renderTarget == null)
				return null;

			if ((renderTarget.Description.OptionFlags & SharpDX.Direct3D11.ResourceOptionFlags.Shared) == 0)
				throw new ArgumentException("Texture must be created with ResourceOptionFlags.Shared");

			Format format = ToD3D9(renderTarget.Description.Format);
			if (format == Format.Unknown)
				throw new ArgumentException("Texture format is not compatible with OpenSharedResource");

			using (var resource = renderTarget.QueryInterface<SharpDX.DXGI.Resource>())
			{
				IntPtr handle = resource.SharedHandle;
				if (handle == IntPtr.Zero)
					throw new ArgumentNullException("Handle");
				return new Texture(device, renderTarget.Description.Width, renderTarget.Description.Height, 1, Usage.RenderTarget, format, Pool.Default, ref handle);
			}
		}

		#endregion

		#region D3D11.Texture2D: GetBitmap()

		public unsafe static WriteableBitmap GetBitmap(this SharpDX.Direct3D11.Texture2D tex)
		{
			DataRectangle db;
			using (var copy = tex.GetCopy())
			using (var surface = copy.QueryInterface<SharpDX.DXGI.Surface>())
			{
				db = surface.Map(DXGI.MapFlags.Read);
				// can't destroy the surface now with WARP driver

				int w = tex.Description.Width;
				int h = tex.Description.Height;
				var wb = new WriteableBitmap(w, h, 96.0, 96.0, PixelFormats.Bgra32, null);
				wb.Lock();
				try
				{
					uint* wbb = (uint*)wb.BackBuffer;

					db.Data.Position = 0;
					for (int y = 0; y < h; y++)
					{
						db.Data.Position = y * db.Pitch;
						for (int x = 0; x < w; x++)
						{
							var c = db.Data.Read<uint>();
							wbb[y * w + x] = c;
						}
					}
				}
				finally
				{
					wb.AddDirtyRect(new Int32Rect(0, 0, w, h));
					wb.Unlock();
				}
				return wb;
			}
		}

		static SharpDX.Direct3D11.Texture2D GetCopy(this SharpDX.Direct3D11.Texture2D tex)
		{
			var teximg = new SharpDX.Direct3D11.Texture2D(tex.Device, new SharpDX.Direct3D11.Texture2DDescription
			{
				Usage = SharpDX.Direct3D11.ResourceUsage.Staging,
				BindFlags = SharpDX.Direct3D11.BindFlags.None,
				CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.Read,
				Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
				OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
				ArraySize = tex.Description.ArraySize,
				Height = tex.Description.Height,
				Width = tex.Description.Width,
				MipLevels = tex.Description.MipLevels,
				SampleDescription = tex.Description.SampleDescription,
			});
			tex.Device.ImmediateContext.CopyResource(tex, teximg);
			return teximg;
		}

		#endregion

		#region D3D10.Texture2D: GetBitmap()

		[StructLayout(LayoutKind.Explicit, Pack = 4)]
		struct FIXREAD
		{
			[FieldOffset(0)]
			public byte B0;
			[FieldOffset(1)]
			public byte B1;
			[FieldOffset(2)]
			public byte B2;
			[FieldOffset(3)]
			public byte B3;

			[FieldOffset(0)]
			public uint UINT;

			public void Fix()
			{
				// TODO there is a bug somewhere to fix!
				if (UINT != 0 && B3 == 0)
					B3 = 255;
			}
		}

		public unsafe static WriteableBitmap GetBitmap(this SharpDX.Direct3D10.Texture2D tex)
		{
			DataRectangle db;
			using (var copy = tex.GetCopy())
			using (var surface = copy.AsSurface())
				db = surface.Map(DXGI.MapFlags.Read);

			int w = tex.Description.Width;
			int h = tex.Description.Height;
			var wb = new WriteableBitmap(w, h, 96.0, 96.0, PixelFormats.Bgra32, null);
			wb.Lock();
			uint* wbb = (uint*)wb.BackBuffer;
			db.Data.Position = 0;
			for (int y = 0; y < h; y++)
			{
				db.Data.Position = y * db.Pitch;
				for (int x = 0; x < w; x++)
				{
					var c = db.Data.Read<FIXREAD>();
					c.Fix();
					wbb[y * w + x] = c.UINT;
				}
			}
			wb.AddDirtyRect(new Int32Rect(0, 0, w, h));
			wb.Unlock();
			return wb;
		}

		internal static SharpDX.Direct3D10.Texture2D GetCopy(this SharpDX.Direct3D10.Texture2D tex)
		{
			var teximg = new SharpDX.Direct3D10.Texture2D(tex.Device, new SharpDX.Direct3D10.Texture2DDescription
			{
				Usage = SharpDX.Direct3D10.ResourceUsage.Staging,
				BindFlags = SharpDX.Direct3D10.BindFlags.None,
				CpuAccessFlags = SharpDX.Direct3D10.CpuAccessFlags.Read,
				Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
				OptionFlags = SharpDX.Direct3D10.ResourceOptionFlags.None,
				ArraySize = tex.Description.ArraySize,
				Height = tex.Description.Height,
				Width = tex.Description.Width,
				MipLevels = tex.Description.MipLevels,
				SampleDescription = tex.Description.SampleDescription,
			});
			tex.Device.CopyResource(tex, teximg);
			return teximg;
		}

		#endregion
	}
}
