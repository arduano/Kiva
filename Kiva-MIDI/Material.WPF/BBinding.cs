using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace BetterWPF
{
    public class BBinding : Binding
    {
        public BBinding(TempValueProperty<object> t) : base("Val")
        {
            Source = t;
        }

        public BBinding(DependencyProperty dp, object source) : base()
        {
            Path = new PropertyPath(dp);
            Source = source;
        }
    }

    public class BBinding<T> : Binding
    {
        public BBinding(TempValueProperty<T> t) : base("Val")
        {
            Source = t;
        }
    }
}
