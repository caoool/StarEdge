//
// Fingers Gestures
// (c) 2015 Digital Ruby, LLC
// http://www.digitalruby.com
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 

using System;

namespace DigitalRubyShared
{
    /// <summary>
    /// A scale gesture detects two fingers moving towards or away from each other to scale something
    /// </summary>
    public class ScaleGestureRecognizer : GestureRecognizer
    {
        private float previousDistance;
        private float previousDistanceX;
        private float previousDistanceY;
        private float centerX;
        private float centerY;

        public ScaleGestureRecognizer()
        {
            ScaleMultiplier = ScaleMultiplierX = ScaleMultiplierY = 1.0f;
            ZoomSpeed = 3.0f;
            ThresholdUnits = 0.15f;
            ScaleThresholdPercent = 0.01f;
            ScaleFocusMoveThresholdUnits = 0.04f;
            MinimumNumberOfTouchesToTrack = MaximumNumberOfTouchesToTrack = 2;
        }

        private void UpdateCenter(float distance, float distanceX, float distanceY)
        {
            previousDistance = distance;
            previousDistanceX = distanceX;
            previousDistanceY = distanceY;
            centerX = FocusX;
            centerY = FocusY;
        }

        private void ProcessTouches()
        {
            CalculateFocus(CurrentTrackedTouches);

            if (!TrackedTouchCountIsWithinRange)
            {
                return;
            }

            float distance = DistanceBetweenPoints(CurrentTrackedTouches[0].X, CurrentTrackedTouches[0].Y, CurrentTrackedTouches[1].X, CurrentTrackedTouches[1].Y);
            float distanceX = Distance(CurrentTrackedTouches[0].X - CurrentTrackedTouches[1].X);
            float distanceY = Distance(CurrentTrackedTouches[0].Y - CurrentTrackedTouches[1].Y);

            if (State == GestureRecognizerState.Possible)
            {
                if (previousDistance == 0.0f)
                {
                    previousDistance = distance;
                    previousDistanceX = distanceX;
                    previousDistanceY = distanceY;
                }
                else
                {
                    float diff = Math.Abs(previousDistance - distance);
                    if (diff >= ThresholdUnits)
                    {
                        UpdateCenter(distance, distanceX, distanceY);
                        SetState(GestureRecognizerState.Began);
                    }
                }
            }
            else if (State == GestureRecognizerState.Executing)
            {
                float focusChange = DistanceBetweenPoints(FocusX, FocusY, centerX, centerY);
                if (focusChange >= ScaleFocusMoveThresholdUnits)
                {
                    UpdateCenter(distance, distanceX, distanceY);
                }
                else
                {
                    ScaleMultiplier = previousDistance <= 0.0f ? 1.0f : (distance / previousDistance);
                    ScaleMultiplierX = (previousDistanceX <= 0.0f || Math.Abs(previousDistanceX - distanceX) < ThresholdUnits) ? 1.0f : (distanceX / previousDistanceX);
                    ScaleMultiplierY = (previousDistanceY <= 0.0f || Math.Abs(previousDistanceY - distanceY) < ThresholdUnits) ? 1.0f : (distanceY / previousDistanceY);
                    if (ScaleMultiplier < (1.0f - ScaleThresholdPercent) || ScaleMultiplier > (1.0f + ScaleThresholdPercent))
                    {
                        ScaleMultiplier = 1.0f + ((ScaleMultiplier - 1.0f) * ZoomSpeed);
                        previousDistance = distance;

                        // x scale modifier
                        if (ScaleMultiplierX < (1.0f - ScaleThresholdPercent) || ScaleMultiplierX > (1.0f + ScaleThresholdPercent))
                        {
                            ScaleMultiplierX = 1.0f + ((ScaleMultiplierX - 1.0f) * ZoomSpeed);
                            previousDistanceX = distanceX;
                        }
                        else
                        {
                            ScaleMultiplierX = 1.0f;
                        }

                        // y scale modifier
                        if (ScaleMultiplierY < (1.0f - ScaleThresholdPercent) || ScaleMultiplierY > (1.0f + ScaleThresholdPercent))
                        {
                            ScaleMultiplierY = 1.0f + ((ScaleMultiplierY - 1.0f) * ZoomSpeed);
                            previousDistanceY = distanceY;
                        }
                        else
                        {
                            ScaleMultiplierY = 1.0f;
                        }

                        SetState(GestureRecognizerState.Executing);
                    }
                    else
                    {
                        // not enough change to send a callback
                    }
                }
            }
            else if (State == GestureRecognizerState.Began)
            {
                centerX = (CurrentTrackedTouches[0].X + CurrentTrackedTouches[1].X) * 0.5f;
                centerY = (CurrentTrackedTouches[0].Y + CurrentTrackedTouches[1].Y) * 0.5f;
                SetState(GestureRecognizerState.Executing);
            }
            else
            {
                SetState(GestureRecognizerState.Possible);
            }
        }

        protected override void TouchesBegan(System.Collections.Generic.IEnumerable<GestureTouch> touches)
        {
            previousDistance = 0.0f;
        }

        protected override void TouchesMoved()
        {
            ProcessTouches();
        }

        protected override void TouchesEnded()
        {
            if (State == GestureRecognizerState.Executing)
            {
                CalculateFocus(CurrentTrackedTouches);
                SetState(GestureRecognizerState.Ended);
            }
            else
            {
                // didn't get to the executing state, fail the gesture
                SetState(GestureRecognizerState.Failed);
            }
        }

        /// <summary>
        /// The current scale multiplier. Multiply your current scale value by this to scale.
        /// </summary>
        /// <value>The scale multiplier.</value>
        public float ScaleMultiplier { get; private set; }

        /// <summary>
        /// The current scale multiplier for x axis. Multiply your current scale x value by this to scale.
        /// </summary>
        /// <value>The scale multiplier.</value>
        public float ScaleMultiplierX { get; private set; }

        /// <summary>
        /// The current scale multiplier for y axis. Multiply your current scale y value by this to scale.
        /// </summary>
        /// <value>The scale multiplier.</value>
        public float ScaleMultiplierY { get; private set; }

        /// <summary>
        /// Additional multiplier for ScaleMultipliers. This will making scaling happen slower or faster. Default is 3.0.
        /// </summary>
        /// <value>The zoom speed.</value>
        public float ZoomSpeed { get; set; }

        /// <summary>
        /// How many units the distance between the fingers must increase or decrease from the start distance to begin executing. Default is 0.15.
        /// </summary>
        /// <value>The threshold in units</value>
        public float ThresholdUnits { get; set; }

        /// <summary>
        /// The threshold in percent (i.e. 0.1) that must change to signal any listeners about a new scale. Default is 0.01.
        /// </summary>
        /// <value>The threshold percent</value>
        public float ScaleThresholdPercent { get; set; }

        /// <summary>
        /// If the focus moves more than this amount, reset the scale threshold percent. This helps avoid
        /// a wobbly zoom when panning and zooming at the same time. Default is 0.04.
        /// </summary>
        /// <value>The scale focus threshold in units</value>
        public float ScaleFocusMoveThresholdUnits { get; set; }
    }
}
