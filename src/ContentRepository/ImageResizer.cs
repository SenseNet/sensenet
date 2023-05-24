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

                // if the size of the original image is the same as the specified resizing size then we just return the original stream
                if (bitmap.Width == System.Convert.ToInt32(x) && bitmap.Height == System.Convert.ToInt32(y))
                {
                    originalStream.Position = 0;
                    return originalStream;
                }

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
                    // simpler resizing
                    //var width = Convert.ToInt32(w);
                    //var height = Convert.ToInt32(h);
                    //var info = new SKImageInfo(width, height);
                    //var quality = SKFilterQuality.High;
                    //targetBitmap = bitmap.Resize(info, quality);

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

        //public static Stream CreateCropedImageFile(Stream originalStream, double x, double y, double q, ImageFormat outputFormat, SmoothingMode smoothingMode, InterpolationMode interpolationMode, PixelOffsetMode pixelOffsetMode, double verticalDiff, double horizontalDiff)
        //{


        //    if (originalStream == null)
        //        return new MemoryStream();

        //    Stream newMemoryStream;

        //    using (var originalImage = System.Drawing.Image.FromStream(originalStream))
        //    {
        //        using (var bmp = new Bitmap((int)x, (int)y))
        //        {
        //            double verticalOffset = verticalDiff;
        //            double horizontalOffset = horizontalDiff;
        //            if(horizontalDiff == double.MaxValue)
        //            {
        //                horizontalOffset = originalImage.Width - x;
        //            }else if(horizontalDiff < 0)
        //            {
        //                horizontalOffset = (originalImage.Width - x)/2;
        //            }

        //            if(horizontalOffset<0)
        //                horizontalOffset = 0;

        //            if (verticalDiff == double.MaxValue)
        //            {
        //                verticalOffset = originalImage.Height - y;
        //            }else if(verticalDiff < 0)
        //            {
        //                verticalOffset = (originalImage.Height - y)/2;
        //            }

        //            if(verticalOffset<0)
        //                verticalOffset = 0;

        //            bmp.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);
        //            using (var graphic = Graphics.FromImage(bmp))
        //            {
        //                graphic.SmoothingMode = smoothingMode;
        //                graphic.InterpolationMode = interpolationMode;
        //                graphic.PixelOffsetMode = pixelOffsetMode;
        //                graphic.DrawImage(originalImage, new Rectangle(0, 0, (int)x, (int)y), (int)horizontalOffset, (int)verticalOffset, (int)x, (int)y, GraphicsUnit.Pixel);
        //                newMemoryStream = new MemoryStream();
        //                bmp.Save(newMemoryStream, originalImage.RawFormat);
                        
        //                if(bmp != null)
        //                    bmp.Dispose();
        //            }
        //        }
        //    }
        //    newMemoryStream.Position = 0;
        //    return newMemoryStream;
        //}
        public static Stream CreateCropedImageFile(Stream originalStream, double x, double y, double q, SKEncodedImageFormat outputFormat, bool antiAlias, SKFilterQuality filterQuality, double verticalDiff, double horizontalDiff)
        {
            if (originalStream == null)
                return new MemoryStream();

            Stream newMemoryStream;

            using (var originalBitmap = SKBitmap.Decode(originalStream))
            {
var left = Convert.ToInt32(horizontalDiff);
var top = Convert.ToInt32(verticalDiff);
var right = left + Convert.ToInt32(x);
var bottom = top + Convert.ToInt32(y);

                var cropRect = new SKRectI(left, top, right, bottom);
                var croppedBitmap = new SKBitmap(cropRect.Width, cropRect.Height);
                var okay = originalBitmap.ExtractSubset(croppedBitmap, cropRect);

                var image = SKImage.FromBitmap(croppedBitmap);
                var data = image.Encode(outputFormat, 90);
                newMemoryStream = new MemoryStream();
                data.SaveTo(newMemoryStream);

                /*
                using (var bmp = new Bitmap((int)x, (int)y))
                {
                    double verticalOffset = verticalDiff;
                    double horizontalOffset = horizontalDiff;
                    if (horizontalDiff == double.MaxValue)
                    {
                        horizontalOffset = originalImage.Width - x;
                    }
                    else if (horizontalDiff < 0)
                    {
                        horizontalOffset = (originalImage.Width - x) / 2;
                    }

                    if (horizontalOffset < 0)
                        horizontalOffset = 0;

                    if (verticalDiff == double.MaxValue)
                    {
                        verticalOffset = originalImage.Height - y;
                    }
                    else if (verticalDiff < 0)
                    {
                        verticalOffset = (originalImage.Height - y) / 2;
                    }

                    if (verticalOffset < 0)
                        verticalOffset = 0;

                    bmp.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);
                    using (var graphic = Graphics.FromImage(bmp))
                    {
                        graphic.SmoothingMode = smoothingMode;
                        graphic.InterpolationMode = interpolationMode;
                        graphic.PixelOffsetMode = pixelOffsetMode;
                        graphic.DrawImage(originalImage, new Rectangle(0, 0, (int)x, (int)y), (int)horizontalOffset, (int)verticalOffset, (int)x, (int)y, GraphicsUnit.Pixel);
                        newMemoryStream = new MemoryStream();
                        bmp.Save(newMemoryStream, originalImage.RawFormat);

                        if (bmp != null)
                            bmp.Dispose();
                    }
                }
                */
            }
            newMemoryStream.Position = 0;
            return newMemoryStream;
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
