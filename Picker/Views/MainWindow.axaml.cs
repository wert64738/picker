using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Picker.Views
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private CancellationTokenSource? _cancellationTokenSource;
        private bool isLiveViewLocked = false;

        public MainWindow()
        {
            InitializeComponent();
            StartLiveView();

            this.KeyDown += (sender, e) =>
            {
                if (e.Key == Avalonia.Input.Key.F)
                {
                    // Toggle live view lock on 'F' key press
                    isLiveViewLocked = !isLiveViewLocked;
                    Console.WriteLine(isLiveViewLocked ? "Live View Locked" : "Live View Unlocked");
                }
            };

            this.AddHandler(PointerPressedEvent, (sender, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    BeginMoveDrag(e);
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            base.OnClosed(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (GetCursorPos(out POINT point))
            {
                switch (e.Key)
                {
                    case Key.Up:
                        SetCursorPos(point.X, point.Y - 1);
                        break;
                    case Key.Down:
                        SetCursorPos(point.X, point.Y + 1);
                        break;
                    case Key.Left:
                        SetCursorPos(point.X - 1, point.Y);
                        break;
                    case Key.Right:
                        SetCursorPos(point.X + 1, point.Y);
                        break;
                }
            }
        }

        private void StartLiveView()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (!isLiveViewLocked && GetCursorPos(out POINT point))
                    {
                        using var bmp = new Bitmap(11, 11);
                        using var gfx = Graphics.FromImage(bmp);
                        gfx.CopyFromScreen(point.X - 5 - 2, point.Y - 5 - 2, 0, 0, new System.Drawing.Size(11, 11));


                        var centerColor = bmp.GetPixel(5, 5);

                        using var zoomedBmp = new Bitmap(44, 44);
                        using (var gfxZoom = Graphics.FromImage(zoomedBmp))
                        {
                            gfxZoom.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                            gfxZoom.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
                            gfxZoom.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                            gfxZoom.DrawImage(bmp, new Rectangle(0, 0, 44, 44), new Rectangle(0, 0, 11, 11), GraphicsUnit.Pixel);

                            using var pen = new System.Drawing.Pen(System.Drawing.Color.Red, 1)
                            {
                                Alignment = System.Drawing.Drawing2D.PenAlignment.Center
                            };

                            gfxZoom.DrawRectangle(pen, 18, 18, 3, 3);
                        }

                        var avaloniaBitmap = ConvertToAvaloniaBitmap(zoomedBmp);

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            LiveViewImage.Source = avaloniaBitmap;

                            XCoordText.Text = point.X.ToString();
                            YCoordText.Text = point.Y.ToString();

                            ColorPreviewLarge.Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(centerColor.R, centerColor.G, centerColor.B));
                            RGBValuesText.Text = $"R: {centerColor.R}, G: {centerColor.G}, B: {centerColor.B}";
                            ColorHexText.Text = $"#{centerColor.R:X2}{centerColor.G:X2}{centerColor.B:X2}";
                        });
                    }

                    await Task.Delay(32);
                }
            }, token);
        }

        private static Avalonia.Media.Imaging.Bitmap ConvertToAvaloniaBitmap(Bitmap bitmap)
        {
            using var memoryStream = new System.IO.MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
            return new Avalonia.Media.Imaging.Bitmap(memoryStream);
        }
    }
}
