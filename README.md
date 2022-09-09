# LanternExtractor
EverQuest Trilogy Client file extractor that exports game data into formats usable in modern game engines. 

This project is part of the [LanternEQ Project](https://www.lanterneq.com) which aims to reverse engineer and re-implement classic EverQuest in the Unity Engine.

## Overview
There have been many fantastic tools over the years that extract EverQuest content. Sadly, as most of these were written 15+ years ago, they can be hard to find, buggy on modern hardware and sometimes written in legacy programming languages. LanternExtractor fixes this by combining all of this functionality and more into one simple tool.

Although the extractor supports multiple export formats, the main focus is exporting assets to a human readable intermediate text format which can then be reconstructed in game engines.

The extractor also supports:
  - Raw archive content extraction
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

## What’s Next
  - Particle systems
  - Post Velious zone support

## How To Use
Please visit the [wiki](https://github.com/LanternEQ/LanternExtractor/wiki) for more info.

## Thanks
- Windcatcher - WLD file format document without which this project wouldn't be possible.
- Harakiri - Private classic test server.
- clickclickmoon - S3D (PFS) format documentation
