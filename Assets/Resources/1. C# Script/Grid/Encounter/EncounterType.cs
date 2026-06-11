using UnityEngine;
using System.Collections.Generic;

namespace EncounterSystem
{
    public enum EncounterType{
        WaveTunnel,
        GradualSqueeze,
        ZigZagCorridor,
        SplitPath,
        SpiralDrift,
        AlternatingPressure,
        ClearSection,
        PinchPoint,
        Accordion,
        ScissorDoors,
        Heartbeat,
        Corkscrew,
        Mirror,
        Tornado,
        Pendulum,
        Staggered,
        Funnel,
        Ripple,
        Serpentine,
        CrissCross,
        Whirlpool,
        ShiftWave
    }
    
    [System.Serializable]
    public class EncounterDefinition
    {
        public EncounterType type;
        public int duration = 5;
        public float intensity = 0.5f;
        public AnimationCurve intensityCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }
    
    [System.Serializable]
    public class DifficultySettings
    {
        public float baseCorridorWidth = 4f;
        public float minCorridorWidth = 2f;
        public float driftSpeed = 0.5f;
        [Range(0, 1)] public float encounterFrequency = 0.3f;
        [Range(0, 1)] public float clearSectionChance = 0.2f;
        public int corridorCount = 2;
        public float corridorSpacing = 5f;
    }
    
    public struct CorridorSegment
    {
        public int left;
        public int right;
        public float intensity;
        public int corridorIndex;
    }
    
    public struct EncounterContext
    {
        public int encounterProgress;
        public int maxProgress;
        public float finalIntensity;
        public IReadOnlyList<float> corridorCenters;
        public IReadOnlyList<float> corridorWidths;
        public DifficultySettings difficulty;
        
        public float Progress01 => maxProgress > 0 ? (float)encounterProgress / maxProgress : 0f;
        public float ProgressPhase => Progress01;
        
        public EncounterContext(int progress, int max, float intensity, IReadOnlyList<float> centers, IReadOnlyList<float> widths, DifficultySettings diff){
            encounterProgress = progress;
            maxProgress = max;
            finalIntensity = intensity;
            corridorCenters = centers;
            corridorWidths = widths;
            difficulty = diff;
        }
    }
}