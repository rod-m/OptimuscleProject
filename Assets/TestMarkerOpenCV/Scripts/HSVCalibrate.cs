using CustomEditorExtensions;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine;

namespace OpenCVTest
{
    [CreateAssetMenu(fileName = "HSVCalibrate", menuName = "HSVCalibrate", order = 0)]
    public class HSVCalibrate : ScriptableObject
    {
        public bool active = true;
        [MinMaxRangedInteger(1, 255)] public RangedInteger hueRange;
        [MinMaxRangedInteger(1, 255)] public RangedInteger saturationRange;
        [MinMaxRangedInteger(1, 255)] public RangedInteger valueRange;

        [Header("Choose marker display thickness")]
        [Range(1, 6)] public int thickness = 2;
        [Header("Choose colour for markers")]
       [SerializeField] Color markerDisplayColor = Color.magenta;
       public Scalar GetMarkerDisplayColor()
       {
           float H, S, V;
           Color.RGBToHSV(markerDisplayColor, out H, out S, out V);
           return new Scalar(H * 255,S * 100,V * 100);
           
       }
        public Scalar GetHSVColorFrom()
        {
            return new Scalar(hueRange.minValue , saturationRange.minValue , valueRange.maxValue);
        }
        public Scalar GetHSVColorTo()
        {
            return new Scalar(hueRange.maxValue , saturationRange.maxValue , valueRange.maxValue);
        }
        public Scalar GetHFrom()
        {
            return new Scalar(hueRange.minValue);
        }
        public Scalar GetHTo()
        {
            return new Scalar(hueRange.maxValue);
        }
        public Scalar GetSatFrom()
        {
            return new Scalar(saturationRange.minValue);
        }
        public Scalar GetSatTo()
        {
            return new Scalar(saturationRange.maxValue);
        }
        public Scalar GetValFrom()
        {
            return new Scalar(valueRange.minValue);
        }
        public Scalar GetValTo()
        {
            return new Scalar(valueRange.maxValue);
        }
    }
}