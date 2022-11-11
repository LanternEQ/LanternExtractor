using System.IO;
using System.Linq;
using LanternExtractor.EQ.Wld.Fragments;

namespace LanternExtractor.EQ.Wld.Exporters
{
    public class ParticleSystemWriter : TextAssetWriter
    {
        public ParticleSystemWriter()
        {
            _export.AppendLine(LanternStrings.ExportHeaderTitle + "Particle System");
            _export.AppendLine(LanternStrings.ExportHeaderFormat +
                               "Sprite, SpriteAnimationDelayMs, Movement, Flags, SimultaneousParticles, SpawnRadius, SpawnAngle, SpawnLifespan, SpawnVelocity," +
                               "SpawnNormalX, SpawnNormalY, SpawnNormalZ, SpawnRate, SpawnScale, ColorR, ColorG, ColorB, ColorX");
        }

        public override void AddFragmentData(WldFragment data)
        {
            ParticleCloud particle = data as ParticleCloud;

            if (particle == null)
            {
                return;
            }

            _export.Append(GetSpriteString(particle.ParticleSprite.BitmapInfoReference.BitmapInfo));
            _export.Append(',');
            _export.Append(particle.ParticleSprite.BitmapInfoReference.BitmapInfo.AnimationDelayMs);
            _export.Append(',');
            _export.Append(particle.ParticleMovement.ToString());
            _export.Append(',');
            _export.Append(particle.Flags.ToString(_numberFormat));
            _export.Append(',');
            _export.Append(particle.SimultaneousParticles.ToString(_numberFormat));
            _export.Append(',');
            _export.Append(particle.SpawnRadius.ToString(_numberFormat));
            _export.Append(',');
            _export.Append(particle.SpawnAngle.ToString(_numberFormat));
            _export.Append(',');
            _export.Append(particle.SpawnLifespan.ToString(_numberFormat));
            _export.Append(',');
            _export.Append(particle.SpawnVelocity.ToString(_numberFormat));
            _export.Append(',');
            _export.Append(particle.SpawnNormal.x.ToString(_numberFormat));
            _export.Append(',');
            _export.Append(particle.SpawnNormal.y.ToString(_numberFormat));
            _export.Append(',');
            _export.Append(particle.SpawnNormal.z.ToString(_numberFormat));
            _export.Append(',');
            _export.Append(particle.SpawnRate.ToString(_numberFormat));
            _export.Append(',');
            _export.Append(particle.SpawnScale.ToString(_numberFormat));
            _export.Append(',');
            _export.Append(particle.Color.R.ToString(_numberFormat));
            _export.Append(',');
            _export.Append(particle.Color.G.ToString(_numberFormat));
            _export.Append(',');
            _export.Append(particle.Color.B.ToString(_numberFormat));
            _export.Append(',');
            _export.Append(particle.Color.A.ToString(_numberFormat));
        }

        private string GetSpriteString(BitmapInfo bitmapInfo)
        {
            var filenames = bitmapInfo.BitmapNames.Select(b => Path.ChangeExtension(b.Filename, null));
            return string.Join(':', filenames);
        }
    }
}
