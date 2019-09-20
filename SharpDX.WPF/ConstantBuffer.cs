using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

#region D3D11: ConstantBuffer

namespace SharpDX.Direct3D11
{
	public class ConstantBuffer<T> : IDisposable, INotifyPropertyChanged
		where T : struct
	{
		private Direct3D11.Device _device;
		private Direct3D11.Buffer _buffer;
		private DataStream _dataStream;

		public Direct3D11.Buffer Buffer { get { return _buffer; } }
		public Direct3D11.Device Device { get { return _device; } }

		public ConstantBuffer(Direct3D11.Device device)
			: this(device, new Direct3D11.BufferDescription
			{
				Usage = Direct3D11.ResourceUsage.Default,
				BindFlags = Direct3D11.BindFlags.ConstantBuffer,
				CpuAccessFlags = Direct3D11.CpuAccessFlags.None,
				OptionFlags = Direct3D11.ResourceOptionFlags.None,
				StructureByteStride = 0
			})
		{
		}

		public ConstantBuffer(Direct3D11.Device device, Direct3D11.BufferDescription desc)
		{
			desc.SizeInBytes = Marshal.SizeOf(typeof(T));

			if (device == null)
				throw new ArgumentNullException("device");

			this._device = device;
			_device.AddReference();

			_buffer = new Direct3D11.Buffer(device, desc);
			_dataStream = new DataStream(desc.SizeInBytes, true, true);
		}

		~ConstantBuffer()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}
		void Dispose(bool disposing)
		{
			if (_device == null)
				return;

			if (disposing)
				_dataStream.Dispose();
			// NOTE: SharpDX 1.3 requires explicit Dispose() of all resource
			_device.Release();
			_buffer.Release();
			_device = null;
			_buffer = null;
		}

		public T Value
		{
			get { return bufvalue; }
			set
			{
				if (_device == null)
					throw new ObjectDisposedException(GetType().Name);

				bufvalue = value;

				Marshal.StructureToPtr(value, _dataStream.DataPointer, false);
				var dataBox = new DataBox(0, 0, _dataStream);
				_device.ImmediateContext.UpdateSubresource(dataBox, _buffer, 0);

				OnPropertyChanged("Value");
			}
		}
		T bufvalue;

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

#endregion

#region D3D10: ConstantBuffer

namespace SharpDX.Direct3D10
{
	public class ConstantBuffer<T> : IDisposable, INotifyPropertyChanged
		where T : struct
	{
		private Direct3D10.Device _device;
		private Direct3D10.Buffer _buffer;
		private DataStream _dataStream;

		public Direct3D10.Buffer Buffer { get { return _buffer; } }
		public Direct3D10.Device Device { get { return _device; } }

		public ConstantBuffer(Direct3D10.Device device)
			: this(device, new Direct3D10.BufferDescription
			{
				Usage = Direct3D10.ResourceUsage.Default,
				BindFlags = Direct3D10.BindFlags.ConstantBuffer,
				CpuAccessFlags = Direct3D10.CpuAccessFlags.None,
				OptionFlags = Direct3D10.ResourceOptionFlags.None,
			})
		{
		}

		public ConstantBuffer(Direct3D10.Device device, Direct3D10.BufferDescription desc)
		{
			desc.SizeInBytes = Marshal.SizeOf(typeof(T));

			if (device == null)
				throw new ArgumentNullException("device");

			this._device = device;
			_device.AddReference();

			_buffer = new Direct3D10.Buffer(device, desc);
			_dataStream = new DataStream(desc.SizeInBytes, true, true);
		}

		~ConstantBuffer()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}
		void Dispose(bool disposing)
		{
			if (_device == null)
				return;

			if (disposing)
				_dataStream.Dispose();
			// NOTE: SharpDX 1.3 requires explicit Dispose() of all resource
			_device.Release();
			_buffer.Release();
			_device = null;
			_buffer = null;
		}

		public T Value
		{
			get { return bufvalue; }
			set
			{
				if (_device == null)
					throw new ObjectDisposedException(GetType().Name);

				bufvalue = value;

				Marshal.StructureToPtr(value, _dataStream.DataPointer, false);
				var dataBox = new DataBox(0, 0, _dataStream);
				_device.UpdateSubresource(dataBox, _buffer, 0);

				OnPropertyChanged("Value");
			}
		}
		T bufvalue;

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
#endregion
