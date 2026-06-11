using UnityEngine;

namespace EncounterSystem
{
    public interface IEncounterPattern
    {
        EncounterType Type { get; }
        void ApplyEffect(ref float effectiveWidth, ref float effectiveCenter, int corridorIndex, EncounterContext context);
    }
}