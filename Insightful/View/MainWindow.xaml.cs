using Insightful.Model;
using Insightful.ViewModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Insightful.View
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            var configPath = Path.Combine(AppContext.BaseDirectory, "package.json");
            _viewModel = new MainWindowViewModel(configPath);
            DataContext = _viewModel;
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosed(e);
        }

        private void SettingsBorder_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SettingsBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEEEEE"));
            SettingsBorder.CornerRadius = new CornerRadius(4);
            SettingsBorder.Padding = new Thickness(4);
            SettingsButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
        }

        private void SettingsBorder_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SettingsBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00000000"));
            SettingsBorder.CornerRadius = new CornerRadius(0);
            SettingsBorder.Padding = new Thickness(0);
            SettingsButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EEEEEE"));
        }

        private void SettingsBorder_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            new SettingsWindow().ShowDialog();
        }

        private void Border_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            new NoteWindow(_viewModel.CurrentWindowData).ShowDialog();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}