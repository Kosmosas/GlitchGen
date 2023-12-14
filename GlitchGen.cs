namespace GlitchGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Threading;

    internal static class Glitches
    {
        private enum Blocks
        {
            Image,
            Monochrome,
            Random,
            SingleRandom,
        }

        internal static Bitmap BlocksImage(Bitmap bitmap)
        {
            return RandomBlocks(Blocks.Image, bitmap);
        }

        internal static Bitmap BlocksMonochrome(Bitmap bitmap)
        {
            return RandomBlocks(Blocks.Monochrome, bitmap);
        }

        internal static Bitmap BlocksRandom(Bitmap bitmap)
        {
            return RandomBlocks(Blocks.Random, bitmap);
        }

        internal static Bitmap BlocksSingleRandom(Bitmap bitmap)
        {
            return RandomBlocks(Blocks.SingleRandom, bitmap);
        }

        internal static Bitmap Compress1(Bitmap bitmap)
        {
            return CompressToLevel(bitmap, 1);
        }

        internal static Bitmap Compress25(Bitmap bitmap)
        {
            return CompressToLevel(bitmap, 25);
        }

        internal static Bitmap Compress50(Bitmap bitmap)
        {
            return CompressToLevel(bitmap, 50);
        }

        internal static Bitmap Compress75(Bitmap bitmap)
        {
            return CompressToLevel(bitmap, 75);
        }

        internal static Bitmap CompressRandom(Bitmap bitmap)
        {
            return CompressToLevel(bitmap, RNG.Random.Next(1, 90));
        }

        internal static Bitmap CompressRepeat1(Bitmap bitmap)
        {
            return RepeatedCompressToLevel(bitmap, 1);
        }

        internal static Bitmap CompressRepeat25(Bitmap bitmap)
        {
            return RepeatedCompressToLevel(bitmap, 25);
        }

        internal static Bitmap CompressRepeat50(Bitmap bitmap)
        {
            return RepeatedCompressToLevel(bitmap, 50);
        }

        internal static Bitmap CompressRepeat75(Bitmap bitmap)
        {
            return RepeatedCompressToLevel(bitmap, 75);
        }

        internal static Bitmap CompressRepeatRandom(Bitmap bitmap)
        {
            return RepeatedCompressToLevel(bitmap, RNG.Random.Next(1, 90));
        }

        internal static Bitmap EndCorruptionFreeze(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            var imageCopy = new Bitmap(bitmap);

            var amount = (int)((float)RNG.Random.Next(80, 95) / 100f * bitmap.Height);
            var offset = RNG.Random.Next(-5, 6);

            for (int y = amount; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var colour = imageCopy.GetPixel(x, amount - 1);
                    var xModified = (x + (offset * (y - amount)) + bitmap.Width) % bitmap.Width;
                    graphics.FillRectangle(new SolidBrush(colour), new Rectangle(xModified, y, 1, 1));
                }
            }

            return bitmap;
        }

        internal static Bitmap EndCorruptionRandomLines(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);

            var amount = (int)((float)RNG.Random.Next(50, 95) / 100f * bitmap.Height);

            for (int y = amount; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width;)
                {
                    var amountX = RNG.Random.Next(10, 100);
                    var colour = Color.FromArgb(RNG.Random.Next(100, 200), RNG.Random.Next(255), RNG.Random.Next(255), RNG.Random.Next(255));
                    graphics.FillRectangle(new SolidBrush(colour), new Rectangle(x, y, amountX, 1));
                    x += amountX;
                }
            }

            return bitmap;
        }

        internal static Bitmap EndCorruptionStretch(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);

            var amount = (int)((float)RNG.Random.Next(50, 95) / 100f * bitmap.Height);

            for (int y = amount; y < bitmap.Height;)
            {
                var amountY = RNG.Random.Next(1, 5);
                for (int x = 0; x < bitmap.Width;)
                {
                    var amountX = RNG.Random.Next(10, 100);
                    var colour = GetAverageColour(bitmap, new Rectangle(x, y, amountX, amountY));
                    graphics.FillRectangle(new SolidBrush(colour), new Rectangle(x, y, amountX, amountY));
                    x += amountX;
                }

                y += amountY;
            }

            return bitmap;
        }

        internal static Bitmap FileCorruptBlank(Bitmap bitmap)
        {
            var duplicate = (Image)bitmap.Clone();
            var filename = CreateRandomTemporaryFilename();
            duplicate.Save(filename);
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite))
            {
                int blankLength = RNG.Random.Next(1, 10);
                long blankIndex = RNG.Random.Next((int)stream.Length - blankLength);

                for (int position = 0; position < blankLength; position++)
                {
                    stream.Position = blankIndex + position;
                    stream.WriteByte(0x00);
                }
            }

            var input = (Bitmap)Image.FromFile(filename);
            var output = new Bitmap(bitmap.Width, bitmap.Height);
            var graphics = Graphics.FromImage(output);
            graphics.DrawImage(input, 0, 0, bitmap.Width, bitmap.Height);

            return output;
        }

        internal static Bitmap FileCorruptDelete(Bitmap bitmap)
        {
            var duplicate = (Image)bitmap.Clone();
            var filename = CreateRandomTemporaryFilename();
            duplicate.Save(filename);
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite))
            {
                int skipLength = RNG.Random.Next(1, 10);
                long skipIndex = RNG.Random.Next((int)stream.Length - skipLength);

                int bufferSize;
                checked
                {
                    bufferSize = (int)(stream.Length - (skipLength + skipIndex));
                }

                byte[] buffer = new byte[bufferSize];

                // read all data after
                stream.Position = skipIndex + skipLength;
                stream.Read(buffer, 0, bufferSize);

                // write to displacement
                stream.Position = skipIndex;
                stream.Write(buffer, 0, bufferSize);
                stream.SetLength(stream.Position); // trim the file
            }

            var input = (Bitmap)Image.FromFile(filename);
            var output = new Bitmap(bitmap.Width, bitmap.Height);
            var graphics = Graphics.FromImage(output);
            graphics.DrawImage(input, 0, 0, bitmap.Width, bitmap.Height);

            return output;
        }

        internal static Bitmap FileCorruptNoise(Bitmap bitmap)
        {
            var duplicate = (Image)bitmap.Clone();
            var filename = CreateRandomTemporaryFilename();
            duplicate.Save(filename);
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite))
            {
                int noiseLength = RNG.Random.Next(1, 10);
                long noiseIndex = RNG.Random.Next((int)stream.Length - noiseLength);

                var bytes = new byte[noiseLength];
                RNG.Random.NextBytes(bytes);

                for (int position = 0; position < noiseLength; position++)
                {
                    stream.Position = noiseIndex + position;
                    stream.WriteByte(bytes[position]);
                }
            }

            var input = (Bitmap)Image.FromFile(filename);
            var output = new Bitmap(bitmap.Width, bitmap.Height);
            var graphics = Graphics.FromImage(output);
            graphics.DrawImage(input, 0, 0, bitmap.Width, bitmap.Height);

            return output;
        }

        internal static Bitmap HorizontalMirror(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            var height = RNG.Random.Next(5, 100);
            var startY = RNG.Random.Next(bitmap.Height - height);
            var imageCopy = new Bitmap(bitmap);

            for (int y = startY; y < startY + height; y++)
            {
                for (int x = 1; x <= bitmap.Width; x++)
                {
                    var colour = imageCopy.GetPixel(bitmap.Width - x, y);
                    graphics.FillRectangle(new SolidBrush(colour), new Rectangle(x, y, 1, 1));
                }
            }

            return bitmap;
        }

        internal static Bitmap HorizontalErrorBars(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            var colour = RNG.Random.NextDouble() > 0.5 ? Brushes.Black : Brushes.White;

            for (int y = RNG.Random.Next(50, 150); y < bitmap.Height + 500; y += RNG.Random.Next(100, 300))
            {
                var heightBlock = RNG.Random.Next(25, 75);
                var heightRandom = RNG.Random.Next(5, 15);
                var width = 25 * RNG.Random.Next(1, 5);
                var startY = y + (3 * heightRandom);

                graphics.FillRectangle(colour, new RectangleF(0, startY, bitmap.Width, heightBlock));
                if (RNG.Random.NextDouble() > 0.5)
                {
                    for (int x = 0; x < bitmap.Width + width; x += width)
                    {
                        var height = heightRandom * RNG.Random.Next(0, 3);
                        graphics.FillRectangle(colour, new RectangleF(x, startY - height, width, height));
                    }
                }

                if (RNG.Random.NextDouble() > 0.5)
                {
                    for (int x = 0; x < bitmap.Width + width; x += width)
                    {
                        var height = heightRandom * RNG.Random.Next(0, 3);
                        graphics.FillRectangle(colour, new RectangleF(x, startY + heightBlock, width, height));
                    }
                }
            }

            return bitmap;
        }

        internal static Bitmap HorizontalColourNoise(Bitmap bitmap)
        {
            var height = RNG.Random.Next(5, 100);
            var startY = RNG.Random.Next(bitmap.Height - height);
            return GenerateColourNoiseAtLocation(bitmap, new Rectangle(0, startY, bitmap.Width, height));
        }

        internal static Bitmap HorizontalMonochromeNoise(Bitmap bitmap)
        {
            var height = RNG.Random.Next(5, 100);
            var startY = RNG.Random.Next(bitmap.Height - height);
            return GenerateMonochromeNoiseAtLocation(bitmap, new Rectangle(0, startY, bitmap.Width, height));
        }

        internal static Bitmap HorizontalBinaryNoise(Bitmap bitmap)
        {
            var height = 16 * RNG.Random.Next(2, 6);
            var startY = RNG.Random.Next(bitmap.Height - height);
            return GenerateBinaryNoiseAtLocation(bitmap, new Rectangle(0, startY, bitmap.Width, height));
        }

        internal static Bitmap HorizontalOffsetSingle(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            var height = RNG.Random.Next(5, 100);
            var startY = RNG.Random.Next(bitmap.Height - height);
            var imageCopy = new Bitmap(bitmap);
            var offsetAmount = RNG.Random.Next(-100, 100);

            for (int y = startY; y < startY + height; y++)
            {
                for (int x = 1; x <= bitmap.Width; x++)
                {
                    var colour = imageCopy.GetPixel((x + offsetAmount + imageCopy.Width) % imageCopy.Width, y);
                    graphics.FillRectangle(new SolidBrush(colour), new Rectangle(x, y, 1, 1));
                }
            }

            return bitmap;
        }

        internal static Bitmap HorizontalOffsetAllWavey(Bitmap bitmap)
        {
            var imageCopy = new Bitmap(bitmap);
            var pn = new PerlinNoise();
            var vertical = 1;

            var z = RNG.Random.NextDouble() * 1000;
            for (int y = 0; y < bitmap.Height; y += vertical)
            {
                var p1 = bitmap.Height * pn.OctavePerlin((float)y / (float)bitmap.Height, 0, z, 1, 1);
                var p2 = bitmap.Height * pn.OctavePerlin((float)y / (float)bitmap.Height, 0, z, 10, 0.5);
                var offsetAmount = (int)((p1 + p2) - bitmap.Height);
                for (int y2 = y; y2 < y + vertical && y2 < bitmap.Height; y2++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        var colour = imageCopy.GetPixel((x + offsetAmount + bitmap.Width) % bitmap.Width, y2);
                        bitmap.SetPixel(x, y2, colour);
                    }
                }
            }

            return bitmap;
        }

        internal static Bitmap HorizontalOffsetAllRandom(Bitmap bitmap)
        {
            var imageCopy = new Bitmap(bitmap);
            var pn = new PerlinNoise();
            var vertical = RNG.Random.Next(1, 15);
            var bumpy = 0.25 + RNG.Random.NextDouble();

            var z = RNG.Random.NextDouble() * 1000;
            for (int y = 0; y < bitmap.Height; y += vertical)
            {
                var p1 = bitmap.Height * pn.OctavePerlin((float)y / (float)bitmap.Height, 0, z, 1, 1);
                var p2 = bitmap.Height * pn.OctavePerlin((float)y / (float)bitmap.Height, 0, z, 10, bumpy);
                var offsetAmount = (int)((p1 + p2) - bitmap.Height);
                for (int y2 = y; y2 < y + vertical && y2 < bitmap.Height; y2++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        var colour = imageCopy.GetPixel((x + offsetAmount + bitmap.Width) % bitmap.Width, y2);
                        bitmap.SetPixel(x, y2, colour);
                    }
                }
            }

            return bitmap;
        }

        internal static Bitmap HorizontalFreezeRandom(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            for (int y = 0; y < bitmap.Height; y++)
            {
                var x = RNG.Random.Next((int)(bitmap.Width * 0.9));
                var colour = bitmap.GetPixel(x, y);
                graphics.FillRectangle(new SolidBrush(colour), new Rectangle(x, y, bitmap.Width - x, 1));
            }

            return bitmap;
        }

        internal static Bitmap HorizontalFreezeFixed(Bitmap bitmap)
        {
            var x = (int)(bitmap.Width * 0.1) + RNG.Random.Next((int)(bitmap.Width * 0.8));
            var graphics = Graphics.FromImage(bitmap);
            for (int y = 0; y < bitmap.Height; y++)
            {
                var colour = bitmap.GetPixel(x, y);
                graphics.FillRectangle(new SolidBrush(colour), new Rectangle(x, y, bitmap.Width - x, 1));
            }

            return bitmap;
        }

        internal static Bitmap HorizontalFreezeWavesBumpy(Bitmap bitmap)
        {
            return HorizontalFrozenWaves(bitmap, isSmooth: false);
        }

        internal static Bitmap HorizontalFreezeWavesSmooth(Bitmap bitmap)
        {
            return HorizontalFrozenWaves(bitmap, isSmooth: true);
        }

        internal static Bitmap HueGradientRainbow(Bitmap bitmap, int offset)
        {
            var temp = Path.GetTempFileName();
            bitmap.Save(temp,ImageFormat.Png);

            var retBpm = new Bitmap(bitmap);
            using (Bitmap imagea = new Bitmap(temp))
            {
                using (var graphics = Graphics.FromImage(retBpm))
                {
                    //var offset = 0;// RNG.Random.Next(360);
                    var direction = RNG.Random.NextDouble() > 0.5 ? 1 : -1;
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            var randomHue = (offset + (direction * (int)(360f * ((float)x / (float)bitmap.Width)))) % 360;

                            var colour = imagea.GetPixel(x, y);
                            if (colour.A.Equals(0))
                            {
                                colour = Color.White;
                            }

                            var hsb = ColourConverter.RGBtoHSB(new ColourConverter.RGB() { R = colour.R, G = colour.G, B = colour.B });
                            hsb.H = randomHue;
                            var rgb = ColourConverter.HSBtoRGB(hsb);
                            var newColour = Color.FromArgb(rgb.R, rgb.G, rgb.B);
                            graphics.FillRectangle(new SolidBrush(newColour), new Rectangle(x, y, 1, 1));
                        }
                    }
                }
            }


            if (File.Exists(temp))
                File.Delete(temp);

            return retBpm;
        }

        internal static Bitmap HueGradientRandom(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            var start = RNG.Random.Next(360);
            var amount = RNG.Random.Next(360);
            var direction = RNG.Random.NextDouble() > 0.5 ? 1 : -1;
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var randomHue = (360 + start + (direction * (int)(amount * ((float)x / (float)bitmap.Width)))) % 360;

                    var colour = bitmap.GetPixel(x, y);
                    var hsb = ColourConverter.RGBtoHSB(new ColourConverter.RGB() { R = colour.R, G = colour.G, B = colour.B });
                    hsb.H = randomHue;
                    var rgb = ColourConverter.HSBtoRGB(hsb);
                    var newColour = Color.FromArgb(rgb.R, rgb.G, rgb.B);
                    graphics.FillRectangle(new SolidBrush(newColour), new Rectangle(x, y, 1, 1));
                }
            }

            return bitmap;
        }

        internal static Bitmap HueLuminosity(Bitmap bitmap)
        {
            var randomHueStart = RNG.Random.Next(360);
            var randomHueFinish = RNG.Random.Next(360);

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var colour = bitmap.GetPixel(x, y);
                    var average = (colour.R + colour.G + colour.B) / 3;
                    var hue = Map(average, 0, 255, randomHueStart, randomHueFinish);
                    var hsb = new ColourConverter.HSB() { H = (int)hue, B = 100, S = 100 };
                    var rgb = ColourConverter.HSBtoRGB(hsb);
                    var newColour = Color.FromArgb(rgb.R, rgb.G, rgb.B);
                    bitmap.SetPixel(x, y, newColour);
                }
            }

            return bitmap;
        }

        internal static Bitmap HueShift(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            var offset = RNG.Random.Next(10, 350);

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var colour = bitmap.GetPixel(x, y);
                    var hsb = ColourConverter.RGBtoHSB(new ColourConverter.RGB() { R = colour.R, G = colour.G, B = colour.B });
                    hsb.H += offset;
                    hsb.H %= 360;
                    var rgb = ColourConverter.HSBtoRGB(hsb);
                    var newColour = Color.FromArgb(rgb.R, rgb.G, rgb.B);
                    graphics.FillRectangle(new SolidBrush(newColour), new Rectangle(x, y, 1, 1));
                }
            }

            return bitmap;
        }

        internal static Bitmap HueUnify(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            var randomHue = RNG.Random.Next(360);

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var colour = bitmap.GetPixel(x, y);
                    var hsb = ColourConverter.RGBtoHSB(new ColourConverter.RGB() { R = colour.R, G = colour.G, B = colour.B });
                    hsb.H = randomHue;
                    var rgb = ColourConverter.HSBtoRGB(hsb);
                    var newColour = Color.FromArgb(rgb.R, rgb.G, rgb.B);
                    graphics.FillRectangle(new SolidBrush(newColour), new Rectangle(x, y, 1, 1));
                }
            }

            return bitmap;
        }

        internal static Bitmap OffsetRed(Bitmap bitmap)
        {
            return OffsetImage(bitmap, RNG.Random.Next(-100, 100), RNG.Random.Next(-100, 100), isOffsettingRed: true, isOffsettingGreen: false, isOffsettingBlue: false);
        }

        internal static Bitmap OffsetGreen(Bitmap bitmap)
        {
            return OffsetImage(bitmap, RNG.Random.Next(-100, 100), RNG.Random.Next(-100, 100), isOffsettingRed: false, isOffsettingGreen: true, isOffsettingBlue: false);
        }

        internal static Bitmap OffsetBlue(Bitmap bitmap)
        {
            return OffsetImage(bitmap, RNG.Random.Next(-100, 100), RNG.Random.Next(-100, 100), isOffsettingRed: false, isOffsettingGreen: false, isOffsettingBlue: true);
        }

        internal static Bitmap OffsetEverything(Bitmap bitmap)
        {
            return OffsetImage(bitmap, RNG.Random.Next(-100, 100), RNG.Random.Next(-100, 100), isOffsettingRed: true, isOffsettingGreen: true, isOffsettingBlue: true);
        }

        internal static Bitmap OverallBorder(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            var colour = Color.FromArgb(RNG.Random.Next(255), RNG.Random.Next(255), RNG.Random.Next(255));
            graphics.DrawRectangle(
                new Pen(colour, RNG.Random.Next(7)),
                new Rectangle(1, 1, bitmap.Width - 2, bitmap.Height - 2));
            return bitmap;
        }

        internal static Bitmap OverallInvertColours(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var colour = bitmap.GetPixel(x, y);
                    var inverted = Color.FromArgb(255 - colour.R, 255 - colour.G, 255 - colour.B);
                    graphics.FillRectangle(new SolidBrush(inverted), new Rectangle(x, y, 1, 1));
                }
            }

            return bitmap;
        }

        internal static Bitmap OverallNoise(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            var totalPixels = bitmap.Width * bitmap.Height;
            var amount = (float)RNG.Random.Next(1, 3) / 100f * (float)totalPixels;

            for (int a = 0; a < amount; a++)
            {
                var x = RNG.Random.Next(bitmap.Width);
                var y = RNG.Random.Next(bitmap.Height);
                var colour = Color.FromArgb(128, RNG.Random.Next(255), RNG.Random.Next(255), RNG.Random.Next(255));
                graphics.FillRectangle(new SolidBrush(colour), new Rectangle(x, y, RNG.Random.Next(1, 3), RNG.Random.Next(1, 3)));
            }

            return bitmap;
        }

        internal static Bitmap OverallRgbSplit(Bitmap bitmap)
        {
            var imageCopy = new Bitmap(bitmap);

            int offsetAmountRedX = RNG.Random.Next(-30, 30);
            int offsetAmountGreenX = RNG.Random.Next(-30, 30);
            int offsetAmountBlueX = RNG.Random.Next(-30, 30);
            int offsetAmountRedY = RNG.Random.Next(-30, 30);
            int offsetAmountGreenY = RNG.Random.Next(-30, 30);
            int offsetAmountBlueY = RNG.Random.Next(-30, 30);

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var offsetRedX = (x + offsetAmountRedX + bitmap.Width) % bitmap.Width;
                    var offsetRedY = (y + offsetAmountRedY + bitmap.Height) % bitmap.Height;
                    var offsetRedColour = imageCopy.GetPixel(offsetRedX, offsetRedY).R;

                    var offsetGreenX = (x + offsetAmountGreenX + bitmap.Width) % bitmap.Width;
                    var offsetGreenY = (y + offsetAmountGreenY + bitmap.Height) % bitmap.Height;
                    var offsetGreenColour = imageCopy.GetPixel(offsetGreenX, offsetGreenY).G;

                    var offsetBlueX = (x + offsetAmountBlueX + bitmap.Width) % bitmap.Width;
                    var offsetBlueY = (y + offsetAmountBlueY + bitmap.Height) % bitmap.Height;
                    var offsetBlueColour = imageCopy.GetPixel(offsetBlueX, offsetBlueY).B;

                    var rightColour = Color.FromArgb(offsetRedColour, offsetGreenColour, offsetBlueColour);
                    bitmap.SetPixel(x, y, rightColour);
                }
            }

            return bitmap;
        }

        internal static Bitmap OverallScanlines(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            for (int y = 0; y < bitmap.Height; y += 2)
            {
                graphics.DrawLine(new Pen(Color.Black), new Point(0, y), new Point(bitmap.Width, y));
            }

            return bitmap;
        }

        internal static Bitmap OverallUniformColourChange(Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            var colour = Color.FromArgb(RNG.Random.Next(20, 80), RNG.Random.Next(255), RNG.Random.Next(255), RNG.Random.Next(255));
            graphics.FillRectangle(new SolidBrush(colour), new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            return bitmap;
        }

        internal static Bitmap SaturateBright(Bitmap bitmap)
        {
            return SaturateImage(bitmap, 10);
        }

        internal static Bitmap SaturateGreyscale(Bitmap bitmap)
        {
            return SaturateImage(bitmap, 0);
        }

        internal static Bitmap SaturateRandom(Bitmap bitmap)
        {
            return SaturateImage(bitmap, RNG.Random.NextDouble() * 2);
        }

        internal static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();

            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }

        internal static string CreateRandomTemporaryFilename()
        {
            var folder = Path.GetTempPath() + Path.DirectorySeparatorChar;
            var chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var random = "gg_tmp_";
            do
            {
                random += string.Concat(chars.OrderBy(x => RNG.Random.NextDouble()).Take(8));
            }
            while (File.Exists(folder + random + ".png"));

            return folder + random + ".png";
        }

        internal static Bitmap GenerateColourNoiseAtLocation(Bitmap bitmap, Rectangle rect)
        {
            var graphics = Graphics.FromImage(bitmap);

            for (int y = rect.Y; y < rect.Y + rect.Height; y++)
            {
                for (int x = rect.X; x <= rect.X + rect.Width; x++)
                {
                    var colour = Color.FromArgb(RNG.Random.Next(255), RNG.Random.Next(255), RNG.Random.Next(255));
                    graphics.FillRectangle(new SolidBrush(colour), new Rectangle(x, y, 1, 1));
                }
            }

            return bitmap;
        }

        internal static Bitmap GenerateMonochromeNoiseAtLocation(Bitmap bitmap, Rectangle rect)
        {
            var graphics = Graphics.FromImage(bitmap);

            for (int y = rect.Y; y < rect.Y + rect.Height; y++)
            {
                for (int x = rect.X; x <= rect.X + rect.Width; x++)
                {
                    var colour = RNG.Random.NextDouble() > 0.5 ? Color.Black : Color.White;
                    graphics.FillRectangle(new SolidBrush(colour), new Rectangle(x, y, 1, 1));
                }
            }

            return bitmap;
        }

        internal static Bitmap GenerateBinaryNoiseAtLocation(Bitmap bitmap, Rectangle rect)
        {
            var graphics = Graphics.FromImage(bitmap);
            graphics.FillRectangle(Brushes.White, rect.X, rect.Y, rect.Width, rect.Height);
            for (int x = 0; x < bitmap.Width; x += 0)
            {
                var across = 32 * RNG.Random.Next(1, 3);
                var size = new[] { 4, 8, 16 }[RNG.Random.Next(3)];
                var isBlack = RNG.Random.NextDouble() > 0.5;
                var isStretched = RNG.Random.NextDouble() > 0.5;
                if (isStretched)
                {
                    for (int b = 0; b < rect.Height; b += size)
                    {
                        isBlack = !isBlack;
                        if (isBlack)
                        {
                            graphics.FillRectangle(Brushes.Black, x, rect.Y + b, across, size);
                        }
                    }
                }
                else
                {
                    for (int a = 0; a < across; a += size)
                    {
                        isBlack = !isBlack;
                        for (int b = 0; b < rect.Height; b += size)
                        {
                            isBlack = !isBlack;
                            if (isBlack)
                            {
                                graphics.FillRectangle(Brushes.Black, a + x, rect.Y + b, size, size);
                            }
                        }
                    }
                }

                x += across;
            }

            return bitmap;
        }

        internal static Bitmap GenerateMonochromeSquareNoiseAtLocation(Bitmap bitmap, Rectangle rect)
        {
            var graphics = Graphics.FromImage(bitmap);
            graphics.FillRectangle(Brushes.White, rect.X, rect.Y, rect.Width, rect.Height);
            for (int x = 0; x < bitmap.Width;)
            {
                var across = 32 * RNG.Random.Next(1, 3);
                var size = new[] { 4, 8, 16, 32 }[RNG.Random.Next(4)];
                for (int a = 0; a < across; a += size)
                {
                    for (int b = 0; b < rect.Height; b += size)
                    {
                        if (RNG.Random.NextDouble() > 0.5)
                        {
                            graphics.FillRectangle(Brushes.Black, a + x, rect.Y + b, size, size);
                        }
                    }
                }

                x += across;
            }

            return bitmap;
        }

        internal static Bitmap ReduceColourPalette(Bitmap bitmap)
        {
            var level = RNG.Random.Next(2, 17);
            var sectors = 255f / (float)level;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var r = (int)(Math.Round(bitmap.GetPixel(x, y).R / sectors) * sectors);
                    var g = (int)(Math.Round(bitmap.GetPixel(x, y).G / sectors) * sectors);
                    var b = (int)(Math.Round(bitmap.GetPixel(x, y).B / sectors) * sectors);
                    bitmap.SetPixel(x, y, Color.FromArgb(255, r, g, b));
                }
            }

            return bitmap;
        }

        private static Bitmap HorizontalFrozenWaves(Bitmap bitmap, bool isSmooth)
        {
            var graphics = Graphics.FromImage(bitmap);
            var start = RNG.Random.NextDouble() * 100;
            var interval = RNG.Random.NextDouble() * 0.01;
            var now = start;

            for (int y = 0; y < bitmap.Height; y++)
            {
                now += interval;
                var x = (int)(bitmap.Width * 0.5) + (int)(((Math.Cos(now) + 1) / 2) * bitmap.Width * 0.4);
                if (!isSmooth)
                {
                    x += RNG.Random.Next((int)(-bitmap.Width * 0.05), (int)(bitmap.Width * 0.05));
                }

                var colour = bitmap.GetPixel(x, y);
                graphics.FillRectangle(new SolidBrush(colour), new Rectangle(x, y, bitmap.Width - x, 1));
            }

            return bitmap;
        }

        private static Bitmap RandomBlocks(Blocks blocks, Bitmap bitmap)
        {
            var graphics = Graphics.FromImage(bitmap);
            var height = RNG.Random.Next(5, 40);
            var startY = RNG.Random.Next(bitmap.Height - height);

            var booSwitch = RNG.Random.NextDouble() > 0.5;
            var randomHue = RNG.Random.Next(360);
            var alpha = 255;

            if (RNG.Random.NextDouble() > 0.5)
            {
                alpha = RNG.Random.Next(50, 200);
            }

            for (int x = 0; x < bitmap.Width + 500;)
            {
                var width = RNG.Random.Next(5, 30);
                var area = new Rectangle(x, startY, width, height);
                var colour = booSwitch ? Color.White : Color.Black;

                switch (blocks)
                {
                    case Blocks.Monochrome:
                        booSwitch = !booSwitch;
                        break;

                    case Blocks.Random:
                        colour = Color.FromArgb(RNG.Random.Next(255), RNG.Random.Next(255), RNG.Random.Next(255));
                        break;

                    case Blocks.SingleRandom:
                        var hsb = new ColourConverter.HSB
                        {
                            H = randomHue,
                            B = RNG.Random.Next(50, 100),
                            S = RNG.Random.Next(100)
                        };

                        var rgb = ColourConverter.HSBtoRGB(hsb);
                        colour = Color.FromArgb(rgb.R, rgb.G, rgb.B);
                        break;

                    case Blocks.Image:
                        colour = GetAverageColour(bitmap, area);
                        break;
                }

                colour = Color.FromArgb(alpha, colour.R, colour.G, colour.B);

                graphics.FillRectangle(new SolidBrush(colour), area);

                x += width;
            }

            return bitmap;
        }

        private static Bitmap OffsetImage(Bitmap bitmap, int offsetAmountX, int offsetAmountY, bool isOffsettingRed, bool isOffsettingGreen, bool isOffsettingBlue)
        {
            var graphics = Graphics.FromImage(bitmap);
            var imageCopy = new Bitmap(bitmap);

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var originalColour = imageCopy.GetPixel(x, y);
                    var offsetX = (x + offsetAmountX + bitmap.Width) % bitmap.Width;
                    var offsetY = (y + offsetAmountY + bitmap.Height) % bitmap.Height;
                    var offsetColour = imageCopy.GetPixel(offsetX, offsetY);
                    var nextColour = Color.FromArgb(
                        isOffsettingRed ? originalColour.R : offsetColour.R,
                        isOffsettingGreen ? originalColour.G : offsetColour.G,
                        isOffsettingBlue ? originalColour.B : offsetColour.B);
                    graphics.FillRectangle(new SolidBrush(nextColour), new Rectangle(offsetX, offsetY, 1, 1));
                }
            }

            return bitmap;
        }

        private static Color GetAverageColour(Bitmap bitmap, Rectangle pixels)
        {
            var count = 0;
            int red = 0, green = 0, blue = 0;

            for (int x = 0; x < pixels.Width; x++)
            {
                for (int y = 0; y < pixels.Height; y++)
                {
                    if (pixels.X + x < bitmap.Width &&
                        pixels.Y + y < bitmap.Height)
                    {
                        count++;
                        red += bitmap.GetPixel(pixels.X + x, pixels.Y + y).R;
                        green += bitmap.GetPixel(pixels.X + x, pixels.Y + y).G;
                        blue += bitmap.GetPixel(pixels.X + x, pixels.Y + y).B;
                    }
                }
            }

            if (count > 0)
            {
                return Color.FromArgb(red / count, green / count, blue / count);
            }
            else
            {
                return Color.Transparent;
            }
        }

        private static Bitmap SaturateImage(Bitmap bitmap, double level)
        {
            var graphics = Graphics.FromImage(bitmap);

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var colour = bitmap.GetPixel(x, y);
                    var hsb = ColourConverter.RGBtoHSB(new ColourConverter.RGB() { R = colour.R, G = colour.G, B = colour.B });
                    if (hsb.S > 10)
                    {
                        hsb.S = (int)Math.Min(100, hsb.S * level);
                    }

                    var rgb = ColourConverter.HSBtoRGB(hsb);
                    var newColour = Color.FromArgb(rgb.R, rgb.G, rgb.B);
                    graphics.FillRectangle(new SolidBrush(newColour), new Rectangle(x, y, 1, 1));
                }
            }

            return bitmap;
        }

        private static Bitmap RepeatedCompressToLevel(Bitmap bitmap, int level)
        {
            var jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            var myEncoderParameters = new EncoderParameters(1);
            var folder = Path.GetTempPath() + Path.DirectorySeparatorChar.ToString();

            for (int i = 100; i > level; i--)
            {
                var filename = folder + "JpegCompress" + i.ToString() + ".jpg";
                myEncoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, i);
                bitmap.Save(filename, jpgEncoder, myEncoderParameters);
                var tempImage = new Bitmap(filename);
                bitmap = new Bitmap(tempImage);
                tempImage.Dispose();
                Thread.SpinWait(100);
                File.Delete(filename);
                Thread.SpinWait(100);
            }

            return bitmap;
        }

        private static Bitmap CompressToLevel(Bitmap bitmap, int level)
        {
            var jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            var myEncoderParameters = new EncoderParameters(1);
            myEncoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, level);
            var filename = Path.GetTempFileName();
            bitmap.Save(filename + ".jpg", jpgEncoder, myEncoderParameters);
            bitmap.Dispose();
            return new Bitmap(filename + ".jpg");
        }

        private static float Map(float s, float a1, float a2, float b1, float b2)
        {
            return b1 + ((s - a1) * (b2 - b1) / (a2 - a1));
        }
    }

    internal static class ColourConverter
    {
        internal static HSB RGBtoHSB(RGB rgb)
        {
            decimal red = rgb.R / 255m, green = rgb.G / 255m, blue = rgb.B / 255m;
            decimal minValue = Math.Min(red, Math.Min(green, blue));
            decimal maxValue = Math.Max(red, Math.Max(green, blue));
            decimal s = 0, v = maxValue;
            decimal h = (int)Color.FromArgb(rgb.R, rgb.G, rgb.B).GetHue();

            if (maxValue != 0)
            {
                s = 1m - (minValue / maxValue);
            }

            return new HSB
            {
                H = Convert.ToInt32(Math.Round(h, MidpointRounding.AwayFromZero)),
                S = Convert.ToInt32(Math.Round(s * 100, MidpointRounding.AwayFromZero)),
                B = Convert.ToInt32(Math.Round(v * 100, MidpointRounding.AwayFromZero))
            };
        }

        internal static HSL RGBtoHSL(RGB rgb)
        {
            var color = Color.FromArgb(rgb.R, rgb.G, rgb.B);
            return new HSL
            {
                H = (int)color.GetHue(),
                S = (int)(color.GetSaturation() * 100),
                L = (int)(color.GetBrightness() * 100)
            };
        }

        internal static RGB HSBtoRGB(HSB hsb)
        {
            decimal hue = hsb.H, sat = hsb.S / 100m, val = hsb.B / 100m;

            decimal r = 0, g = 0, b = 0;

            if (sat == 0)
            {
                r = val;
                g = val;
                b = val;
            }
            else
            {
                decimal sectorPos = hue / 60m;
                int sectorNumber = Convert.ToInt32(Math.Floor(sectorPos));

                decimal fractionalSector = sectorPos - sectorNumber;

                decimal p = val * (1 - sat);
                decimal q = val * (1 - (sat * fractionalSector));
                decimal t = val * (1 - (sat * (1 - fractionalSector)));

                switch (sectorNumber)
                {
                    case 0:
                    case 6:
                        r = val;
                        g = t;
                        b = p;
                        break;
                    case 1:
                        r = q;
                        g = val;
                        b = p;
                        break;
                    case 2:
                        r = p;
                        g = val;
                        b = t;
                        break;
                    case 3:
                        r = p;
                        g = q;
                        b = val;
                        break;
                    case 4:
                        r = t;
                        g = p;
                        b = val;
                        break;
                    case 5:
                        r = val;
                        g = p;
                        b = q;
                        break;
                }
            }

            return new RGB
            {
                R = Convert.ToInt32(Math.Round(r * 255, MidpointRounding.AwayFromZero)),
                G = Convert.ToInt32(Math.Round(g * 255, MidpointRounding.AwayFromZero)),
                B = Convert.ToInt32(Math.Round(b * 255, MidpointRounding.AwayFromZero))
            };
        }

        internal struct HSB
        {
            public int H;
            public int S;
            public int B;
        }

        internal struct HSL
        {
            public int H;
            public int S;
            public int L;
        }

        internal struct RGB
        {
            public int R;
            public int G;
            public int B;
        }
    }

    internal static class RNG
    {
        public static Random Random => new Random();
    }

    internal class PerlinNoise
    {
        // Hash lookup table as defined by Ken Perlin. This is a randomly arranged array of all numbers from 0-255 inclusive.
        private static readonly int[] Permutation =
        {
            151, 160, 137, 91, 90, 15,
            131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23,
            190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33,
            88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166,
            77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244,
            102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196,
            135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123,
            5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42,
            223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
            129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228,
            251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107,
            49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254,
            138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180
        };

        // Doubled permutation to avoid overflow
        private static readonly int[] P;

        private readonly int repeat;

        static PerlinNoise()
        {
            P = new int[512];
            for (int x = 0; x < 512; x++)
            {
                P[x] = Permutation[x % 256];
            }
        }

        internal PerlinNoise(int repeat = -1)
        {
            this.repeat = repeat;
        }

        internal double OctavePerlin(double x, double y, double z, int octaves, double persistence)
        {
            double total = 0;
            double frequency = 1;
            double amplitude = 1;
            double maxValue = 0;            // Used for normalizing result to 0.0 - 1.0
            for (int i = 0; i < octaves; i++)
            {
                total += this.Perlin(x * frequency, y * frequency, z * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }

        internal double Perlin(double x, double y, double z)
        {
            if (this.repeat > 0)
            {
                // If we have any repeat on, change the coordinates to their "local" repetitions
                x %= this.repeat;
                y %= this.repeat;
                z %= this.repeat;
            }

            int xi = (int)x & 255; // Calculate the "unit cube" that the point asked will be located in
            int yi = (int)y & 255; // The left bound is ( |_x_|,|_y_|,|_z_| ) and the right bound is that
            int zi = (int)z & 255; // plus 1.  Next we calculate the location (from 0.0 to 1.0) in that cube.
            double xf = x - (int)x; // We also fade the location to smooth the result.
            double yf = y - (int)y;
            double zf = z - (int)z;
            double u = Fade(xf);
            double v = Fade(yf);
            double w = Fade(zf);

            int aaa, aba, aab, abb, baa, bba, bab, bbb;
            aaa = P[P[P[xi] + yi] + zi];
            aba = P[P[P[xi] + this.Inc(yi)] + zi];
            aab = P[P[P[xi] + yi] + this.Inc(zi)];
            abb = P[P[P[xi] + this.Inc(yi)] + this.Inc(zi)];
            baa = P[P[P[this.Inc(xi)] + yi] + zi];
            bba = P[P[P[this.Inc(xi)] + this.Inc(yi)] + zi];
            bab = P[P[P[this.Inc(xi)] + yi] + this.Inc(zi)];
            bbb = P[P[P[this.Inc(xi)] + this.Inc(yi)] + this.Inc(zi)];

            double x1, x2, y1, y2;

            // The gradient function calculates the dot product between a pseudorandom gradient vector and the vector from the input coordinate to the 8 surrounding points in its unit cube.
            x1 = this.Lerp(Grad(aaa, xf, yf, zf), Grad(baa, xf - 1, yf, zf), u);

            // This is all then lerped together as a sort of weighted average based on the faded (u,v,w) values we made earlier.
            x2 = this.Lerp(Grad(aba, xf, yf - 1, zf), Grad(bba, xf - 1, yf - 1, zf), u);
            y1 = this.Lerp(x1, x2, v);
            x1 = this.Lerp(Grad(aab, xf, yf, zf - 1), Grad(bab, xf - 1, yf, zf - 1), u);
            x2 = this.Lerp(Grad(abb, xf, yf - 1, zf - 1), Grad(bbb, xf - 1, yf - 1, zf - 1), u);
            y2 = this.Lerp(x1, x2, v);

            // For convenience we bound it to 0 - 1 (theoretical min/max before is -1 - 1)
            return (this.Lerp(y1, y2, w) + 1) / 2;
        }

        private static double Grad(int hash, double x, double y, double z)
        {
            int h = hash & 15;                                    // Take the hashed value and take the first 4 bits of it (15 == 0b1111)
            double u = h < 8 /* 0b1000 */ ? x : y;                // If the most significant bit (MSB) of the hash is 0 then set u = x.  Otherwise y.
            double v;                                             // In Ken Perlin's original implementation this was another conditional operator (?:). I expanded it for readability.

            if (h < 4 /* 0b0100 */)
            {
                // If the first and second significant bits are 0 set v = y
                v = y;
            }
            else if (h == 12 /* 0b1100 */ || h == 14 /* 0b1110*/)
            {
                // If the first and second significant bits are 1 set v = x
                v = x;
            }
            else
            {
                // If the first and second significant bits are not equal (0/1, 1/0) set v = z
                v = z;
            }

            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v); // Use the last 2 bits to decide if u and v are positive or negative.  Then return their addition.
        }

        private static double Fade(double t)
        {
            // Fade function as defined by Ken Perlin.This eases coordinate values
            // so that they will "ease" towards integral values.This ends up smoothing the final output.
            return t * t * t * ((t * ((t * 6) - 15)) + 10); // 6t^5 - 15t^4 + 10t^3
        }

        private int Inc(int num)
        {
            num++;
            if (this.repeat > 0)
            {
                num %= this.repeat;
            }

            return num;
        }

        private double Lerp(double a, double b, double x)
        {
            return a + (x * (b - a));
        }
    }

    internal static class ImageHelper
    {

        /// <summary>
        /// Masks an image and returns a two color bitmap.
        /// </summary>
        /// <param name="image">The image to convert to a maskColor.</param>
        /// <param name="maskColor">The color used for masking.</param>
        /// <param name="unmaskColor">
        /// The color used when an image pixel has not a masking color.
        /// If the value is <see cref="P:Color.Empty"/> the original color
        /// will be used.
        /// </param>
        /// <param name="colorsToMask">The colors to replace with the maskColor color.</param>
        /// <returns></returns>
        internal static Bitmap GetMask(Bitmap image, Color maskColor, Color unmaskColor)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            bool isUnmaskDefined = unmaskColor != Color.Empty;
            Bitmap bitmap = new Bitmap(image);
            EqualColorPredicate equalColorPredicate = new EqualColorPredicate();

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    equalColorPredicate.Pixel = pixel;

                    if (equalColorPredicate.Matches(pixel))
                        bitmap.SetPixel(x, y, maskColor);
                    else if (isUnmaskDefined)
                        bitmap.SetPixel(x, y, unmaskColor);
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Helper predicate to check color equality.
        /// </summary>
        /// <remarks>
        /// Its not possibile to use <see cref="M:Color.Equals"/>
        /// because the methode returns <c>false</c> if one of the colors
        /// compared is a named color and the other not, even if their
        /// RGB and alpha values are the same.
        /// </remarks>
        private class EqualColorPredicate
        {
            private Color pixel;

            public EqualColorPredicate()
            {
            }

            public Color Pixel
            {
                get { return this.pixel; }
                set { this.pixel = value; }
            }

            public bool Matches(Color color)
            {
                return color.R > 128 || color.G > 128 || color.B > 128;
            }
        }

    }
}
