using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.DXGI;

namespace SharpDX.WPF
{
	public static class DeviceUtil
	{
		public static int AdapterCount
		{
			get
			{
				if (sAdapterCount == -1)
					using (var f = new Factory())
						sAdapterCount = f.GetAdapterCount();
				return sAdapterCount;
			}
		}
		static int sAdapterCount = -1; // cache it, as the underlying code rely on Exception to find the value!!!

		public static IEnumerable<Adapter> GetAdapters(DisposeGroup dg)
		{
			// NOTE: SharpDX 1.3 requires explicit Dispose() of everything
			// hence the DisposeGroup, to enforce it
			using (var f = new Factory())
			{
				int n = AdapterCount;
				for (int i = 0; i < n; i++)
					yield return dg.Add(f.GetAdapter(i));
			}
		}

		public static Adapter GetBestAdapter(DisposeGroup dg)
		{
			Direct3D.FeatureLevel high = Direct3D.FeatureLevel.Level_9_1;
			Adapter ada = null;
			foreach (var item in GetAdapters(dg))
			{
				var level = Direct3D11.Device.GetSupportedFeatureLevel(item);
				if (ada == null || level > high)
				{
					ada = item;
					high = level;
				}
			}
			return ada;
		}

		public static SharpDX.Direct3D10.Device1 Create10(
			Direct3D10.DeviceCreationFlags cFlags = Direct3D10.DeviceCreationFlags.None
			, Direct3D10.FeatureLevel minLevel = Direct3D10.FeatureLevel.Level_9_1)
		{
			using (var dg = new DisposeGroup())
			{
				var ada = GetBestAdapter(dg);
				if (ada == null)
					return null;
				var level = Direct3D11.Device.GetSupportedFeatureLevel(ada);
				Direct3D10.FeatureLevel level10 = Direct3D10.FeatureLevel.Level_10_1;
				if (level < Direct3D.FeatureLevel.Level_10_1)
					level10 = (Direct3D10.FeatureLevel)(int)level;
				if (level10 < minLevel)
					return null;
				return new Direct3D10.Device1(ada, cFlags, level10);
			}
		}

		public static SharpDX.Direct3D11.Device Create11(
			Direct3D11.DeviceCreationFlags cFlags = Direct3D11.DeviceCreationFlags.None,
			Direct3D.FeatureLevel minLevel = Direct3D.FeatureLevel.Level_9_1
		)
		{
			using (var dg = new DisposeGroup())
			{
				var ada = GetBestAdapter(dg);
				if (ada == null)
					return null;
				var level = Direct3D11.Device.GetSupportedFeatureLevel(ada);
				if (level < minLevel)
					return null;
				return new Direct3D11.Device(ada, cFlags, level);
			}
		}
	}
}
