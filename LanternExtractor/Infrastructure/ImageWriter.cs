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

        private static void WriteBmpAsPng(byte[] bytes, string filePath, string fileName, bool isMasked, bool rotate,
            ILogger logger)
        {
            var byteStream = new MemoryStream(bytes);

            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            Directory.CreateDirectory(filePath);

            Bitmap image;

            try
            {
                image = new Bitmap(byteStream);
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

            Bitmap cloneBitmap;

            if (isMasked)
            {
                cloneBitmap = image.Clone(new Rectangle(0, 0, image.Width, image.Height),
                    PixelFormat.Format8bppIndexed);

                int paletteIndex = GetPaletteIndex(fileName);
                var palette = cloneBitmap.Palette;

                if (Environment.OSVersion.Platform != PlatformID.MacOSX &&
                    Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    palette.Entries[paletteIndex] = Color.FromArgb(0, 0, 0, 0);
                    cloneBitmap.Palette = palette;
                }
                else
                {
                    // Due to a bug with the MacOS implementation of System.Drawing, setting a color palette value to
                    // transparent does not work. The workaround is to ensure that the first palette value (the transparent
                    // key) is unique and then use MakeTransparent()
                    Color transparencyColor = palette.Entries[paletteIndex];
                    bool isUnique = false;
                    
                    while (!isUnique)
                    {
                        isUnique = true;

                        for (var i = 1; i < cloneBitmap.Palette.Entries.Length; i++)
                        {
                            Color paletteValue = cloneBitmap.Palette.Entries[i];

                            if (paletteValue == transparencyColor)
                            {
                                Random random = new Random();
                                transparencyColor = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
                                isUnique = false;
                                break;
                            }
                        }
                    }

                    palette.Entries[paletteIndex] = transparencyColor;
                    cloneBitmap.Palette = palette;
                    cloneBitmap.MakeTransparent(transparencyColor);

                    // For some reason, this now has to be done to ensure the pixels are actually set to transparent
                    // Another head scratching MacOS bug
                    for (int i = 0; i < cloneBitmap.Width; ++i)
                    {
                        for (int j = 0; j < cloneBitmap.Height; ++j)
                        {
                            if (cloneBitmap.GetPixel(i, j) == transparencyColor)
                            {
                                cloneBitmap.SetPixel(i, j, Color.FromArgb(0, 0, 0, 0));
                            }
                        }
                    }
                }
            }
            else
            {
                cloneBitmap = image.Clone(new Rectangle(0, 0, image.Width, image.Height), PixelFormat.Format32bppArgb);
                if (image.PixelFormat != PixelFormat.Format8bppIndexed)
                {
                    cloneBitmap.MakeTransparent(Color.Magenta);
                }
            }

            cloneBitmap.Save(Path.Combine(filePath, fileName), ImageFormat.Png);
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
