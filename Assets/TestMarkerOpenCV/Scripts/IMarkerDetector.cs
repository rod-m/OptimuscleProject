using OpenCVForUnity.CoreModule;
using UnityEngine;

namespace OpenCVTest
{
    public interface IMarkerDetector
    {
        void FindMarkers(ref Mat cameraFeed, ref Texture2D texture, bool flip);
    }
}