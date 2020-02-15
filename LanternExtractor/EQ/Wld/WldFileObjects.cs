using System.Collections.Generic;
using System.IO;
using System.Text;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileObjects : WldFile
    {
        public WldFileObjects(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings, WldFile wldToIbject = null) : base(
            wldFile, zoneName, type, logger, settings, wldToIbject)
        {
        }

        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected override void ExportWldData()
        {
            ExportZoneObjectMeshes();
            ExportMaterialList();
        }
        
         /// <summary>
        /// Export zone object meshes to .obj files and collision meshes if there are non-solid polygons
        /// Additionally, it exports a list of vertex animated instances
        /// </summary>
        private void ExportZoneObjectMeshes()
        {
            if (!_fragmentTypeDictionary.ContainsKey(FragmentType.Mesh))
            {
                _logger.LogWarning("Cannot export zone object meshes. No meshes found.");
                return;
            }

            string objectsExportFolder = _zoneName + "/" + LanternStrings.ExportObjectsFolder;
            Directory.CreateDirectory(objectsExportFolder);

            // The information about models that use vertex animation
            var animatedMeshInfo = new StringBuilder();

            for (int i = 0; i < _fragmentTypeDictionary[FragmentType.Mesh].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[FragmentType.Mesh][i] is Mesh objectMesh))
                {
                    continue;
                }

                string fixedObjectName = objectMesh.Name.Split('_')[0].ToLower();

                // These values are not used
                int addedVertices = 0;
                Material lastUsedMaterial = null;

                List<string> meshStrings = objectMesh.GetMeshExport(0, lastUsedMaterial,
                    ObjExportType.Textured, out addedVertices, out lastUsedMaterial, _settings, _logger);

                if (meshStrings == null || meshStrings.Count == 0)
                {
                    continue;
                }

                var objectExport = new StringBuilder();

                // If there are more than one outputs, it's an additional frame for an animated mesh
                for (int j = 0; j < meshStrings.Count; ++j)
                {
                    objectExport.AppendLine(LanternStrings.ExportHeaderTitle + "Object Mesh - " + fixedObjectName);
                    objectExport.AppendLine(LanternStrings.ObjMaterialHeader + fixedObjectName +
                                            LanternStrings.FormatMtlExtension);

                    // Most of the time, there will only be one
                    if (j == 0)
                    {
                        objectExport.Append(meshStrings[0]);
                        File.WriteAllText(objectsExportFolder + fixedObjectName + LanternStrings.ObjFormatExtension,
                            objectExport.ToString());

                        if (meshStrings.Count != 1)
                        {
                            animatedMeshInfo.Append(fixedObjectName);
                            animatedMeshInfo.Append(",");
                            animatedMeshInfo.Append(meshStrings.Count);
                            animatedMeshInfo.Append(",");
                            animatedMeshInfo.Append(objectMesh.AnimatedVertices.Delay);
                            animatedMeshInfo.AppendLine();
                        }

                        continue;
                    }

                    objectExport.Append(meshStrings[j - 1]);
                    File.WriteAllText(
                        objectsExportFolder + fixedObjectName + "_frame" + j + LanternStrings.ObjFormatExtension,
                        objectExport.ToString());

                    objectExport.Clear();
                }

                // Write animated mesh vertex entries (if they exist)
                if (animatedMeshInfo.Length != 0)
                {
                    var animatedMeshesHeader = new StringBuilder();
                    animatedMeshesHeader.AppendLine(LanternStrings.ExportHeaderTitle + "Animated Vertex Meshes");
                    animatedMeshesHeader.AppendLine(
                        LanternStrings.ExportHeaderFormat + "ModelName, Frames, Frame Delay");

                    File.WriteAllText(_zoneName + "/" + _zoneName + "_animated_meshes.txt",
                        animatedMeshesHeader + animatedMeshInfo.ToString());
                }

                // Collision mesh
                if (objectMesh.ExportSeparateCollision)
                {
                    var collisionExport = new StringBuilder();
                    collisionExport.AppendLine(LanternStrings.ExportHeaderTitle + "Object Collision Mesh - " +
                                               fixedObjectName);

                    addedVertices = 0;
                    lastUsedMaterial = null;
                    meshStrings = objectMesh.GetMeshExport(0, lastUsedMaterial, ObjExportType.Collision,
                        out addedVertices, out lastUsedMaterial, _settings, _logger);

                    if (meshStrings == null || meshStrings.Count == 0)
                    {
                        continue;
                    }

                    File.WriteAllText(
                        objectsExportFolder + fixedObjectName + "_collision" + LanternStrings.ObjFormatExtension,
                        meshStrings[0]);
                }

                // Materials
                var materialsExport = new StringBuilder();
                materialsExport.AppendLine(LanternStrings.ExportHeaderTitle + "Material Definitions");
                materialsExport.Append(objectMesh.MaterialList.GetMaterialListExport(_settings));

                File.WriteAllText(objectsExportFolder + fixedObjectName + LanternStrings.FormatMtlExtension,
                    materialsExport.ToString());
            }
        } 
    }
}