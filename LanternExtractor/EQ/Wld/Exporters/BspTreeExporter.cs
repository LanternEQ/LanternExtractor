using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class BspTreeExporter : TextAssetExporter
    {
        public BspTreeExporter()
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "BSP Tree");
            _export.AppendLine(LanternStrings.ExportHeaderFormat +
                               "Normal nodes: NormalX, NormalY, NormalZ, SplitDistance, LeftNodeId, RightNodeId");
            _export.AppendLine(LanternStrings.ExportHeaderFormat +
                               "Leaf nodes: BSPRegionId, RegionType");
        }

        public override void AddFragmentData(WldFragment data)
        {
            BspTree tree = data as BspTree;

            if (tree == null)
            {
                return;
            }

            foreach (var node in tree.Nodes)
            {
                if (node.Region == null)
                {
                    _export.Append(node.NormalX.ToString(_numberFormat));
                    _export.Append(node.NormalZ.ToString(_numberFormat));
                    _export.Append(node.NormalY.ToString(_numberFormat));
                    _export.Append(node.SplitDistance.ToString(_numberFormat));
                    _export.Append(node.LeftNode.ToString(_numberFormat));
                    _export.Append(node.RightNode.ToString(_numberFormat));
                }
                else
                {
                    RegionType type = node.Region.Type?.RegionType ?? RegionType.Normal;
                    _export.AppendLine(node.RegionId.ToString(_numberFormat));
                    _export.AppendLine(type.ToString());
                }
            }
        }
    }
}