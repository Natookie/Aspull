using System.Collections.Generic;
using EncounterSystem.Patterns;

namespace EncounterSystem
{
    public static class PatternLibrary
    {
        private static readonly Dictionary<EncounterType, IEncounterPattern> _patterns;
        
        static PatternLibrary()
        {
            _patterns = new Dictionary<EncounterType, IEncounterPattern>
            {
                { EncounterType.WaveTunnel, new WaveTunnelPattern() },
                { EncounterType.GradualSqueeze, new GradualSqueezePattern() },
                { EncounterType.ZigZagCorridor, new ZigZagCorridorPattern() },
                { EncounterType.SplitPath, new SplitPathPattern() },
                { EncounterType.SpiralDrift, new SpiralDriftPattern() },
                { EncounterType.AlternatingPressure, new AlternatingPressurePattern() },
                { EncounterType.PinchPoint, new PinchPointPattern() },
                { EncounterType.Accordion, new AccordionPattern() },
                { EncounterType.ScissorDoors, new ScissorDoorsPattern() },
                { EncounterType.Heartbeat, new HeartbeatPattern() },
                { EncounterType.Corkscrew, new CorkscrewPattern() },
                { EncounterType.Mirror, new MirrorPattern() },
                { EncounterType.Tornado, new TornadoPattern() },
                { EncounterType.Pendulum, new PendulumPattern() },
                { EncounterType.Staggered, new StaggeredPattern() },
                { EncounterType.Funnel, new FunnelPattern() },
                { EncounterType.Ripple, new RipplePattern() },
                { EncounterType.Serpentine, new SerpentinePattern() },
                { EncounterType.CrissCross, new CrissCrossPattern() },
                { EncounterType.Whirlpool, new WhirlpoolPattern() },
                { EncounterType.ShiftWave, new ShiftWavePattern() }
            };
        }
        
        public static void ApplyPattern(EncounterType type, ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context){
            if(_patterns.TryGetValue(type, out IEncounterPattern pattern))
                pattern.ApplyEffect(ref effectiveWidth, ref effectiveCenter, corridorIndex, context);
        }
        
        public static void RegisterPattern(EncounterType type, IEncounterPattern pattern) => _patterns[type] = pattern;
        public static bool HasPattern(EncounterType type) => _patterns.ContainsKey(type);
    }
}