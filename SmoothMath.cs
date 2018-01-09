using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimePPDisplayer
{
    public static class SmoothMath
    {
        //From: http://devblog.aliasinggames.com/inertialdamp-unity-smoothdamp-alternative/
        public static double SmoothDamp(double previousValue, double targetValue, ref double speed, double smoothTime, double h)
        {
            double T1 = 0.36f * smoothTime;
            double T2 = 0.64f * smoothTime;
            double x = previousValue - targetValue;
            double newSpeed = speed + h * (-1f / (T1 * T2) * x - (T1 + T2) / (T1 * T2) * speed);
            double newValue = x + h * speed;
            speed = newSpeed;
            return targetValue + newValue;
        }
    }
}
