using System.IO;
using UnityEngine;

namespace OptimuscleMarkers
{
    public class SaveCalibrationData : MonoBehaviour
    {
        public HSVCalibrate hsvData;
        public void HSVSaveDataButton()
        {
       

            var serializedSave = JsonUtility.ToJson(hsvData);
            var saveFileName = Application.persistentDataPath + "/Save_Colour_Calibration.json";
            Debug.Log(saveFileName);
            File.WriteAllText(saveFileName, serializedSave);
        }
    }
}
