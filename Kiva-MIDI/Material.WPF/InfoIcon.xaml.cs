using BetterWPF;
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
using KivaShared;
using MessageBox = KivaShared.MessageBox;

namespace Kiva_MIDI
{
    /// <summary>
    /// Interaction logic for InfoIcon.xaml
    /// </summary>
    public partial class InfoIcon : UserControl
    {
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(InfoIcon), new PropertyMetadata(""));


        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(InfoIcon), new PropertyMetadata(""));

        public bool Visibile
        {
            get { return (bool)GetValue(VisibileProperty); }
            set { SetValue(VisibileProperty, value); }
        }

        public static readonly DependencyProperty VisibileProperty =
            DependencyProperty.Register("Visibile", typeof(bool), typeof(InfoIcon), new PropertyMetadata(true));

        public InfoIcon()
        {
            InitializeComponent();

            SetResourceReference(VisibileProperty, "ShowInfoIcon");


            new InplaceConverter(new[] { new BBinding(VisibileProperty, this) }, (b) => (bool)b[0] ? Visibility.Visible : Visibility.Hidden)
                .Set(grid, Grid.VisibilityProperty);
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(Text, Title, Window.GetWindow(this));
        }
    }
}
