using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using UnityEngine;
namespace OptimuscleMarkers
{
    public interface IMarkerDetector
    {
        List<MarkerLabel> FindMarkers(ref Mat cameraFeed, ref Texture2D texture, bool flip);
    }
    public interface IGetMarkerList
    {
        List<MarkerLabel> GetMarkers();
    }
}