﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ParsecVDisplay.Components
{
    public partial class DisplayItem : UserControl
    {
        public bool Active { get; set; } = true;
        public string DisplayNum { get; set; } = "1";
        public string DisplayName { get; set; } = "Display [1]";
        public string DisplayPath { get; set; } = "\\\\.\\DISPLAY1";
        public string DisplayMode { get; set; } = "1920 x 1080 @ 60 Hz";

        Display Display;
        Display.ModeSet SelectedResolution;

        public DisplayItem()
        {
            InitializeComponent();

            xResolution.Items.Clear();
            xRefreshRate.Items.Clear();

            DataContext = this;
            ContextMenu.Resources = App.Current.Resources;
        }

        internal DisplayItem(Display display) : this()
        {
            Display = display;
            Active = display.Active;

            DisplayNum = $"{display.Identifier}";
            DisplayName = $"Display [{display.Identifier}]";
            DisplayPath = display.DeviceName;

            xDeg.Visibility = display.CurrentOrientation == Display.Orientation.Landscape
                ? Visibility.Hidden : Visibility.Visible;
            xDeg.Text = string.Format("{0}°", ((int)display.CurrentOrientation * 90));

            if (display.Active)
            {
                DisplayMode = display.CurrentMode.ToString();
            }
            else
            {
                DisplayMode = "[offline]";
            }
        }

        bool UpdateRefreshRates()
        {
            xRefreshRate.Items.Clear();
            var list = SelectedResolution.RefreshRates;

            bool hasDefault = false;
            MenuItem _60hz = null;

            for (int i = 0; i < list.Count; i++)
            {
                int hz = list[i];

                var mi = new MenuItem
                {
                    Header = $"{hz} Hz",
                    IsCheckable = true,
                    IsChecked = Display.CurrentMode.Hz == hz,
                };

                if (hz == 60) _60hz = mi;
                if (!hasDefault) hasDefault = mi.IsChecked;

                xRefreshRate.Items.Add(mi);
            }

            if (!hasDefault && _60hz != null)
            {
                _60hz.IsChecked = true;
                return true;
            }

            return false;
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ContextMenu.DataContext = this;
            ContextMenu.IsOpen = true;

            if (Active && SelectedResolution == null)
            {
                foreach (var res in Display.SupportedResolutions)
                {
                    bool @checked = Display.CurrentMode.Width == res.Width
                        && Display.CurrentMode.Height == res.Height;

                    if (@checked)
                        SelectedResolution = res;

                    xResolution.Items.Add(new MenuItem
                    {
                        IsCheckable = true,
                        IsChecked = @checked,
                        Header = $"{res.Width} × {res.Height}",
                    });
                }

                // Handle removed custom resolution
                if (SelectedResolution == null)
                {
                    SelectedResolution = new Display.ModeSet
                    {
                        Width = Display.CurrentMode.Width,
                        Height = Display.CurrentMode.Height,
                        RefreshRates = new List<int> { Display.CurrentMode.Hz }
                    };

                    xResolution.Items.Add(new MenuItem
                    {
                        IsCheckable = false,
                        IsChecked = true,
                        Header = $"{SelectedResolution.Width} × {SelectedResolution.Height} [UnSupported]",
                    });
                }

                UpdateRefreshRates();

                int oridentationIndex = (int)Display.CurrentOrientation;
                (xOrientation.Items[oridentationIndex] as MenuItem).IsChecked = true;
            }
        }

        private void ChangeResolution(object sender, RoutedEventArgs e)
        {
            if (Active && e.OriginalSource != null && e.OriginalSource is MenuItem)
            {
                var srcItem = e.OriginalSource as MenuItem;
                if (srcItem.Header.ToString().Contains("[UnSupported]"))
                {
                    return;
                }

                for (int i = 0; i < xResolution.Items.Count; i++)
                {
                    var item = xResolution.Items[i] as MenuItem;
                    if (item == srcItem)
                    {
                        item.IsChecked = true;
                        SelectedResolution = Display.SupportedResolutions[i];
                        int hz = UpdateRefreshRates() ? 60 : Display.CurrentMode.Hz;
                        Display.ChangeMode(SelectedResolution.Width, SelectedResolution.Height, hz, null);
                    }
                    else
                    {
                        item.IsChecked = false;
                    }
                }
            }
        }

        private void ChangeOrientation(object sender, RoutedEventArgs e)
        {
            if (Active && e.OriginalSource != null)
            {
                for (int i = 0; i < xOrientation.Items.Count; i++)
                {
                    var item = xOrientation.Items[i] as MenuItem;
                    if (item == e.OriginalSource)
                    {
                        item.IsChecked = true;
                        var orientation = (Display.Orientation)i;
                        Display.ChangeMode(null, null, null, orientation);
                    }
                    else
                    {
                        item.IsChecked = false;
                    }
                }
            }
        }

        private void ChangeRefreshRate(object sender, RoutedEventArgs e)
        {
            if (Active && e.OriginalSource != null)
            {
                for (int i = 0; i < xRefreshRate.Items.Count; i++)
                {
                    var item = xRefreshRate.Items[i] as MenuItem;
                    if (item == e.OriginalSource)
                    {
                        item.IsChecked = true;
                        int hz = SelectedResolution.RefreshRates[i];
                        Display.ChangeMode(null, null, hz, null);
                    }
                    else
                    {
                        item.IsChecked = false;
                    }
                }
            }
        }

        private void TakeScreenshot(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".png");
                Display.TakeScreenshot(path);
                Helper.ShellExec(path);
            });
        }

        private void MirrorScreen(object sender, RoutedEventArgs e)
        {
            var mirrorWindow = new MirrorWindow();
            mirrorWindow.MirrorScreen(Display.DeviceName);
            mirrorWindow.Show();
        }

        private void RemoveDisplay(object sender, RoutedEventArgs e)
        {
            Tray.Instance.Invoke(() => Tray.Instance.RemoveDisplay(Display.DisplayIndex));
        }
    }
}