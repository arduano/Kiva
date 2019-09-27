using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace BetterWPF
{
    class ReverseStoryboard
    {
        class FakeTimeline : AnimationTimeline
        {
            Action<double> callback;

            public FakeTimeline(Action<double> callback)
            {
                this.callback = callback;
            }

            public override Type TargetPropertyType => typeof(double);

            protected override Freezable CreateInstanceCore()
            {
                return this;
            }

            public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
            {
                callback((double)animationClock.CurrentProgress);
                return 0;
            }
        }

        List<AnimationTimeline> Animations { get; } = new List<AnimationTimeline>();
        List<AnimationTimeline> Properties { get; } = new List<AnimationTimeline>();

        Storyboard story = new Storyboard();
        FakeTimeline timeline;

        bool reversing = false;
        double forwardTimeReached = 0;
        double reverseTimeReached = 0;

        public ReverseStoryboard() 
        {
            timeline = new FakeTimeline(Render);
        }

        void Render(double time)
        {
            if (reversing)
            {
                reverseTimeReached = (1 - time) / forwardTimeReached;
                time = reverseTimeReached;
            }
            else
            {
                forwardTimeReached = time * (1 - reverseTimeReached) + reverseTimeReached;
                time = forwardTimeReached;
            }
            for(int i = 0; i < Animations.Count; i++)
            {

            }
        }

        public void Start()
        {

        }
    }
}
