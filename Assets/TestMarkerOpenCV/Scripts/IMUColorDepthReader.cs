using System;
using System.Collections;
using System.Collections.Generic;
using Orbbec;
using OrbbecUnity;
using UnityEngine;
using UnityEngine.UI;

namespace OpenCVTest
{
    public class IMUColorDepthReader : MonoBehaviour
    {
        public OrbbecDevice orbbecDevice;
        public Text textDevice;

        public Text textDepth;
        public Text textColor;


        private Sensor depthSensor;
        private Sensor colorSensor;

        private OrbbecImuFrame obDepthFrame;
        private OrbbecImuFrame obColorFrame;

        private ulong timestampColor;
        private ulong timestampDepth;

        private ulong timeDiff;
        private ulong timeCount;
        private ulong timeAverage;

        private ulong timeTotal;

        private ulong[] cameraColorTimestamp = new ulong[4];
        private ulong[] cameraDepthTimestamp = new ulong[4];

        //private Dictionary<>
        // Start is called before the first frame update
        void Start()
        {
            orbbecDevice.onDeviceFound.AddListener(OnDeviceFound);
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
                    obDepthFrame = new OrbbecImuFrame();
                    //obDepthFrame.value = new Vector3(depthValue.x, depthValue.y, depthValue.z);
                    obDepthFrame.format = Format.OB_FORMAT_RGB_POINT;
                    obDepthFrame.frameType = FrameType.OB_FRAME_DEPTH;
                    obDepthFrame.timestamp = depthFrame.GetTimeStamp();
                    obDepthFrame.systemTimestamp = depthFrame.GetSystemTimeStamp();

                    //Debug.LogFormat("depthValue:({0})", depthValue);
                    // accelFrame.Dispose();
                }
            }

            if (frame.GetFrameType() == FrameType.OB_FRAME_COLOR)
            {
                //Orbbec.FrameType.OB_FRAME_DEPTH
                var colorFrame = frame.As<ColorFrame>();
                if (colorFrame != null)
                {
                    //var coloralue = colorFrame.GetValueScale();
                    obColorFrame = new OrbbecImuFrame();
                    //obColorFrame.value = new Vector3(depthValue.x, depthValue.y, depthValue.z);
                    obColorFrame.format = Format.OB_FORMAT_RGB888;
                    obColorFrame.frameType = FrameType.OB_FRAME_COLOR;
                    obColorFrame.timestamp = colorFrame.GetTimeStamp();
                    obColorFrame.systemTimestamp = colorFrame.GetSystemTimeStamp();

                    //Debug.LogFormat("depthValue:({0})", depthValue);
                    // accelFrame.Dispose();
                }
            }

            frame.Dispose();
        }

        void Update()
        {
            ulong newDiff;
            bool colorBeforeDepth = timestampColor < timestampDepth;
            if (colorBeforeDepth)
            {
                newDiff = timestampDepth - timestampColor;
            }
            else
            {
                newDiff = timestampColor - timestampDepth;
            }

            timeTotal += newDiff;
            timeCount++;


            timeAverage = timeTotal / timeCount;
            if (newDiff > timeDiff)
            {
                timeDiff = newDiff;
            }

            if (obDepthFrame != null)
            {
                timestampDepth = obDepthFrame.timestamp;
                textDepth.text = string.Format("Depth Timestamp:{0}",
                    obDepthFrame.timestamp);
            }

            if (obColorFrame != null)
            {
                timestampColor = obColorFrame.timestamp;
                textColor.text = string.Format(
                    "Color Timestamp:{0}\n\nCurrent time delay is {1}\n\nMaximum Time delay between Color & Depth:{2}\nAverage delay {3}\ncolor Before Depth {4}",
                    obColorFrame.timestamp, newDiff, timeDiff, timeAverage, colorBeforeDepth);
            }
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
        }
    }
}