using SharpDX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Kiva_MIDI
{
	/// <summary>
	/// A vanilla implementation of <see cref="IDirect3D"/> with some common wiring already done.
	/// </summary>
	public abstract partial class D3D : IDirect3D, IDisposable
	{
		public D3D()
		{
			OnInteractiveInit();
		}

		partial void OnInteractiveInit();

		~D3D() { Dispose(false); }
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing) 
		{
		}

		/// <summary>
		/// Size set with call to <see cref="Reset(DrawEventArgs)"/>
		/// </summary>
		public Vector2 RenderSize { get; protected set; }

		public virtual void Reset(DrawEventArgs args)
		{
			int w = (int)Math.Ceiling(args.RenderSize.Width);
			int h = (int)Math.Ceiling(args.RenderSize.Height);
			if (w < 1 || h < 1)
				return;

			RenderSize = new Vector2(w, h);

			Reset(w, h);
			if (Resetted != null)
				Resetted(this, args);

			Render(args);

			if (args.Target != null)
				SetBackBuffer(args.Target);
		}

		public virtual void Reset(int w, int h)
		{
		}

		public event EventHandler<DrawEventArgs> Resetted;

		/// <summary>
		/// SharpDX 1.3 requires explicit dispose of all its ComObject.
		/// This method makes it easy.
		/// (Remark: I attempted to hack a correct Dispose implementation but it crashed the app on first GC!)
		/// </summary>
		public static void Set<T>(ref T field, T newValue)
			where T : IDisposable
		{
			if (field != null)
				field.Dispose();
			field = newValue;
		}

		public abstract System.Windows.Media.Imaging.WriteableBitmap ToImage();

		public abstract void SetBackBuffer(DXImageSource dximage);

		/// <summary>
		/// Time in the last <see cref="DrawEventArgs"/> passed to <see cref="Render(DrawEventArgs)"/>
		/// </summary>
		public TimeSpan RenderTime { get; protected set; }

		public void Render(DrawEventArgs args)
		{
			RenderTime = args.TotalTime;

            BeginRender(args);
			RenderScene(args);
			EndRender(args);
            //SetBackBuffer(args.Target);
		}

		public virtual void BeginRender(DrawEventArgs args) { }
		public virtual void RenderScene(DrawEventArgs args)
		{
			if (Rendering != null)
				Rendering(this, args);
		}
		public virtual void EndRender(DrawEventArgs args) { }

		public event EventHandler<DrawEventArgs> Rendering;
	}
}
