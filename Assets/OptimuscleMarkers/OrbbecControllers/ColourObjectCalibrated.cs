using System;
using OpenCVForUnity.CoreModule;
using UnityEngine;

namespace OptimuscleMarkers.OrbbecControllers
{
    [Serializable]
    public class ColorObject
    {
        int xPos, yPos;
        public string type = "Object";
     
        [SerializeField] Color HSVminC = UnityEngine.Color.green;
        [SerializeField] Color HSVmaxC = UnityEngine.Color.green;
        [SerializeField] Color colour = UnityEngine.Color.yellow;
        
        public ColorObject()
        {
            //set values for default constructor
            setType("Object");
           
        }
       
        

        public int getXPos()
        {
            return xPos;
        }

        public void setXPos(int x)
        {
            xPos = x;
        }

        public int getYPos()
        {
            return yPos;
        }

        public void setYPos(int y)
        {
            yPos = y;
        }

        public Scalar getHSVmin()
        {
            //return HSVmin;
            float H, S, V;
            Color.RGBToHSV(HSVminC, out H, out S, out V);
            return new Scalar(H * 255,S * 100,V * 100);
        }

        public Scalar getHSVmax()
        {
            //return HSVmax;
            float H, S, V;
            Color.RGBToHSV(HSVmaxC, out H, out S, out V);
             return new Scalar(H * 255,S * 100,V * 100);
        }
        
        public string getType()
        {
            return type;
        }

        public void setType(string t)
        {
            type = t;
        }

        public Scalar getColor()
        {
            return new Scalar(colour.r, colour.g, colour.b, 1) * 255;
            //return Color;
        }

       
    }
}