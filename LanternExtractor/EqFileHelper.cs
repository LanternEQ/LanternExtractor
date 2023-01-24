using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LanternExtractor
{
    public static class EqFileHelper
    {
        public static bool IsEquipmentArchive(string archiveName)
        {
            return archiveName.StartsWith("gequip");
        }

        public static bool IsCharactersArchive(string archiveName)
        {
            return archiveName.Contains("_chr") || archiveName.StartsWith("chequip") || archiveName.Contains("_amr");
        }

        public static bool IsObjectsArchive(string archiveName)
        {
            return archiveName.Contains("_obj");
        }

        public static bool IsSkyArchive(string archiveName)
        {
            return archiveName == "sky";
        }

        public static bool IsSoundArchive(string archiveName)
        {
            return archiveName.StartsWith("snd");
        }

        public static bool IsClientDataFile(string archiveName)
        {
            return archiveName == "clientdata";
        }

        public static List<string> GetValidEqFilePaths(string directory, string archiveName)
        {
            archiveName = archiveName.ToLower();

            var validFiles = new List<string>();

            if (archiveName == "all")
            {
                validFiles = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                    .Where(s => (s.EndsWith(".s3d") || s.EndsWith(".pfs")) && !s.Contains("chequip") &&
                                !s.EndsWith("_lit.s3d")).ToList();
            }
            else if (archiveName == "equipment")
            {
                validFiles = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                    .Where(s => (s.EndsWith(".s3d") || s.EndsWith(".pfs")) && s.Contains("gequip")).ToList();
            }
            else if (archiveName.EndsWith(".s3d") || archiveName.EndsWith(".pfs"))
            {
                string archivePath = directory + archiveName;

                if (File.Exists(archivePath))
                {
                    validFiles.Add(archivePath);
                }
            }
            else
            {
                // If it's the shortname of a PFS file, we assume it's standalone - used for sound files
                string archivePath = directory + archiveName + ".pfs";

                if (File.Exists(archivePath))
                {
                    validFiles.Add(archivePath);

                    return validFiles;
                }

                // Try and find all associated files with the shortname - can theoretically be a non zone file
                string mainArchivePath = directory + archiveName + ".s3d";
                if (File.Exists(mainArchivePath))
                {
                    validFiles.Add(mainArchivePath);
                }

                // Some zones have additional object archives for things added past their initial release
                // None of them contain fragments that are linked to other related archives.
                string extensionObjectsArchivePath = directory + archiveName + "_2_obj.s3d";
                if (File.Exists(extensionObjectsArchivePath))
                {
                    validFiles.Add(extensionObjectsArchivePath);
                }

                string objectsArchivePath = directory + archiveName + "_obj.s3d";
                if (File.Exists(objectsArchivePath))
                {
                    validFiles.Add(objectsArchivePath);
                }

                string charactersArchivePath = directory + archiveName + "_chr.s3d";
                if (File.Exists(charactersArchivePath))
                {
                    validFiles.Add(charactersArchivePath);
                }

                // Some zones have additional character archives for things added past their initial release
                // None of them contain fragments that are linked to other related archives.
                // "qeynos" must be excluded because both qeynos and qeynos2 are used as shortnames
                string extensionCharactersArchivePath = directory + archiveName + "2_chr.s3d";
                if (File.Exists(extensionCharactersArchivePath) && archiveName != "qeynos")
                {
                    validFiles.Add(extensionCharactersArchivePath);
                }
            }

            return validFiles;
        }
    }
}
