using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class BspTreeWriter : TextAssetWriter
    {
        public BspTreeWriter()
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
                    _export.Append(",");
                    _export.Append(node.NormalZ.ToString(_numberFormat));
                    _export.Append(",");
                    _export.Append(node.NormalY.ToString(_numberFormat));
                    _export.Append(",");
                    _export.Append(node.SplitDistance.ToString(_numberFormat));
                    _export.Append(",");
                    _export.Append(node.LeftNode.ToString(_numberFormat));
                    _export.Append(",");
                    _export.Append(node.RightNode.ToString(_numberFormat));
                    _export.AppendLine();


                    if (node.RightNode == -1 && node.LeftNode == -1)
                    {
                        
                    }
                }
                else
                {
                    RegionType type = RegionType.Normal;

                    if (node.Region.Type != null)
                    {
                        type = node.Region.Type.RegionType;
                    }
                    
                    _export.Append(node.RegionId.ToString(_numberFormat));
                    _export.Append(",");
                    _export.Append(type.ToString());

                    if (type != RegionType.Normal)
                    {
                        BspRegionType.ZonelineInfo zoneline = node.Region.Type?.Zoneline;

                        if (zoneline != null)
                        {
                            _export.Append(",");
                            _export.Append(zoneline.Type.ToString()); 
                            _export.Append(",");

                            if (zoneline.Type == BspRegionType.ZonelineType.Reference)
                            {
                                _export.Append(zoneline.Index);
                            }
                            else
                            {                                
                                _export.Append(zoneline.ZoneIndex);
                                _export.Append(",");
                                _export.Append(zoneline.Position.x);
                                _export.Append(",");
                                _export.Append(zoneline.Position.y);
                                _export.Append(",");
                                _export.Append(zoneline.Position.z);
                                _export.Append(",");
                                _export.Append(zoneline.Heading);
                            }
                        }
                    }
                    
                    _export.AppendLine();
                }
            }
        }
    }
}