using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using LanternExtractor.EQ.Pfs;
using LanternExtractor.EQ.Wld.DataTypes;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Wld
{
    public class WldFileZone : WldFile
    {
        public WldFileZone(PfsFile wldFile, string zoneName, WldType type, ILogger logger, Settings settings) : base(wldFile, zoneName, type, logger, settings)
        {
        }
        
        /// <summary>
        /// Writes the files relevant to this WLD type to disk
        /// </summary>
        protected override void ExportWldData()
        {
            ExportZoneMeshes();
            ExportMaterialList();
            ExportBspTree();
        }

        /// <summary>
        /// Exports the zone meshes to an .obj file
        /// This includes the textures mesh, the collision mesh, and the water and lava meshes (if they exist)
        /// </summary>
        private void ExportZoneMeshes()
        {
            if (!_fragmentTypeDictionary.ContainsKey(0x36))
            {
                _logger.LogWarning("Cannot export zone meshes. No meshes found.");
                return;
            }

            string zoneExportFolder = _zoneName + "/" + LanternStrings.ExportZoneFolder;
            Directory.CreateDirectory(zoneExportFolder);

            bool useMeshGroups = _settings.ExportZoneMeshGroups;

            // Get all valid meshes
            var zoneMeshes = new List<Mesh>();
            bool shouldExportCollisionMesh = false;

            // Loop through once and validate meshes
            // If all surfaces are solid, there is no reason to export a separate collision mesh
            for (int i = 0; i < _fragmentTypeDictionary[0x36].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x36][i] is Mesh zoneMesh))
                {
                    continue;
                }

                zoneMeshes.Add(zoneMesh);

                if (!shouldExportCollisionMesh && zoneMesh.ExportSeparateCollision)
                {
                    shouldExportCollisionMesh = true;
                }
            }

            // Zone mesh
            var zoneExport = new StringBuilder();
            zoneExport.AppendLine(LanternStrings.ExportHeaderTitle + "Zone Mesh");
            zoneExport.AppendLine(LanternStrings.ObjMaterialHeader + _zoneName + LanternStrings.FormatMtlExtension);

            // Collision mesh
            var collisionExport = new StringBuilder();
            collisionExport.AppendLine(LanternStrings.ExportHeaderTitle + "Collision Mesh");

            // Water mesh
            var waterExport = new StringBuilder();
            waterExport.AppendLine(LanternStrings.ExportHeaderTitle + "Water Mesh");
            waterExport.AppendLine(LanternStrings.ObjMaterialHeader + _zoneName + LanternStrings.FormatMtlExtension);

            // Lava mesh
            var lavaExport = new StringBuilder();
            lavaExport.AppendLine(LanternStrings.ExportHeaderTitle + "Lava Mesh");
            lavaExport.AppendLine(LanternStrings.ObjMaterialHeader + _zoneName + LanternStrings.FormatMtlExtension);

            // Materials file
            var materialsExport = new StringBuilder();
            materialsExport.AppendLine(LanternStrings.ExportHeaderTitle + "Material Definitions");

            // Zone mesh export
            int vertexBase = 0;
            int addedVertices = 0;
            Material lastUsedMaterial = null;

            for (int i = 0; i < zoneMeshes.Count; ++i)
            {
                Mesh zoneMesh = zoneMeshes[i];

                if (useMeshGroups)
                {
                    zoneExport.AppendLine("g " + i);
                }

                List<string> outputStrings = zoneMesh.GetMeshExport(vertexBase, lastUsedMaterial,
                    ObjExportType.Textured, out addedVertices, out lastUsedMaterial, _settings, _logger);

                if (outputStrings == null || outputStrings.Count == 0)
                {
                    _logger.LogError("Mesh has no valid output: " + zoneMesh);
                    continue;
                }

                zoneExport.Append(outputStrings[0]);
                vertexBase += addedVertices;
            }

            File.WriteAllText(zoneExportFolder + _zoneName + LanternStrings.ObjFormatExtension, zoneExport.ToString());

            // Collision mesh export
            if (shouldExportCollisionMesh)
            {
                vertexBase = 0;
                lastUsedMaterial = null;

                for (int i = 0; i < zoneMeshes.Count; ++i)
                {
                    Mesh zoneMesh = zoneMeshes[i];

                    if (useMeshGroups)
                    {
                        collisionExport.AppendLine("g " + i);
                    }

                    List<string> outputStrings = zoneMesh.GetMeshExport(vertexBase, lastUsedMaterial,
                        ObjExportType.Collision, out addedVertices, out lastUsedMaterial, _settings, _logger);

                    if (outputStrings == null || outputStrings.Count == 0)
                    {
                        continue;
                    }

                    collisionExport.Append(outputStrings[0]);
                    vertexBase += addedVertices;
                }

                File.WriteAllText(zoneExportFolder + _zoneName + "_collision" + LanternStrings.ObjFormatExtension,
                    collisionExport.ToString());
            }

            // Theoretically, there should only be one texture list here
            // Exceptions include sky.s3d
            for (int i = 0; i < _fragmentTypeDictionary[0x31].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x31][i] is MaterialList materialList))
                {
                    continue;
                }

                materialsExport.Append(materialList.GetMaterialListExport(_settings));
            }

            File.WriteAllText(zoneExportFolder + _zoneName + LanternStrings.FormatMtlExtension,
                materialsExport.ToString());
        }

        /// <summary>
        /// Export zone object meshes to .obj files and collision meshes if there are non-solid polygons
        /// Additionally, it exports a list of vertex animated instances
        /// </summary>
        private void ExportZoneObjectMeshes()
        {
            if (!_fragmentTypeDictionary.ContainsKey(0x36))
            {
                _logger.LogWarning("Cannot export zone object meshes. No meshes found.");
                return;
            }

            string objectsExportFolder = _zoneName + "/" + LanternStrings.ExportObjectsFolder;
            Directory.CreateDirectory(objectsExportFolder);

            // The information about models that use vertex animation
            var animatedMeshInfo = new StringBuilder();

            for (int i = 0; i < _fragmentTypeDictionary[0x36].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x36][i] is Mesh objectMesh))
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

        /// <summary>
        /// Exports the list of material and their associated shader types
        /// This is not the same as the material definition files associated with each model
        /// </summary>
        private void ExportMaterialList()
        {
            if (!_fragmentTypeDictionary.ContainsKey(0x31))
            {
                _logger.LogWarning("Cannot export material list. No list found.");
                return;
            }

            var materialListExport = new StringBuilder();

            materialListExport.AppendLine(LanternStrings.ExportHeaderTitle + "Material List Information");
            materialListExport.AppendLine(LanternStrings.ExportHeaderFormat +
                                          "BitmapName, BitmapCount, AnimationDelayMs (optional)");

            for (int i = 0; i < _fragmentTypeDictionary[0x31].Count; ++i)
            {
                if (!(_fragmentTypeDictionary[0x31][i] is MaterialList materialList))
                {
                    continue;
                }

                foreach (Material material in materialList.Materials)
                {
                    if (material.ShaderType == ShaderType.Invisible)
                    {
                        continue;
                    }

                    string materialPrefix = MaterialList.GetMaterialPrefix(material.ShaderType);

                    string textureName = material.TextureInfoReference.TextureInfo.BitmapNames[0]
                        .Filename;

                    textureName = textureName.Substring(0, textureName.Length - 4);
                    materialListExport.Append(materialPrefix);
                    materialListExport.Append(textureName);
                    materialListExport.Append(",");
                    materialListExport.Append(material.TextureInfoReference.TextureInfo.BitmapNames.Count);

                    if (material.TextureInfoReference.TextureInfo.IsAnimated)
                    {
                        materialListExport.Append("," + material.TextureInfoReference.TextureInfo.AnimationDelayMs);
                    }

                    materialListExport.AppendLine();
                }
            }

            string fileName = _zoneName + "/" + _zoneName + "_materials";

            if (_wldType == WldType.Objects)
            {
                fileName += "_objects";
            }
            else if (_wldType == WldType.Characters)
            {
                fileName += "_characters";
            }

            fileName += ".txt";

            string directory = Path.GetDirectoryName(fileName);

            if (!Directory.Exists(directory))
            {
                // TODO: Handle error
                return;
            }

            File.WriteAllText(fileName, materialListExport.ToString());
        }

        private void ExportBspTree()
        {
            if (!_fragmentTypeDictionary.ContainsKey(0x21))
            {
                _logger.LogWarning("Cannot export BSP tree. No tree found.");
                return;
            }

            if (_fragmentTypeDictionary[0x21].Count != 1)
            {
                _logger.LogWarning("Cannot export BSP tree. Incorrect number of trees found.");
                return;
            }

            if (_bspRegions.Count == 0)
            {
                return;
            }

            if (_fragmentTypeDictionary.ContainsKey(0x29))
            {
                foreach (WldFragment fragment in _fragmentTypeDictionary[0x29])
                {
                    RegionFlag region = fragment as RegionFlag;

                    if (region == null)
                    {
                        continue;
                    }

                    region.LinkRegionType(_bspRegions);
                }
            }

            string zoneExportFolder = _zoneName + "/";

            Directory.CreateDirectory(zoneExportFolder);

            // Used for ensuring the output uses a period for a decimal number
            var format = new NumberFormatInfo {NumberDecimalSeparator = "."};

            BspTree bspTree = _fragmentTypeDictionary[0x21][0] as BspTree;

            if (bspTree == null)
            {
                return;
            }

            StringBuilder bspTreeExport = new StringBuilder();
            bspTreeExport.AppendLine(LanternStrings.ExportHeaderTitle + "BSP Tree");
            bspTreeExport.AppendLine(LanternStrings.ExportHeaderFormat +
                                     "Normal nodes: NormalX, NormalY, NormalZ, SplitDistance, LeftNodeId, RightNodeId");
            bspTreeExport.AppendLine(LanternStrings.ExportHeaderFormat +
                                     "Leaf nodes: BSPRegionId, RegionType");
            bspTreeExport.AppendLine(bspTree.GetBspTreeExport(_fragmentTypeDictionary[0x22], _logger));

            File.WriteAllText(zoneExportFolder + _zoneName + "_bsp_tree.txt", bspTreeExport.ToString());
        }
    }
}