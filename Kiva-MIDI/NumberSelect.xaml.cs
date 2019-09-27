using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Kiva_MIDI
{
    /// <summary>
    /// Interaction logic for NumberSelect.xaml
    /// </summary>
    public partial class NumberSelect : UserControl
    {
        public decimal Value
        { get => (decimal)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(decimal), typeof(NumberSelect), new PropertyMetadata((decimal)0, new PropertyChangedCallback(OnPropertyChange)));
        public int DecimalPoints
        { get => (int)GetValue(DecimalPointsProperty); set => SetValue(DecimalPointsProperty, value); }
        public static readonly DependencyProperty DecimalPointsProperty = DependencyProperty.Register("DecimalPoints", typeof(int), typeof(NumberSelect), new PropertyMetadata((int)0, new PropertyChangedCallback(OnPropertyChange)));
        public decimal Minimum
        { get => (decimal)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(decimal), typeof(NumberSelect), new PropertyMetadata((decimal)0, new PropertyChangedCallback(OnPropertyChange)));
        public decimal Maximum
        { get => (decimal)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(decimal), typeof(NumberSelect), new PropertyMetadata((decimal)1000, new PropertyChangedCallback(OnPropertyChange)));
        public decimal Step
        { get => (decimal)GetValue(StepProperty); set => SetValue(StepProperty, value); }
        public static readonly DependencyProperty StepProperty = DependencyProperty.Register("Step", typeof(decimal), typeof(NumberSelect), new PropertyMetadata((decimal)1));

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged", RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<decimal>), typeof(NumberSelect));

        public event RoutedPropertyChangedEventHandler<decimal> ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        private static void OnPropertyChange(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((NumberSelect)sender).UpdateValue();
        }

        string prevText = "";

        public NumberSelect()
        {
            InitializeComponent();

            this.DataContext = this;
            prevText = Value.ToString();
            textBox.Text = prevText;
        }

        void UpdateValue()
        {
            var d = Value;
            d = Decimal.Round(d, DecimalPoints);
            if (d < Minimum) d = Minimum;
            if (d > Maximum) d = Maximum;
            if (d != Value) Value = d;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                decimal _d = Convert.ToDecimal(textBox.Text);
                decimal d = Decimal.Round(_d, DecimalPoints);
                if (d < Minimum) d = Minimum;
                if (d > Maximum) d = Maximum;
                if (_d != d)
                {
                    textBox.Text = d.ToString();
                    textBox.SelectionStart = textBox.Text.Length;
                }
                else
                {
                    var old = Value;
                    Value = d;
                    try
                    {
                        RaiseEvent(new RoutedPropertyChangedEventArgs<decimal>(old, d, ValueChangedEvent));
                    }
                    catch { }
                }
            }
            catch
            {
                textBox.Text = prevText;
            }
        }

        private void TextBox_TextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var d = Value + Step;
            if (d < Minimum) d = Minimum;
            if (d > Maximum) d = Maximum;
            var old = Value;
            Value = d;
            textBox.Text = Value.ToString();
            if (old != d)
                RaiseEvent(new RoutedPropertyChangedEventArgs<decimal>(old, d, ValueChangedEvent));
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var d = Value - Step;
            if (d < Minimum) d = Minimum;
            if (d > Maximum) d = Maximum;
            var old = Value;
            Value = d;
            textBox.Text = Value.ToString();
            if (old != d)
                RaiseEvent(new RoutedPropertyChangedEventArgs<decimal>(old, d, ValueChangedEvent));
        }
    }
}
