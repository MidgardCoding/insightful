using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Insightful.View
{
    /// <summary>
    /// Logika interakcji dla klasy PackageCreatorWindow.xaml
    /// </summary>
    public partial class PackageCreatorWindow : Window
    {
        public PackageCreatorWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Button_SearchApp_Click(object sender, RoutedEventArgs e)
        {
            var appSearch = new OpenFileDialog();
            appSearch.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
            if (appSearch.ShowDialog() == true)
            {
                AppSearchButton.Content = appSearch.FileName;
            }
        }

        private void Button_CreatePackage_Click(object sender, RoutedEventArgs e)
        {
            if (!AreAllFieldsFilled())
            {
                MessageBox.Show(
                    "Please complete all fields before creating the package.",
                    "Missing Data",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string packageName = PackageNameTextBox.Text.Trim();
            string appName = AppNameTextBox.Text.Trim();
            string appSource = AppSearchButton.Content?.ToString()?.Trim() ?? string.Empty;

            List<ShortcutData> shortcuts = GetShortcutDataFromUi();
            List<AppNoteData> notes = GetNoteDataFromUi();

            var appData = new PackageAppData
            {
                AppTitle = appName,
                AppSrc = appSource,
                Shortcuts = shortcuts,
                AppNotes = notes
            };

            var packageData = new Dictionary<string, List<PackageAppData>>
            {
                [packageName] = new List<PackageAppData> { appData }
            };

            var saveDialog = new SaveFileDialog
            {
                Title = "Save package file",
                Filter = "Insight package (*.insight)|*.insight",
                DefaultExt = ".insight",
                AddExtension = true,
                FileName = GetSafeFileName(packageName)
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            string json = JsonSerializer.Serialize(packageData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(saveDialog.FileName, json);

            MessageBox.Show(
                "Package has been created successfully.",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Button_NewShortcut_Click(object sender, RoutedEventArgs e)
        {
            ShortcutSectionsPanel.Children.Add(CreateShortcutSectionBorder());
        }

        private void Button_NewNote_Click(object sender, RoutedEventArgs e)
        {
            NoteSectionsPanel.Children.Add(CreateNoteSectionBorder());
        }

        private void Button_RemoveShortcut_Click(object sender, RoutedEventArgs e)
        {
            RemoveSectionFromPanel(sender as FrameworkElement, ShortcutSectionsPanel, CreateShortcutSectionBorder);
        }

        private void Button_RemoveNote_Click(object sender, RoutedEventArgs e)
        {
            RemoveSectionFromPanel(sender as FrameworkElement, NoteSectionsPanel, CreateNoteSectionBorder);
        }

        private void Button_ImportPackage_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new OpenFileDialog
            {
                Title = "Import package",
                Filter = "Insight or JSON package (*.insight;*.json)|*.insight;*.json|All Files (*.*)|*.*"
            };

            if (openDialog.ShowDialog() != true)
            {
                return;
            }

            string filePath = openDialog.FileName;
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            if (!LooksLikeJson(fileContent))
            {
                MessageBox.Show(
                    "The selected file does not look like a valid JSON document.",
                    "Invalid Package File",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!fileContent.Contains("\"AppTitle\"", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "The selected file is missing required keywords, for example: AppTitle.",
                    "Invalid Package Structure",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                Dictionary<string, List<PackageAppData>>? packageData =
                    JsonSerializer.Deserialize<Dictionary<string, List<PackageAppData>>>(fileContent);

                if (packageData == null || packageData.Count == 0)
                {
                    throw new InvalidDataException("No package data found.");
                }

                KeyValuePair<string, List<PackageAppData>> selectedPackage = packageData
                    .FirstOrDefault(x => !string.Equals(x.Key, "default", StringComparison.OrdinalIgnoreCase) && x.Value != null && x.Value.Count > 0);

                if (string.IsNullOrWhiteSpace(selectedPackage.Key) || selectedPackage.Value == null || selectedPackage.Value.Count == 0)
                {
                    throw new InvalidDataException("No application entries found.");
                }

                string packageName = string.Equals(selectedPackage.Key, "apps", StringComparison.OrdinalIgnoreCase)
                    ? System.IO.Path.GetFileNameWithoutExtension(filePath)
                    : selectedPackage.Key;
                PackageAppData appData = selectedPackage.Value[0];

                if (string.IsNullOrWhiteSpace(appData.AppTitle))
                {
                    throw new InvalidDataException("Missing AppTitle.");
                }

                LoadPackageIntoUi(packageName, appData);

                MessageBox.Show(
                    "Package has been imported successfully.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Could not import this package file. Make sure it contains valid package JSON data.",
                    "Import Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool AreAllFieldsFilled()
        {
            bool isAppSourceSelected = !string.IsNullOrWhiteSpace(AppSearchButton.Content?.ToString())
                                       && !string.Equals(AppSearchButton.Content?.ToString(), "Search", StringComparison.OrdinalIgnoreCase);

            bool areShortcutSectionsFilled = AreAllSectionTextBoxesFilled(ShortcutSectionsPanel);
            bool areNoteSectionsFilled = AreAllSectionTextBoxesFilled(NoteSectionsPanel);

            return !string.IsNullOrWhiteSpace(PackageNameTextBox.Text)
                   && !string.IsNullOrWhiteSpace(AppNameTextBox.Text)
                   && isAppSourceSelected
                   && areShortcutSectionsFilled
                   && areNoteSectionsFilled;
        }

        private static bool AreAllSectionTextBoxesFilled(Panel sectionsPanel)
        {
            foreach (Border section in sectionsPanel.Children.OfType<Border>())
            {
                foreach (TextBox textBox in FindVisualChildren<TextBox>(section))
                {
                    if (string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private List<ShortcutData> GetShortcutDataFromUi()
        {
            var shortcuts = new List<ShortcutData>();

            foreach (Border section in ShortcutSectionsPanel.Children.OfType<Border>())
            {
                List<TextBox> textBoxes = FindVisualChildren<TextBox>(section).ToList();
                if (textBoxes.Count >= 2)
                {
                    shortcuts.Add(new ShortcutData
                    {
                        Name = textBoxes[0].Text.Trim(),
                        KeyCombination = textBoxes[1].Text.Trim()
                    });
                }
            }

            return shortcuts;
        }

        private List<AppNoteData> GetNoteDataFromUi()
        {
            var notes = new List<AppNoteData>();

            foreach (Border section in NoteSectionsPanel.Children.OfType<Border>())
            {
                List<TextBox> textBoxes = FindVisualChildren<TextBox>(section).ToList();
                if (textBoxes.Count >= 2)
                {
                    notes.Add(new AppNoteData
                    {
                        NoteTitle = textBoxes[0].Text.Trim(),
                        NoteContent = textBoxes[1].Text.Trim()
                    });
                }
            }

            return notes;
        }

        private Border CreateShortcutSectionBorder()
        {
            var sectionGrid = new Grid();
            sectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            sectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var namePanel = new StackPanel { Margin = new Thickness(10, 0, 10, 5) };
            namePanel.Children.Add(new TextBlock
            {
                Text = "Shortcut Name:",
                FontSize = 16,
                FontWeight = FontWeights.Light,
                Foreground = Brushes.White
            });
            namePanel.Children.Add(new TextBox { Style = (Style)FindResource("TextBoxGlass") });

            var keyPanel = new StackPanel { Margin = new Thickness(10, 0, 10, 5) };
            Grid.SetColumn(keyPanel, 1);
            keyPanel.Children.Add(new TextBlock
            {
                Text = "Shortcut Key Combination:",
                FontSize = 16,
                FontWeight = FontWeights.Light,
                Foreground = Brushes.White
            });
            keyPanel.Children.Add(new TextBox { Style = (Style)FindResource("TextBoxGlass") });

            var removeButton = new Button
            {
                Style = (Style)FindResource("SecondaryGlassButton"),
                Content = "Remove",
                Height = 22,
                Width = 90,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10, 8, 10, 0)
            };
            removeButton.Click += Button_RemoveShortcut_Click;
            Grid.SetColumnSpan(removeButton, 2);
            Grid.SetRow(removeButton, 1);

            sectionGrid.Children.Add(namePanel);
            sectionGrid.Children.Add(keyPanel);
            sectionGrid.Children.Add(removeButton);

            return new Border
            {
                Style = (Style)FindResource("NoteCardStyle"),
                Margin = new Thickness(10),
                Padding = new Thickness(10),
                Child = sectionGrid
            };
        }

        private Border CreateNoteSectionBorder()
        {
            var sectionGrid = new Grid();
            sectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            sectionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titlePanel = new StackPanel { Margin = new Thickness(10, 0, 10, 5) };
            titlePanel.Children.Add(new TextBlock
            {
                Text = "Note Title:",
                FontSize = 16,
                FontWeight = FontWeights.Light,
                Foreground = Brushes.White
            });
            titlePanel.Children.Add(new TextBox { Style = (Style)FindResource("TextBoxGlass") });

            var contentPanel = new StackPanel { Margin = new Thickness(10, 0, 10, 5) };
            Grid.SetColumn(contentPanel, 1);
            contentPanel.Children.Add(new TextBlock
            {
                Text = "Note Content:",
                FontSize = 16,
                FontWeight = FontWeights.Light,
                Foreground = Brushes.White
            });
            contentPanel.Children.Add(new TextBox { Style = (Style)FindResource("TextBoxGlass") });

            var removeButton = new Button
            {
                Style = (Style)FindResource("SecondaryGlassButton"),
                Content = "Remove",
                Height = 22,
                Width = 90,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10, 8, 10, 0)
            };
            removeButton.Click += Button_RemoveNote_Click;
            Grid.SetColumnSpan(removeButton, 2);
            Grid.SetRow(removeButton, 1);

            sectionGrid.Children.Add(titlePanel);
            sectionGrid.Children.Add(contentPanel);
            sectionGrid.Children.Add(removeButton);

            return new Border
            {
                Style = (Style)FindResource("NoteCardStyle"),
                Margin = new Thickness(10),
                Padding = new Thickness(10),
                Child = sectionGrid
            };
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
            {
                yield break;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T correctlyTyped)
                {
                    yield return correctlyTyped;
                }

                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }

        private static bool LooksLikeJson(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            string trimmed = content.Trim();
            return (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
                   || (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal));
        }

        private void LoadPackageIntoUi(string packageName, PackageAppData appData)
        {
            PackageNameTextBox.Text = packageName;
            AppNameTextBox.Text = appData.AppTitle ?? string.Empty;
            AppSearchButton.Content = string.IsNullOrWhiteSpace(appData.AppSrc) ? "Search" : appData.AppSrc;

            ShortcutSectionsPanel.Children.Clear();
            NoteSectionsPanel.Children.Clear();

            List<ShortcutData> shortcuts = appData.Shortcuts ?? new List<ShortcutData>();
            if (shortcuts.Count == 0)
            {
                shortcuts.Add(new ShortcutData());
            }

            foreach (ShortcutData shortcut in shortcuts)
            {
                Border section = CreateShortcutSectionBorder();
                SetSectionTextBoxValues(section, shortcut.Name, shortcut.KeyCombination);
                ShortcutSectionsPanel.Children.Add(section);
            }

            List<AppNoteData> notes = appData.AppNotes ?? new List<AppNoteData>();
            if (notes.Count == 0)
            {
                notes.Add(new AppNoteData());
            }

            foreach (AppNoteData note in notes)
            {
                Border section = CreateNoteSectionBorder();
                SetSectionTextBoxValues(section, note.NoteTitle, note.NoteContent);
                NoteSectionsPanel.Children.Add(section);
            }
        }

        private static void SetSectionTextBoxValues(Border section, string firstValue, string secondValue)
        {
            List<TextBox> textBoxes = FindVisualChildren<TextBox>(section).ToList();
            if (textBoxes.Count >= 2)
            {
                textBoxes[0].Text = firstValue ?? string.Empty;
                textBoxes[1].Text = secondValue ?? string.Empty;
            }
        }

        private static void RemoveSectionFromPanel(
            FrameworkElement? clickedElement,
            Panel targetPanel,
            Func<Border> createDefaultSection)
        {
            Border? border = FindParentBorder(clickedElement);
            if (border == null)
            {
                return;
            }

            targetPanel.Children.Remove(border);

            if (targetPanel.Children.Count == 0)
            {
                targetPanel.Children.Add(createDefaultSection());
            }
        }

        private static Border? FindParentBorder(DependencyObject? child)
        {
            while (child != null && child is not Border)
            {
                child = VisualTreeHelper.GetParent(child);
            }

            return child as Border;
        }

        private static string GetSafeFileName(string packageName)
        {
            foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
            {
                packageName = packageName.Replace(invalidChar, '_');
            }

            return string.IsNullOrWhiteSpace(packageName) ? "package" : packageName;
        }

        private void Button_MergePackages_Click(object sender, RoutedEventArgs e)
        {
            MergeWindow mergeWindow = new MergeWindow();
            mergeWindow.ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Package Creator makes it easy to create a data package for Insightful. Fill in the fields to create a valid file.\n\n" +
                "The Package Details specify the package name, the name of the application it describes and the path to the application.\n\n" +
                "App Shortcuts let you add information about the keyboard shortcuts supported by the app. This allows you to display actual data which you can change at any time.\n\n" +
                "App Notes (or User Notes) are notes that help you describe how an app works or what you want to do with it.\n\n" +
                "If you want to update the data in an existing package you can import it by clicking the Import Package button. This will allow you to overwrite the existing package or create a duplicate with the updated data.\n\n" +
                "When you're ready to bring your package to life click Merge Packages to link the data you've created to the main Insight Package.",
                "Package Creator - Help",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    internal sealed class PackageAppData
    {
        public string AppTitle { get; set; } = string.Empty;
        public string AppSrc { get; set; } = string.Empty;
        public List<ShortcutData> Shortcuts { get; set; } = new();
        public List<AppNoteData> AppNotes { get; set; } = new();
    }

    internal sealed class ShortcutData
    {
        public string Name { get; set; } = string.Empty;
        public string KeyCombination { get; set; } = string.Empty;
    }

    internal sealed class AppNoteData
    {
        public string NoteTitle { get; set; } = string.Empty;
        public string NoteContent { get; set; } = string.Empty;
    }
}
