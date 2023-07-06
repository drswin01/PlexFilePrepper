using Ookii.Dialogs.WinForms;
using Prepper.Abstractions;
using Prepper.Controls;
using Prepper.Providers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Prepper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string TheTvDbApiKeyEnvironmentVariableName = "TvDbApiKey";
        private const string TheTvDbPinKeyEnvironmentVariableName = "TvDbPin";
        private const string TheTvDbSubscribeUrl = "https://thetvdb.com/subscribe";
        private ReferenceSelectionControl selectionControl;

        public MainViewModel ViewModel { get; set; }
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseButtonClick(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new();
            dialog.ShowDialog();
            if (Directory.Exists(dialog.SelectedPath))
            {
                ViewModel.SelectedDirectory = dialog.SelectedPath;
            }
            SelectedDirectoryPrompt.Visibility = Visibility.Hidden;
        }

        private void LoadFilesButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LoadFilesFromDirectory();
        }

        private void PrepButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PreparePrep();
            PrepPreviewWindow preview = new("None", ViewModel.CurrentPrep)
            {
                Owner = this
            };
            bool? result = preview.ShowDialog();
            if (result != null)
            {
                bool convertedResult = (bool)result;
                if (convertedResult) ViewModel.DoPrep().GetAwaiter().GetResult();
            }
            ViewModel.CurrentPrep?.Clear();
        }

        private void FileListOrPrepListScroller_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            if (e.VerticalChange == 0 && e.HorizontalChange == 0) { return; }
            if (sender == FileListScroller)
            {
                PrepListScroller.ScrollToVerticalOffset(e.VerticalOffset);
                PrepListScroller.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
            else
            {
                FileListScroller.ScrollToVerticalOffset(e.VerticalOffset);
                FileListScroller.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void FileListMoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = FileList.SelectedIndex;
            ViewModel.MoveFileUp(selectedIndex);
            FileList.SelectedIndex = selectedIndex == -1 ? selectedIndex : selectedIndex - 1;
        }

        private void FileListMoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = FileList.SelectedIndex;
            ViewModel.MoveFileDown(selectedIndex);
            FileList.SelectedIndex = selectedIndex == -1 ? selectedIndex : selectedIndex + 1;
        }

        private void PrepListMoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = PrepList.SelectedIndex;
            ViewModel.MovePrepUp(selectedIndex);
            PrepList.SelectedIndex = selectedIndex == -1 ? selectedIndex : selectedIndex - 1;
        }

        private void PrepListMoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = PrepList.SelectedIndex;
            ViewModel.MovePrepDown(selectedIndex);
            PrepList.SelectedIndex = selectedIndex == -1 ? selectedIndex : selectedIndex + 1;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ControlBox_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void SelectedDirectoryTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            SelectedDirectoryPrompt.Visibility = Visibility.Hidden;
        }

        private void SelectedDirectoryTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SelectedDirectoryTextBox.Text == null || SelectedDirectoryTextBox.Text == string.Empty)
            {
                SelectedDirectoryPrompt.Visibility = Visibility.Visible;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Get API key
            TvDbLogin login = new()
            {
                Owner = this
            };
            string? apiKeyEnv = Environment.GetEnvironmentVariable(TheTvDbApiKeyEnvironmentVariableName);
            string? pinEnv = Environment.GetEnvironmentVariable(TheTvDbPinKeyEnvironmentVariableName);
            if (apiKeyEnv == null || pinEnv == null)
            {
                this.Opacity = 0.35;
                login.ShowDialog();
                if (string.IsNullOrWhiteSpace(login.ApiKey) || string.IsNullOrWhiteSpace(login.Pin))
                {
                    if (MessageBox.Show("A TvDb v4 API key and pin is required. " +
                        $"\nWould you like to visit {TheTvDbSubscribeUrl} to buy a subscription to TheTvDb?", "TvDb API key required for Prepper",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Exclamation).Equals(MessageBoxResult.Yes))
                    {
                        Process.Start(new ProcessStartInfo(TheTvDbSubscribeUrl) { UseShellExecute = true });
                    }
                    Close();
                    return;
                }
            }
            string apiKey = apiKeyEnv ?? login.ApiKey!;
            string pin = pinEnv ?? login.Pin!;

            //Attempt to login and initialize provider
            TheTvDbProvider? tvDb = null;
            try
            {
                tvDb = TheTvDbProvider.GetProvider(apiKey, pin);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing TheTvDbProvider");
            }

            if (tvDb == null)
            {
                MessageBox.Show("Unable to get TvDbProvider, closing application");
                Close();
                return;
            }
            this.Opacity = 1;
            var providers = new List<IReferenceProvider>
            {
                tvDb
            };

            //Setting view model and defaulting controls
            ViewModel = new MainViewModel(providers);
            DataContext = ViewModel;
            selectionControl = new ReferenceSelectionControl(ViewModel.SelectedProvider);
            selectionControl.AddEpisodesToPrep += ViewModel.AddToPrep;
            ReferenceSelectionPanel.Children.Add(selectionControl);
        }

        private void FileListRemoveAllButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveAllFiles();
        }

        private void FileListRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveFile(FileList.SelectedIndex);
        }

        private void PrepListRemoveAllButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveAllPreps();
        }

        private void PrepListRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RemovePrep(PrepList.SelectedIndex);
        }

        private void ReorderListButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AutoReorderFiles();
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState.Equals(WindowState.Normal)) 
            {
                this.WindowState = WindowState.Maximized;
            }
            else 
            {
                this.WindowState = WindowState.Normal;
            }
        }
    }
}
