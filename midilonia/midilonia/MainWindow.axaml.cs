using Avalonia.Controls;

namespace midilonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        DataContext = this;
        InitializeComponent();
    }
}
