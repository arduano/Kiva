using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.DXGI;
using Direct3D11 = SharpDX.Direct3D11;
using Direct3D = SharpDX.Direct3D;

namespace Kiva_MIDI
{
	public static class DeviceUtil
	{
		public static SharpDX.Direct3D11.Device Create11(
			Direct3D11.DeviceCreationFlags cFlags = Direct3D11.DeviceCreationFlags.None,
			Direct3D.FeatureLevel minLevel = Direct3D.FeatureLevel.Level_9_1
		)
		{
			using (var dg = new DisposeGroup())
			{
				var level = Direct3D11.Device.GetSupportedFeatureLevel();
				if (level < minLevel)
					return null;
				return new Direct3D11.Device(Direct3D.DriverType.Hardware, cFlags, level);
			}
		}
	}
}
