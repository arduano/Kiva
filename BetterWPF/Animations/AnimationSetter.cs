using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BetterWPF.Animations
{
    class AnimationSetter
    {
        public DependencyProperty Property { get; set; }
        public DependencyObject Source { get; set; }
        public object Value { get; set; }

        public AnimationSetter(DependencyObject src, DependencyProperty dp, object val)
        {
            Property = dp;
            Source = src;
            Value = val;
        }

        public bool SameProperty(AnimationSetter s)
        {
            return s.Property == Property && s.Source == Source;
        }
    }
}
