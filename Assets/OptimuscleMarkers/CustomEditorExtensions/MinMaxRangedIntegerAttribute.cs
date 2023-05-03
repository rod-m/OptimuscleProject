using System;

namespace OptimuscleMarkers.CustomEditorExtensions
{
    public class MinMaxRangedIntegerAttribute : Attribute
    {
        public MinMaxRangedIntegerAttribute(int min, int max)
        {
            Min = min;
            Max = max;
        }
        public int Min { get; private set; }
        public int Max { get; private set; }
    }
}