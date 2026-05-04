using Insightful.Model;
using System;
using System.Collections.Generic;
using System.Text;
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
    /// Logika interakcji dla klasy NoteWindow.xaml
    /// </summary>
    public partial class NoteWindow : Window
    {
        public string NoteTitle { get; set; }
        public string NoteContent { get; set; }
        public WindowData CurrentWindowData { get; set; }

        public NoteWindow(WindowData currentWindowData)
        {
            CurrentWindowData = currentWindowData;
            DataContext = this;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

}
