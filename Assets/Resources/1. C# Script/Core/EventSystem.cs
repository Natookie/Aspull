using System.Collections.Generic;
using UnityEngine;

public static class EventSystem
{
    public static System.Action<List<Vector2Int>, bool[], List<EncounterSystem.CorridorSegment>, int> OnRowGenerated;
    public static System.Action<EncounterSystem.EncounterType> OnEncounterStarted;
    public static System.Action OnEncounterEnded;
    public static System.Action<float> OnEncounterProgress;
    public static System.Action<Vector2Int> OnItemConsumed;
}