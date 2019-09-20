using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using System.Windows;
using System.Windows.Input;

namespace SharpDX.WPF
{
	// about Quaternions
	// http://en.wikipedia.org/wiki/Quaternions_and_spatial_rotation
	// http://www.itk.org/CourseWare/Training/QuaternionsI.pdf
	// http://www.cprogramming.com/tutorial/3d/quaternions.html

	partial class BaseCamera
	{
		partial void OnInitInteractive()
		{
			SetScalers((float)Math.PI / 5, 3);
		}

		protected Vector2 pMouseDown, pMouseLast;
		protected Dictionary<Key, bool> downKeys = new Dictionary<Key, bool>();

		protected static Vector2 GetVector(UIElement ui, MouseEventArgs e) 
		{
			var p = e.GetPosition(ui);
			return new Vector2((float)p.X, (float)(ui.RenderSize.Height - p.Y));
		}

		public void HandleMouseDown(UIElement ui, MouseButtonEventArgs e)
		{
			pMouseDown = GetVector(ui, e);
			pMouseLast = pMouseDown;
		}

		protected float GetMouseAngle(Vector2 dp, UIElement ui)
		{
			float div = (float)Math.Max(ui.RenderSize.Width, ui.RenderSize.Height) / 2;
			if (div < 1)
				div = 1;

			float angle = dp.Length() / div;
			return angle;
		}

		public void HandleMouseMove(UIElement ui, MouseEventArgs e)
		{
			var pMouse = GetVector(ui, e);
			var dp = pMouse - pMouseLast;

			{
				var rAxis = Vector3.Cross(new Vector3(dp.X, dp.Y, 0), new Vector3(0, 0, -1));
				if (rAxis.LengthSquared() >= 0.00001)
				{
					float angle = GetMouseAngle(dp, ui);
					var tmpQuat = Quaternion.RotationAxis(rAxis, angle);
					MouseRotation(tmpQuat);
				}
			}

			pMouseLast = pMouse;
		}

		public void HandleMouseUp(UIElement ui, MouseButtonEventArgs e)
		{
		}

		public void HandleMouseWheel(UIElement ui, MouseWheelEventArgs e)
		{
			var dp = e.Delta > 0 ? new Vector3(0, 0, -1) : new Vector3(0, 0, 1);
			KeyMove(dp);
		}

		public void HandleKeyDown(UIElement ui, KeyEventArgs e)
		{
			downKeys[e.Key] = true;

			switch (e.Key)
			{
				case Key.W:
				case Key.Up:
				case Key.S:
				case Key.Down:
				case Key.D:
				case Key.Right:
				case Key.A:
				case Key.Left:
				case Key.PageUp:
				case Key.PageDown:
					// speed
					break;
				case Key.E:
				case Key.Q:
					// roll speed
					break;
				case Key.Home:
					Reset();
					break;
				default:
					return;
			}
			e.Handled = true;
		}

		static readonly Vector3 Zero3 = new Vector3();

		static Vector3 GetSpeed(Key k)
		{
			switch (k)
			{
				case Key.W:
				case Key.Up:
					return new Vector3(0, 0, 1);
				case Key.S:
				case Key.Down:
					return new Vector3(0, 0, -1);
				case Key.D:
				case Key.Right:
					return new Vector3(1, 0, 0);
				case Key.A:
				case Key.Left:
					return new Vector3(-1, 0, 0);
				case Key.PageUp:
					return new Vector3(0, 1, 0);
				case Key.PageDown:
					return new Vector3(0, -1, 0);
			}
			return Zero3;
		}
		static float GetRollSpeed(Key k)
		{
			switch (k)
			{
				case Key.E:
					return 1;
				case Key.Q:
					return -1;
			}
			return 0;
		}

		public void HandleKeyUp(UIElement ui, KeyEventArgs e)
		{
			downKeys.Remove(e.Key);
		}

		#region EnableYAxisMovement

		public bool EnableYAxisMovement
		{
			get { return mEnableYAxisMovement; }
			set
			{
				if (value == mEnableYAxisMovement)
					return;
				mEnableYAxisMovement = value;
			}
		}
		bool mEnableYAxisMovement = true;

		#endregion

		public void FrameMove(TimeSpan elapsed)
		{
			float rSpeed = 0;
			Vector3 speed = new Vector3();
			foreach (var item in downKeys.Keys)
			{
				speed += GetSpeed(item);
				rSpeed += GetRollSpeed(item);
			}

			KeyMove(speed * (float)elapsed.TotalSeconds);
			KeyRoll(rSpeed * (float)elapsed.TotalSeconds);
		}

		public void SetScalers(float sRotation, float sMove)
		{
			RotationScaler = sRotation;
			MoveScaler = sMove;
		}

		public float RotationScaler { get; set; }
		public float MoveScaler { get; set; }


		// === apply the changes!

		protected Quaternion qViewRotation;

		protected virtual void KeyMove(Vector3 dp)
		{
			if (!EnableYAxisMovement)
				dp.Y = 0;
			dp *= MoveScaler;
			dp = Matrix.RotationQuaternion(qViewRotation).TransformNormal(dp);
			Position += dp;
			LookAt += dp;
			UpdateView();
		}

		protected virtual void KeyRoll(float angle)
		{
			angle *= RotationScaler;
			var m = Matrix.RotationZ(angle);
			Up = m.TransformNormal(Up);
			UpdateView();
		}

		protected virtual void MouseRotation(Quaternion dMouse)
		{
			var mRot = Matrix.RotationQuaternion(dMouse);

			LookAt = Position + mRot.TransformNormal(LookAt - Position);
			Up = mRot.TransformNormal(Up);

			qViewRotation *= dMouse;
			qViewRotation.Normalize();
		}
	}

	partial class FirstPersonCamera : BaseCamera
	{
	}

	partial class ModelViewerCamera
	{
		protected override void MouseRotation(Quaternion dMouse)
		{
			qModelRotation = qModelRotation * dMouse;
			UpdateWorld();
		}
	}
}
