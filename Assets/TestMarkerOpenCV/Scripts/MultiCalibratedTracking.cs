
#if !(PLATFORM_LUMIN && !UNITY_EDITOR)
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
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
        public OrbbecFrameSource frameSource;
        private Texture2D depthTexture;
        public Transform markerParent;
        public Transform markerModel;
        public int numberMarkers = 30;
        private List<Transform> markerTransforms = new List<Transform>();
        //private Texture2D colorTexture;
        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;
        
        /// <summary>
        /// max number of objects to be detected in frame
        /// </summary>
        [SerializeField] int MAX_NUM_OBJECTS = 24;   
        /// <summary>
        /// max number of objects to be detected in frame
        /// </summary>
        [SerializeField] int DILATE_SIZE = 6;
        [SerializeField] int ERODE_SIZE = 2;

        /// <summary>
        /// minimum and maximum object area
        /// </summary>
        int MIN_OBJECT_AREA = 15 * 15; //20 * 20;

        private int cam_width = 640;
        private int cam_height = 480;
        /// <summary>
        /// max object area
        /// </summary>
        int MAX_OBJECT_AREA = 40 * 40;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The threshold mat.
        /// </summary>
        Mat thresholdMat;

        /// <summary>
        /// The hsv mat.
        /// </summary>
        Mat hsvMat;

        private bool useDepthColour = false;

        public ColourCalibrate[] markerColourRanges;
        private ColourCalibrate colourCalibrate;


      
        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        // Use this for initialization
        void Start()
        {
          
            fpsMonitor = GetComponent<FpsMonitor>();

            //webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

#if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
           // webCamTextureToMatHelper.Initialize();
          
        }



        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (rgbMat != null)
                rgbMat.Dispose();
            if (thresholdMat != null)
                thresholdMat.Dispose();
            if (hsvMat != null)
                hsvMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
            if (grayMat != null)
                grayMat.Dispose();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update()
        {
            UpdateDepthTexture();
            UpdateColorTexture();
           
        }
        

       
        private OrbbecVideoFrame obDepthFrame;

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
            if(obDepthFrame ==null || obDepthFrame.width == 0 || obDepthFrame.height == 0 || obDepthFrame.data == null || obDepthFrame.data.Length == 0)
            {
                return;
            }
            if(obDepthFrame.frameType != Orbbec.FrameType.OB_FRAME_DEPTH)
            {
                return;
            }
            if(depthTexture == null)
            {
                depthTexture = new Texture2D(obDepthFrame.width, obDepthFrame.height, TextureFormat.RG16, false);
                cam_width = obDepthFrame.width;
                cam_height = obDepthFrame.height;
                
                //GetComponent<Renderer>().material.mainTexture = depthTexture;
                SetupFPSMonitor();
            }
            if(depthTexture.width != obDepthFrame.width || depthTexture.height != obDepthFrame.height)
            {
                depthTexture.Reinitialize(obDepthFrame.width, obDepthFrame.height);
            }
            depthTexture.LoadRawTextureData(obDepthFrame.data);
            depthTexture.Apply();
            Mat rgbaMat = new Mat(obDepthFrame.height, obDepthFrame.width, CvType.CV_8UC3);
            Utils.texture2DToMat(depthTexture, rgbaMat, false);
            Utils.matToTexture2D(rgbaMat, depthTexture);
        }
        void UpdateColorTexture()
        {
            var obColorFrame = frameSource.GetColorFrame();

            if(obColorFrame == null || obColorFrame.width == 0 || obColorFrame.height == 0 || obColorFrame.data == null || obColorFrame.data.Length == 0)
            {
                return;
            }
            if(obColorFrame.frameType != Orbbec.FrameType.OB_FRAME_COLOR || obColorFrame.format != Orbbec.Format.OB_FORMAT_RGB888)
            {
                return;
            }
            if(texture == null)
            {
                texture = new Texture2D(obColorFrame.width, obColorFrame.height, TextureFormat.RGB24, false);
                GetComponent<Renderer>().material.mainTexture = texture;
                gameObject.transform.localScale = new Vector3(obColorFrame.width, obColorFrame.height, 1);
                rgbMat = new Mat(obColorFrame.height, obColorFrame.width, CvType.CV_8UC3);
                thresholdMat = new Mat();
                hsvMat = new Mat();
                grayMat = new Mat(obColorFrame.height, obColorFrame.width, CvType.CV_8UC1);
            }
            if(texture.width != obColorFrame.width || texture.height != obColorFrame.height)
            {
                texture.Reinitialize(obColorFrame.width, obColorFrame.height);
            }
            texture.LoadRawTextureData(obColorFrame.data);
            texture.Apply();
            //Mat rgbaMat = webCamTextureToMatHelper.GetMat();
            Mat rgbaMat = new Mat(obColorFrame.height, obColorFrame.width, CvType.CV_8UC3);
            Utils.texture2DToMat(texture, rgbaMat, false);
            FindMarkers(rgbaMat);
            
        }
      
        private void FindMarkers(Mat rgbaMat)
        {
   
            Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
            foreach(var colCal in markerColourRanges)
            {
                if(!colCal.active_setting)continue;
                Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
                Core.inRange(hsvMat, colCal.colorCalibrated.getHSVmin(), colCal.colorCalibrated.getHSVmax(), thresholdMat);
                morphOps(thresholdMat);
                TrackFilteredObject(colCal, thresholdMat, rgbMat);

            }
            Imgproc.cvtColor(rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);

            Utils.matToTexture2D(rgbaMat, texture);
        }
        ushort GetDistanceFromCameraToPixel(int _x, int _y)
        {
            if (obDepthFrame == null) return 0;
            // depth frame data byte array of unsigned short - 2bit
            int pointSize = Marshal.SizeOf(typeof(ushort));
            IntPtr dataPtr = Marshal.AllocHGlobal(obDepthFrame.data.Length);
            Marshal.Copy(obDepthFrame.data, 0, dataPtr, obDepthFrame.data.Length);
            int data_pointer = 0;
            if(_y > 0)
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
        /// Draws the object.
        /// </summary>
        /// <param name="theColorObjects">The color objects.</param>
        /// <param name="frame">Frame.</param>
        /// <param name="temp">Temp.</param>
        /// <param name="contours">Contours.</param>
        /// <param name="hierarchy">Hierarchy.</param>
        private void drawObject(List<ColorObject> theColorObjects, Mat frame, Mat temp, List<MatOfPoint> contours, Mat hierarchy, ColourCalibrate _colourCalibrate)
        {
            for (int i = 0; i < theColorObjects.Count; i++)
            {
                var p = theColorObjects[i];
                int _pixelX = p.getXPos();
                int _pixelY = p.getYPos();
                Int64 distance = 0;
                var depthScalar = p.getColor();
                if (depthTexture != null)
                {
                    distance = GetDistanceFromCameraToPixel(_pixelX, _pixelY);
                    var depthPx = depthTexture.GetPixel(_pixelX, _pixelY);
                    depthScalar = new Scalar(depthPx.r*256, depthPx.g*256, depthPx.b*256, 256);
                }
               
                
                Imgproc.drawContours(frame, contours, i, _colourCalibrate.colorCalibrated.getColor(), 3, 8, hierarchy, int.MaxValue, new Point());
                Imgproc.circle(frame, new Point(p.getXPos(), p.getYPos()), 5, _colourCalibrate.colorCalibrated.getColor());
                Imgproc.putText(frame, p.getXPos() + " , " + p.getYPos() + " " + distance, new Point(p.getXPos(), p.getYPos() + 20), 1, 1, _colourCalibrate.colorCalibrated.getColor(), 2);
                Imgproc.putText(frame, _colourCalibrate.colorCalibrated.getType(), new Point(p.getXPos(), p.getYPos() - 20), 1, 2, _colourCalibrate.colorCalibrated.getColor(), 2);
                Vector3 mPos = Camera.main.transform.forward * distance;
                mPos.x = p.getXPos();
                mPos.y = p.getYPos();
                if (distance > 0 && enablePointCloudObjects)
                {
                    if (markerTransforms.Count <= i)
                    {
                        var posM = Instantiate(markerModel, mPos, Quaternion.identity, markerParent);
                        posM.localPosition = mPos;
                        markerTransforms.Add(posM);
                    }else
                    {

                        var mk = markerTransforms[i];
                        mk.position = mPos;
                    }
                }
               
                    
            }
        }

        /// <summary>
        /// Morphs the ops.
        /// </summary>
        /// <param name="thresh">Thresh.</param>
        private void morphOps(Mat thresh)
        {
            //create structuring element that will be used to "dilate" and "erode" image.
            //the element chosen here is a 3px by 3px rectangle
            Mat erodeElement = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(ERODE_SIZE, ERODE_SIZE));
            //dilate with larger element so make sure object is nicely visible 8x8
            Mat dilateElement = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(DILATE_SIZE, DILATE_SIZE));

            Imgproc.erode(thresh, thresh, erodeElement);
           // Imgproc.erode(thresh, thresh, erodeElement);

            Imgproc.dilate(thresh, thresh, dilateElement);
            //Imgproc.dilate(thresh, thresh, dilateElement);
        }

        /// <summary>
        /// Tracks the filtered object.
        /// </summary>
        /// <param name="theColorObject">The color object.</param>
        /// <param name="threshold">Threshold.</param>
        /// <param name="HSV">HS.</param>
        /// <param name="cameraFeed">Camera feed.</param>
        private void TrackFilteredObject(ColourCalibrate _colourCalibrate, Mat threshold, Mat cameraFeed)
        {

            List<ColorObject> colorObjects = new List<ColorObject>();
            Mat temp = new Mat();
            threshold.copyTo(temp);
            //these two vectors needed for output of findContours
            List<MatOfPoint> contours = new List<MatOfPoint>();
            Mat hierarchy = new Mat();
            //find contours of filtered image using openCV findContours function
            Imgproc.findContours(temp, contours, hierarchy, Imgproc.RETR_CCOMP, Imgproc.CHAIN_APPROX_SIMPLE);

            bool colorObjectFound = false;
            if (hierarchy.rows() > 0)
            {
                int numObjects = hierarchy.rows();

                //if number of objects greater than MAX_NUM_OBJECTS we have a noisy filter
                if (numObjects < _colourCalibrate.max_objects)
                {
                    for (int index = 0; index >= 0; index = (int)hierarchy.get(0, index)[0])
                    {

                        Moments moment = Imgproc.moments(contours[index]);
                        double area = moment.get_m00();

                        //if the area is less than 20 px by 20px then it is probably just noise
                        //if the area is the same as the 3/2 of the image size, probably just a bad filter
                        //we only want the object with the largest area so we safe a reference area each
                        //iteration and compare it to the area in the next iteration.
                        if (area > _colourCalibrate.object_area.minValue && area < _colourCalibrate.object_area.maxValue)
                        {

                            ColorObject colorObject = new ColorObject();

                            colorObject.setXPos((int)(moment.get_m10() / area));
                            colorObject.setYPos((int)(moment.get_m01() / area));
                            colorObject.setType(_colourCalibrate.colorCalibrated.getType());
                            
                            colorObjects.Add(colorObject);
                            colorObjectFound = true;

                        }
                        else
                        {
                            colorObjectFound = false;
                        }
                    }
                    //let user know you found an object
                    if (colorObjectFound)
                    {
                        //draw object location on screen
                        drawObject(colorObjects, cameraFeed, temp, contours, hierarchy, _colourCalibrate);
                    }

                }
                else
                {
                    Imgproc.putText(cameraFeed, "TOO MUCH NOISE!", new Point(5, cameraFeed.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(250, 250, 250, 255), 2, Imgproc.LINE_AA, false);
                }
            }
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
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            //SceneManager.LoadScene("OpenCVForUnityExample");
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
    }
}

#endif