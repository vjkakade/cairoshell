﻿using CairoDesktop.Interop;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CairoDesktop
{
    public partial class TaskButton
    {
        public static readonly DependencyProperty TextWidthProperty = DependencyProperty.Register("TextWidth", typeof(double), typeof(TaskButton), new PropertyMetadata(new double()));
        public static readonly DependencyProperty ListModeProperty = DependencyProperty.Register("ListMode", typeof(bool), typeof(TaskButton), new PropertyMetadata(new bool()));
        public double TextWidth
        {
            get { return (double)GetValue(TextWidthProperty); }
            set { SetValue(TextWidthProperty, value); }
        }

        public bool ListMode
        {
            get { return (bool)GetValue(ListModeProperty); }
            set { SetValue(ListModeProperty, value); }
        }

        public WindowsTasks.ApplicationWindow Window;
        private DispatcherTimer dragTimer;
        private DispatcherTimer thumbTimer;
        public TaskThumbWindow ThumbWindow;

        public TaskButton()
        {
            InitializeComponent();
        }

        public void SelectWindow()
        {
            if (Window != null)
            {
                if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) ||
                    System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift))
                {
                    Shell.StartProcess(Window.WinFileName);
                    return;
                }

                if (Window.State == WindowsTasks.ApplicationWindow.WindowState.Active)
                {
                    Window.Minimize();
                }
                else
                {
                    Window.BringToFront();
                }
            }

            closeThumb(true);
        }

        public void ConfigureContextMenu()
        {
            if (Window != null)
            {
                Visibility vis = Visibility.Collapsed;
                NativeMethods.WindowShowStyle wss = Window.ShowStyle;
                int ws = Window.WindowStyles;

                // show pin option if this app is not yet in quick launch
                if (Window.QuickLaunchAppInfo == null)
                    vis = Visibility.Visible;

                miPin.Visibility = vis;
                miPinSeparator.Visibility = vis;

                // disable window operations depending on current window state. originally tried implementing via bindings but found there is no notification we get regarding maximized state
                miMaximize.IsEnabled = (wss != NativeMethods.WindowShowStyle.ShowMaximized && (ws & (int)NativeMethods.WindowStyles.WS_MAXIMIZEBOX) != 0);
                miMinimize.IsEnabled = (wss != NativeMethods.WindowShowStyle.ShowMinimized && (ws & (int)NativeMethods.WindowStyles.WS_MINIMIZEBOX) != 0);
                miRestore.IsEnabled = (wss != NativeMethods.WindowShowStyle.ShowNormal);
                miMove.IsEnabled = wss == NativeMethods.WindowShowStyle.ShowNormal;
                miSize.IsEnabled = (wss == NativeMethods.WindowShowStyle.ShowNormal && (ws & (int)NativeMethods.WindowStyles.WS_MAXIMIZEBOX) != 0);
            }
        }

        #region Events
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Window = DataContext as WindowsTasks.ApplicationWindow;

            Window.PropertyChanged += Window_PropertyChanged;

            if (!ListMode)
            {
                switch (Configuration.Settings.Instance.TaskbarIconSize)
                {
                    case 0:
                        imgIcon.Width = 32;
                        imgIcon.Height = 32;
                        break;
                    case 10:
                        imgIcon.Width = 24;
                        imgIcon.Height = 24;
                        break;
                    default:
                        imgIcon.Width = 16;
                        imgIcon.Height = 16;
                        break;
                }

                btn.ToolTip = null;
            }
            else
            {
                // Task list display changes
                btn.Style = FindResource("CairoTaskListButtonStyle") as Style;
                ToolTipService.SetPlacement(btn, System.Windows.Controls.Primitives.PlacementMode.Right);
                WinTitle.TextAlignment = TextAlignment.Left;
                imgIcon.Margin = new Thickness(3, 0, 6, 0);
            }

            // drag support - delayed activation using system setting
            dragTimer = new DispatcherTimer { Interval = SystemParameters.MouseHoverTime };
            dragTimer.Tick += dragTimer_Tick;

            // thumbnails - delayed activation using system setting
            thumbTimer = new DispatcherTimer { Interval = SystemParameters.MouseHoverTime };
            thumbTimer.Tick += thumbTimer_Tick;
        }

        private void Window_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                // handle progress changes
                case "ProgressState":
                    pbProgress.IsIndeterminate = Window.ProgressState == NativeMethods.TBPFLAG.TBPF_INDETERMINATE;
                    break;
            }
        }

        private void btnClick(object sender, RoutedEventArgs e)
        {
            SelectWindow();
        }

        private void miRestore_Click(object sender, RoutedEventArgs e)
        {
            if (Window != null)
            {
                Window.Restore();
            }
        }

        private void miMove_Click(object sender, RoutedEventArgs e)
        {
            if (Window != null)
            {
                Window.Move();
            }
        }

        private void miSize_Click(object sender, RoutedEventArgs e)
        {
            if (Window != null)
            {
                Window.Size();
            }
        }

        private void miMinimize_Click(object sender, RoutedEventArgs e)
        {
            if (Window != null)
            {
                Window.Minimize();
            }
        }

        private void miMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (Window != null)
            {
                Window.Maximize();
            }
        }

        private void miNewWindow_Click(object sender, RoutedEventArgs e)
        {
            if (Window != null)
            {
                Shell.StartProcess(Window.WinFileName);
            }
        }

        private void miClose_Click(object sender, RoutedEventArgs e)
        {
            if (Window != null)
            {
                Window.Close();
            }
        }

        /// <summary>
        /// Handler that adjusts the visibility and usability of menu items depending on window state and app's inclusion in Quick Launch.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContextMenu_Opening(object sender, ContextMenuEventArgs e)
        {
            ConfigureContextMenu();
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            if (!IsMouseOver) closeThumb(true);
        }

        private void miPin_Click(object sender, RoutedEventArgs e)
        {
            if (Window != null)
            {
                Window.PinToQuickLaunch();
            }
        }

        private void miTaskMan_Click(object sender, RoutedEventArgs e)
        {
            Shell.StartTaskManager();
        }

        private void btn_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Middle)
            {
                if (Window != null)
                {
                    switch (Configuration.Settings.Instance.TaskbarMiddleClick)
                    {
                        case 1:
                            Window.Close();
                            break;
                        default:
                            Shell.StartProcess(Window.WinFileName);
                            break;
                    }
                }
            }
        }

        private void btn_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!ListMode)
                thumbTimer.Start();
        }

        private void btn_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!ListMode)
            {
                thumbTimer.Stop();
                closeThumb();
            }
        }
        #endregion

        #region Drag support
        private bool inDrag = false;

        private void dragTimer_Tick(object sender, EventArgs e)
        {
            if (inDrag && Window != null)
            {
                Window.BringToFront();
            }

            dragTimer.Stop();
        }

        private void btn_DragEnter(object sender, DragEventArgs e)
        {
            if (!inDrag)
            {
                inDrag = true;
                dragTimer.Start();
            }
        }

        private void btn_DragLeave(object sender, DragEventArgs e)
        {
            if (inDrag)
            {
                dragTimer.Stop();
                inDrag = false;
            }
        }
        #endregion

        #region Thumbnails
        private void thumbTimer_Tick(object sender, EventArgs e)
        {
            thumbTimer.Stop();
            if (IsMouseOver) openThumb();
        }

        private void openThumb()
        {
            if (!ListMode && ThumbWindow == null)
            {
                ThumbWindow = new TaskThumbWindow(this);
                ThumbWindow.Show();
            }
        }

        private void closeThumb(bool force = false)
        {
            thumbTimer.Stop();
            if (!ListMode && ThumbWindow != null && !btn.ContextMenu.IsOpen && (!ThumbWindow.IsMouseOver || force))
            {
                ThumbWindow.Close();
                ThumbWindow = null;
            }
        }

        public Point GetThumbnailAnchor()
        {
            Window ancestor = System.Windows.Window.GetWindow(this);
            if (ancestor != null)
            {
                var generalTransform = TransformToAncestor(ancestor);
                var anchorPoint = generalTransform.Transform(new Point(0, 0));

                anchorPoint.Y += ancestor.Top;
                anchorPoint.X += ancestor.Left;

                return anchorPoint;
            }

            return new Point(0, 0);
        }
        #endregion
    }
}