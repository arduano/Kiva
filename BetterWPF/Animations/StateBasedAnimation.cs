using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BetterWPF.Animations
{
    class StateBasedAnimation : DispatcherObject
    {
        Dictionary<string, List<AnimationSetter>> animations = new Dictionary<string, List<AnimationSetter>>();

        double fps = 60;

        public StateBasedAnimation(string[] states)
        {
            foreach(var s in states)
            {
                animations.Add(s, new List<AnimationSetter>());
            }
        }

        public void Add(string state, params AnimationSetter[] setters)
        {
            foreach(var s in setters)
            {
                animations[state].RemoveAll(cs => cs.SameProperty(s));

            }
        }
    }
}
