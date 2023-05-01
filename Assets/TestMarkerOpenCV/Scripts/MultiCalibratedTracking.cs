#if !(PLATFORM_LUMIN && !UNITY_EDITOR)
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnityExample;
//using Orbbec;
using OrbbecUnity;

namespace OpenCVTest
{
    public class MultiCalibratedTracking : MonoBehaviour
    {
        public bool enablePointCloudObjects = false;
        public bool flip = false;
        public OrbbecFrameSource frameSource;
        private OrbbecVideoFrame obDepthFrame;
        private Texture2D depthTexture;
        public Transform markerParent;
        public Transform markerModel;

        private List<Transform> markerTransforms = new List<Transform>();
        
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;
        
        private int cam_width = 640;
        private int cam_height = 480;
        
        private bool useDepthColour = false;

        private ColourCalibrate colourCalibrate;

        private IMarkerDetector _markerDetector;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        // Use this for initialization
        void Start()
        {
            _markerDetector = GetComponent<IMarkerDetector>();
            fpsMonitor = GetComponent<FpsMonitor>();

            //webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

#if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
            // webCamTextureToMatHelper.Initialize();
        }

        // Update is called once per frame
        void Update()
        {
            UpdateDepthTexture();
            UpdateColorTexture();
        }


        

        void SetupFPSMonitor()
        {
            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", cam_width.ToString());
                fpsMonitor.Add("height", cam_height.ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }
        }

        void UpdateDepthTexture()
        {
            obDepthFrame = frameSource.GetDepthFrame();
            if (obDepthFrame == null || obDepthFrame.width == 0 || obDepthFrame.height == 0 ||
                obDepthFrame.data == null || obDepthFrame.data.Length == 0)
            {
                return;
            }

            if (obDepthFrame.frameType != Orbbec.FrameType.OB_FRAME_DEPTH)
            {
                return;
            }

            if (depthTexture == null)
            {
                depthTexture = new Texture2D(obDepthFrame.width, obDepthFrame.height, TextureFormat.RG16, false);
                cam_width = obDepthFrame.width;
                cam_height = obDepthFrame.height;

                //GetComponent<Renderer>().material.mainTexture = depthTexture;
                SetupFPSMonitor();
            }

            if (depthTexture.width != obDepthFrame.width || depthTexture.height != obDepthFrame.height)
            {
                depthTexture.Reinitialize(obDepthFrame.width, obDepthFrame.height);
            }

            depthTexture.LoadRawTextureData(obDepthFrame.data);
            depthTexture.Apply();
            Mat rgbaMat = new Mat(obDepthFrame.height, obDepthFrame.width, CvType.CV_8UC3);
            Utils.texture2DToMat(depthTexture, rgbaMat, flip);
            Utils.matToTexture2D(rgbaMat, depthTexture, flip);
        }

        void UpdateColorTexture()
        {
            var obColorFrame = frameSource.GetColorFrame();

            if (obColorFrame == null || obColorFrame.width == 0 || obColorFrame.height == 0 ||
                obColorFrame.data == null || obColorFrame.data.Length == 0)
            {
                return;
            }

            if (obColorFrame.frameType != Orbbec.FrameType.OB_FRAME_COLOR ||
                obColorFrame.format != Orbbec.Format.OB_FORMAT_RGB888)
            {
                return;
            }

            if (texture == null)
            {
                texture = new Texture2D(obColorFrame.width, obColorFrame.height, TextureFormat.RGB24, false);
                GetComponent<Renderer>().material.mainTexture = texture;
                gameObject.transform.localScale = new Vector3(obColorFrame.width, obColorFrame.height, 1);
            }

            if (texture.width != obColorFrame.width || texture.height != obColorFrame.height)
            {
                texture.Reinitialize(obColorFrame.width, obColorFrame.height);
            }

            var timeStamp = obColorFrame.systemTimestamp;
            var timeStampDepth = obDepthFrame.systemTimestamp;
            Debug.Log($"col time: {timeStamp} > {timeStampDepth}");
            texture.LoadRawTextureData(obColorFrame.data);
            texture.Apply();
            //Mat rgbaMat = webCamTextureToMatHelper.GetMat();
            Mat rgbaMat = new Mat(obColorFrame.height, obColorFrame.width, CvType.CV_8UC3);
            Utils.texture2DToMat(texture, rgbaMat, false);
            var markers = _markerDetector.FindMarkers(ref rgbaMat, ref texture, false);
            if (markers.Count > 0 && depthTexture != null)
            {
                int i = 0;
                foreach (var marker in markers)
                {
                    var _pixelX = marker.x;
                    var _pixelY = marker.y;
                    var distance = GetDistanceFromCameraToPixel(_pixelX, _pixelY, obDepthFrame.data);
                    Imgproc.putText(rgbaMat, $"{distance}mm", new Point(_pixelX - 15, _pixelY + 15), 1, 1,
                        new Scalar(250, 80, 80, 225), 1);
                    PointCloud(i, _pixelX, _pixelY, distance);
                    i++;
                }
                // getTimeStamp
                Utils.matToTexture2D(rgbaMat, texture, flip);
            }
        }

        void PointCloud(int index, int _pixelX, int _pixelY, ushort distance)
        {
            if (distance > 0 && enablePointCloudObjects)
            {
                Vector3 mPos = new Vector3(_pixelX, _pixelY, distance);
                if (markerTransforms.Count <= index)
                {
                    var posM = Instantiate(markerModel, mPos, Quaternion.identity, markerParent);
                    posM.localPosition = mPos;
                    markerTransforms.Add(posM);
                }
                else
                {
                    var mk = markerTransforms[index];
                    mk.position = mPos;
                }
            }
        }

        ushort GetDistanceFromCameraToPixel(int _x, int _y, byte[] _data)
        {
            if (_data == null) return 0;
            // depth frame data byte array of unsigned short - 2bit
            int pointSize = Marshal.SizeOf(typeof(ushort));
            IntPtr dataPtr = Marshal.AllocHGlobal(_data.Length);
            Marshal.Copy(_data, 0, dataPtr, _data.Length);
            int data_pointer = 0;
            if (_y > 0)
            {
                // add up all rows, subtract one to get all complete rows
                data_pointer = cam_width * (_y - 1);
            }

            // add x as remainder of final y row
            data_pointer += _x;
            IntPtr pointPtrZ = new IntPtr(dataPtr.ToInt64() + (data_pointer * pointSize));
            ushort distance = Marshal.PtrToStructure<ushort>(pointPtrZ);
            Marshal.FreeHGlobal(dataPtr);
            return distance; // is mm
        }


        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            //webCamTextureToMatHelper.Dispose();
            OnWebCamTextureToMatHelperDisposed();
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            //webCamTextureToMatHelper.Play();
            useDepthColour = !useDepthColour;
            if (useDepthColour)
            {
                GetComponent<Renderer>().material.mainTexture = depthTexture;
            }
            else
            {
                GetComponent<Renderer>().material.mainTexture = texture;
            }
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            //webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            //webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            //webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
        }
        
        
        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

    
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

    }
}

#endif