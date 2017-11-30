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
    /// Allows a long tap and hold to move an object around and release it at a new point. Add this script to the object to drag.
    /// </summary>
    [AddComponentMenu("Fingers Gestures/Drag and Drop")]
    public class FingersDragDropScript : MonoBehaviour
    {
        [Tooltip("The camera to use to convert screen coordinates to world coordinates. Defaults to Camera.main.")]
        public Camera Camera;

        [Tooltip("Whether to bring the object to the front when a gesture executes on it")]
        public bool BringToFront = true;

        private LongPressGestureRecognizer longPressGesture;
        private Rigidbody2D rigidBody;
        private SpriteRenderer spriteRenderer;
        private int startSortOrder;
        private Vector2 panStart;

        private void LongPressGestureUpdated(GestureRecognizer r)
        {
            FingersPanRotateScaleScript.StartOrResetGesture(r, BringToFront, Camera, gameObject, spriteRenderer);
            if (r.State == GestureRecognizerState.Began)
            {
                panStart = (rigidBody == null ? (Vector2)gameObject.transform.position : rigidBody.position);
                Debug.Log("Drag/drop began");
            }
            else if (r.State == GestureRecognizerState.Executing)
            {
                Vector2 screenMovement = new Vector2(longPressGesture.DistanceX, longPressGesture.DistanceY);
                Vector2 worldMovement = Camera.ScreenToWorldPoint(screenMovement) - Camera.ScreenToWorldPoint(Vector2.zero);
                //Debug.LogFormat("Screen movement: {0}, World movement: {1}", screenMovement, worldMovement);
                if (rigidBody == null)
                {
                    transform.position = panStart + worldMovement;
                }
                else
                {
                    rigidBody.MovePosition(panStart + worldMovement);
                }
            }
            else if (r.State == GestureRecognizerState.Ended)
            {
                if (spriteRenderer != null && BringToFront)
                {
                    spriteRenderer.sortingOrder = startSortOrder;
                }
                Debug.Log("Drag/drop ended");
            }
        }

        private void Start()
        {
            this.Camera = (this.Camera == null ? Camera.main : this.Camera);
            longPressGesture = new LongPressGestureRecognizer();
            longPressGesture.StateUpdated += LongPressGestureUpdated;
            rigidBody = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                startSortOrder = spriteRenderer.sortingOrder;
            }
            FingersScript.Instance.AddGesture(longPressGesture);
        }

        private void Update()
        {

        }
    }
}
