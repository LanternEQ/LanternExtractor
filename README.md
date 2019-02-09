# LanternExtractor
EverQuest S3D file extractor that exports game data into formats usable in modern game engines.

# Overview
There have been many fantastic tools over the years that extract S3D archive content, models and zones. Sadly, as most of these tools were written 10+ years ago, they can be hard to find, buggy on modern hardware and sometimes written in languages that are no longer considered standard. LanternExtractor fixes this by combining all of this functionality and more into one simple tool.

# Features
Extracts:
- S3D file contents
- Zone data
  - Textured mesh
  - Collision mesh
- Object data
  - Textured meshes
  - Collision meshes
  - Vertex animation meshes
  - Instance list
- Light instances
- Music and sound instances

# How to use
You can extract the contents of an S3D file by dropping it on the executable or by supplying the archive name via the command line.

You can customize the output by editing the settings.txt file. Ensure that your EverQuest path is set correctly.

The `ExtractWld` option toggles beteen the simple S3D extraction and the full WLD unpack.

# Thanks
- Windcatcher - WLD file format document without which this project wouldn't be possible.
- clickclickmoon - S3D (PFS) format documentation
