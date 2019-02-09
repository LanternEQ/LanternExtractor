# LanternExtractor
EverQuest S3D file extractor that exports game data into formats usable in modern game engines.

# Overview
There have been many fantastic tools over the years that extract S3D archive content, models and zones. Sadly, as most of these tools were written 10+ years ago, they can be hard to find, buggy on modern hardware and sometimes written in languages that are no longer considered standard. LanternExtractor fixes this by combining all of this functionality and more into one simple tool.

If you are just interested in binaries, you can find them at: http://www.lanterneq.com/extractor/

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
- Character data (experimental)
- Light instances
- Music and sound instances

# How To Use
Run the extractor by invoking it from the command line. The argument is the shortname of the zone you want to extract. For example, `lanternextractor gfaydark` will extract the contents of Greater Faydark.

You can customize the output by editing the settings.txt file. Ensure that your EverQuest path is set correctly.

The `ExtractWld` option toggles beteen the simple S3D extraction and the full WLD unpack.

# Thanks
- Windcatcher - WLD file format document without which this project wouldn't be possible.
- clickclickmoon - S3D (PFS) format documentation
