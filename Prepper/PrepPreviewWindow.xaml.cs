using Prepper.Abstractions;
using System.Collections.Generic;
using System.Windows;

namespace Prepper
{
    /// <summary>
    /// Interaction logic for PrepPreviewWindow.xaml
    /// </summary>
    public partial class PrepPreviewWindow : Window
    {
        private readonly List<IPrep> prepDetails;

        public PrepPreviewWindow(string prepOverviewAddedText, List<IPrep> prepDetails)
        {
            InitializeComponent();
            PrepPreviewOverviewTextBlock.Text = "Please review the prep below";
            this.prepDetails = prepDetails;
        }

        private void ContinuePrepButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void AbortPrepButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_Loaded(object sender, System.EventArgs e)
        {
            if (prepDetails == null)
            {
                DialogResult = null;
                Close();
            }
            prepDetails?.ForEach(p => PrepDetailList.Items.Add(p));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ControlBox_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
