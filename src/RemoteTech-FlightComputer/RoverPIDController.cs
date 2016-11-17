using System;

namespace RemoteTech.FlightComputer
{
    class RoverPIDController
    {
        private float
            mKp, mKd, mKi,
            mOldVal,
            mOldD,
            McMin, McMax,
            mSum,
            mValue,
            mErrScalar;

        public RoverPIDController(float p, float i, float d, float clampMin, float clampMax, float errorScale = 0f)
        {
            mKp = p;
            mKi = i;
            mKd = d;
            McMin = clampMin;
            McMax = clampMax;
            mErrScalar = errorScale;
            Reset();
        }

        public void Reset()
        {
            mSum = mOldVal = mOldD = mValue = 0;
        }

        public void SetClamp(float clampMin, float clampMax)
        {
            McMin = clampMin;
            McMax = clampMax;
        }

        public float Control(float v)
        {
            mOldD = (v - mOldVal) / TimeWarp.deltaTime;

            mSum += v / TimeWarp.deltaTime;

            mOldVal = value;

            mValue = mKp * v + mKi * mSum + mKd * mOldD;

            mValue = mValue < McMin ? McMin : mValue > McMax ? McMax : mValue;

            if (mErrScalar > 0) {
                float ErrorScale = Math.Abs(v) / mErrScalar;
                if (ErrorScale > 1)
                    ErrorScale = 1;
                mValue *= ErrorScale;
            }

            return mValue;
        }

        public float value => mValue;

        public static implicit operator float(RoverPIDController v)
        {
            return v.mValue;
        }
    }
}
