using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine; //using Orbbec;
using Rect = OpenCVForUnity.CoreModule.Rect;
namespace OptimuscleMarkers.OrbbecControllers
{
    public class BasicMarkerDetector : MonoBehaviour, IMarkerDetector
    {
        public ColourCalibrate[] markerColourRanges;
        [SerializeField] int DILATE_SIZE = 1;
        [SerializeField] int ERODE_SIZE = 1;
        [SerializeField] bool debug;
        [SerializeField] bool printCoords;
        [SerializeField] bool showOutput;
        List<Rect> markers = new List<Rect>();
        public List<MarkerLabel> FindMarkers(ref Mat cameraFeed, ref Texture2D texture, bool flip)
        {
            Mat rgbMat = new Mat();
            Mat hsvMat = new Mat();
            Mat thresholdMat = new();
            Imgproc.cvtColor(cameraFeed, rgbMat, Imgproc.COLOR_RGBA2RGB);
            foreach(var colCal in markerColourRanges)
            {
                if(!colCal.active_setting)continue;
                Imgproc.cvtColor(rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
                Core.inRange(hsvMat, colCal.colorCalibrated.getHSVmin(), colCal.colorCalibrated.getHSVmax(), thresholdMat);
                morphOps(thresholdMat);
                TrackFilteredObject(colCal, thresholdMat, rgbMat);

            }
            Imgproc.cvtColor(rgbMat, cameraFeed, Imgproc.COLOR_RGB2RGBA);
            Utils.matToTexture2D(cameraFeed, texture, flip);
            List<MarkerLabel> markerDatas = new List<MarkerLabel>();
            return markerDatas;
        }
        /// <summary>
        /// Draws the object.
        /// </summary>
        /// <param name="theColorObjects">The color objects.</param>
        /// <param name="_cameraFeed">Frame.</param>
        /// <param name="temp">Temp.</param>
        /// <param name="contours">Contours.</param>
        /// <param name="hierarchy">Hierarchy.</param>
        private void drawObject(List<ColorObject> theColorObjects, Mat _cameraFeed, Mat temp, List<MatOfPoint> contours, Mat hierarchy, ColourCalibrate _colourCalibrate)
        {
            for (int i = 0; i < theColorObjects.Count; i++)
            {
                var p = theColorObjects[i];
                int _pixelX = p.getXPos();
                int _pixelY = p.getYPos();
                Int64 distance = 0;
                var depthScalar = p.getColor();
                // if (depthTexture != null)
                // {
                //     distance = GetDistanceFromCameraToPixel(_pixelX, _pixelY);
                //     var depthPx = depthTexture.GetPixel(_pixelX, _pixelY);
                //     depthScalar = new Scalar(depthPx.r*256, depthPx.g*256, depthPx.b*256, 256);
                // }
               
                
                Imgproc.drawContours(_cameraFeed, contours, i, _colourCalibrate.colorCalibrated.getColor(), 3, 8, hierarchy, int.MaxValue, new Point());
                Imgproc.circle(_cameraFeed, new Point(p.getXPos(), p.getYPos()), 5, _colourCalibrate.colorCalibrated.getColor());
                Imgproc.putText(_cameraFeed, p.getXPos() + " , " + p.getYPos() + " " + distance, new Point(p.getXPos(), p.getYPos() + 20), 1, 1, _colourCalibrate.colorCalibrated.getColor(), 2);
                Imgproc.putText(_cameraFeed, _colourCalibrate.colorCalibrated.getType(), new Point(p.getXPos(), p.getYPos() - 20), 1, 2, _colourCalibrate.colorCalibrated.getColor(), 2);
                Vector3 mPos = Camera.main.transform.forward * distance;
                mPos.x = p.getXPos();
                mPos.y = p.getYPos();
                markers.Add(new Rect(p.getXPos()-2, p.getYPos()-2,4, 4));
                
               
                    
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
            Imgproc.erode(thresh, thresh, erodeElement);

            Imgproc.dilate(thresh, thresh, dilateElement);
            Imgproc.dilate(thresh, thresh, dilateElement);
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
                markers = new List<Rect>();
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
        
    }
}