using System.Windows;
using System.Windows.Input;

namespace Prepper
{
    /// <summary>
    /// Interaction logic for TvDbLogin.xaml
    /// </summary>
    public partial class TvDbLogin : Window
    {
        public string? ApiKey { get; set; }
        public string? Pin { get; set; }

        public TvDbLogin()
        {
            InitializeComponent();
        }

        private void ControlBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ApiKey = ApiKeyBox.Password;
            Pin = PinBox.Password;
            Close();
        }

        private void ApiKeyBox_KeyDown(object sender, KeyEventArgs e)
        {
            ApiKeyPromptPanel.Visibility = Visibility.Hidden;
        }

        private void ApiKeyBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ApiKeyBox.Password == null || ApiKeyBox.Password == string.Empty) ApiKeyPromptPanel.Visibility = Visibility.Visible;
        }

        private void PinBox_KeyDown(object sender, KeyEventArgs e)
        {
            PinPromptPanel.Visibility = Visibility.Hidden;
        }

        private void PinBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (PinBox.Password == null || PinBox.Password == string.Empty) PinPromptPanel.Visibility = Visibility.Visible;
        }

        private void ShowApiKeyButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ShowPinKeyButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
