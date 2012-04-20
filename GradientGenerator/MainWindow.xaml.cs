using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xaml;
using Microsoft.Win32;

namespace GradientGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string currentFile = "";
        private PixelColor[,] pixels;

        public MainWindow()
        {
            InitializeComponent();
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PixelColor
        {
            public byte Blue;
            public byte Green;
            public byte Red;
            public byte Alpha;
        }

        public PixelColor[,] GetPixels(BitmapSource source)
        {
            var height = source.PixelHeight;
            var width = source.PixelWidth;
            var pixelBytes = new byte[height * width * 4];
            var pixels = new PixelColor[height, width];
            source.CopyPixels(pixelBytes, ((source as BitmapImage).PixelWidth * (source as BitmapImage).Format.BitsPerPixel + 7) / 8, 0);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y, x] = new PixelColor
                    {
                        Blue = pixelBytes[(y * width + x) * 4 + 0],
                        Green = pixelBytes[(y * width + x) * 4 + 1],
                        Red = pixelBytes[(y * width + x) * 4 + 2],
                        Alpha = pixelBytes[(y * width + x) * 4 + 3],
                    };

            return pixels;
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var fd = new OpenFileDialog();
            BitmapSource image = null;
            if (fd.ShowDialog() == true)
            {
                image = new BitmapImage(new Uri(fd.FileName));
                currentFile = System.IO.Path.GetFileNameWithoutExtension(fd.FileName);
            }
            else
            {
                return;
            }

            Image.Source = image;
            pixels = GetPixels(image);
            UpdateAll();
        }

        private void UpdateAll()
        {
            if (pixels == null)
                return;

            Column.Maximum = pixels.GetLength(1);
            int column = (int)Column.Value - 1;
            if (column == -1)
            {
                Column.Value = 0;
                column = 0;
            }

            int precision = (int)Precision.Value;

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<LinearGradientBrush x:Key=\"{0}\" EndPoint=\"0.5,1\" StartPoint=\"0.5,0\">\n", currentFile);
            int lastOffset = 0;
            for (int i = 1; i < pixels.GetLength(0); i++)
            {
                bool stillGradient = true;
                for (int j = lastOffset + 1; j <= i; j++)
                {
                    if (Math.Abs((pixels[j, column].Red - pixels[lastOffset, column].Red) * (i - lastOffset) - (pixels[i, column].Red - pixels[lastOffset, column].Red) * (j - lastOffset)) > precision)
                    {
                        stillGradient = false;
                        break;
                    }
                    if (Math.Abs((pixels[j, column].Green - pixels[lastOffset, column].Green) * (i - lastOffset) - (pixels[i, column].Green - pixels[lastOffset, column].Green) * (j - lastOffset)) > precision)
                    {
                        stillGradient = false;
                        break;
                    }
                    if (Math.Abs((pixels[j, column].Blue - pixels[lastOffset, column].Blue) * (i - lastOffset) - (pixels[i, column].Blue - pixels[lastOffset, column].Blue) * (j - lastOffset)) > precision)
                    {
                        stillGradient = false;
                        break;
                    }
                    if (Math.Abs((pixels[j, column].Alpha - pixels[lastOffset, column].Alpha) * (i - lastOffset) - (pixels[i, column].Alpha - pixels[lastOffset, column].Alpha) * (j - lastOffset)) > precision)
                    {
                        stillGradient = false;
                        break;
                    }
                }

                if (!stillGradient)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "    <GradientStop Offset=\"{0}\" Color=\"#{1:x2}{2:x2}{3:x2}{4:x2}\" />\n", lastOffset / ((float)pixels.GetLength(0) - 1), pixels[lastOffset, column].Alpha, pixels[lastOffset, column].Red, pixels[lastOffset, column].Green, pixels[lastOffset, column].Blue);

                    lastOffset = --i;
                }
            }
            sb.AppendFormat(CultureInfo.InvariantCulture, "    <GradientStop Offset=\"{0}\" Color=\"#{1:x2}{2:x2}{3:x2}{4:x2}\" />\n", lastOffset / ((float)pixels.GetLength(0) - 1), pixels[lastOffset, column].Alpha, pixels[lastOffset, column].Red, pixels[lastOffset, column].Green, pixels[lastOffset, column].Blue);
            lastOffset = pixels.GetLength(0) - 1;
            sb.AppendFormat(CultureInfo.InvariantCulture, "    <GradientStop Offset=\"{0}\" Color=\"#{1:x2}{2:x2}{3:x2}{4:x2}\" />\n", lastOffset / ((float)pixels.GetLength(0) - 1), pixels[lastOffset, column].Alpha, pixels[lastOffset, column].Red, pixels[lastOffset, column].Green, pixels[lastOffset, column].Blue);

            sb.AppendLine("</LinearGradientBrush>");

            Output.Text = sb.ToString();

            UpdateGradient();
        }

        private void UpdateGradient()
        {
            try
            {
                var sForGradient = String.Format("<Grid xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n<Grid.Background>{0}</Grid.Background>\n</Grid>", Output.Text);
                var child = XamlServices.Parse(sForGradient) as Grid;
                GradientView.Children.Clear();
                GradientView.Children.Add(child);


                var bg = child.Background as LinearGradientBrush;
                if (bg != null)
                {
                    Start.Ticks = new DoubleCollection(from x in bg.GradientStops select x.Offset);
                    End.Ticks = Start.Ticks;

                    var start = Start.Value;
                    var end = End.Value;

                    if (start > end)
                    {
                        var tmp = start;
                        start = end;
                        end = tmp;
                    }

                    var sbTopEdge = new StringBuilder();
                    var sbBottomEdge = new StringBuilder();
                    var sbCenter = new StringBuilder();
                    sbTopEdge.AppendFormat("<LinearGradientBrush x:Key=\"{0}_top_edge\" EndPoint=\"0.5,1\" StartPoint=\"0.5,0\">\n", currentFile);
                    sbBottomEdge.AppendFormat("<LinearGradientBrush x:Key=\"{0}_bottom_edge\" EndPoint=\"0.5,1\" StartPoint=\"0.5,0\">\n", currentFile);
                    sbCenter.AppendFormat("<LinearGradientBrush x:Key=\"{0}_center\" EndPoint=\"0.5,1\" StartPoint=\"0.5,0\">\n", currentFile);

                    double max1 = 0;
                    double max2 = 0;

                    foreach (var stop in bg.GradientStops)
                    {
                        if (stop.Offset < start)
                        {
                            max1 = Math.Max(max1, stop.Offset);
                        }
                        if (stop.Offset > end)
                        {
                            max2 = Math.Max(max2, 1 - stop.Offset);
                        }
                    }

                    foreach (var stop in bg.GradientStops)
                    {
                        if (stop.Offset < start)
                        {
                            if(max1 > 0)
                                sbTopEdge.AppendFormat(CultureInfo.InvariantCulture, "    <GradientStop Offset=\"{0}\" Color=\"#{1:x2}{2:x2}{3:x2}{4:x2}\" />\n", stop.Offset / max1, stop.Color.A, stop.Color.R, stop.Color.G, stop.Color.B);
                            else
                                sbTopEdge.AppendFormat(CultureInfo.InvariantCulture, "    <GradientStop Offset=\"0\" Color=\"#{0:x2}{1:x2}{2:x2}{3:x2}\" />\n    <GradientStop Offset=\"1\" Color=\"#{0:x2}{1:x2}{2:x2}{3:x2}\" />\n", stop.Color.A, stop.Color.R, stop.Color.G, stop.Color.B);
                        }
                        if (stop.Offset > end)
                        {
                            if(max2 > 0)
                                sbBottomEdge.AppendFormat(CultureInfo.InvariantCulture, "    <GradientStop Offset=\"{0}\" Color=\"#{1:x2}{2:x2}{3:x2}{4:x2}\" />\n", 1 - (1 - stop.Offset) / max2, stop.Color.A, stop.Color.R, stop.Color.G, stop.Color.B);
                            else
                                sbBottomEdge.AppendFormat(CultureInfo.InvariantCulture, "    <GradientStop Offset=\"0\" Color=\"#{0:x2}{1:x2}{2:x2}{3:x2}\" />\n    <GradientStop Offset=\"1\" Color=\"#{0:x2}{1:x2}{2:x2}{3:x2}\" />\n", stop.Color.A, stop.Color.R, stop.Color.G, stop.Color.B);
                        }
                        if (start < end && start <= stop.Offset && stop.Offset <= end)
                        {
                            sbCenter.AppendFormat(CultureInfo.InvariantCulture, "    <GradientStop Offset=\"{0}\" Color=\"#{1:x2}{2:x2}{3:x2}{4:x2}\" />\n", (stop.Offset - start) / (end - start), stop.Color.A, stop.Color.R, stop.Color.G, stop.Color.B);
                        }
                    }

                    sbTopEdge.AppendLine("</LinearGradientBrush>");
                    sbBottomEdge.AppendLine("</LinearGradientBrush>");
                    sbCenter.AppendLine("</LinearGradientBrush>");

                    OutputTopEdge.Text = sbTopEdge.ToString();
                    OutputBottomEdge.Text = sbBottomEdge.ToString();
                    OutputCenter.Text = sbCenter.ToString();

                    try
                    {
                        var sForGradientTopEdge = String.Format("<Grid xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n<Grid.Background>{0}</Grid.Background>\n</Grid>", OutputTopEdge.Text);
                        var c = XamlServices.Parse(sForGradientTopEdge) as Grid;
                        GradientViewTopEdge.Children.Clear();
                        GradientViewTopEdge.Children.Add(c);
                    }
                    catch { }

                    try
                    {
                        var sForGradientBottomEdge = String.Format("<Grid xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n<Grid.Background>{0}</Grid.Background>\n</Grid>", OutputBottomEdge.Text);
                        var c = XamlServices.Parse(sForGradientBottomEdge) as Grid;
                        GradientViewBottomEdge.Children.Clear();
                        GradientViewBottomEdge.Children.Add(c);
                    }
                    catch { }

                    try
                    {
                        var sForGradientCenter = String.Format("<Grid xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n<Grid.Background>{0}</Grid.Background>\n</Grid>", OutputCenter.Text);
                        var c = XamlServices.Parse(sForGradientCenter) as Grid;
                        GradientViewCenter.Children.Clear();
                        GradientViewCenter.Children.Add(c);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void Output_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateGradient();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateGradient();
        }

        private void Column_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateAll();
        }

        private void Precision_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdateAll();
        }

        private void GradientViewCenter_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(OutputCenter.Text);
        }

        private void GradientViewTopEdge_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(OutputTopEdge.Text);
        }

        private void GradientViewBottomEdge_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(OutputBottomEdge.Text);
        }

        private void GradientView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(Output.Text);
        }
    }
}
