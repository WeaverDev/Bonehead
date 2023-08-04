using System;
using UnityEngine;

/// <summary>
/// Wrapper for a velocity driven, framerate-independent smoothing function.
/// </summary>
public static class SmoothDamp
{
    // Smoothing with a speed equal or greather than this value will equal to copying the target value
    public const float MaxSpeed = 100000000;

    [Serializable]
    public struct Float
    {
        public float currentValue;
        private float pastTarget;

        public void Reset(float newValue)
        {
            currentValue = newValue;
            pastTarget = newValue;
        }

        public float Step(float target, float speed)
        {
            var deltaTime = Time.deltaTime;

            var t = deltaTime * speed;
            if (0 == t) return currentValue;
            else if (t < MaxSpeed)
            {
                var v = (target - pastTarget) / t;
                var f = currentValue - pastTarget + v;

                pastTarget = target;

                return currentValue = target - v + f * Mathf.Exp(-t);
            }
            else
            {
                return currentValue = target;
            }
        }

        public static implicit operator float(Float rhs)
        {
            return rhs.currentValue;
        }
    }

    [Serializable]
    public struct Angle
    {
        public float currentValue;
        private float pastTarget;

        public void Reset(float newValue)
        {
            currentValue = newValue;
            pastTarget = newValue;
        }

        public float Step(float target, float speed)
        {
            target = currentValue + Mathf.DeltaAngle(currentValue, target);

            var deltaTime = Time.deltaTime;

            var t = deltaTime * speed;
            if (0 == t) return currentValue;
            else if (t < MaxSpeed)
            {
                var v = (target - pastTarget) / t;
                var f = currentValue - pastTarget + v;

                pastTarget = target;

                return currentValue = target - v + f * Mathf.Exp(-t);
            }
            else
            {
                return currentValue = target;
            }
        }

        public static implicit operator float(Angle rhs) => rhs.currentValue;
    }

    [Serializable]
    public struct Vector3
    {
        public float x { get { return currentValue.x; } }
        public float y { get { return currentValue.y; } }
        public float z { get { return currentValue.z; } }

        public UnityEngine.Vector3 currentValue;
        private UnityEngine.Vector3 pastTarget;

        public void Reset(UnityEngine.Vector3 newValue)
        {
            currentValue = newValue;
            pastTarget = newValue;
        }

        public UnityEngine.Vector3 Step(UnityEngine.Vector3 target, float speed)
        {
            var deltaTime = Time.deltaTime;

            var t = deltaTime * speed;
            if (0 == t) return currentValue;
            else if (t < MaxSpeed)
            {
                var v = (target - pastTarget) / t;
                var f = currentValue - pastTarget + v;

                pastTarget = target;

                return currentValue = target - v + f * Mathf.Exp(-t);
            }
            else
            {
                return currentValue = target;
            }
        }

        public static implicit operator UnityEngine.Vector3(Vector3 rhs) => rhs.currentValue;
    }

    [Serializable]
    public struct EulerAngles
    {
        public UnityEngine.Vector3 currentValue;
        private UnityEngine.Vector3 pastTarget;

        public EulerAngles(EulerAngles toCopy)
        {
            this.currentValue = toCopy.currentValue;
            this.pastTarget = toCopy.pastTarget;
        }

        public void Reset(UnityEngine.Vector3 newValue)
        {
            currentValue = newValue;
            pastTarget = newValue;
        }

        public UnityEngine.Vector3 Step(UnityEngine.Vector3 target, float speed)
        {
            target.x = currentValue.x + Mathf.DeltaAngle(currentValue.x, target.x);
            target.y = currentValue.y + Mathf.DeltaAngle(currentValue.y, target.y);
            target.z = currentValue.z + Mathf.DeltaAngle(currentValue.z, target.z);

            var deltaTime = Time.deltaTime;

            var t = deltaTime * speed;
            if (0 == t) return currentValue;
            else if (t < MaxSpeed)
            {
                var v = (target - pastTarget) / t;
                var f = currentValue - pastTarget + v;

                pastTarget = target;

                return currentValue = target - v + f * Mathf.Exp(-t);
            }
            else
            {
                return currentValue = target;
            }
        }

        public static implicit operator UnityEngine.Vector3(EulerAngles rhs) => rhs.currentValue;
    }
}