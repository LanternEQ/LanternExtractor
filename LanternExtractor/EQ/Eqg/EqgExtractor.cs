using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanternExtractor.EQ.Archive;
using LanternExtractor.EQ.Eqg.DataTypes;
using LanternExtractor.Infrastructure;
using LanternExtractor.Infrastructure.Logger;

namespace LanternExtractor.EQ.Eqg
{
    /// <summary>
    /// Extracts models, animations, zones and textures from EQG archives (used by zones and models
    /// released after the original trilogy). Models are exported as OBJ when ModelExportFormat is OBJ,
    /// otherwise as glTF (the intermediate format is specific to WLD data).
    ///
    /// Output mirrors the S3D layout:
    ///   Exports/{zone}/Zone/{zone}.obj|.gltf         terrain (and placed objects if ExportZoneWithObjects)
    ///   Exports/{zone}/Objects/{model}.obj|.gltf     static models
    ///   Exports/{zone}/Characters/{model}.obj|.gltf  skinned models and the zone's characters
    /// Each folder has its own Textures folder containing the textures its models use.
    ///
    /// Unlike S3D zones, EQG zones do not bundle their characters. The client lists them in
    /// {zone}_chr.txt and {zone}_assets.txt, which reference other EQG or S3D archives. Those are
    /// extracted into the zone's Characters folder.
    /// </summary>
    public static class EqgExtractor
    {
        private const string TexturesFolder = "Textures";
        private const string ZoneFolder = "Zone";
        private const string ObjectsFolder = "Objects";
        private const string CharactersFolder = "Characters";

        private static readonly HashSet<string> ImageExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".dds", ".bmp", ".png", ".jpg", ".tga" };

        /// <param name="zoneShortName">
        /// Set when this archive is being extracted as a dependency of a zone. All models are then
        /// written to that zone's Characters folder.
        /// </param>
        public static void Extract(ArchiveBase archive, string archiveName, string rootFolder, ILogger logger,
            Settings settings, string zoneShortName = null)
        {
            var isDependency = zoneShortName != null;
            var exportRoot = Path.Combine(rootFolder, zoneShortName ?? archiveName.ToLower());
            var exportObj = settings.ModelExportFormat == ModelExportFormat.Obj;

            if (settings.ModelExportFormat == ModelExportFormat.Intermediate)
            {
                logger.LogInfo($"EqgExtractor: {archiveName} is an EQG archive. Exporting models as glTF.");
            }

            var models = new Dictionary<string, EqgModel>(StringComparer.OrdinalIgnoreCase);
            var animations = new List<EqgAnimation>();
            var zones = new List<EqgZone>();
            ReadArchive(archive, logger, models, animations, zones);

            var images = archive.GetAllFiles()
                .Where(f => ImageExtensions.Contains(Path.GetExtension(f.Name)))
                .GroupBy(f => Path.GetFileNameWithoutExtension(f.Name).ToLower())
                .ToDictionary(g => g.Key, g => g.First());

            // Group models by output folder
            var modelsByFolder = new Dictionary<string, List<EqgModel>>();
            foreach (var model in models.Values)
            {
                string folder;
                if (model.Type == EqgModelType.Ter)
                {
                    // Terrain is exported through the zone file when there is one
                    if (zones.Count > 0)
                    {
                        continue;
                    }

                    folder = ZoneFolder;
                }
                else
                {
                    folder = isDependency || model.IsSkinned() ? CharactersFolder : ObjectsFolder;
                }

                if (!modelsByFolder.TryGetValue(folder, out var list))
                {
                    list = new List<EqgModel>();
                    modelsByFolder[folder] = list;
                }

                list.Add(model);
            }

            foreach (var entry in modelsByFolder)
            {
                ExportModels(entry.Value, models.Values, animations, Path.Combine(exportRoot, entry.Key), images,
                    exportObj, archiveName, settings, logger);
            }

            foreach (var zone in zones)
            {
                var zoneModels = zone.Placements
                    .Where(p => p.ModelFileName != null && models.ContainsKey(p.ModelFileName))
                    .Select(p => models[p.ModelFileName])
                    .Where(m => m.Type == EqgModelType.Ter || settings.ExportZoneWithObjects)
                    .Distinct();
                ExportZone(zone, models, zoneModels, Path.Combine(exportRoot, ZoneFolder), images, exportObj,
                    archiveName, settings, logger);
            }

            if (!isDependency)
            {
                ExtractZoneDependencies(archive.FilePath, archiveName, rootFolder, logger, settings);
            }
        }

        private static void ReadArchive(ArchiveBase archive, ILogger logger, Dictionary<string, EqgModel> models,
            List<EqgAnimation> animations, List<EqgZone> zones)
        {
            foreach (var file in archive.GetAllFiles())
            {
                switch (Path.GetExtension(file.Name).ToLower())
                {
                    case ".mod":
                    case ".mds":
                    case ".ter":
                        // A handful of .mod files are actually LOD text files
                        if (!EqgReader.GetMagic(file.Bytes).StartsWith("EQG"))
                        {
                            continue;
                        }

                        var model = EqgReader.ReadModel(file.Bytes, file.Name, logger);
                        if (model != null)
                        {
                            models[file.Name] = model;
                        }
                        break;
                    case ".ani":
                        var animation = EqgReader.ReadAnimation(file.Bytes, file.Name, logger);
                        if (animation != null)
                        {
                            animations.Add(animation);
                        }
                        break;
                    case ".zon":
                        var zone = EqgReader.ReadZone(file.Bytes, file.Name, logger);
                        if (zone != null)
                        {
                            zones.Add(zone);
                        }
                        break;
                }
            }
        }

        private static void ExportModels(List<EqgModel> models, IEnumerable<EqgModel> allModels,
            List<EqgAnimation> animations, string folder, Dictionary<string, ArchiveFile> images, bool exportObj,
            string archiveName, Settings settings, ILogger logger)
        {
            var textureFolder = Path.Combine(folder, TexturesFolder);
            WriteTextures(models, images, textureFolder, logger);

            var gltfExporter = new EqgGltfExporter(textureFolder, settings, logger);
            var objExporter = new EqgObjExporter(textureFolder, logger);
            var extension = exportObj ? LanternStrings.ObjFormatExtension : gltfExporter.FileExtension;
            var exportedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Export MDS first so they keep the plain name when a MOD shares their base name
            foreach (var model in models.OrderBy(m => m.Type == EqgModelType.Mds ? 0 : 1)
                         .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                var exportName = model.Name.ToLower();
                if (!exportedNames.Add(exportName))
                {
                    exportName = $"{exportName}_{model.Type.ToString().ToLower()}";
                    exportedNames.Add(exportName);
                }

                var outputPath = Path.Combine(folder, exportName + extension);
                var modelAnimations = model.Bones.Count > 0
                    ? GetAnimationsForModel(model, allModels, animations)
                    : null;

                try
                {
                    if (exportObj)
                    {
                        objExporter.ExportModel(model, modelAnimations, settings.ExportAllAnimationFrames,
                            outputPath, $"{TexturesFolder}/");
                    }
                    else
                    {
                        gltfExporter.ExportModel(model, modelAnimations, outputPath, $"{TexturesFolder}/");
                    }
                }
                catch (Exception e)
                {
                    logger.LogError($"EqgExtractor: Failed to export model {model.FileName} in {archiveName}: {e}");
                }
            }
        }

        private static void ExportZone(EqgZone zone, IDictionary<string, EqgModel> models,
            IEnumerable<EqgModel> zoneModels, string folder, Dictionary<string, ArchiveFile> images, bool exportObj,
            string archiveName, Settings settings, ILogger logger)
        {
            var textureFolder = Path.Combine(folder, TexturesFolder);
            WriteTextures(zoneModels, images, textureFolder, logger);

            try
            {
                if (exportObj)
                {
                    new EqgObjExporter(textureFolder, logger).ExportZone(zone, models, settings.ExportZoneWithObjects,
                        Path.Combine(folder, zone.Name.ToLower() + LanternStrings.ObjFormatExtension),
                        $"{TexturesFolder}/");
                }
                else
                {
                    var exporter = new EqgGltfExporter(textureFolder, settings, logger);
                    exporter.ExportZone(zone, models, settings.ExportZoneWithObjects,
                        Path.Combine(folder, zone.Name.ToLower() + exporter.FileExtension), $"{TexturesFolder}/");
                }
            }
            catch (Exception e)
            {
                logger.LogError($"EqgExtractor: Failed to export zone {zone.Name} in {archiveName}: {e}");
            }
        }

        /// <summary>
        /// Extracts the archives listed in {zone}_chr.txt and {zone}_assets.txt into the zone's folder.
        /// _chr.txt lines are "model,archive" (after a count on the first line), _assets.txt lines are archive names.
        /// </summary>
        private static void ExtractZoneDependencies(string archivePath, string archiveName, string rootFolder,
            ILogger logger, Settings settings)
        {
            var directory = Path.GetDirectoryName(archivePath) ?? string.Empty;
            var zoneShortName = archiveName.ToLower();
            var dependencyPaths = new List<string>();

            foreach (var listFile in new[] { $"{archiveName}_chr.txt", $"{archiveName}_assets.txt" })
            {
                var listPath = Path.Combine(directory, listFile);
                if (!File.Exists(listPath))
                {
                    continue;
                }

                foreach (var rawLine in File.ReadAllLines(listPath))
                {
                    var line = rawLine.Trim();
                    if (line.Length == 0 || int.TryParse(line, out _))
                    {
                        continue;
                    }

                    var commaIndex = line.IndexOf(',');
                    var dependency = (commaIndex >= 0 ? line.Substring(commaIndex + 1) : line).Trim();
                    var dependencyPath = ResolveArchivePath(directory, dependency);

                    if (dependencyPath == null)
                    {
                        logger.LogWarning($"EqgExtractor: {listFile} references missing archive {dependency}");
                        continue;
                    }

                    if (!string.Equals(dependencyPath, archivePath, StringComparison.OrdinalIgnoreCase) &&
                        !dependencyPaths.Contains(dependencyPath, StringComparer.OrdinalIgnoreCase))
                    {
                        dependencyPaths.Add(dependencyPath);
                    }
                }
            }

            foreach (var dependencyPath in dependencyPaths)
            {
                ArchiveExtractor.Extract(dependencyPath, rootFolder, logger, settings, zoneShortName);
            }
        }

        private static string ResolveArchivePath(string directory, string archive)
        {
            if (string.IsNullOrEmpty(archive))
            {
                return null;
            }

            if (Path.HasExtension(archive) && File.Exists(Path.Combine(directory, archive)))
            {
                return Path.Combine(directory, archive);
            }

            foreach (var extension in new[] { LanternStrings.EqgFormatExtension, LanternStrings.S3dFormatExtension })
            {
                var path = Path.Combine(directory, archive + extension);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// Animations are named {animation}_{model}.ani (e.g. idle_ba_1_ahf.ani).
        /// If the archive contains a single skinned model, all animations are assumed to belong to it.
        /// </summary>
        private static List<EqgAnimation> GetAnimationsForModel(EqgModel model, IEnumerable<EqgModel> allModels,
            List<EqgAnimation> animations)
        {
            var suffix = "_" + model.Name;
            var matches = new List<EqgAnimation>();

            foreach (var animation in animations)
            {
                if (!animation.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var renamed = new EqgAnimation
                {
                    Name = animation.Name.Substring(0, animation.Name.Length - suffix.Length),
                    FileName = animation.FileName
                };
                renamed.Tracks.AddRange(animation.Tracks);
                matches.Add(renamed);
            }

            if (matches.Count == 0 && allModels.Count(m => m.Bones.Count > 0) == 1)
            {
                matches.AddRange(animations);
            }

            return matches;
        }

        /// <summary>
        /// Writes every texture referenced by the given models' materials as a PNG
        /// </summary>
        private static void WriteTextures(IEnumerable<EqgModel> models, Dictionary<string, ArchiveFile> images,
            string textureFolder, ILogger logger)
        {
            var textureNames = models
                .SelectMany(m => m.Materials)
                .SelectMany(m => m.Properties)
                .Where(p => p.Type == EqgMaterialPropertyType.Texture && !string.IsNullOrEmpty(p.StringValue))
                .Select(p => Path.GetFileNameWithoutExtension(p.StringValue).ToLower())
                .Distinct();

            foreach (var textureName in textureNames)
            {
                if (!images.TryGetValue(textureName, out var file) ||
                    File.Exists(Path.Combine(textureFolder, textureName + ".png")))
                {
                    continue;
                }

                try
                {
                    ImageWriter.WriteEqgImageAsPng(file.Bytes, textureFolder, file.Name.ToLower(), logger);
                }
                catch (Exception e)
                {
                    logger.LogWarning($"EqgExtractor: Unable to convert texture {file.Name}: {e.Message}");
                }
            }
        }
    }
}
