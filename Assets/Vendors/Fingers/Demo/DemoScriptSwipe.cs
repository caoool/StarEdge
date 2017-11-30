//
// Fingers Gestures
// (c) 2015 Digital Ruby, LLC
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DigitalRubyShared
{
    public class DemoScriptSwipe : MonoBehaviour
    {
        [Tooltip("Emit this particle system in the swipe direction.")]
        public ParticleSystem SwipeParticleSystem;

        [Tooltip("Set the required touches for the swipe.")]
        [Range(1, 10)]
        public int SwipeTouchCount = 1;

        [Tooltip("Whether to reset all touch states on swipe end, this allows multiple swipes without lifting the finger.")]
        public bool ResetStateOnEnd = true;

        private SwipeGestureRecognizer swipe;

        private void Start()
        {
            swipe = new SwipeGestureRecognizer();
            swipe.StateUpdated += Swipe_Updated;
            swipe.DirectionThreshold = 0;
            swipe.EndImmediately = true;
            swipe.MinimumNumberOfTouchesToTrack = swipe.MaximumNumberOfTouchesToTrack = SwipeTouchCount;
            FingersScript.Instance.AddGesture(swipe);
            TapGestureRecognizer tap = new TapGestureRecognizer();
            tap.StateUpdated += Tap_Updated;
            FingersScript.Instance.AddGesture(tap);
        }

        private void Update()
        {
            swipe.MinimumNumberOfTouchesToTrack = swipe.MaximumNumberOfTouchesToTrack = SwipeTouchCount;
        }

        private void Tap_Updated(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Ended)
            {
                Debug.Log("Tap");
            }
        }

        private void Swipe_Updated(GestureRecognizer gesture)
        {
            Debug.LogFormat("Swipe state: {0}", gesture.State);

            SwipeGestureRecognizer swipe = gesture as SwipeGestureRecognizer;
            if (swipe.State == GestureRecognizerState.Ended)
            {
                float angle = Mathf.Atan2(-swipe.DistanceY, swipe.DistanceX) * Mathf.Rad2Deg;
                SwipeParticleSystem.transform.rotation = Quaternion.Euler(angle, 90.0f, 0.0f);
                Vector3 pos = Camera.main.ScreenToWorldPoint(new Vector3(gesture.StartFocusX, gesture.StartFocusY, 0.0f));
                pos.z = 0.0f;
                SwipeParticleSystem.transform.position = pos;
                SwipeParticleSystem.Play();

                if (ResetStateOnEnd)
                {
                    // allow the gesture to restart, using existing touches even if they are not lifted
                    gesture.BeginGestureRestart();
                }
            }
        }
    }
}