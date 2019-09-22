using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Kiva_MIDI
{
	/// <summary>
	/// A <see cref="UIElement"/> displaying DirectX scene. 
	/// Takes care of resizing and refreshing a <see cref="DXImageSource"/>.
	/// It does no Direct3D work, which is delegated to
	/// the <see cref="IDirect3D"/> <see cref="Renderer"/> object.
	/// </summary>
	public class DXElement : FrameworkElement, INotifyPropertyChanged
    {
		DXImageSource surface;
		Stopwatch renderTimer;

		public DXElement()
        {
			base.SnapsToDevicePixels = true;

            renderTimer = new Stopwatch();
			surface = new DXImageSource();
			surface.IsFrontBufferAvailableChanged += delegate 
			{
				UpdateReallyLoopRendering();
				if (!IsReallyLoopRendering && surface.IsFrontBufferAvailable)
					Render();
			};
			IsVisibleChanged += delegate { UpdateReallyLoopRendering(); };
        }

		/// <summary>
		/// The image source where the DirectX scene (from the <see cref="Renderer"/>) will be rendered.
		/// </summary>
		public DXImageSource Surface { get { return surface; } }

		#region Renderer

		/// <summary>
		/// The D3D device that will handle the drawing
		/// </summary>
		public IDirect3D Renderer
		{
			get { return (IDirect3D)GetValue(RendererProperty); }
			set { SetValue(RendererProperty, value); }
		}

		public static readonly DependencyProperty RendererProperty =
			DependencyProperty.Register(
				"Renderer",
				typeof(IDirect3D),
				typeof(DXElement),
				new PropertyMetadata((d, e) => ((DXElement)d).OnRendererChanged((IDirect3D)e.OldValue, (IDirect3D)e.NewValue)));

		private void OnRendererChanged(IDirect3D oldValue, IDirect3D newValue)
		{
			UpdateSize();
			UpdateReallyLoopRendering();
			Focusable = newValue is IInteractiveDirect3D;
		}

		#endregion Renderer

		#region IsLoopRendering

		/// <summary>
		/// Wether or not the DirectX scene will be redrawn continuously
		/// </summary>
		public bool IsLoopRendering
		{
			get { return mIsLoopRendering; }
			set
			{
				if (value == mIsLoopRendering)
					return;
				mIsLoopRendering = value;
				UpdateReallyLoopRendering();
				OnPropertyChanged("IsLoopRendering");
			}
		}
		bool mIsLoopRendering = true;

		#endregion

		#region size overrides

		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			if (IsInDesignMode)
				return;
			UpdateReallyLoopRendering();
		}

		protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
		{
			base.ArrangeOverride(finalSize);
			UpdateSize();
			return finalSize;
		}

		protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
		{
			int w = (int)Math.Ceiling(availableSize.Width);
			int h = (int)Math.Ceiling(availableSize.Height);
			return new System.Windows.Size(w, h);
		}

		protected override Visual GetVisualChild(int index)
		{
			throw new ArgumentOutOfRangeException();
		}
		protected override int VisualChildrenCount { get { return 0; } }

		protected override void OnRender(DrawingContext dc)
		{
			dc.DrawImage(Surface, new Rect(RenderSize));
		}

		#endregion

		#region internals: ..LoopRendering.., UpdateSize()

		bool IsReallyLoopRendering
		{
			get { return mIsReallyLoopRendering; }
		}
		bool mIsReallyLoopRendering;

		void UpdateReallyLoopRendering()
		{
			var newValue =
				!IsInDesignMode
				&& IsLoopRendering
				&& Renderer != null
				&& Surface.IsFrontBufferAvailable
				&& VisualParent != null
				&& IsVisible
				;

			if (newValue != IsReallyLoopRendering)
			{
				mIsReallyLoopRendering = newValue;
				if (IsReallyLoopRendering)
				{
					renderTimer.Start();
					CompositionTarget.Rendering += OnLoopRendering;
				}
				else
				{
					CompositionTarget.Rendering -= OnLoopRendering;
					renderTimer.Stop();
				}
			}
		}

		void OnLoopRendering(object sender, EventArgs e) 
		{
			if (!IsReallyLoopRendering)
				return;
			Render(); 
		}

		void UpdateSize()
		{
			if (Renderer == null)
				return;
			Renderer.Reset(GetDrawEventArgs());
		}

		#endregion
		
		#region Render()

		/// <summary>
		/// Will redraw the underlying surface once.
		/// </summary>
		public void Render()
		{
			if (Renderer == null || IsInDesignMode)
				return;

			Renderer.Render(GetDrawEventArgs());
            Surface.Invalidate();
		}

		public DrawEventArgs GetDrawEventArgs()
		{
			var eargs = new DrawEventArgs
			{
				TotalTime = renderTimer.Elapsed,
				DeltaTime = lastDEA != null ? renderTimer.Elapsed - lastDEA.TotalTime : TimeSpan.Zero,
				RenderSize = DesiredSize,
				Target = Surface,
			};
			lastDEA = eargs;
			return eargs;
		}
		DrawEventArgs lastDEA;

		#endregion

		#region override input: Key, Mouse

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			if (Renderer is IInteractiveDirect3D)
				((IInteractiveDirect3D)Renderer).OnMouseDown(this, e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (Renderer is IInteractiveDirect3D)
				((IInteractiveDirect3D)Renderer).OnMouseMove(this, e);
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);
			if (Renderer is IInteractiveDirect3D)
				((IInteractiveDirect3D)Renderer).OnMouseUp(this, e);
		}

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			base.OnMouseWheel(e);
			if (Renderer is IInteractiveDirect3D)
				((IInteractiveDirect3D)Renderer).OnMouseWheel(this, e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (Renderer is IInteractiveDirect3D)
				((IInteractiveDirect3D)Renderer).OnKeyDown(this, e);
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
			if (Renderer is IInteractiveDirect3D)
				((IInteractiveDirect3D)Renderer).OnKeyUp(this, e);
		}

		#endregion

		#region IsInDesignMode
		
        /// <summary>
        /// Gets a value indicating whether the control is in design mode
        /// (running in Blend or Visual Studio).
        /// </summary>
        public bool IsInDesignMode
        {
            get { return DesignerProperties.GetIsInDesignMode(this); }
        }

		#endregion    

		#region INotifyPropertyChanged Members

		void OnPropertyChanged(string name)
		{
			var e = PropertyChanged;
			if (e != null)
				e(this, new PropertyChangedEventArgs(name));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
