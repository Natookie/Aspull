using UnityEngine;

namespace EncounterSystem.Patterns
{
    public class WaveTunnelPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.WaveTunnel;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            float sinValue = TrigLookup.Sin(context.ProgressPhase, 2f);
            effectiveWidth = context.corridorWidths[corridorIndex] - context.finalIntensity * 2f;
            effectiveCenter = context.corridorCenters[corridorIndex] + sinValue * context.finalIntensity * 2f;
        }
    }
    
    public class GradualSqueezePattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.GradualSqueeze;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            effectiveWidth = context.difficulty.baseCorridorWidth + (context.difficulty.minCorridorWidth - context.difficulty.baseCorridorWidth) * context.finalIntensity;
        }
    }
    
    public class ZigZagCorridorPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.ZigZagCorridor;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            float sinValue = TrigLookup.Sin(context.ProgressPhase, 2.4f);
            effectiveWidth = context.corridorWidths[corridorIndex] - context.finalIntensity;
            effectiveCenter = context.corridorCenters[corridorIndex] + sinValue * context.finalIntensity * 3f;
        }
    }
    
    public class SplitPathPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.SplitPath;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            effectiveWidth = context.corridorWidths[corridorIndex] + context.finalIntensity * 2f;
            int step = context.encounterProgress % 3;
            if(step == 0) effectiveCenter = context.corridorCenters[corridorIndex] - context.finalIntensity * 2f;
            else if(step == 1) effectiveCenter = context.corridorCenters[corridorIndex] + context.finalIntensity * 2f;
        }
    }
    
    public class SpiralDriftPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.SpiralDrift;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            var (sin, cos) = TrigLookup.GetSinCos(context.ProgressPhase, 1.6f);
            effectiveCenter = context.corridorCenters[corridorIndex] + sin * context.finalIntensity * 2f;
            effectiveCenter += TrigLookup.Cos(context.ProgressPhase, 0.6f) * context.finalIntensity;
        }
    }
    
    public class AlternatingPressurePattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.AlternatingPressure;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            int side = (context.encounterProgress / 2) % 2;
            if(side == 0) effectiveCenter = context.corridorCenters[corridorIndex] - context.finalIntensity * 2f;
            else effectiveCenter = context.corridorCenters[corridorIndex] + context.finalIntensity * 2f;
        }
    }
    
    public class PinchPointPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.PinchPoint;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            float pinch = TrigLookup.Sin(context.ProgressPhase, 1f);
            effectiveWidth = context.difficulty.baseCorridorWidth - pinch * context.finalIntensity * 3f;
            float gridCenter = context.difficulty.corridorCount * context.difficulty.corridorSpacing * 0.5f;
            effectiveCenter = Mathf.Lerp(context.corridorCenters[corridorIndex], gridCenter, Mathf.Abs(pinch));
        }
    }
    
    public class AccordionPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.Accordion;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            float pulse = TrigLookup.Sin(context.ProgressPhase, 3f);
            effectiveWidth = context.difficulty.baseCorridorWidth + pulse * context.finalIntensity * 3f;
            effectiveCenter = context.corridorCenters[corridorIndex] + pulse * context.finalIntensity;
        }
    }
    
    public class ScissorDoorsPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.ScissorDoors;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            float openAmount = TrigLookup.Sin(context.ProgressPhase, 3.6f);
            openAmount = openAmount * openAmount;
            effectiveWidth = context.difficulty.minCorridorWidth + openAmount * context.finalIntensity * 4f;
            
            int step = context.encounterProgress % 2;
            if(step == 0) effectiveCenter = context.corridorCenters[corridorIndex] - context.finalIntensity * 2f;
            else effectiveCenter = context.corridorCenters[corridorIndex] + context.finalIntensity * 2f;
        }
    }
    
    public class HeartbeatPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.Heartbeat;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            float beat = Mathf.Abs(TrigLookup.Sin(context.ProgressPhase, 4f));
            effectiveWidth = context.difficulty.minCorridorWidth + beat * context.finalIntensity * 4f;
            
            if(context.encounterProgress % 2 == 0) effectiveCenter = context.corridorCenters[corridorIndex];
            else effectiveCenter = context.corridorCenters[corridorIndex] + TrigLookup.Sin(context.ProgressPhase, 2f) * 2f;
        }
    }
    
    public class CorkscrewPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.Corkscrew;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            effectiveWidth = context.difficulty.baseCorridorWidth * (1f - context.finalIntensity * 0.5f);
            var (sin, cos) = TrigLookup.GetSinCos(context.ProgressPhase, 1.6f);
            effectiveCenter = context.corridorCenters[corridorIndex] + sin * context.finalIntensity * 4f + cos * context.finalIntensity * 2f;
        }
    }
    
    public class MirrorPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.Mirror;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            if(corridorIndex == 0) effectiveCenter = context.corridorCenters[0];
            else if(corridorIndex == 1 && context.difficulty.corridorCount > 1){
                float distance = context.corridorCenters[1] - context.corridorCenters[0];
                effectiveCenter = context.corridorCenters[0] - distance;
            }
            effectiveWidth = context.difficulty.baseCorridorWidth * (1f - context.finalIntensity * 0.3f);
        }
    }
    
    public class TornadoPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.Tornado;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            float swirl = TrigLookup.Sin(context.ProgressPhase, 2.4f) * TrigLookup.Cos(context.ProgressPhase, 1.4f);
            effectiveCenter = context.corridorCenters[corridorIndex] + swirl * context.finalIntensity * 5f;
            effectiveWidth = context.difficulty.baseCorridorWidth - Mathf.Abs(swirl) * context.finalIntensity * 2f;
        }
    }
    
    public class PendulumPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.Pendulum;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            float swing = TrigLookup.Sin(context.ProgressPhase, 1f);
            effectiveCenter = context.corridorCenters[corridorIndex] + swing * context.finalIntensity * 6f;
            effectiveWidth = context.difficulty.baseCorridorWidth - Mathf.Abs(swing) * context.finalIntensity;
        }
    }
    
    public class StaggeredPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.Staggered;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            effectiveWidth = context.difficulty.baseCorridorWidth * 0.7f;
            int offset = (context.encounterProgress / 2) % 3;
            effectiveCenter = context.corridorCenters[corridorIndex] + (offset - 1) * context.finalIntensity * 3f;
        }
    }
    
    public class FunnelPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.Funnel;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            effectiveWidth = context.difficulty.baseCorridorWidth - context.Progress01 * context.finalIntensity * 4f;
            float narrowing = Mathf.Pow(context.Progress01, 1.5f);
            if(corridorIndex == 0) effectiveCenter = context.corridorCenters[corridorIndex] - narrowing * 2f;
            else if(corridorIndex == 1) effectiveCenter = context.corridorCenters[corridorIndex] + narrowing * 2f;
        }
    }
    
    public class RipplePattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.Ripple;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            float ripple = (TrigLookup.Sin(context.ProgressPhase, 2.4f) + 
                           TrigLookup.Sin(context.ProgressPhase, 4.8f) * 0.5f + 
                           TrigLookup.Sin(context.ProgressPhase, 7.2f) * 0.25f) / 1.75f;
            effectiveCenter = context.corridorCenters[corridorIndex] + ripple * context.finalIntensity * 3f;
            effectiveWidth = context.difficulty.baseCorridorWidth - Mathf.Abs(ripple) * context.finalIntensity * 1.5f;
        }
    }
    
    public class SerpentinePattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.Serpentine;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            float snake = TrigLookup.Sin(context.ProgressPhase, 0.8f) + TrigLookup.Sin(context.ProgressPhase, 1.8f) * 0.5f;
            effectiveCenter = context.corridorCenters[corridorIndex] + snake * context.finalIntensity * 4f;
            effectiveWidth = context.difficulty.baseCorridorWidth - 1f;
        }
    }
    
    public class CrissCrossPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.CrissCross;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            effectiveWidth = context.difficulty.baseCorridorWidth * 0.6f;
            if(context.difficulty.corridorCount >= 2){
                float crossPoint = Mathf.PingPong(context.encounterProgress * 0.3f, 1f);
                if(corridorIndex == 0) effectiveCenter = Mathf.Lerp(context.corridorCenters[0], context.corridorCenters[1], crossPoint);
                else if(corridorIndex == 1) effectiveCenter = Mathf.Lerp(context.corridorCenters[1], context.corridorCenters[0], crossPoint);
            }
        }
    }
    
    public class WhirlpoolPattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.Whirlpool;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            float radius = context.finalIntensity * 5f;
            float offsetX = TrigLookup.Cos(context.ProgressPhase, 1f) * radius * (1f - context.Progress01);
            effectiveCenter = context.corridorCenters[corridorIndex] + offsetX;
            effectiveWidth = context.difficulty.baseCorridorWidth - context.finalIntensity * 2f;
        }
    }
    
    public class ShiftWavePattern : IEncounterPattern
    {
        public EncounterType Type => EncounterType.ShiftWave;
        
        public void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            float wave = TrigLookup.Sin(context.ProgressPhase, 1.2f);
            effectiveCenter = context.corridorCenters[corridorIndex] + wave * context.finalIntensity * 3f;
            
            if(Mathf.Abs(TrigLookup.Sin(context.ProgressPhase, 2.2f)) > 0.5f) 
                effectiveWidth = context.difficulty.minCorridorWidth;
            else 
                effectiveWidth = context.difficulty.baseCorridorWidth;
        }
    }
}