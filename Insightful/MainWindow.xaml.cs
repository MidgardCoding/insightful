using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace Insightful
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Load the data and set it as the DataContext for the window
            var windowData = LoadDataFromJson("package.json");
            this.DataContext = windowData;
        }

        /// <summary>
        /// Method for loading and deserializing a JSON file.
        /// </summary>
        /// <param name="filePath">JSON path</param>
        public WindowData LoadDataFromJson(string filePath)
        {
            try
            {
                // Check if the file exists before trying to read it
                if (File.Exists(filePath))
                {
                    string jsonContent = File.ReadAllText(filePath);

                    // Deserializing the JSON file into our data model
                    return JsonConvert.DeserializeObject<WindowData>(jsonContent);
                }
                else
                {
                    // We return default data if the file is missing
                    var createNewJson = MessageBox.Show($"File '{filePath}' not found. Do you want to create a new one with default data?", "File Not Found", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (createNewJson == MessageBoxResult.Yes)
                    {
                        var defaultData = new WindowData
                        {
                            AppTitle = "Insightful HUD v1.0",
                            AppSrc = "C:\\WINDOWS\\system32\\Taskmgr.exe",
                            Shortcuts = new List<ShortcutItem>
                            {
                                new ShortcutItem { Name = "Copy", KeyCombination = "Ctrl + C" },
                                new ShortcutItem { Name = "Paste", KeyCombination = "Ctrl + V" }
                            }
                        };
                        File.WriteAllText(filePath, JsonConvert.SerializeObject(defaultData, Formatting.Indented));
                        return defaultData;
                    }
                    else
                    {
                        return new WindowData
                        {
                            AppTitle = "No provided app title",
                            Shortcuts = new List<ShortcutItem>
                            {
                                new ShortcutItem { Name = "No shortcuts available", KeyCombination = "" }
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                // Handling possible parsing errors
                return new WindowData
                {
                    AppTitle = "Error loading data",
                    Shortcuts = new List<ShortcutItem>
                    {
                        new ShortcutItem { Name = "Error", KeyCombination = ex.Message }
                    }
                };
            }
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

        }
    }

    /// <summary>
    /// A data model corresponding to the main JSON structure.
    /// </summary>
    public class WindowData
    {
        public string AppTitle { get; set; }
        public List<ShortcutItem> Shortcuts { get; set; }
        public string AppSrc { get; set; }
    }

    /// <summary>
    /// Data model for a single keyboard shortcut.
    /// </summary>
    public class ShortcutItem
    {
        public string Name { get; set; }
        public string KeyCombination { get; set; }
    }
}