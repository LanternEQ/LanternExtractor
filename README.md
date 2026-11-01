# LanternExtractor
LanternExtractor is a C# utility designed to parse legacy file formats from the EverQuest Trilogy client (1999-2001) and export them into open standards.

It supports:
  - Raw archive content extraction
  - Intermediate text format
  - OBJ export
  - glTF export

## Features

The intermediate format supports:
- S3D file contents
- Zone data
  - Textured mesh
  - Collision mesh
  - Vertex colors
  - BSP tree (region data)
  - Ambient light
  - Light instances
  - Music instances
  - Sound instances
- Object data
  - Textured meshes
  - Collision meshes
  - Vertex animations
  - Skeletal animations
  - Instance list
  - Per instance vertex colors
- Character data
  - Textured meshes
  - Skeletal animations
  - Skins
- Equipment data
  - Texture mesh
  - Skeletal animations

EQG archives (post-Velious zones, characters and items) are exported to glTF or OBJ:
- Static models (`.mod`)
- Skinned models (`.mds`, and `.mod` files with weighted bones)
- Skeletal animations (`.ani`)
- Terrain (`.ter`)
- Zones (`.zon` v1) with object placements
- Textures (`.dds` converted to PNG)

EQG output follows the S3D layout: `Exports/<zone>/Zone/`, `Objects/` and `Characters/`, each with its own `Textures/`.
EQG zones don't bundle their characters; extracting a zone also extracts the archives listed in its `<zone>_chr.txt` and `<zone>_assets.txt` into its `Characters/` folder.
Set `ModelExportFormat = 1` for OBJ (bind pose, plus one OBJ per animation frame with `ExportAllAnimationFrames`) or `2` for glTF (includes skeletons and animations).
Use `lantern.exe <name>.eqg` for a single archive or `lantern.exe eqg` to extract every EQG archive.

## What’s Next
  - Particle systems
  - Post Velious terrain zones (`.zon` v4 / EQTZP)

## How To Use
Please visit the [wiki](https://github.com/LanternEQ/LanternExtractor/wiki) for more info.

---

## Disclaimer & Copyright

**LanternExtractor** is a community-driven, open-source project designed for educational use and data interoperability.

1.  **No Assets Included:** This repository contains **source code only**. It does not contain any copyrighted assets (models, textures, sounds, or game data) from EverQuest. Users must provide their own legally obtained copy of the game to utilize these tools.
2.  **Trademark:** EverQuest is a registered trademark of Daybreak Game Company LLC. This project is not affiliated with, endorsed by, or licensed by Daybreak Game Company LLC.
