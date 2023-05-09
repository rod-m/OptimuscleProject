using UnityEngine;
using UnityEngine.Events;
namespace OptimuscleMarkers
{
    public class GetScalarInteger : MonoBehaviour
    {
        
        [System.Serializable]
        public class FloatEvent : UnityEvent<float> { }

        public HSVCalibrate hsvCalibrate;

        public string propertyName;
        public FloatEvent Binding;

        void Awake()
        {
            Binding.Invoke(hsvCalibrate.GetScalar(propertyName));
        }
    }
}