using UnityEngine;

namespace EncounterSystem
{
    public static class TrigLookup
    {
        private const int TABLE_SIZE = 1024;
        private static readonly float[] sinTable;
        private static readonly float[] cosTable;
        
        static TrigLookup()
        {
            sinTable = new float[TABLE_SIZE];
            cosTable = new float[TABLE_SIZE];
            
            for(int i = 0; i < TABLE_SIZE; i++){
                float angle = (i / (float)TABLE_SIZE) * Mathf.PI * 2f;
                sinTable[i] = Mathf.Sin(angle);
                cosTable[i] = Mathf.Cos(angle);
            }
        }
        
        public static float Sin(float t){
            t -= Mathf.Floor(t);
            int index = Mathf.FloorToInt(t * TABLE_SIZE);
            index = Mathf.Clamp(index, 0, TABLE_SIZE - 1);
            return sinTable[index];
        }
        
        public static float Cos(float t){
            t -= Mathf.Floor(t);
            int index = Mathf.FloorToInt(t * TABLE_SIZE);
            index = Mathf.Clamp(index, 0, TABLE_SIZE - 1);
            return cosTable[index];
        }
        
        public static float Sin(float t, float multiplier) => Sin(t * multiplier);
        public static float Cos(float t, float multiplier) => Cos(t * multiplier);
        public static (float sin, float cos) GetSinCos(float t){
            t -= Mathf.Floor(t);
            int index = Mathf.FloorToInt(t * TABLE_SIZE);
            index = Mathf.Clamp(index, 0, TABLE_SIZE - 1);
            return (sinTable[index], cosTable[index]);
        }
        
        public static (float sin, float cos) GetSinCos(float t, float multiplier) => GetSinCos(t * multiplier);
    }
}