using UnityEngine;
using CustomEditorExtensions;
namespace OpenCVTest
{
    [CreateAssetMenu(fileName = "Colour Calibrate", menuName = "", order = 0)]
    public class ColourCalibrate : ScriptableObject
    {
        public bool active_setting = true;
        public ColorObject colorCalibrated;
        /// <summary>
        /// max number of objects to be detected in frame
        /// </summary>
        
        public int max_objects = 50;
       
        /// <summary>
        /// minimum and maximum object area
        /// </summary>
        
        [MinMaxRangedIntegerAttribute(1, 200)] public RangedInteger object_area;
       
       
    }
}