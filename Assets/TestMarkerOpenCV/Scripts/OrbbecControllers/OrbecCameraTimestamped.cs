using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OrbbecUnity;
using Orbbec;
using TMPro;
using Point = OpenCVForUnity.CoreModule.Point;

namespace OpenCVTest
{
    public class OrbecCameraTimestamped : MonoBehaviour
    {
        public OrbbecDevice orbbecDevice;
        private OrbbecFrameSource frameSource;
        public bool flip = false;
        public TextMeshProUGUI textDevice;
        Texture2D texture;
        private Sensor depthSensor;
        private Sensor colorSensor;

        private OrbbecImuFrame obImuDepthFrame;
        private OrbbecImuFrame obImuColorFrame;
        //private OrbbecVideoFrame obDepthFrame;
        private IMarkerDetector _markerDetector;
        private uint cam_width = 640;
        private uint cam_height = 480;

        void Start()
        {
            InitTexture((int)cam_width, (int)cam_height);
            _markerDetector = GetComponent<IMarkerDetector>();

            orbbecDevice.onDeviceFound.AddListener(OnDeviceFound);
        }

        private void Update()
        {
            if (obImuColorFrame != null && obImuColorFrame.data != null)
            {
                if (obImuColorFrame.data.Length == 0)
                {
                    return;
                }

                if (obImuColorFrame.frameType != Orbbec.FrameType.OB_FRAME_COLOR ||
                    obImuColorFrame.format != Orbbec.Format.OB_FORMAT_RGB888)
                {
                    return;
                }
    
                UpdateColorTexture(cam_width, cam_height, obImuColorFrame.timestamp,
                    obImuColorFrame.data);
            }
           
        }

        private void OnDeviceFound(Device device)
        {
            try
            {
                var cameraInfo = device.GetDeviceInfo();
                int pid = cameraInfo.Pid();
                int vid = cameraInfo.Vid();
                string uid = cameraInfo.Uid();
                string v_name = cameraInfo.Name();

                Debug.Log($"Camera pid {pid} uid {uid}");
                depthSensor = device.GetSensor(SensorType.OB_SENSOR_DEPTH);

                var dSensorProfileList = depthSensor.GetStreamProfileList();
                var dSensorProfile = dSensorProfileList.GetProfile(0);
                depthSensor.Start(dSensorProfile, OnFrame);

                colorSensor = device.GetSensor(SensorType.OB_SENSOR_COLOR);
                var colorSensorProfileList = colorSensor.GetStreamProfileList();
                var colorSensorProfile = colorSensorProfileList.GetProfile(0);
                colorSensor.Start(colorSensorProfile, OnFrame);

                colorSensorProfileList.Dispose();
                dSensorProfileList.Dispose();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(e);
                textDevice.text = "Device has no imu sensor";
            }
        }

        private void OnFrame(Frame frame)
        {
           
            if (frame.GetFrameType() == FrameType.OB_FRAME_DEPTH)
            {
                var depthFrame = frame.As<DepthFrame>();

                if (depthFrame != null)
                {
                    // var depthValue = depthFrame.G;
                    obImuDepthFrame = new OrbbecImuFrame();
                    //obDepthFrame.value = new Vector3(depthValue.x, depthValue.y, depthValue.z);
                    obImuDepthFrame.format = Format.OB_FORMAT_RGB_POINT;
                    obImuDepthFrame.frameType = FrameType.OB_FRAME_DEPTH;
                    obImuDepthFrame.timestamp = depthFrame.GetTimeStamp();
                    obImuDepthFrame.systemTimestamp = depthFrame.GetSystemTimeStamp();
                    
                }
            }

            obImuColorFrame = null;
            if (frame.GetFrameType() == FrameType.OB_FRAME_COLOR)
            {
       
                //var colorFrame = frame.As<ColorFrame>();
                var colorFrame = frame.As<VideoFrame>();
                if (colorFrame != null)
                {
                    var dataSize = colorFrame.GetDataSize();
                    if(dataSize < 921600) return;
                    obImuColorFrame = new OrbbecImuFrame();
                    obImuColorFrame.format = Format.OB_FORMAT_RGB888;
                    obImuColorFrame.frameType = FrameType.OB_FRAME_COLOR;
                    obImuColorFrame.timestamp = colorFrame.GetTimeStamp();
                    obImuColorFrame.systemTimestamp = colorFrame.GetSystemTimeStamp();
                    
                    obImuColorFrame.data= new byte[dataSize];
                    colorFrame.CopyData(ref obImuColorFrame.data);
                  
                }
            }

            frame.Dispose();
        }

        void InitTexture(int _w, int _h)
        {
            //var obColorFrame = frameSource.GetColorFrame();
            if (texture == null)
            {
                texture = new Texture2D(_w, _h, TextureFormat.RGB24, false);
                GetComponent<Renderer>().material.mainTexture = texture;
                gameObject.transform.localScale = new Vector3(_w, _h, 1);
            }

            if (texture.width != _w || texture.height != _h)
            {
                texture.Reinitialize((int)_w, (int)_h);
            }
        }

        void UpdateColorTexture(uint _w, uint _h, ulong _timeStamp, byte[] _data)
        {
 
            //InitTexture(_w, _h);
            
            Debug.Log($"col _timeStamp: {_timeStamp}");
            texture.LoadRawTextureData(_data);
            texture.Apply();
        
            Mat rgbaMat = new Mat((int)_h, (int)_w, CvType.CV_8UC3);
            Utils.texture2DToMat(texture, rgbaMat, false);
            var markers = _markerDetector.FindMarkers(ref rgbaMat, ref texture, false);
            if (markers.Count > 0)
            {
                int i = 0;
                foreach (var marker in markers)
                {
                    var _pixelX = (uint)marker.x;
                    var _pixelY = (uint)marker.y;
                    var distance = GetDistanceFromCameraToPixel(_pixelX, _pixelY, obImuDepthFrame.data);
                    Imgproc.putText(rgbaMat, $"{distance}mm", new Point(_pixelX - 15, _pixelY + 15), 1, 1,
                        new Scalar(250, 80, 80, 225), 1);
            
                    i++;
                }

                // getTimeStamp
                Utils.matToTexture2D(rgbaMat, texture, flip);
            }
            
        }

        ushort GetDistanceFromCameraToPixel(uint _x, uint _y, byte[] _data)
        {
            if (_data == null) return 0;
            // depth frame data byte array of unsigned short - 2bit
            int pointSize = Marshal.SizeOf(typeof(ushort));
            IntPtr dataPtr = Marshal.AllocHGlobal(_data.Length);
            Marshal.Copy(_data, 0, dataPtr, _data.Length);
            uint data_pointer = 0;
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

        void OnDestroy()
        {
            if (colorSensor != null)
            {
                colorSensor.Stop();
                colorSensor.Dispose();
            }

            if (depthSensor != null)
            {
                depthSensor.Stop();
                depthSensor.Dispose();
            }

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }
    }
}