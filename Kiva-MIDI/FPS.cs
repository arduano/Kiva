using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace SharpDX.WPF
{
	public class FPS : INotifyPropertyChanged
	{
		#region AveragingInterval

		public TimeSpan AveragingInterval
		{
			get { return mAveragingInterval; }
			set
			{
				if (value == mAveragingInterval)
					return;
				if (value < TimeSpan.FromSeconds(0.1))
					throw new ArgumentOutOfRangeException();
				
				mAveragingInterval = value;
				OnPropertyChanged("AveragingInterval");
			}
		}
		TimeSpan mAveragingInterval = TimeSpan.FromSeconds(1);

		#endregion

		List<TimeSpan> frames = new List<TimeSpan>();

		public void AddFrame(TimeSpan ts)
		{
			var sec = AveragingInterval;
			var index = frames.FindLastIndex(aTS => ts - aTS > sec);
			if (index > -1)
				frames.RemoveRange(0, index);
			frames.Add(ts);

			UpdateValue();
		}

		public void Clear()
		{
			frames.Clear();
			UpdateValue();
		}

		void UpdateValue()
		{
			if (frames.Count < 2)
			{
				Value = -1;
			}
			else
			{
				var dt = frames[frames.Count - 1] - frames[0];
				Value = dt.Ticks > 100 ? frames.Count / dt.TotalSeconds : -1;
			}
		}

		#region Value

		public double Value
		{
			get { return mValue; }
			private set
			{
				if (value == mValue)
					return;
				mValue = value;
				OnPropertyChanged("Value");
			}
		}
		double mValue;

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
