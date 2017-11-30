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
    [AddComponentMenu("Fingers Gestures/Gesture/Swipe Gesture", 2)]
    public class SwipeGestureRecognizerComponentScript : GestureRecognizerComponentScript<SwipeGestureRecognizer>
    {
        [Header("Swipe gesture properties")]
        [Tooltip("The swipe direction required to recognize the gesture (default is any)")]
        public SwipeGestureRecognizerDirection Direction = SwipeGestureRecognizerDirection.Any;

        [Tooltip("The minimum distance the swipe must travel to be recognized.")]
        [Range(0.01f, 10.0f)]
        public float MinimumDistanceUnits = 1.0f;

        [Tooltip("The minimum units per second the swipe must travel to be recognized.")]
        [Range(0.01f, 10.0f)]
        public float MinimumSpeedUnits = 3.0f;

        [Tooltip("For set directions, this is the amount that the swipe must be proportionally in that direction " +
            "vs the other direction. For example, a swipe down gesture will need to move in the y axis " +
            "by this multiplier more versus moving along the x axis.")]
        [Range(0.0f, 5.0f)]
        public float DirectionThreshold = 1.5f;

        [Tooltip("End the swipe gesture immediately if recognized, reglardless of whether the touch is lifted. Default is false.")]
        public bool EndImmediately;

        [Tooltip("Whether to fail if the gesture changes direction mid swipe")]
        public bool FailOnDirectionChange;

        protected override void Start()
        {
            base.Start();

            Gesture.Direction = Direction;
            Gesture.MinimumDistanceUnits = MinimumDistanceUnits;
            Gesture.MinimumSpeedUnits = MinimumSpeedUnits;
            Gesture.DirectionThreshold = DirectionThreshold;
            Gesture.EndImmediately = EndImmediately;
            Gesture.FailOnDirectionChange = FailOnDirectionChange;
        }

    }
}
