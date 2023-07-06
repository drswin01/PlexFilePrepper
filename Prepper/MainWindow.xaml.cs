using Ookii.Dialogs.WinForms;
using Prepper.Abstractions;
using Prepper.Controls;
using Prepper.Providers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

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
        private bool mRestoreIfMove = false;

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
            Application.Current.Shutdown();
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
                    Application.Current.Shutdown();
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
                Application.Current.Shutdown();
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
            SwitchWindowState();
        }

        void Window_SourceInitialized(object sender, EventArgs e)
        {
            IntPtr mWindowHandle = (new WindowInteropHelper(this)).Handle;
            HwndSource.FromHwnd(mWindowHandle).AddHook(new HwndSourceHook(WindowProc));
        }

        private static System.IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    break;
            }

            return IntPtr.Zero;
        }


        private static void WmGetMinMaxInfo(System.IntPtr hwnd, System.IntPtr lParam)
        {
            POINT lMousePosition;
            GetCursorPos(out lMousePosition);

            IntPtr lPrimaryScreen = MonitorFromPoint(new POINT(0, 0), MonitorOptions.MONITOR_DEFAULTTOPRIMARY);
            MONITORINFO lPrimaryScreenInfo = new MONITORINFO();
            if (GetMonitorInfo(lPrimaryScreen, lPrimaryScreenInfo) == false)
            {
                return;
            }

            IntPtr lCurrentScreen = MonitorFromPoint(lMousePosition, MonitorOptions.MONITOR_DEFAULTTONEAREST);

            MINMAXINFO lMmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO))!;

            if (lPrimaryScreen.Equals(lCurrentScreen) == true)
            {
                lMmi.ptMaxPosition.X = lPrimaryScreenInfo.rcWork.Left;
                lMmi.ptMaxPosition.Y = lPrimaryScreenInfo.rcWork.Top;
                lMmi.ptMaxSize.X = lPrimaryScreenInfo.rcWork.Right - lPrimaryScreenInfo.rcWork.Left;
                lMmi.ptMaxSize.Y = lPrimaryScreenInfo.rcWork.Bottom - lPrimaryScreenInfo.rcWork.Top;
            }
            else
            {
                lMmi.ptMaxPosition.X = lPrimaryScreenInfo.rcMonitor.Left;
                lMmi.ptMaxPosition.Y = lPrimaryScreenInfo.rcMonitor.Top;
                lMmi.ptMaxSize.X = lPrimaryScreenInfo.rcMonitor.Right - lPrimaryScreenInfo.rcMonitor.Left;
                lMmi.ptMaxSize.Y = lPrimaryScreenInfo.rcMonitor.Bottom - lPrimaryScreenInfo.rcMonitor.Top;
            }

            Marshal.StructureToPtr(lMmi, lParam, true);
        }

        private void SwitchWindowState()
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                    {
                        WindowState = WindowState.Maximized;
                        break;
                    }
                case WindowState.Maximized:
                    {
                        WindowState = WindowState.Normal;
                        break;
                    }
            }
        }

        private void rctHeader_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if ((ResizeMode == ResizeMode.CanResize) || (ResizeMode == ResizeMode.CanResizeWithGrip))
                {
                    SwitchWindowState();
                }

                return;
            }

            else if (WindowState == WindowState.Maximized)
            {
                mRestoreIfMove = true;
                return;
            }

            DragMove();
        }


        private void rctHeader_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mRestoreIfMove = false;
        }

        private void rctHeader_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (mRestoreIfMove)
            {
                mRestoreIfMove = false;

                double percentHorizontal = e.GetPosition(this).X / ActualWidth;
                double targetHorizontal = RestoreBounds.Width * percentHorizontal;

                double percentVertical = e.GetPosition(this).Y / ActualHeight;
                double targetVertical = RestoreBounds.Height * percentVertical;

                WindowState = WindowState.Normal;

                POINT lMousePosition;
                GetCursorPos(out lMousePosition);

                //Left = lMousePosition.X - targetHorizontal;
                //Top = lMousePosition.Y - targetVertical;

                DragMove();
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);


        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr MonitorFromPoint(POINT pt, MonitorOptions dwFlags);

        enum MonitorOptions : uint
        {
            MONITOR_DEFAULTTONULL = 0x00000000,
            MONITOR_DEFAULTTOPRIMARY = 0x00000001,
            MONITOR_DEFAULTTONEAREST = 0x00000002
        }


        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);


        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        };


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor = new RECT();
            public RECT rcWork = new RECT();
            public int dwFlags = 0;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.Left = left;
                this.Top = top;
                this.Right = right;
                this.Bottom = bottom;
            }
        }
    }
}
