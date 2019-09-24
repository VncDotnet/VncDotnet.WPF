using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using VncDotnet.Messages;

namespace VncDotnet.WPF
{
    public class VncDotnetControl : Control, IVncHandler
    {
        static VncDotnetControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VncDotnetControl), new FrameworkPropertyMetadata(typeof(VncDotnetControl)));
        }

        public RfbConnection? Connection { get; internal set; }
        public RfbConnection? PreEstablishedConnection { get; internal set; }

        private WriteableBitmap? Bitmap;
        private MonitorSnippet? Section = null;
        private int FramebufferWidth;
        private int FramebufferHeight;

        public void Start(string host, int port, string? password, CancellationToken token)
        {
            Start(host, port, password, RfbConnection.SupportedSecurityTypes, token);
        }

        public void Start(string host, int port, string? password, MonitorSnippet? section, CancellationToken token)
        {
            Start(host, port, password, RfbConnection.SupportedSecurityTypes, section, token);
        }

        public void Start(string host, int port, string? password, IEnumerable<SecurityType> securityTypes, CancellationToken token)
        {
            Start(host, port, password, securityTypes, null, token);
        }

        public void Start(string host, int port, string? password, IEnumerable<SecurityType> securityTypes, MonitorSnippet? section, CancellationToken token)
        {
            Task.Run(() => ReconnectLoop(host, port, password, securityTypes, section, token));
        }

        public async Task Attach(RfbConnection preEstablishedConnection, MonitorSnippet? section)
        {
            Section = section;
            PreEstablishedConnection = preEstablishedConnection;
            await PreEstablishedConnection.Attach(this);
        }

        private async Task ReconnectLoop(string host, int port, string? password, IEnumerable<SecurityType> securityTypes, MonitorSnippet? section, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Connection = await RfbConnection.ConnectAsync(host, port, password, securityTypes, section, token);
                    Section = section;
                    await Connection.Start();
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    Debug.WriteLine($"ReconnectLoop caught {e.Message}\n{e.StackTrace}");
                }
                await Task.Delay(1000);
            }
        }

        private int BitmapX()
        {
            if (Section != null)
            {
                return Section.X;
            }
            return 0;
        }

        private int BitmapY()
        {
            if (Section != null)
            {
                return Section.Y;
            }
            return 0;
        }

        public async Task Stop()
        {
            if (Connection != null)
            {
                Connection.Stop();
            }

            if (PreEstablishedConnection != null)
            {
                await PreEstablishedConnection.Detach(this);
            }
        }

        public void HandleFramebufferUpdate(IEnumerable<(RfbRectangleHeader, byte[])> rectangles)
        {
            Application.Current?.Dispatcher.Invoke(new Action(() =>
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                if (Bitmap == null)
                    throw new InvalidOperationException("Bitmap is null");
                using (var ctx = Bitmap.GetBitmapContext())
                {
                    Bitmap.Lock();

                    foreach ((var header, var data) in rectangles)
                    {
                        if (data != null)
                        {
                            for (ushort ry = 0; ry < header.Height; ry++)
                            {
                                int bitmapRow = ry + header.Y - BitmapY();

                                // skip rows outside the bitmap
                                if (bitmapRow < 0 || bitmapRow >= FramebufferHeight)
                                    continue;

                                // cull rows partly outside the bitmap
                                int rowLength = header.Width;

                                // left border
                                int leftSurplus = 0;
                                if (BitmapX() > header.X)
                                {
                                    int diff = BitmapX() - header.X;
                                    rowLength -= diff;
                                    leftSurplus += diff;
                                }
                                if (leftSurplus > header.Width)
                                    continue;
                                int leftPadding = 0;
                                if (header.X > BitmapX())
                                    leftPadding = header.X - BitmapX();

                                // right border
                                int rightSurplus = 0;
                                if (header.X + header.Width > BitmapX() + FramebufferWidth)
                                {
                                    rightSurplus = header.X + header.Width - BitmapX() - FramebufferWidth;
                                    rowLength -= rightSurplus;
                                }

                                // GO!
                                if (rowLength > 0)
                                {
                                    int srcOffset = ((ry * header.Width) + leftSurplus) * 4;
                                    int dstOffset = (((bitmapRow * FramebufferWidth) + leftPadding) * 4);
                                    if (dstOffset < 0 || dstOffset + (rowLength * 4) > Bitmap.PixelHeight * Bitmap.PixelWidth * 4)
                                        throw new InvalidDataException();
                                    Marshal.Copy(data,
                                        srcOffset,
                                        Bitmap.BackBuffer + dstOffset,
                                        rowLength * 4);
                                }
                            }
                        }
                    }

                    Bitmap.AddDirtyRect(new Int32Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight));
                    Bitmap.Unlock();
                }
                stopwatch.Stop();
            }));
        }

        public void HandleResolutionUpdate(int framebufferWidth, int framebufferHeight)
        {
            Application.Current?.Dispatcher.Invoke(new Action(() =>
            {
                var image = (Image)GetTemplateChild("Scene");
                if (Section != null)
                {
                    FramebufferWidth = Section.Width;
                    FramebufferHeight = Section.Height;
                    Bitmap = BitmapFactory.New(FramebufferWidth, FramebufferHeight);
                }
                else
                {
                    FramebufferWidth = framebufferWidth;
                    FramebufferHeight = framebufferHeight;
                    Bitmap = BitmapFactory.New(framebufferWidth, framebufferHeight);
                }
                image.Source = Bitmap;
            }));
        }
    }
}
