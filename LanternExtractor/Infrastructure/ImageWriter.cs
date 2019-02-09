using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using LanternExtractor.EQ.Wld;

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
        public static void WriteImage(Stream bytes, string filePath, string fileName, ShaderType type)
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

            var image = new Bitmap(bytes);

            Bitmap cloneBitmap;

            if (type == ShaderType.MaskedDiffuse)
            {
                cloneBitmap = image.Clone(new Rectangle(0, 0, image.Width, image.Height), PixelFormat.Format8bppIndexed);
            }
            else
            {
                cloneBitmap = image.Clone(new Rectangle(0, 0, image.Width, image.Height), PixelFormat.Format32bppArgb);
            }
                
            fileName = GetExportedImageName(fileName, type);

            // Handle special cases
            if (type == ShaderType.MaskedDiffuse)
            {
                // For masked diffuse textures, the first index in the palette is the mask index.
                // We simply set it to invisible
                var palette = cloneBitmap.Palette;
        
                for (int i = 0; i < palette.Entries.Length; ++i)
                {
                    palette.Entries[0] = Color.FromArgb(0, 0, 0, 0);              
                }

                cloneBitmap.Palette = palette;
            }
            else if (type == ShaderType.Transparent)
            {
                // For transparent textures, we set it to 50% transparency - possible that this value exists somewhere
                for (int i = 0; i < cloneBitmap.Width; ++i)
                {
                    for (int j = 0; j < cloneBitmap.Height; ++j)
                    {
                        Color pixelColor = cloneBitmap.GetPixel(i, j);

                        cloneBitmap.SetPixel(i, j, Color.FromArgb(256 / 2, pixelColor.R, pixelColor.G, pixelColor.B));
                    }
                }
            }

            cloneBitmap.Save(filePath + fileName, ImageFormat.Png);
        }

        /// <summary>
        /// Gets the name under which the texture will be exported based on the shader
        /// As textures can be used in two different shaders, we prepend the name to denote the shader
        /// </summary>
        /// <param name="fileName">The image filename</param>
        /// <param name="type">The shader type</param>
        /// <returns>The corrected output name for the image</returns>
        public static string GetExportedImageName(string fileName, ShaderType type)
        {
            switch (type)
            {
                case ShaderType.Diffuse:
                    return "d_" + fileName;
                case ShaderType.Transparent:
                    return "t_" + fileName;
                case ShaderType.MaskedDiffuse:
                    return "m_" + fileName;
                case ShaderType.MaskedTransparent:
                    return "mt_" + fileName;
                case ShaderType.AlphaFromBrightness:
                    return "b_" + fileName;
                default:
                    return "nf_" + fileName;
            }
        }
    }
}