using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;

// made after DXUTCamera.h & DXUTCamera.cpp

namespace SharpDX.WPF
{
	//--------------------------------------------------------------------------------------
	// Simple base camera class that moves and rotates.  The base class
	//       records mouse and keyboard input for use by a derived class, and 
	//       keeps common state.
	//--------------------------------------------------------------------------------------
	public abstract partial class BaseCamera
	{
		public BaseCamera()
		{
			SetViewParams(new Vector3(), new Vector3(0, 0, 1), new Vector3(0, 1, 0));
			SetProjParams((float)Math.PI / 3, 1, 0.05f, 100.0f);
			OnInitInteractive();
		}
		partial void OnInitInteractive();

		#region View

		Matrix mView; 

		Vector3 mPosition, mDefaultPosition;
		Vector3 mLookAt, mDefaultLookAt;
		Vector3 mUp, mDefaultUp;

		public void SetViewParams(Vector3 eye, Vector3 lookAt)
		{
			SetViewParams(eye, lookAt, mUp);
		}

		public virtual void SetViewParams(Vector3 eye, Vector3 lookAt, Vector3 vUp)
		{
			mDefaultPosition = mPosition = eye;
			mDefaultLookAt = mLookAt = lookAt;
			mDefaultUp = mUp = vUp;
			qViewRotation = Quaternion.Identity;
			UpdateView();
		}

		protected virtual void UpdateView()
		{
			mView = Matrix.LookAtLH(mPosition, mLookAt, mUp);
		}

		public void Reset()
		{
			SetViewParams(mDefaultPosition, mDefaultLookAt, mDefaultUp);
		}

		public Vector3 Position
		{
			get { return mPosition; }
			set
			{
				mPosition = value;
				UpdateView();
			}
		}

		public Vector3 LookAt
		{
			get { return mLookAt; }
			set
			{
				mLookAt = value;
				UpdateView();
			}
		}

		public Vector3 Up
		{
			get { return mUp; }
			set
			{
				mUp = value;
				UpdateView();
			}
		}

		public Matrix View { get { return mView; } }

		#endregion

		#region Projection

		Matrix mProj;              // Projection matrix

		float mFOV;                // Field of view
		float mAspect;              // Aspect ratio
		float mNearPlane;           // Near plane
		float mFarPlane;            // Far plane

		void UpdateProj()
		{
			mProj = Matrix.PerspectiveFovLH(mFOV, mAspect, mNearPlane, mFarPlane);
		}

		public void SetProjParams(float fFOV, float fAspect, float fNearPlane, float fFarPlane)
		{
			mFOV = fFOV;
			mAspect = fAspect;
			mNearPlane = fNearPlane;
			mFarPlane = fFarPlane;
			UpdateProj();
		}

		public float NearPlane
		{
			get { return mNearPlane; }
			set
			{
				mNearPlane = value;
				UpdateProj();
			}
		}
		public float FarPlane
		{
			get { return mFarPlane; }
			set
			{
				mFarPlane = value;
				UpdateProj();
			}
		}
		public float AspectRatio
		{
			get { return mAspect; }
			set
			{
				mAspect = value;
				UpdateProj();
			}
		}
		public float FieldOfView
		{
			get { return mAspect; }
			set
			{
				mFOV = value;
				UpdateProj();
			}
		}

		public Matrix Projection { get { return mProj; } }

		#endregion
	}

	//--------------------------------------------------------------------------------------
	// Simple first person camera class that moves and rotates.
	//       It allows yaw and pitch but not roll.  It uses WM_KEYDOWN and 
	//       GetCursorPos() to respond to keyboard and mouse input and updates the 
	//       view matrix based on input.  
	//--------------------------------------------------------------------------------------
	public partial class FirstPersonCamera : BaseCamera
	{
		public override void SetViewParams(Vector3 eye, Vector3 lookAt, Vector3 vUp)
		{
			base.SetViewParams(eye, lookAt, vUp);
			qViewRotation = Quaternion.Identity;
		}
	}


	//--------------------------------------------------------------------------------------
	// Simple model viewing camera class that rotates around the object.
	//--------------------------------------------------------------------------------------
	public partial class ModelViewerCamera : BaseCamera
	{
		float mfRadius;
		Matrix mWorld;
		Quaternion qModelRotation;

		public override void SetViewParams(Vector3 eye, Vector3 lookAt, Vector3 vUp)
		{
			base.SetViewParams(eye, lookAt, vUp);

			qModelRotation = Quaternion.Identity;
			mfRadius = (eye - lookAt).Length();
			UpdateWorld();
		}

		void UpdateWorld()
		{
			mWorld = Matrix.RotationQuaternion(qModelRotation);
		}

		public Matrix World { get { return mWorld; } }
	}
}
