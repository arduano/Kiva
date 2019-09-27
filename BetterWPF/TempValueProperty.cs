using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BetterWPF
{
    public class TempValueProperty<T> : FrameworkElement
    {
        public T Val
        {
            get { return (T)GetValue(ValProperty); }
            set { SetValue(ValProperty, value); }
        }

        public static readonly DependencyProperty ValProperty =
            DependencyProperty.Register("Val", typeof(T), typeof(TempValueProperty<T>), new PropertyMetadata(default(T)));
    }
}
