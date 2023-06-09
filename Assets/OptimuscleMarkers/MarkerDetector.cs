﻿using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OptimuscleMarkers
{
    public enum CleanOption
    {
        GrowOne,
        GrowTwo,
        GrowThree
    }

    public class MarkerDetector : MonoBehaviour, IMarkerDetector, IMarkerShowOutput
    {
        protected Texture2D texture;
        protected Mat cameraFeed;
        
        /// <summary>
        /// scriptable object for tuning colour range
        /// </summary>
        [Header("Add a scriptable object from Assets->HSVCalibrate")]
        [SerializeField] HSVCalibrate markerColourRange;
        
        public void SetHueMin(int value)
        {
            markerColourRange.hueRange.minValue = value;
            PlayerPrefs.SetInt("HueMin", value);
        }
        public void SetHueMax(int value)
        {
            markerColourRange.hueRange.maxValue = value;
            PlayerPrefs.SetInt("HueMax", value);
        }
        [Header("Debugging and callibrating")]
        [SerializeField] bool debug;
        [SerializeField] bool printCoords;
      

        /// <summary>
        /// visualise what the object detector can see
        /// </summary>
        [Header("Visualise what the object detector can see")]
        [SerializeField] bool showCombinedHSVMat;

        private bool flip;
        Scalar hueFrom;
        Scalar hueTo;

        Scalar satFrom;
        Scalar satTo;

        Scalar valFrom;
        Scalar valTo;

        /// <summary>
        /// How much cleaning up of material to do
        /// </summary>
        [Header("How much cleaning up of material to do")]
        [SerializeField] CleanOption clean = CleanOption.GrowOne;


        [Header("Define camera size")] public int gridRows = 480;
        public int gridCols = 640;

        List<Rect> markers = new List<Rect>();
        private List<Rect> markerBoundGrid = new List<Rect>();
        private Rect boundsRectangle = new Rect();
        private int x, y, w, h;
        private int width;
        private int height;
        private int minX, minY, maxX, maxY;

        
        private void Start()
        {
            width = gridCols;
            height = gridRows;
            maxX = width;
            maxY = height;
            MarkerLabelGridSetup();
    
        }

        int rectSizeSortFunction(Rect a, Rect b)
        {
            if (b.area() < a.area())
            {
                return 1;
            }

            return -1;
        }
        int distSortFunction(Vector2Int a, Vector2Int b)
        {
            if (a[1] < b[1])
            {
                return 1;
            }

            return -1;
        }

        bool rectsOverlap(Rect a, Rect b)
        {
            return (a & b).area() > 0;
        }

        bool rectsAreClose(Rect a, Rect b, int dist)
        {
            a += new Point(-dist / 2, -dist / 2);
            a += new Size(dist, dist);

            return (a & b).area() > 0;
        }

        double ScalarVal(Scalar a)
        {
            return a.val[0] + a.val[1] + a.val[2] + a.val[3];
        }

        void DrawMarkerLabelGrid()
        {
            Scalar cellColor = new Scalar(40, 125, 155);
            foreach (var cellRect in markerBoundGrid)
            {
                Imgproc.rectangle(cameraFeed, cellRect, cellColor);
            }
        }


        void MarkerLabelGridSetup()
        {
            bool checkBounds = UpdateBoundsParams(ref minX, ref minY, ref maxX, ref maxY);
            if (!checkBounds) return;
            markerBoundGrid = new List<Rect>();
            Debug.Log("Updating Marker Detection Grid");
            //markerColumns
            for (int r = 0; r < markerColourRange.markerRows; r++)
            {
                for (int c = 0; c < markerColourRange.markerColumns; c++)
                {
                    Rect cellRect = new Rect();
                    cellRect.width = boundsRectangle.width / markerColourRange.markerColumns;
                    cellRect.height = boundsRectangle.height / markerColourRange.markerRows;
                    cellRect.x = boundsRectangle.x + c * cellRect.width;
                    cellRect.y = boundsRectangle.y + r * cellRect.height;
                    markerBoundGrid.Add(cellRect);
                }
            }
        }

        string GetMarkerGridLabel(Rect cell)
        {
            int _gr = boundsRectangle.height / markerColourRange.markerRows;
            int _gc = boundsRectangle.width / markerColourRange.markerColumns;
            int rel_x = cell.x - boundsRectangle.x;
            int rel_y = cell.y - boundsRectangle.y;
            int r = rel_y / _gr;
            int c = rel_x / _gc;
            string _label = $"r{r}-c{c}";

            return _label;
        }

        public List<MarkerLabel> FindMarkers(ref Mat _cameraFeed, ref Texture2D _texture, bool flip)
        {
            cameraFeed = _cameraFeed;
            texture = _texture;
            Mat hsvImg = new Mat();
            //cvtColor(img, hsvImg, cv::COLOR_BGR2HSV);
            Imgproc.cvtColor(cameraFeed, hsvImg, Imgproc.COLOR_BGR2HSV);

            List<Mat> hsv = new List<Mat>();
            Core.split(hsvImg, hsv);
            //Hue threshold 
            Mat hueMasked = new Mat();
            // if (PlayerPrefs.HasKey("HueMin"))
            // {
            //     hueFrom = new Scalar(PlayerPrefs.GetInt("HueMin"));
            // }
            // else
            // {
                 hueFrom = markerColourRange.GetHFrom();
            // }
            
            hueTo = markerColourRange.GetHTo();
            if (hueFrom.val[0] < hueTo.val[0])
                Core.inRange(hsv[0], hueFrom, hueTo, hueMasked);

            else
            {
                Core.inRange(hsv[0], hueTo, hueFrom, hueMasked);
                Core.bitwise_not(hueMasked, hueMasked);
            }

            //Saturation threshold
            satFrom = markerColourRange.GetSatFrom();
            satTo = markerColourRange.GetSatTo();
            Mat saturationMasked = new Mat();
            Core.inRange(hsv[1], satFrom, satTo, saturationMasked);
            //Value threshold
            valFrom = markerColourRange.GetValFrom();
            valTo = markerColourRange.GetValTo();
            Mat valueMasked = new Mat();
            Core.inRange(hsv[2], valFrom, valTo, valueMasked);

            //Combine
            Mat combined = new Mat();

            Core.bitwise_and(hueMasked, saturationMasked, combined);
            Core.bitwise_and(combined, valueMasked, combined);
            //
            //Clean with dilate and erode
            Mat thresh = new Mat();
            Point anchor = new Point(-1, -1);
            switch (clean)
            {
                case CleanOption.GrowOne:
                    //Just grow one
                    Imgproc.dilate(combined, combined, thresh, anchor, 1);
                    break;
                case CleanOption.GrowTwo:
                    Imgproc.dilate(combined, combined, thresh, anchor, 2); //Connect fractured parts
                    Imgproc.erode(combined, combined, thresh, anchor, 3); //Shrink back and beyond to remove tiny blobs
                    Imgproc.dilate(combined, combined, thresh, anchor, 1); //Grow back to normal size
                    break;
                case CleanOption.GrowThree:
                    Imgproc.dilate(combined, combined, thresh, anchor, 3); //Connect fractured parts
                    Imgproc.erode(combined, combined, thresh, anchor, 4); //Shrink back and beyond to remove tiny blobs
                    Imgproc.dilate(combined, combined, thresh, anchor, 1); //Grow back to normal size
                    break;
           
            }

            if (showCombinedHSVMat)
            {
                Imgproc.cvtColor(combined, cameraFeed, Imgproc.COLOR_RGB2RGBA);
            }

            //Connected components
            Mat labels = new Mat();
            Mat stats = new Mat();
            Mat centroids = new Mat();
            int total = Imgproc.connectedComponentsWithStats(combined, labels, stats, centroids);

            //Filter components

            List<Rect> connectedComponents = new List<Rect>(total);
            int x, y, w, h;
            width = cameraFeed.cols();
            height = cameraFeed.rows();
            int minX = 0, minY = 0, maxX = width, maxY = height;
            bool checkBounds = UpdateBoundsParams(ref minX, ref minY, ref maxX, ref maxY);

            bool add;
            for (int i = 0; i < stats.rows(); i++)
            {
                x = (int) stats.get(i, Imgproc.CC_STAT_LEFT)[0];
                y = (int) stats.get(i, Imgproc.CC_STAT_TOP)[0];
                w = (int) stats.get(i, Imgproc.CC_STAT_WIDTH)[0];
                h = (int) stats.get(i, Imgproc.CC_STAT_HEIGHT)[0];
                add = true;

                if (markerColourRange.sizeRange.maxValue > 1)
                    add = w <= markerColourRange.sizeRange.maxValue && h <= markerColourRange.sizeRange.maxValue;
                if (add && markerColourRange.sizeRange.minValue > 0)
                    add = w >= markerColourRange.sizeRange.minValue && h >= markerColourRange.sizeRange.minValue;

                if (checkBounds)
                    add = add && x >= minX && x + w <= maxX && y >= minY && y + h <= maxY;
                if (add)
                {
                    Rect rect = new Rect(x, y, w, h);
                    connectedComponents.Add(rect);
                }
            }


            int _markerCount = 0;
            //Merge overlapping and reject components that are too close to others
            if (connectedComponents.Count > 0)
            {
                //Sort by size
                connectedComponents.Sort(rectSizeSortFunction);
                markers = new List<Rect>();
                foreach (var it in connectedComponents)
                {
                    bool merged = false;

                    foreach (var it2 in markers)
                    {
                        if (rectsOverlap(it, it2))
                        {
                            merged = true;
                            break;
                        }
                    }

                    bool keep = !merged;
                    if (!merged && markerColourRange.minDistance > 0)
                    {
                        foreach (var it2 in markers)
                        {
                            if (rectsAreClose(it, it2, markerColourRange.minDistance))
                            {
                                keep = false;
                                break;
                            }
                        }
                    }

                    if (keep)
                    {
                        _markerCount++;
                        markers.Add(it);
                    }
                }
            }

            //Assign labels

            List<List<Rect>> grid = new List<List<Rect>>();
            List<MarkerLabel> markerDatas = new List<MarkerLabel>();
            if (_markerCount > 0 && gridRows > 0 && gridCols > 0)
            {
                int gridLeft = width;
                int gridRight = 0;
                int gridTop = height;
                int gridBottom = 0;
                //int maxMarkers = gridRows * gridCols;
                //Calculate marker distances to center
                List<Vector2Int> distances = new List<Vector2Int>();

                Vector2 center = new Vector2(width / 2, height / 2);
                int m = 0;
                foreach (var it in markers)
                {
                    Vector2 _p1 = new Vector2(it.x, it.y);
                    int dist = (int) Vector2.Distance(_p1, center);
                    Vector2Int d = new Vector2Int(m, dist);
                    m++;
                    distances.Add(d);
                }

                //Sort
                distances.Sort(distSortFunction);


                //Get bounding box around markers
                foreach (var d in distances)
                {
                    var marker = markers[d.x];
                    if (marker.x < gridLeft)
                        gridLeft = marker.x;
                    if (marker.y < gridTop)
                        gridTop = marker.y;
                    if (marker.x + marker.width > gridRight)
                        gridRight = marker.x + marker.width;
                    if (marker.y + marker.height > gridBottom)
                        gridBottom = marker.y + marker.height;
                    MarkerLabel _markerData = new MarkerLabel();
                    _markerData.label = GetMarkerGridLabel(marker);
                    _markerData.distance = 0;
                    _markerData.x = marker.x;
                    _markerData.y = marker.y;
                    _markerData.width = marker.width;
                    _markerData.height = marker.height;
                    markerDatas.Add(_markerData);
                }

                //Create grid cells
                int cellWidth = 640;
                int cellHeight = 480;
                if (gridCols > 1)
                    cellWidth = (gridRight - gridLeft) / (gridCols - 1);
                if (gridRows > 1)
                    cellHeight = (gridBottom - gridTop) / (gridRows - 1);
                int left = gridLeft - cellWidth / 2;
                int top = gridTop - cellHeight / 2;
                for (int r = 0; r < gridRows; r++)
                {
                    List<Rect> row = new List<Rect>();
                    for (int c = 0; c < gridCols; c++)
                    {
                        row.Add(new Rect(left + c * cellWidth, top + r * cellHeight, cellWidth, cellHeight));
                    }

                    grid.Add(row);
                }

                //Draw on image
                // Area of interest

                if (debug && checkBounds)
                {
                    Imgproc.rectangle(cameraFeed, boundsRectangle,
                        new Scalar(90, 128, 255));
                }

                // Grid
                if (debug && gridRight > 0)
                {
                    //Bounding box
                    Imgproc.rectangle(cameraFeed,
                        new Rect(gridLeft, gridTop, gridRight - gridLeft, gridBottom - gridTop),
                        new Scalar(0, 255, 255));
                    //Cells
                    Scalar cellColor = new Scalar(0, 0, 255);
                    for (int r = 0; r < gridRows; r++)
                    {
                        for (int c = 0; c < gridCols; c++)
                        {
                            Imgproc.rectangle(cameraFeed, grid[r][c], cellColor);
                        }
                    }
                }

                if (debug)
                {
                    if (markerColourRange.liveUpdateGrid)
                    {
                        MarkerLabelGridSetup();
                    }
                    DrawMarkerLabelGrid();

                }
            }

            return markerDatas;
        }

        public void ShowOutputMarkers(List<MarkerLabel> markerDatas)
        {
            foreach (var it in markerDatas)
            {
                if (it.distance == 0) continue;
                x = it.x;
                y = it.y;
                w = it.width;
                h = it.height;
                Rect boundRect = new Rect(x - 1, y - 1, w + 2, h + 2);
                Imgproc.rectangle(cameraFeed, boundRect.tl(), boundRect.br(),
                    markerColourRange.GetMarkerDisplayColor(), markerColourRange.thickness, 8, 0);
                if (printCoords)
                {
                    Imgproc.putText(cameraFeed, $"{it.distance}", new Point(x - 10, y - 15), 1, 1.6,
                        new Scalar(50, 250, 50, 255), 1);
                }
            }

            Utils.matToTexture2D(cameraFeed, texture, flip);
        }

        private bool UpdateBoundsParams(ref int minX, ref int minY, ref int maxX, ref int maxY)
        {
            bool checkBounds = markerColourRange.boundHorizontal.minValue > 0.0 ||
                               markerColourRange.boundVertical.minValue > 0.0 ||
                               markerColourRange.boundHorizontal.maxValue > 1.0 ||
                               markerColourRange.boundVertical.maxValue > 1.0;

            if (checkBounds)
            {
                minX = (int) ((double) width * markerColourRange.boundHorizontal.minValue / 100.0);
                minY = (int) ((double) height * markerColourRange.boundVertical.minValue / 100.0);
                maxX = (int) ((double) width * markerColourRange.boundHorizontal.maxValue / 100.0);
                maxY = (int) ((double) height * markerColourRange.boundVertical.maxValue / 100.0);
                boundsRectangle = new Rect(minX, minY, maxX - minX, maxY - minY);
            }

            return checkBounds;
        }

      
    }
}