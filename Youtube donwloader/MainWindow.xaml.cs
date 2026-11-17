using System.Windows;
using System.Windows.Controls;

namespace Youtube_donwloader
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left && e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                if (e.ClickCount == 2)
                {
                    ToggleMaximizeRestore();
                    e.Handled = true;
                }
                else if (e.ClickCount == 1)
                {
                    DragMove();
                }
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximizeRestore();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            UpdateMaximizeGlyph();
        }

        private void ToggleMaximizeRestore()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            UpdateMaximizeGlyph();
        }

        private void UpdateMaximizeGlyph()
        {
            MaximizeGlyph.Visibility = WindowState == WindowState.Maximized ? Visibility.Collapsed : Visibility.Visible;
            RestoreGlyph.Visibility = WindowState == WindowState.Maximized ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText() && DataContext is ViewModels.MainViewModel viewModel)
            {
                viewModel.VideoUrl = Clipboard.GetText();
            }
        }

        private void Quality_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton { Tag: string format } && DataContext is ViewModels.MainViewModel viewModel)
            {
                viewModel.SelectedFormat = format;
            }
        }

        private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox { SelectedItem: ComboBoxItem { Content: string themeName } })
            {
                ThemeManager.Apply(themeName);
            }
        }

    }
}