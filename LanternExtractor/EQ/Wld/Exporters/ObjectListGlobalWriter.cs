using System.Collections.Generic;
using System.IO;
using LanternExtractor.EQ.Wld.Fragments;
using LanternExtractor.EQ.Wld.Helpers;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class ObjectListGlobalWriter : TextAssetWriter
    {
        private List<string> _objects = new List<string>();
        
        public ObjectListGlobalWriter(int modelCount)
        {
            string filePath = "characters/meshes_characters.txt";
            if (!File.Exists(filePath))
            {
                return;
            }
            
            var text = File.ReadAllLines(filePath);

            foreach (var line in text)
            {
                _objects.Add(line);
            }
        }
        
        public override void AddFragmentData(WldFragment data)
        {
            if (!(data is Mesh))
            {
                return;
            }

            string meshName = FragmentNameCleaner.CleanName(data);
            if (_objects.Contains(meshName))
            {
                return;
            }
            
            _objects.Add(meshName);
        }
        
        public override void WriteAssetToFile(string fileName)
        {
            _objects.Sort();
            
            foreach (var o in _objects)
            {
                _export.AppendLine(o);
            }
            
            base.WriteAssetToFile(fileName);
        }
    }
}