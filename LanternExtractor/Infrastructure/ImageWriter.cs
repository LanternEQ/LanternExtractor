using System;
using System.IO;
using System.Text;
using LanternExtractor.Infrastructure.Logger;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Pfim;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace LanternExtractor.Infrastructure
{
    public static class ImageWriter
    {
        public static void WriteImageAsPng(byte[] bytes, string filePath, string fileName, bool isMasked,
            ILogger logger)
        {
            // https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dx-graphics-dds-pguide#dds-file-layout
            bool ddsMagic = Encoding.ASCII.GetString(bytes, 0, 4) == "DDS ";
            if (fileName.EndsWith(".bmp") && !ddsMagic)
            {
                WriteBmpAsPng(bytes, filePath, Path.GetFileNameWithoutExtension(fileName) + ".png", isMasked, false,
                    logger);
            }
            else
            {
                WriteDdsAsPng(bytes, filePath, Path.GetFileNameWithoutExtension(fileName) + ".png");
            }
        }

        /// <summary>
        /// Writes a texture from an EQG archive as a PNG. Unlike S3D textures, EQG DDS files are
        /// stored top-down and match the UV convention of the models, so they are not flipped.
        /// </summary>
        public static void WriteEqgImageAsPng(byte[] bytes, string filePath, string fileName, ILogger logger)
        {
            var pngName = Path.GetFileNameWithoutExtension(fileName) + ".png";
            bool ddsMagic = bytes.Length >= 4 && Encoding.ASCII.GetString(bytes, 0, 4) == "DDS ";

            if (ddsMagic)
            {
                WriteDdsAsPngUnflipped(bytes, filePath, pngName, logger);
                return;
            }

            using (var image = Image.FromStream(new MemoryStream(bytes)))
            {
                Directory.CreateDirectory(filePath);
                image.Save(Path.Combine(filePath, pngName), ImageFormat.Png);
            }
        }

        private static void WriteDdsAsPngUnflipped(byte[] bytes, string filePath, string fileName, ILogger logger)
        {
            using (IImage image = Pfim.Pfim.FromStream(new MemoryStream(bytes)))
            {
                int bytesPerPixel;
                switch (image.Format)
                {
                    case Pfim.ImageFormat.Rgba32:
                        bytesPerPixel = 4;
                        break;
                    case Pfim.ImageFormat.Rgb24:
                        bytesPerPixel = 3;
                        break;
                    case Pfim.ImageFormat.Rgba16:
                    case Pfim.ImageFormat.R5g6b5:
                    case Pfim.ImageFormat.R5g5b5:
                    case Pfim.ImageFormat.R5g5b5a1:
                        bytesPerPixel = 2;
                        break;
                    case Pfim.ImageFormat.Rgb8:
                        bytesPerPixel = 1;
                        break;
                    default:
                        logger.LogWarning($"ImageWriter: Unsupported DDS pixel format {image.Format} in {fileName}");
                        return;
                }

                // Copy into a 32bpp bitmap row by row, as Pfim strides are not guaranteed to suit GDI+
                using (var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb))
                {
                    var bitmapData = bitmap.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                        ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    try
                    {
                        var row = new byte[image.Width * 4];
                        for (int y = 0; y < image.Height; y++)
                        {
                            int sourceOffset = y * image.Stride;
                            for (int x = 0; x < image.Width; x++)
                            {
                                ReadBgraPixel(image.Data, sourceOffset + x * bytesPerPixel, image.Format, row,
                                    x * 4);
                            }

                            Marshal.Copy(row, 0, bitmapData.Scan0 + y * bitmapData.Stride, row.Length);
                        }
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }

                    Directory.CreateDirectory(filePath);
                    bitmap.Save(Path.Combine(filePath, fileName), ImageFormat.Png);
                }
            }
        }

        /// <summary>
        /// Decodes a single pixel from Pfim's output into 32 bit BGRA
        /// </summary>
        private static void ReadBgraPixel(byte[] data, int source, Pfim.ImageFormat format, byte[] row, int dest)
        {
            int packed = 0;
            if (format != Pfim.ImageFormat.Rgba32 && format != Pfim.ImageFormat.Rgb24 &&
                format != Pfim.ImageFormat.Rgb8)
            {
                packed = data[source] | (data[source + 1] << 8);
            }

            switch (format)
            {
                case Pfim.ImageFormat.Rgba32:
                case Pfim.ImageFormat.Rgb24:
                    row[dest] = data[source];
                    row[dest + 1] = data[source + 1];
                    row[dest + 2] = data[source + 2];
                    row[dest + 3] = format == Pfim.ImageFormat.Rgba32 ? data[source + 3] : (byte)255;
                    break;
                case Pfim.ImageFormat.Rgb8:
                    row[dest] = row[dest + 1] = row[dest + 2] = data[source];
                    row[dest + 3] = 255;
                    break;
                case Pfim.ImageFormat.Rgba16: // A4R4G4B4
                    row[dest] = (byte)((packed & 0xF) * 17);
                    row[dest + 1] = (byte)(((packed >> 4) & 0xF) * 17);
                    row[dest + 2] = (byte)(((packed >> 8) & 0xF) * 17);
                    row[dest + 3] = (byte)(((packed >> 12) & 0xF) * 17);
                    break;
                case Pfim.ImageFormat.R5g6b5:
                    row[dest] = (byte)((packed & 0x1F) << 3);
                    row[dest + 1] = (byte)(((packed >> 5) & 0x3F) << 2);
                    row[dest + 2] = (byte)(((packed >> 11) & 0x1F) << 3);
                    row[dest + 3] = 255;
                    break;
                default: // R5g5b5, R5g5b5a1
                    row[dest] = (byte)((packed & 0x1F) << 3);
                    row[dest + 1] = (byte)(((packed >> 5) & 0x1F) << 3);
                    row[dest + 2] = (byte)(((packed >> 10) & 0x1F) << 3);
                    row[dest + 3] = format == Pfim.ImageFormat.R5g5b5a1 && (packed & 0x8000) == 0
                        ? (byte)0
                        : (byte)255;
                    break;
            }
        }

        private static void WriteBmpAsPng(byte[] bytes, string filePath, string fileName, bool isMasked, bool rotate,
            ILogger logger)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            Directory.CreateDirectory(filePath);

            EqBmp image;
            var byteStream = new MemoryStream(bytes);

            try
            {
                image = new EqBmp(byteStream);
            }
            catch (Exception e)
            {
                logger.LogError("Caught exception while creating bitmap: " + e);
                return;
            }

            // The filename is misspelled in the archive
            // It only works because there is a matching canwall1.png in the objects archive
            // If we find more like this, we can create a function to fix them.
            if (fileName == "canwall1a.png")
            {
                fileName = "canwall1.png";
            }

            if (image.PixelFormat == PixelFormat.Format8bppIndexed && isMasked)
            {
                var paletteIndex = GetPaletteIndex(fileName);
                image.MakePaletteTransparent(paletteIndex);
            }
            else
            {
                image.MakeMagentaTransparent();
            }

            image.WritePng(Path.Combine(filePath, fileName));
        }

        private static void WriteDdsAsPng(byte[] bytes, string filePath, string fileName)
        {
            using (IImage image = Pfim.Pfim.FromStream(new MemoryStream(bytes)))
            {
                PixelFormat format;

                // Convert from Pfim's backend agnostic image format into GDI+'s image format
                switch (image.Format)
                {
                    case Pfim.ImageFormat.Rgba32:
                        format = PixelFormat.Format32bppArgb;
                        break;
                    default:
                        return;
                }

                // Pin pfim's data array so that it doesn't get reaped by GC, unnecessary
                // in this snippet but useful technique if the data was going to be used in
                // control like a picture box
                var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
                try
                {
                    var data = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
                    var bitmap = new Bitmap(image.Width, image.Height, image.Stride, format, data);
                    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    Directory.CreateDirectory(filePath);
                    bitmap.Save(Path.Combine(filePath, fileName), ImageFormat.Png);
                }
                finally
                {
                    handle.Free();
                }
            }
        }

        private static int GetPaletteIndex(string fileName)
        {
            switch (fileName)
            {
                case "clhe0004.png":
                case "kahe0001.png":
                    return 255;
                case "furpile1.png":
                    return 250;
                case "bearrug.png":
                    return 47;
                default:
                    return 0;
            }
        }
    }
}
