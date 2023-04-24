using OpenCVForUnity.CoreModule;
using UnityEngine;
using System.Collections.Generic;
using Rect = OpenCVForUnity.CoreModule.Rect;
namespace OpenCVTest
{
    public interface IMarkerDetector
    {
        List<Rect> FindMarkers(ref Mat cameraFeed, ref Texture2D texture, bool flip);
    }
}