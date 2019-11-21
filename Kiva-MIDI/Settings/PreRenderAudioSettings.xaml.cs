using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for PreRenderAudioSettings.xaml
    /// </summary>
    public partial class PreRenderAudioSettings : UserControl
    {
        public PreRenderAudioSettings()
        {
            InitializeComponent();
            ObservableCollection<dynamic> tasks = new ObservableCollection<dynamic>(new dynamic[4]);
            soundFontsList.ItemsSource = tasks;
        }
    }
}
