using System;
using System.IO;
using LanternExtractor.Infrastructure.Logger;
using System.Drawing;
using System.Drawing.Imaging;

namespace LanternExtractor.Infrastructure
{
    /// <summary>
    /// Class which writes images to disk based on the shader type
    /// </summary>
    public static class ImageWriter
    {
        /// <summary>
        /// Writes bitmap data from memory to disk based on shader type
        /// </summary>
        /// <param name="bytes">The decompressed bitmap bytes</param>
        /// <param name="filePath">The output file path</param>
        /// <param name="fileName">The output file name</param>
        /// <param name="type">The type of shader (affects the output process)</param>
        /// <param name="logger">Logger for debug output</param>
        /// <param name="isMasked"></param>
        public static void WriteImage(Stream bytes, string filePath, string fileName, bool isMasked, bool rotate,
            ILogger logger)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            // Create the directory if it doesn't already exist
            Directory.CreateDirectory(filePath);

            if (bytes == null)
            {
                return;
            }

            Bitmap image;

            try
            {
                image = new Bitmap(bytes);
            }
            catch (Exception e)
            {
                logger.LogError("Caught exception while creating bitmap: " + e);
                return;
            }

            // The filename is misspelled in the archive
            // It only works because there is a matching canwall1.png in the objects archive
            if (fileName == "canwall1a.png")
            {
                fileName = "canwall1.png";
            }

            if (fileName == "it154trans.png")
            {
                
            }

            Bitmap cloneBitmap;

            if (isMasked)
            {
                cloneBitmap = image.Clone(new Rectangle(0, 0, image.Width, image.Height),
                    PixelFormat.Format8bppIndexed);

                int paletteIndex = GetPaletteIndex(fileName);
                var palette = cloneBitmap.Palette;

                Color transparencyColor = palette.Entries[paletteIndex];

                bool isUnique = false;

                // Due to a bug with the MacOS implementation of System.Drawing, setting a color palette value to
                // transparent does not work. The workaround is to ensure that the first palette value (the transparent
                // key) is unique and then use MakeTransparent(). 
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
            }
            else
            {
                cloneBitmap = image.Clone(new Rectangle(0, 0, image.Width, image.Height), PixelFormat.Format32bppArgb);
            }

            if (rotate)
            {
                cloneBitmap.RotateFlip(RotateFlipType.Rotate180FlipX);
            }

            cloneBitmap.Save(filePath + fileName, ImageFormat.Png);
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