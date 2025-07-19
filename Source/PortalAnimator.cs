using RimWorld;
using Verse;
using UnityEngine;

namespace PortalGun
{
    public class PortalAnimator : ThingWithComps
    {
        private int animationTick = 0;
        private float pulseIntensity = 1.0f;
        private Color baseColor = Color.green;
        
        protected override void Tick()
        {
            base.Tick();
            animationTick++;
            
            // Create a pulsing effect
            pulseIntensity = 0.8f + 0.4f * Mathf.Sin(animationTick * 0.1f);
            
            // Spawn occasional flecks for animation
            if (animationTick % 30 == 0) // Every half second
            {
                Vector3 portalCenter = DrawPos;
                portalCenter.x += Rand.Range(-0.3f, 0.3f);
                portalCenter.z += Rand.Range(-0.3f, 0.3f);
                
                FleckMaker.Static(portalCenter, Map, FleckDefOf.AirPuff, 0.5f);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            
            // Add a glowing effect by drawing multiple times with different alphas
            for (int i = 0; i < 3; i++)
            {
                Vector3 drawPos = drawLoc + new Vector3(Rand.Range(-0.05f, 0.05f), 0, Rand.Range(-0.05f, 0.05f));
                Graphics.DrawMesh(MeshPool.plane10, drawPos, Quaternion.identity, 
                    MaterialPool.MatFrom(def.graphicData.texPath, ShaderDatabase.Transparent, 
                    baseColor * (pulseIntensity * (0.3f + i * 0.2f))), 0);
            }
        }
    }
}
