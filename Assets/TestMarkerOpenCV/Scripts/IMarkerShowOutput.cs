﻿using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using UnityEngine;

namespace OpenCVTest
{
    public interface IMarkerShowOutput
    {
        void ShowOutputMarkers(List<MarkerLabel> markerDatas, ref Mat cameraFeed, ref Texture2D texture, bool flip, bool _printCoords );
    }
}