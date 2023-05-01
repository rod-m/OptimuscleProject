using System;
using System.Collections.Generic;
using Intel.RealSense;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVTest;
using UnityEngine;
using UnityEngine.Events;

namespace TestMarkerOpenCV.Scripts.RealSense
{
    public class RealSenseDepthTracker : MonoBehaviour
    {
        
        private static TextureFormat Convert(Format lrsFormat)
        {
            switch (lrsFormat)
            {
                case Format.Z16: return TextureFormat.R16;
                case Format.Disparity16: return TextureFormat.R16;
                case Format.Rgb8: return TextureFormat.RGB24;
                case Format.Rgba8: return TextureFormat.RGBA32;
                case Format.Bgra8: return TextureFormat.BGRA32;
                case Format.Y8: return TextureFormat.Alpha8;
                case Format.Y16: return TextureFormat.R16;
                case Format.Raw16: return TextureFormat.R16;
                case Format.Raw8: return TextureFormat.Alpha8;
                case Format.Disparity32: return TextureFormat.RFloat;
                case Format.Yuyv:
                case Format.Bgr8:
                case Format.Raw10:
                case Format.Xyz32f:
                case Format.Uyvy:
                case Format.MotionRaw:
                case Format.MotionXyz32f:
                case Format.GpioRaw:
                case Format.Any:
                default:
                    throw new ArgumentException(string.Format("librealsense format: {0}, is not supported by Unity",
                        lrsFormat));
            }
        }

        private static int BPP(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.ARGB32:
                case TextureFormat.BGRA32:
                case TextureFormat.RGBA32:
                    return 32;
                case TextureFormat.RGB24:
                    return 24;
                case TextureFormat.R16:
                    return 16;
                case TextureFormat.R8:
                case TextureFormat.Alpha8:
                    return 8;
                default:
                    throw new ArgumentException("unsupported format {0}", format.ToString());
            }
        }

        public RsFrameProvider Source;

        [System.Serializable]
        public class TextureEvent : UnityEvent<Texture>
        {
        }

        public Stream _stream;
        public Format _format;
        public int _streamIndex;

        public FilterMode filterMode = FilterMode.Point;

        protected Texture2D texture;


        [Space] public TextureEvent textureBinding;

        FrameQueue q;
        Predicate<Frame> matcher;

        private IGetMarkerList _markerList;
        private IMarkerShowOutput _showMarkers;
        void Start()
        {
            _showMarkers = FindObjectOfType<MarkerDetector>();
            _markerList = FindObjectOfType<RealSenseTracking>();
  
            Source.OnStart += OnStartStreaming;
            Source.OnStop += OnStopStreaming;
        }

        void OnDestroy()
        {
            if (texture != null)
            {
                Destroy(texture);
                texture = null;
            }

            if (q != null)
            {
                q.Dispose();
            }
        }

        protected void OnStopStreaming()
        {
            Source.OnNewSample -= OnNewSample;
            if (q != null)
            {
                q.Dispose();
                q = null;
            }
        }

        public void OnStartStreaming(PipelineProfile activeProfile)
        {
            q = new FrameQueue(1);
            matcher = new Predicate<Frame>(Matches);
            Source.OnNewSample += OnNewSample;
        }

        private bool Matches(Frame f)
        {
            using (var p = f.Profile)
                return p.Stream == _stream && p.Format == _format && (p.Index == _streamIndex || _streamIndex == -1);
        }

        void OnNewSample(Frame frame)
        {
            try
            {
                if (frame.IsComposite)
                {
                    using (var fs = frame.As<FrameSet>())
                    using (var f = fs.FirstOrDefault(matcher))
                    {
                        if (f != null)
                            q.Enqueue(f);
                        return;
                    }
                }

                if (!matcher(frame))
                    return;

                using (frame)
                {
                    q.Enqueue(frame);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                // throw;
            }
        }

        bool HasTextureConflict(VideoFrame vf)
        {
            return !texture ||
                   texture.width != vf.Width ||
                   texture.height != vf.Height ||
                   BPP(texture.format) != vf.BitsPerPixel;
        }

        protected void LateUpdate()
        {
            if (q != null)
            {
                DepthFrame frame;
                if (q.PollForFrame<DepthFrame>(out frame))
                    using (frame)
                        ProcessFrame(frame);
            }
        }

        private void ProcessFrame(DepthFrame frame)
        {
            if (HasTextureConflict(frame))
            {
                if (texture != null)
                {
                    Destroy(texture);
                }

                using (var p = frame.Profile)
                {
                    bool linear = (QualitySettings.activeColorSpace != ColorSpace.Linear)
                                  || (p.Stream != Stream.Color && p.Stream != Stream.Infrared);
                    texture = new Texture2D(frame.Width, frame.Height, Convert(p.Format), false, linear)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = filterMode
                    };
                }

                textureBinding.Invoke(texture);
            }

            texture.LoadRawTextureData(frame.Data, frame.Stride * frame.Height);
            texture.Apply();
            List<MarkerLabel> markers = _markerList.GetMarkers();
            // rs2::depth_frame dpt_frame = frame.as<rs2::depth_frame>();
           
            if (markers != null)
            {
                float distance;
                foreach (var marker in markers)
                {
                    distance = frame.GetDistance(marker.x, marker.y);
                    if (distance > 0)
                    {
                        marker.distance = distance;
                       
                    }
                }
                Mat rgbaMat = new Mat(frame.Height, frame.Width, CvType.CV_8UC3);
                Utils.texture2DToMat(texture, rgbaMat, false);
                _showMarkers.ShowOutputMarkers(markers, ref rgbaMat, ref texture, false, true);
            }
            
           
            
        }
    }
}