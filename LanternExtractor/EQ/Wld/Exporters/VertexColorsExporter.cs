using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class VertexColorsExporter : TextAssetExporter
    {
        public VertexColorsExporter()
        {
            AddHeader();
        }

        private void AddHeader()
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Vertex Colors");
            _export.AppendLine(LanternStrings.ExportHeaderFormat +
                               "Red, Green, Blue, Sunlight");
        }

        public override void AddFragmentData(WldFragment data)
        {
            VertexColors instance = data as VertexColors;

            if (instance == null)
            {
                return;
            }

            foreach (Color color in instance.Colors)
            {
                _export.Append(color.R);
                _export.Append(",");
                _export.Append(color.G);
                _export.Append(",");
                _export.Append(color.B);
                _export.Append(",");
                _export.Append(color.A);
                _export.AppendLine();
            }
        }

        public override void ClearExportData()
        {
            base.ClearExportData();
            AddHeader();
        }
    }
}