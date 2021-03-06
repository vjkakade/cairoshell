﻿using CairoDesktop.Interop;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace CairoDesktop
{
    /// <summary>
    /// Interaction logic for DwmThumbnail.xaml
    /// </summary>
    public partial class DwmThumbnail : UserControl
    {
        public DwmThumbnail()
        {
            InitializeComponent();

        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {

        }

        public IntPtr Handle
        {
            get
            {
                HwndSource source = (HwndSource)HwndSource.FromVisual(this);
                IntPtr handle = source.Handle;
                return handle;
            }
        }

        private IntPtr _sourceWindowHandle = IntPtr.Zero;
        private IntPtr _thumbHandle;

        public IntPtr SourceWindowHandle
        {
            get
            {
                return _sourceWindowHandle;
            }
            set
            {
                if (_sourceWindowHandle != IntPtr.Zero)
                {
                    NativeMethods.DwmUnregisterThumbnail(_thumbHandle);
                    _thumbHandle = IntPtr.Zero;
                }

                _sourceWindowHandle = value;
                if (_sourceWindowHandle != IntPtr.Zero && NativeMethods.DwmRegisterThumbnail(Handle, _sourceWindowHandle, out _thumbHandle) == 0)
                    Refresh();
            }
        }

        public NativeMethods.Rect Rect
        {
            get
            {
                try
                {
                    if (this == null)
                        return new NativeMethods.Rect(0, 0, 0, 0);

                    Window ancestor = Window.GetWindow(this);
                    if (ancestor != null)
                    {
                        var generalTransform = this.TransformToAncestor(ancestor);
                        var leftTopPoint = generalTransform.Transform(new Point(0, 0));
                        using (var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                        {
                            return new NativeMethods.Rect(
                              (int)(leftTopPoint.X * graphics.DpiX / 96.0),
                              (int)(leftTopPoint.Y * graphics.DpiY / 96.0),
                              (int)(leftTopPoint.X * graphics.DpiX / 96.0) + (int)(ActualWidth * graphics.DpiX / 96.0),
                              (int)(leftTopPoint.Y * graphics.DpiY / 96.0) + (int)(ActualHeight * graphics.DpiY / 96.0)
                             );
                        }
                    }
                    else
                    {
                        return new NativeMethods.Rect(0, 0, 0, 0);
                    }
                }
                catch
                {
                    return new NativeMethods.Rect(0, 0, 0, 0);
                }
            }
        }

        public void Refresh()
        {
            if (this == null)
                return;

            if (_thumbHandle == IntPtr.Zero)
                return;

            if (this != null)
            {
                NativeMethods.DwmQueryThumbnailSourceSize(_thumbHandle, out NativeMethods.PSIZE size);

                var props = new NativeMethods.DWM_THUMBNAIL_PROPERTIES
                {
                    fVisible = true,
                    dwFlags = NativeMethods.DWM_TNP_VISIBLE | NativeMethods.DWM_TNP_RECTDESTINATION | NativeMethods.DWM_TNP_OPACITY,
                    opacity = 255,
                    rcDestination = Rect
                };

                if (this != null)
                    if (size.x < Rect.Width)
                        props.rcDestination.Right = props.rcDestination.Left + size.x;

                if (this != null)
                    if (size.y < Rect.Height)
                        props.rcDestination.Bottom = props.rcDestination.Top + size.y;

                if (this != null)
                    NativeMethods.DwmUpdateThumbnailProperties(_thumbHandle, ref props);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_thumbHandle != IntPtr.Zero)
            {
                NativeMethods.DwmUnregisterThumbnail(_thumbHandle);
                _thumbHandle = IntPtr.Zero;
            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Refresh();
        }

        private void UserControl_LayoutUpdated(object sender, EventArgs e)
        {
            Refresh();
        }
    }
}