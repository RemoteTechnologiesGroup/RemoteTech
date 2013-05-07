using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    //this PID controller is curtesy of Tosh
    class RoverPidController
    {
        public /* private */ float mKp, mKd, mKi;
        private float mOldVal, mOldTime, mOldD;
        private float mClamp;
        private float[] mBuffer = null;
        private int mPtr;
        private float mSum;
        private float mValue;

        public RoverPidController(float Kp, float Ki, float Kd,
                              int integrationBuffer, float clamp)
        {
            mKp = Kp;
            mKi = Ki;
            mKd = Kd;
            mClamp = clamp;
            if (integrationBuffer >= 1)
                mBuffer = new float[integrationBuffer];
            Reset();
        }

        public void Reset()
        {
            mSum = 0;
            mOldTime = -1;
            mOldD = 0;
            if (mBuffer != null)
                for (int i = 0; i < mBuffer.Length; i++)
                    mBuffer[i] = 0;
            mPtr = 0;
        }

        public float Control(float v)
        {
            if (Time.fixedTime > mOldTime)
            {
                if (mOldTime >= 0)
                {
                    mOldD = (v - mOldVal) / (Time.fixedTime - mOldTime);

                    float i = v / (Time.fixedTime - mOldTime);
                    if (mBuffer != null)
                    {
                        mSum -= mBuffer[mPtr];
                        mBuffer[mPtr] = i;
                        mPtr++;
                        if (mPtr >= mBuffer.Length)
                            mPtr = 0;
                    }
                    mSum += i;
                }

                mOldTime = Time.fixedTime;
                mOldVal = value;
            }

            mValue = mKp * v + mKi * mSum + mKd * mOldD;

            if (mClamp > 0)
            {
                if (mValue > mClamp)
                    mValue = mClamp;
                if (mValue < -mClamp)
                    mValue = -mClamp;
            }

            return mValue;
        }

        public float value
        {
            get { return mValue; }
        }

        public static implicit operator float(RoverPidController v)
        {
            return v.mValue;
        }
    }
}
