using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using SharpDX.WPF;
using System.Runtime.InteropServices;
using System.IO;
using System;

namespace SharpDX.WPF
{
	public static class DXUtils
	{
		#region GetOrThrow<T>()

		public static T GetOrThrow<T>(this T obj)
			where T : class, IDisposable
		{
			if (obj == null)
				throw new ObjectDisposedException(typeof(T).Name);
			return obj;
		} 

		#endregion

		#region Matrix: TransformNormal(), TransformCoord(), Multiply()

		public static Vector3 TransformNormal(this Matrix m, Vector3 v)
		{
			var v2 = Multiply(m, v.X, v.Y, v.Z, 0);
			return new Vector3(v2.X, v2.Y, v2.Z);
		}

		public static Vector3 TransformCoord(this Matrix m, Vector3 v)
		{
			var v2 = Multiply(m, v.X, v.Y, v.Z, 1);
			return new Vector3(v2.X, v2.Y, v2.Z);
		}

		public static Vector3 Multiply(this Matrix m, float x, float y, float z, float w)
		{
			return new Vector3(
				m.M11 * x + m.M12 * y + m.M13 * z + m.M14 * w
				, m.M21 * x + m.M22 * y + m.M23 * z + m.M24 * w
				, m.M31 * x + m.M32 * y + m.M33 * z + m.M34 * w
				);
		}

		#endregion

		#region DEG2RAD()

		public static float DEG2RAD(this float degrees)
		{
			return degrees * (float)Math.PI / 180.0f;
		}

		#endregion

		#region D3D10, D3D11: CreateTexture2D()

		public static Direct3D11.Texture2D CreateTexture2D(this Direct3D11.Device device,
			int w, int h,
			Direct3D11.BindFlags flags = Direct3D11.BindFlags.RenderTarget | Direct3D11.BindFlags.ShaderResource,
			Format format = Format.B8G8R8A8_UNorm,
			Direct3D11.ResourceOptionFlags options = Direct3D11.ResourceOptionFlags.Shared)
		{
			var colordesc = new Direct3D11.Texture2DDescription
			{
				BindFlags = flags,
				Format = format,
				Width = w,
				Height = h,
				MipLevels = 1,
				SampleDescription = new SampleDescription(1, 0),
				Usage = Direct3D11.ResourceUsage.Default,
				OptionFlags = options,
				CpuAccessFlags = Direct3D11.CpuAccessFlags.None,
				ArraySize = 1
			};
			return new Direct3D11.Texture2D(device, colordesc);
		}

		public static Direct3D10.Texture2D CreateTexture2D(this Direct3D10.Device device,
			int w, int h,
			Direct3D10.BindFlags flags = Direct3D10.BindFlags.RenderTarget | Direct3D10.BindFlags.ShaderResource,
			Format format = Format.B8G8R8A8_UNorm,
			Direct3D10.ResourceOptionFlags options = Direct3D10.ResourceOptionFlags.Shared)
		{
			var colordesc = new Direct3D10.Texture2DDescription
			{
				BindFlags = flags,
				Format = format,
				Width = w,
				Height = h,
				MipLevels = 1,
				SampleDescription = new SampleDescription(1, 0),
				Usage = Direct3D10.ResourceUsage.Default,
				OptionFlags = options,
				CpuAccessFlags = Direct3D10.CpuAccessFlags.None,
				ArraySize = 1
			};
			return new Direct3D10.Texture2D(device, colordesc);
		}

		#endregion

		#region D3D10, D3D11: CreateBuffer<T>()

		public static Direct3D11.Buffer CreateBuffer<T>(this Direct3D11.Device device, T[] range)
			where T : struct
		{
			int sizeInBytes = Marshal.SizeOf(typeof(T));
			using (var stream = new DataStream(range.Length * sizeInBytes, true, true))
			{
				stream.WriteRange(range);
				return new Direct3D11.Buffer(device, stream, new Direct3D11.BufferDescription
				{
					BindFlags = Direct3D11.BindFlags.VertexBuffer,
					SizeInBytes = (int)stream.Length,
					CpuAccessFlags = Direct3D11.CpuAccessFlags.None,
					OptionFlags = Direct3D11.ResourceOptionFlags.None,
					StructureByteStride = 0,
					Usage = Direct3D11.ResourceUsage.Default,
				});
			}
		}

		public static Direct3D10.Buffer CreateBuffer<T>(this Direct3D10.Device device, T[] range)
			where T : struct
		{
			int sizeInBytes = Marshal.SizeOf(typeof(T));
			using (var stream = new DataStream(range.Length * sizeInBytes, true, true))
			{
				stream.WriteRange(range);
				return new Direct3D10.Buffer(device, stream, new Direct3D10.BufferDescription
				{
					BindFlags = Direct3D10.BindFlags.VertexBuffer,
					SizeInBytes = (int)stream.Length,
					CpuAccessFlags = Direct3D10.CpuAccessFlags.None,
					OptionFlags = Direct3D10.ResourceOptionFlags.None,
					Usage = Direct3D10.ResourceUsage.Default,
				});
			}
		}

		#endregion	

		// add support for normal System.IO.Stream
		#region D3D10, D3D11 UpdateSubresource()

		public static void UpdateSubresource(this Direct3D10.Device device, Stream source, Direct3D10.Resource resource, int subresource)
		{
			byte[] buf = new byte[source.Length];
			source.Position = 0;
			source.Read(buf, 0, buf.Length);

			using (var ds = new DataStream(buf, true, true))
			{
				var db = new DataBox(0, 0, ds);
				device.UpdateSubresource(db, resource, subresource);
			}
		}

		public static void UpdateSubresource(this Direct3D11.DeviceContext devctxt, Stream source, Direct3D11.Resource resource, int subresource)
		{
			byte[] buf = new byte[source.Length];
			source.Position = 0;
			source.Read(buf, 0, buf.Length);

			using (var ds = new DataStream(buf, true, true))
			{
				var db = new DataBox(0, 0, ds);
				devctxt.UpdateSubresource(db, resource, subresource);
			}
		}

		#endregion
	}
}
