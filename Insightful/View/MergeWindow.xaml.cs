using System.IO;
using System.Windows;
using System.Windows.Input;
using Insightful.ViewModel;

namespace Insightful.View;

public partial class MergeWindow : Window
{
    public MergeWindow()
        : this(Path.Combine(AppContext.BaseDirectory, "package.json"))
    {
    }

    public MergeWindow(string packageJsonPath)
    {
        InitializeComponent();
        DataContext = new MergeWindowViewModel(packageJsonPath, Close);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}
