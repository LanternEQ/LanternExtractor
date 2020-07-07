using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class MeshIntermediateMaterialsExport : TextAssetWriter
    {
        private Settings _settings;
        private string _modelName;
        private ILogger _logger;

        public MeshIntermediateMaterialsExport(Settings settings, string modelName, ILogger logger)
        {
            _settings = settings;
            _modelName = modelName;
            _logger = logger;
        }

        public override void AddFragmentData(WldFragment data)
        {
            MaterialList list = data as MaterialList;

            if (list == null)
            {
                return;
            }

            for (var i = 0; i < list.Materials.Count; i++)
            {
                Material material = list.Materials[i];

                if (string.IsNullOrEmpty(material.GetFirstBitmapExportFilename()))
                {
                    _export.Append(i);
                    _export.Append(",");
                    _export.Append("invisible");
                    _export.AppendLine();
                    continue;
                }

                string materialName = material.GetFullMaterialName();

                _export.Append(i);
                _export.Append(",");
                _export.Append(materialName);

                var skinVariants = list.GetMaterialVariants(material, _logger);

                if (skinVariants.Count != 0)
                {
                    foreach (var variant in skinVariants)
                    {
                        _export.Append(";");
                        _export.Append(variant);
                    }
                }
                
                _export.Append(",");
                _export.Append(material.BitmapInfoReference.BitmapInfo.BitmapNames.Count);

                if (material.BitmapInfoReference.BitmapInfo.IsAnimated)
                {
                    _export.Append(",");
                    _export.Append(material.BitmapInfoReference.BitmapInfo.AnimationDelayMs);
                }

                _export.AppendLine();
            }
        }
    }
}