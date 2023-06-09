﻿using System;
using OpenCVForUnity.CoreModule;
using CustomEditorExtensions;
using UnityEngine;

namespace OptimuscleMarkers
{
    [CreateAssetMenu(fileName = "HSVCalibrate", menuName = "HSVCalibrate", order = 0)]
    [Serializable]
    public class HSVCalibrate : ScriptableObject
    {
        [Header("ReDraw the marker label grid")]
        public bool liveUpdateGrid = true;
        [Header("Display video feed and markers")]
        public bool showOutput = true;

        [Header("MARKER FILTERS")]
        [Header("Choose marker HSV color range")]
        [MinMaxRangedInteger(1, 255)] public RangedInteger hueRange;
        [MinMaxRangedInteger(1, 255)] public RangedInteger saturationRange;
        [MinMaxRangedInteger(1, 255)] public RangedInteger valueRange;

        [Header("Choose marker minimum distance overlap")]
        [Range(0, 100)]public int minDistance = 1;
        [Header("Choose marker size range")]
        [MinMaxRangedInteger(0, 50)] public RangedInteger sizeRange;
        [Header("Choose marker bounds clipping")]
        [MinMaxRange(1, 100)] public RangedFloat boundHorizontal;
        [MinMaxRange(1, 100)] public RangedFloat boundVertical;
        [Header("Define marker row and column grid for labels")]
        [Range(1, 16)] public int markerColumns = 8;
        [Range(1, 16)] public int markerRows = 8;
        [Header("MARKER DISPLAY")]
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
       public Scalar GetHFrom()
        {
            return new Scalar(hueRange.minValue);
        }

       public void SetHueMin(float value)
       {
           hueRange.minValue = (int)value;
       }
       public void SetHueMax(float value)
       {
           hueRange.maxValue = (int)value;
       }
       public void SetSatMin(float value)
       {
           saturationRange.minValue = (int)value;
       }
       public void SetSatMax(float value)
       {
           saturationRange.maxValue = (int)value;
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
        public void SetScalarProperty(string property, float val)
        {
            switch (property)
            {
                case "HueMin":
                    hueRange.minValue = (int)val;
                    break;
                case "HueMax":
                    hueRange.maxValue = (int)val;
                    break;
                case "SatMin":
                    saturationRange.minValue = (int)val;
                    break;
                case "SatMax":
                    saturationRange.maxValue = (int)val;
                    break;
                case "ValMin":
                    valueRange.minValue = (int)val;
                    break;
                case "ValMax":
                    valueRange.maxValue = (int)val;
                    break;
            }
        
         
        }
      
        public float GetScalar(string property)
        {
            switch (property)
            {
                case "HueMin":
                    return hueRange.minValue;
                    break;
                case "HueMax":
                    return hueRange.maxValue;
                    break;
                case "SatMin":
                    return saturationRange.minValue;
                    break;
                case "SatMax":
                    return saturationRange.maxValue;
                    break;
                case "ValMin":
                    return valueRange.minValue;
                    break;
                case "ValMax":
                    return valueRange.maxValue;
                    break;
            }
        
            return 1;
        }
    }
}