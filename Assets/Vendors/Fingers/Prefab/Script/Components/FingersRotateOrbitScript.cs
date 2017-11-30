//
// Fingers Gestures
// (c) 2015 Digital Ruby, LLC
// http://www.digitalruby.com
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalRubyShared
{
    /// <summary>
    /// Allows rotating around a target using a two finger rotation gesture
    /// </summary>
    [AddComponentMenu("Fingers Gestures/Rotation Orbit")]
    public class FingersRotateOrbitScript : MonoBehaviour
    {
        [Tooltip("The object to orbit")]
        public Transform OrbitTarget;

        [Tooltip("The object that will orbit around OrbitTarget")]
        public Transform Orbiter;

        [Tooltip("The axis to orbit around")]
        public Vector3 Axis = Vector3.up;

        [Tooltip("The rotation speed in degrees per second")]
        [Range(0.01f, 1000.0f)]
        public float RotationSpeed = 500.0f;

        private RotateGestureRecognizer rotationGesture;

        private void Start()
        {
            rotationGesture = new RotateGestureRecognizer();
            rotationGesture.StateUpdated += RotationGesture_Updated;
            FingersScript.Instance.AddGesture(rotationGesture);
        }

        private void RotationGesture_Updated(GestureRecognizer gesture)
        {
            Orbiter.transform.RotateAround(OrbitTarget.transform.position, Axis, rotationGesture.RotationDegreesDelta * Time.deltaTime * RotationSpeed);
        }
    }
}
