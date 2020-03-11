using System.Globalization;
using System.IO;
using System.Text;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public abstract class TextAssetExporter
    {
        protected StringBuilder _export = new StringBuilder();
        protected NumberFormatInfo _numberFormat = new NumberFormatInfo {NumberDecimalSeparator = "."};

        public abstract void AddFragmentData(WldFragment data);

        public virtual void WriteAssetToFile(string fileName)
        {
            string directory = Path.GetDirectoryName(fileName);

            if (string.IsNullOrEmpty(directory))
            {
                return;
            }
            
            Directory.CreateDirectory(directory);
            
            File.WriteAllText(fileName, _export.ToString());
        }

        public virtual void ClearExportData()
        {
            _export.Clear();
        }
    }
}