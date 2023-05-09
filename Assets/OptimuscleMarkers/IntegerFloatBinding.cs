using UnityEngine;
using UnityEngine.Events;
namespace OptimuscleMarkers
{
    public class IntegerFloatBinding : MonoBehaviour
    {
        [System.Serializable]
        public class StringFloatEvent : UnityEvent<string, float> { }

        public string Name;

        public float Value
        {
            set
            {
                Binding.Invoke(Name, value);
            }
        }
        public StringFloatEvent Binding;
    }
}