using Avalonia.Controls;

namespace midilonia.Views
{
    public partial class SynthList : UserControl
    {
        public SynthList()
        {
            DataContext = App.ViewModel;
            InitializeComponent();
        }
    }
}
