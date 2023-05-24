using System;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;

namespace SenseNet.ContentRepository
{
    public class ImageResizer
    {
        public static Stream CreateResizedImageFile(Stream originalStream, double x, double y, double q)
        {
            return CreateResizedImageFile(originalStream, x, y, q, false);
        }
        public static Stream CreateResizedImageFile(Stream originalStream, double x, double y, double q, SKEncodedImageFormat outputFormat)
        {
            return CreateResizedImageFile(originalStream, x, y, q, false, outputFormat);
        }
        public static Stream CreateResizedImageFile(Stream originalStream, double x, double y, double q, bool allowStretching)
        {
            return CreateResizedImageFile(originalStream, x, y, q, allowStretching, SKEncodedImageFormat.Jpeg);
        }
        public static Stream CreateResizedImageFile(Stream originalStream, double x, double y, double q, bool allowStretching, SKEncodedImageFormat outputFormat)
        {
            return CreateResizedImageFile(originalStream, x, y, q, allowStretching, outputFormat, true, SKFilterQuality.High);
        }

        public static Stream CreateResizedImageFile(Stream originalStream, double x, double y, double q,
            bool allowStretching, SKEncodedImageFormat outputFormat, bool antiAlias, SKFilterQuality filterQuality)
        {
            if (originalStream == null)
                return new MemoryStream();

            Stream stream;
            SKBitmap targetBitmap = null;
            try
            {
                using var bitmap = SKBitmap.Decode(originalStream);

                double iw = bitmap.Width;
                double ih = bitmap.Height;
                double w = 0;
                double h = 0;
                if (allowStretching)
                {
                    w = (x == 0 ? bitmap.Width : x);
                    h = (y == 0 ? bitmap.Height : y);
                }
                else
                {
                    GetRealXY(iw, ih, x, y, out w, out h);
                }

                if (w == 0 || h == 0)
                {
                    targetBitmap = new SKBitmap();
                    bitmap.CopyTo(targetBitmap);
                }
                else
                {
                    var resizeFactorX = Convert.ToSingle(w / iw);
                    var resizeFactorY = Convert.ToSingle(h / ih);

                    targetBitmap = new SKBitmap((int) Math.Round(bitmap.Width * resizeFactorX),
                        (int) Math.Round(bitmap.Height * resizeFactorY), bitmap.ColorType, bitmap.AlphaType);

                    using var paint = new SKPaint();
                    paint.IsAntialias = antiAlias;
                    paint.FilterQuality = filterQuality;

                    using var canvas = new SKCanvas(targetBitmap);
                    canvas.SetMatrix(SKMatrix.CreateScale(resizeFactorX, resizeFactorY));
                    canvas.DrawBitmap(bitmap, 0, 0, paint);
                    canvas.ResetMatrix();

                    canvas.Flush();
                }

                stream = new MemoryStream();
                var image = SKImage.FromBitmap(targetBitmap);
                var data = image.Encode(outputFormat, 90);
                data.SaveTo(stream);
            }
            finally
            {
                targetBitmap?.Dispose();
            }

            stream.Position = 0;
            return stream;
        }

        public static Stream CreateCroppedImageFile(Stream originalStream, double x, double y,
            double offsetX, double offsetY, SKEncodedImageFormat outputFormat)
        {
            if (originalStream == null)
                return new MemoryStream();

            var left = Convert.ToInt32(offsetX);
            var top = Convert.ToInt32(offsetY);
            var right = left + Convert.ToInt32(x);
            var bottom = top + Convert.ToInt32(y);
            var cropRect = new SKRectI(left, top, right, bottom);

            using var originalBitmap = SKBitmap.Decode(originalStream);
            using var croppedBitmap = new SKBitmap(cropRect.Width, cropRect.Height);
            var okay = originalBitmap.ExtractSubset(croppedBitmap, cropRect);

            using var image = SKImage.FromBitmap(croppedBitmap);
            using var data = image.Encode(outputFormat, 90);

            var resultStream = new MemoryStream();
            data.SaveTo(resultStream);

            resultStream.Position = 0;
            return resultStream;
        }

        private static void GetRealXY(double imgX, double imgY, double targetX, double targetY, out double realX, out double realY)
        {
            double xScale = targetX == 0 ? 1 : imgX / targetX;
            double yScale = targetY == 0 ? 1 : imgY / targetY;

            if (yScale < 1)
                yScale = 1;
            if (xScale < 1)
                xScale = 1;

            if (yScale > xScale)
            {
                realX = imgX * 1 / yScale;
                realY = imgY * 1 / yScale;
            }
            else // xScale > yScale
            {
                realX = imgX * 1 / xScale;
                realY = imgY * 1 / xScale;
            }


            return;
        }

        private static bool IsPortrait(long X, long Y)
        {
            return !(X > Y);
        }

        private static Dictionary<string, int> _imageType = null;
        private static readonly object _syncObj = new object();

        public static Dictionary<string, int> ImageTypes
        {
            get
            {
                if (_imageType == null)
                {
                    lock (_syncObj)
                    {
                        if (_imageType == null)
                        {
                            _imageType = new Dictionary<string, int>
                                            {
                                                {"bmp", 0},
                                                {"jpg", 1},
                                                {"jpeg", 1},
                                                {"gif", 2},
                                                {"tif", 3},
                                                {"tiff", 3},
                                                {"png", 4}
                                            };

                        }
                    }
                    
                }
                return _imageType;
            }
        }
    }
}
