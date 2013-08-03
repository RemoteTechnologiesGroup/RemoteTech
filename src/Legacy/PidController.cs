using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech.Legacy {
    class PidController {
        public /* private */ float mKp, mKd, mKi;
        private float mOldVal, mOldTime, mOldD;
        private float McMin, McMax;
        private float[] mBuffer = null;
        private int mPtr;
        private float mSum;
        private float mValue;

        public PidController(float Kp, float Ki, float Kd,
                              int integrationBuffer, float clampMin, float clampMax) {
            mKp = Kp;
            mKi = Ki;
            mKd = Kd;
            McMin = clampMin;
            McMax = clampMax;
            if (integrationBuffer >= 1)
                mBuffer = new float[integrationBuffer];
            Reset();
        }

        public void Reset() {
            mSum = 0;
            mOldTime = -1;
            mOldD = 0;
            if (mBuffer != null)
                for (int i = 0; i < mBuffer.Length; i++)
                    mBuffer[i] = 0;
            mPtr = 0;
        }

        public void setClamp(float clampMin, float clampMax) {
            McMin = clampMin;
            McMax = clampMax;
        }

        public float Control(float v) {
            if (Time.fixedTime > mOldTime) {
                if (mOldTime >= 0) {
                    mOldD = (v - mOldVal) / (Time.fixedTime - mOldTime);

                    float i = v / (Time.fixedTime - mOldTime);
                    if (mBuffer != null) {
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

            
                if (mValue > McMax)
                    mValue = McMax;
                if (mValue < McMin)
                    mValue = McMin;
            

            return mValue;
        }

        public float value {
            get { return mValue; }
        }

        public static implicit operator float(PidController v) {
            return v.mValue;
        }
    }
}
