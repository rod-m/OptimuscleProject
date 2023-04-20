using System;
using UnityEngine;
using System.Collections;
using OpenCVForUnity.CoreModule;

namespace OpenCVTest
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
            return new Scalar(HSVminC.r, HSVminC.g, HSVminC.b, 1) * 255;
        }

        public Scalar getHSVmax()
        {
            //return HSVmax;
            return new Scalar(HSVmaxC.r, HSVmaxC.g, HSVmaxC.b, 1) * 255;
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