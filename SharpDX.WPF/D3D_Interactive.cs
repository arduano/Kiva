using SharpDX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows;

namespace SharpDX.WPF
{
	/// <summary>
	/// An interactive implementation which update the current camera
	/// </summary>
	partial class D3D : IInteractiveDirect3D, INotifyPropertyChanged
	{
		partial void OnInteractiveInit()
		{
		}

		/// <summary>
		/// Override this to focus the view, capture the mouse and select the <see cref="CurrentCamera"/> 
		/// </summary>
		public virtual void OnMouseDown(UIElement ui, MouseButtonEventArgs e)
		{
			if (CurrentCamera != null)
			{
				ui.CaptureMouse();
				ui.Focus();
				CurrentCamera.HandleMouseDown(ui, e);
			}
		}

		public virtual void OnMouseMove(UIElement ui, MouseEventArgs e)
		{
			if (CurrentCamera != null && ui.IsMouseCaptured)
			{
				CurrentCamera.HandleMouseMove(ui, e);
			}
		}

		public virtual void OnMouseUp(UIElement ui, MouseButtonEventArgs e)
		{
			if (CurrentCamera != null && ui.IsMouseCaptured)
			{
				CurrentCamera.HandleMouseUp(ui, e);
			}
			ui.ReleaseMouseCapture();
		}

		public virtual void OnMouseWheel(UIElement ui, MouseWheelEventArgs e)
		{
			if (CurrentCamera != null)
			{
				CurrentCamera.HandleMouseWheel(ui, e);
			}
		}

		public virtual void OnKeyDown(UIElement ui, KeyEventArgs e)
		{
			if (CurrentCamera != null)
			{
				CurrentCamera.HandleKeyDown(ui, e);
			}
		}

		public virtual void OnKeyUp(UIElement ui, KeyEventArgs e)
		{
			if (CurrentCamera != null)
			{
				CurrentCamera.HandleKeyUp(ui, e);
			}
		}

		#region CurrentCamera

		public BaseCamera CurrentCamera
		{
			get { return mCurrentCamera; }
			set
			{
				if (value == mCurrentCamera)
					return;
				mCurrentCamera = value;
				OnPropertyChanged("CurrentCamera");
			}
		}
		BaseCamera mCurrentCamera;

		#endregion

		#region INotifyPropertyChanged Members

		protected void OnPropertyChanged(string name)
		{
			var e = PropertyChanged;
			if (e != null)
				e(this, new PropertyChangedEventArgs(name));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
