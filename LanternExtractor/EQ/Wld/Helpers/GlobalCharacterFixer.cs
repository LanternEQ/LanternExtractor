using System.Linq;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Helpers
{
    public class GlobalCharacterFixer
    {
        private  WldFileCharacters _wld;
        public void Fix(WldFileCharacters wld)
        {
            _wld = wld;
            FixShipNames();
            FixGolemElemental();
            FixDemiLich();
            FixAkanonKing();
            FixKaladimKing();
            FixFayDrake();
            FixTurtleTextures();
            FixBlackAndWhiteDragon();
            FixGhoulTextures(wld);
        }

        /// <summary>
        /// Fix Ghoul face being applied to the back of the leg
        /// Thanks to modestlaw for requesting this
        /// </summary>
        /// <param name="wldFileCharacters"></param>
        private void FixGhoulTextures(WldFileCharacters wldFileCharacters)
        {
            var meshes = wldFileCharacters.GetFragmentsOfType<Mesh>();

            if (meshes.Count == 0)
            {
                return;
            }

            foreach (var mesh in meshes)
            {
                // Fix head material assignment
                if (mesh.Name.StartsWith("GHUHE00"))
                {
                    var materialGroups = mesh.MaterialGroups;
                    materialGroups[1].MaterialIndex = 7;
                }
                else if(mesh.Name.StartsWith("GHU_"))
                {
                    var materialGroups = mesh.MaterialGroups;
                    materialGroups[0].MaterialIndex = 0;
                }
            }
        }

        /// <summary>
        /// Fixes the turtle textures being named incorrectly
        /// They use the sea horse prefix
        /// </summary>
        private void FixTurtleTextures()
        {
            var actors = _wld.GetFragmentsOfType<Actor>();
           
            foreach (var actor in actors)
            {
                if (!actor.Name.StartsWith("STU"))
                {
                    continue;
                }
                
                var materialList = actor.SkeletonReference.SkeletonHierarchy.Meshes.First().MaterialList;

                materialList.Name = materialList.Name.Replace("SEA", "STU");

                foreach (var material in materialList.Materials)
                {
                    material.Name = material.Name.Replace("SEA", "STU");
                    var bitmapNames = material.GetAllBitmapNames();
                    
                    for (int i = 0; i < bitmapNames.Count; ++i)
                    {
                        string originalName = bitmapNames[i];
                        string newName = originalName.Replace("sea", "stu");
                        material.SetBitmapName(i, newName);
                        _wld.FilenameChanges[originalName] = newName;
                    }
                }
            }
        }

        private void FixFayDrake()
        {
            var actors = _wld.GetFragmentsOfType<Actor>();

            foreach (var actor in actors)
            {
                if (!actor.Name.StartsWith("FDR"))
                {
                    continue;
                }

                if (actor.SkeletonReference.SkeletonHierarchy.Meshes.Count != 2)
                {
                    continue;
                }

                // Rename actor
                actor.Name = actor.Name.Replace("FDR", "FDF");
                
                // Rename skeleton reference
                var skeletonRef = actor.SkeletonReference;
                skeletonRef.Name = skeletonRef.Name.Replace("FDR", "FDF");

                // Rename skeleton
                var skeleton = actor.SkeletonReference.SkeletonHierarchy;
                skeleton.Name = skeleton.Name.Replace("FDR", "FDF");

                skeleton.ModelBase = "fdf";
                
                // Rename all main meshes
                foreach (var mesh in actor.SkeletonReference.SkeletonHierarchy.Meshes)
                {
                    mesh.Name = mesh.Name.Replace("FDR", "FDF");
                }
                
                // Rename all secondary meshes
                foreach (var mesh in actor.SkeletonReference.SkeletonHierarchy.SecondaryMeshes)
                {
                    mesh.Name = mesh.Name.Replace("FDR", "FDF");
                }

                // Rename all materials
                var materialList = actor.SkeletonReference.SkeletonHierarchy.Meshes.First().MaterialList;

                materialList.Name = materialList.Name.Replace("FDR", "FDF");

                foreach (var material in materialList.Materials)
                {
                    material.Name = material.Name.Replace("FDR", "FDF");

                    var bitmapNames = material.GetAllBitmapNames();
                    
                    for (int i = 0; i < bitmapNames.Count; ++i)
                    {
                        string originalName = bitmapNames[i];
                        string newName = originalName.Replace("fdr", "fdf");
                        material.SetBitmapName(i, newName);
                        _wld.FilenameChanges[originalName] = newName;
                    }
                }
            }
        }

        /// <summary>
        /// Fixes the unused Kaladim Kind model crown shader assignment
        /// </summary>
        private void FixKaladimKing()
        {
            var crownMaterial = _wld.GetFragmentByName<Material>("KAHE0001_MDF");
            
           if (crownMaterial != null)
           {
               crownMaterial.ShaderType = ShaderType.TransparentMasked;
           }
        }

        /// <summary>
        /// Fixes the unused Ak'Anon Kind model crown shader assignment
        /// </summary>
        private void FixAkanonKing()
        {
            var crownMaterial = _wld.GetFragmentByName<Material>("CLHE0004_MDF");

            if (crownMaterial != null)
            {
                crownMaterial.ShaderType = ShaderType.TransparentMasked;
            }
        }

        private void FixDemiLich()
        {
            var actors = _wld.GetFragmentsOfType<Actor>();

            foreach (var actor in actors)
            {
                if (actor.SkeletonReference == null)
                {
                    continue;
                }
                
                if (!actor.Name.StartsWith("SDE"))
                {
                    continue;
                }
                
                foreach (var mesh in actor.SkeletonReference.SkeletonHierarchy.Meshes)
                {
                    foreach (var material in mesh.MaterialList.Materials)
                    {
                        // This texture needs to be masked
                        if (material.Name == "SDEUA0006_MDF")
                        {
                            material.ShaderType = ShaderType.TransparentMasked;
                        }
                        
                        var bitmapNames = material.GetAllBitmapNames();
                        
                        for (var i = 0; i < bitmapNames.Count; i++)
                        {
                            if (!bitmapNames[i].StartsWith("dml"))
                            {
                                continue;
                            }

                            string originalName = bitmapNames[i];
                            string newName = originalName.Replace("dml", "sde");
                            material.SetBitmapName(i, newName);
                            _wld.FilenameChanges[originalName] = newName;
                        }
                    }
                }
            }
        }

        private void FixGolemElemental()
        {
            var actors = _wld.GetFragmentsOfType<Actor>();

            foreach (var actor in actors)
            {
                if (!actor.Name.StartsWith("GOM"))
                {
                    continue;
                }
                
                foreach (var mesh in actor.SkeletonReference.SkeletonHierarchy.Meshes)
                {
                    foreach (var material in mesh.MaterialList.Materials)
                    {
                        material.Name = material.Name.Replace("GOL", "GOM");

                        var bitmapNames = material.GetAllBitmapNames();

                        for (var i = 0; i < bitmapNames.Count; i++)
                        {
                            string originalName = bitmapNames[i];
                            string newName = originalName.Replace("gol", "gom");
                            material.SetBitmapName(i, newName);
                            _wld.FilenameChanges[originalName] = newName;
                        }
                    }
                }
            }
        }

        private void FixShipNames()
        {
            var actors = _wld.GetFragmentsOfType<Actor>();

            foreach (var actor in actors)
            {
                if (actor.Name.StartsWith("GSP"))
                {
                    actor.MeshReference.Mesh.Name = actor.MeshReference.Mesh.Name.Replace("GHOSTSHIP", "GSP");
                    actor.MeshReference.Mesh.MaterialList.Name =
                        actor.MeshReference.Mesh.MaterialList.Name.Replace("GHOSTSHIP", "GSP");
                }

                if (actor.Name.StartsWith("LAUNCH"))
                {
                    actor.Name = actor.MeshReference.Mesh.Name.Replace("DMSPRITEDEF", "ACTORDEF");
                }

                if (actor.Name.StartsWith("PRE"))
                {
                    if (actor.SkeletonReference == null)
                    {
                        continue;
                    }

                    switch (actor.SkeletonReference.SkeletonHierarchy.Name)
                    {
                        // Bloated Belly in Iceclad
                        case "OGS_HS_DEF":
                        {
                            actor.Name = actor.Name.Replace("PRE", "OGS");
                            break;
                        }
                        // Sea King, Golden Maiden, StormBreaker, SirensBane
                        case "PRE_HS_DEF":
                        {
                            break;
                        }
                    }
                }


                if (actor.Name.StartsWith("SHIP"))
                {
                    if (actor.SkeletonReference == null)
                    {
                        continue;
                    }

                    switch (actor.SkeletonReference.SkeletonHierarchy.Name)
                    {
                        // Icebreaker in Iceclad
                        case "GNS_HS_DEF":
                        {
                            actor.Name = actor.Name.Replace("SHIP", "GNS");
                            break;
                        }
                        // Maidens Voyage in Firiona Vie
                        case "ELS_HS_DEF":
                        {
                            actor.Name = actor.Name.Replace("SHIP", "ELS");
                            break;
                        }
                    }
                }
            }
        }
        
        private void FixBlackAndWhiteDragon()
        {
            var actors = _wld.GetFragmentsOfType<Actor>();
            
            foreach (var actor in actors)
            {
                if (!actor.Name.StartsWith("BWD"))
                {
                    continue;
                }

                var frag = _wld.GetFragmentByName<Material>("BWDCH0101_MDF");
                
                if (frag != null)
                {
                    frag.ShaderType = ShaderType.Diffuse;
                }

                var materialFragment = _wld.GetFragmentByName<MaterialList>("BWD_MP");
                if (materialFragment != null)
                {
                    var slot = materialFragment.Slots["bwd_ch01"];
                    // TODO: Fix this
                    //slot[1] = "d_bwdch0101";
                }
            }
        }
    }
}