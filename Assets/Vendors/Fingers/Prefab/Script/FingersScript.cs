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
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DigitalRubyShared
{
    public class FingersScript : MonoBehaviour
    {
        [Tooltip("True to treat the mouse as a finger, false otherwise. Left, middle and right mouse buttons can be used as individual fingers and will all have the same location.")]
        public bool TreatMousePointerAsFinger = true;

        [Tooltip("Whether to treat touches as mouse pointer? This needs to be set before the script Awake method is called.")]
        public bool SimulateMouseWithTouches = false;

        [Tooltip("Objects that should pass gestures through. By default, some UI components block gestures, such as Panel, Button, Dropdown, etc. See the SetupDefaultPassThroughComponents method for " +
            "the full list of defaults.")]
        public List<GameObject> PassThroughObjects;

        [Tooltip("Whether to show touches using the TouchCircles array. Make sure to turn this off before releasing your game or app.")]
        public bool ShowTouches;

        [Tooltip("If ShowTouches is true, this array is used to show the touches. The FingersScriptPrefab sets this up as 10 circles.")]
        public GameObject[] TouchCircles;

        [Tooltip("The default DPI to use if the DPI cannot be determined")]
        public int DefaultDPI = 200;

        [Tooltip("Whether to clear all gestures and remove them when a new level loads. This is almost always what you want, unless you " +
            "are setting up your gestures once in your first scene and using them for all scenes in your game.")]
        public bool ClearGesturesOnLevelLoad = true;

        private enum CaptureResult
        {
            /// <summary>
            /// Force the gesture to pass through
            /// </summary>
            ForcePassThrough,

            /// <summary>
            /// Force the gesture to be denied unless the platform specific view matches
            /// </summary>
            ForceDenyPassThrough,

            /// <summary>
            /// Do not force or deny the pass through
            /// </summary>
            Default,

            /// <summary>
            /// Pretend this object doesn't exist
            /// </summary>
            Ignore
        }

        private const int mousePointerId1 = int.MaxValue - 2;
        private const int mousePointerId2 = int.MaxValue - 3;
        private const int mousePointerId3 = int.MaxValue - 4;

        private readonly KeyValuePair<float, float>[] mousePrev = new KeyValuePair<float, float>[3];
        private readonly bool[] isMouseDown = new bool[3];
        private readonly List<GestureRecognizer> gestures = new List<GestureRecognizer>();
        private readonly List<GestureRecognizer> gesturesTemp = new List<GestureRecognizer>();
        private readonly HashSet<int> isTouchDown = new HashSet<int>();
        private readonly List<GestureTouch> touchesBegan = new List<GestureTouch>();
        private readonly List<GestureTouch> touchesMoved = new List<GestureTouch>();
        private readonly List<GestureTouch> touchesEnded = new List<GestureTouch>();
        private readonly Dictionary<int, List<GameObject>> gameObjectsForTouch = new Dictionary<int, List<GameObject>>();
        private readonly List<RaycastResult> captureRaycastResults = new List<RaycastResult>();
        private readonly List<GestureTouch> filteredTouches = new List<GestureTouch>();
        private readonly List<GestureTouch> touches = new List<GestureTouch>();
        private readonly Dictionary<int, Vector2> previousTouchPositions = new Dictionary<int, Vector2>();
        private readonly List<Component> components = new List<Component>();
        private readonly HashSet<System.Type> componentTypesToDenyPassThrough = new HashSet<System.Type>();
        private readonly HashSet<System.Type> componentTypesToIgnorePassThrough = new HashSet<System.Type>();
        private readonly Collider2D[] hackResults = new Collider2D[128];

        private float rotateAngle;
        private float pinchScale = 1.0f;
        private GestureTouch rotatePinch1;
        private GestureTouch rotatePinch2;

        private static FingersScript singleton;

        private IEnumerator MainThreadCallback(float delay, System.Action action)
        {
            if (action != null)
            {
                float elapsed = 0.0f;
                yield return null;
                while ((elapsed += Time.unscaledDeltaTime) < delay)
                {
                    yield return null;
                }
                action();
            }
        }

        private CaptureResult ShouldCaptureGesture(GameObject obj)
        {
            if (obj == null)
            {
                return CaptureResult.Default;
            }

            // if we have a capture gesture handler, perform a check to see if the user has custom pass through logic
            else if (CaptureGestureHandler != null)
            {
                bool? tmp = CaptureGestureHandler(obj);
                if (tmp != null)
                {
                    // user has decided on pass through, stop the loop
                    return (tmp.Value ? CaptureResult.ForceDenyPassThrough : CaptureResult.ForcePassThrough);
                }
            }

            // check pass through objects, these always pass the gesture through
            if (PassThroughObjects.Contains(obj))
            {
                // allow the gesture to pass through, do not capture it and stop the loop
                return CaptureResult.ForcePassThrough;
            }
            else
            {
                // if any gesture has a platform specific view that matches the object, use default behavior
                foreach (GestureRecognizer gesture in gestures)
                {
                    if (object.ReferenceEquals(gesture.PlatformSpecificView, obj))
                    {
                        return CaptureResult.Default;
                    }
                }
            }

            obj.GetComponents<Component>(components);

            try
            {
                System.Type type;
                foreach (Component c in components)
                {
                    type = c.GetType();
                    if (componentTypesToDenyPassThrough.Contains(type))
                    {
                        return CaptureResult.ForceDenyPassThrough;
                    }
                    else if (componentTypesToIgnorePassThrough.Contains(type))
                    {
                        return CaptureResult.Ignore;
                    }
                }
            }
            finally
            {
                components.Clear();
            }

            // default is for input UI elements (elements that normally block touches) to not pass through
            return CaptureResult.Default;
        }

        private void PopulateGameObjectsForTouch(int pointerId, float x, float y)
        {
            // Find a game object for a touch id
            if (EventSystem.current == null)
            {
                return;
            }

            List<GameObject> list;
            if (gameObjectsForTouch.TryGetValue(pointerId, out list))
            {
                list.Clear();
            }
            else
            {
                list = new List<GameObject>();
                gameObjectsForTouch[pointerId] = list;
            }

            captureRaycastResults.Clear();
            PointerEventData p = new PointerEventData(EventSystem.current);
            p.Reset();
            p.position = new Vector2(x, y);
            p.clickCount = 1;
            EventSystem.current.RaycastAll(p, captureRaycastResults);

            // HACK: Unity doesn't get collider2d on UI element, get those now
            int hackCount = Physics2D.OverlapPointNonAlloc(p.position, hackResults);
            for (int i = 0; i < hackCount; i++)
            {
                RaycastResult result = new RaycastResult { gameObject = hackResults[i].gameObject };
                if (captureRaycastResults.FindIndex((cmp) =>
                {
                    return cmp.gameObject == result.gameObject;
                }) < 0)
                {
                    captureRaycastResults.Add(result);
                }
            }
            System.Array.Clear(hackResults, 0, hackCount);

            if (captureRaycastResults.Count == 0)
            {
                captureRaycastResults.Add(new RaycastResult());
            }

            // determine what game object, if any should capture the gesture
            foreach (RaycastResult r in captureRaycastResults)
            {
                switch (ShouldCaptureGesture(r.gameObject))
                {
                    case CaptureResult.ForcePassThrough:
                        list.Clear();
                        return;

                    case CaptureResult.ForceDenyPassThrough:
                        // unless a platform specific view matches, deny the gesture
                        list.Add(r.gameObject);
                        return;

                    case CaptureResult.Default:
                        list.Add(r.gameObject);
                        break;

                    default:
                        break;
                }
            }
        }

        private void GestureTouchFromTouch(ref Touch t, out GestureTouch g)
        {
            // convert Unity touch to Gesture touch
            Vector2 prev;
            if (!previousTouchPositions.TryGetValue(t.fingerId, out prev))
            {
                prev.x = t.position.x;
                prev.y = t.position.y;
            }
            g = new GestureTouch(t.fingerId, t.position.x, t.position.y, prev.x, prev.y, t.pressure, t.rawPosition.x, t.rawPosition.y, t);
        }

        private void ProcessTouch(ref Touch t)
        {
            // process the touch, putting it in the appropriate list for it's state
            GestureTouch g;
            GestureTouchFromTouch(ref t, out g);

            // do our own touch up / down tracking, the user can reset touch state so that touches can begin again without a finger being lifted
            if (isTouchDown.Contains(t.fingerId))
            {
                if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                {
                    touchesMoved.Add(g);
                    previousTouchPositions[t.fingerId] = t.position;
                }
                else if (t.phase != TouchPhase.Began)
                {
                    touchesEnded.Add(g);
                    isTouchDown.Remove(t.fingerId);
                    previousTouchPositions.Remove(t.fingerId);
                }
                else
                {
                    Debug.LogError("Invalid state, touch " + t.fingerId + " began but isTouchDown was already true for this touch");
                }
            }
            else if (t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled)
            {
                // if touch down doesn't contain the touch, begin the touch
                touchesBegan.Add(g);
                isTouchDown.Add(t.fingerId);
                previousTouchPositions[t.fingerId] = t.position;
            }
        }

        private void AddMouseTouch(int index, int pointerId, float x, float y)
        {
            // add a touch from the mouse
            float prevX = mousePrev[index].Key;
            float prevY = mousePrev[index].Value;
            prevX = (prevX == float.MinValue ? x : prevX);
            prevY = (prevY == float.MinValue ? y : prevY);

            GestureTouch g = new GestureTouch(pointerId, x, y, prevX, prevY, 0.0f, x, y);
            if (Input.GetMouseButton(index))
            {
                mousePrev[index] = new KeyValuePair<float, float>(x, y);
                if (isMouseDown[index])
                {
                    touchesMoved.Add(g);

                    //Debug.LogFormat("Mouse Move: {0},{1}, Idx: {2}", x, y, index);
                }
                else
                {
                    touchesBegan.Add(g);
                    isMouseDown[index] = true;

                    //Debug.LogFormat("Mouse Down: {0},{1}, Idx: {2}", x, y, index);
                }
            }
            else if (isMouseDown[index])
            {
                mousePrev[index] = new KeyValuePair<float, float>(float.MinValue, float.MinValue);
                touchesEnded.Add(g);
                isMouseDown[index] = false;

                //Debug.LogFormat("Mouse Up: {0},{1}, Idx: {2}", x, y, index);
            }
        }

        private void ProcessTouches()
        {
            // process each touch in the Unity list of touches
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);

                // string d = string.Format ("Touch: {0} {1}", t.position, t.phase);
                // Debug.Log (d);

                ProcessTouch(ref t);
            }
        }

        private void RotateAroundPoint(ref float rotX, ref float rotY, float anchorX, float anchorY, float angleRadians)
        {
            // rotate around a point in 2D space
            float cosTheta = Mathf.Cos(angleRadians);
            float sinTheta = Mathf.Sin(angleRadians);
            float x = rotX - anchorX;
            float y = rotY - anchorY;
            rotX = ((cosTheta * x) - (sinTheta * y)) + anchorX;
            rotY = ((sinTheta * x) + (cosTheta * y)) + anchorY;
        }

        private void ProcessMouseButtons()
        {
            // if not using the mouse, bail
            if (!Input.mousePresent || !TreatMousePointerAsFinger)
            {
                return;
            }

            // add touches based on each mouse button
            float x = Input.mousePosition.x;
            float y = Input.mousePosition.y;
            AddMouseTouch(0, mousePointerId1, x, y);
            AddMouseTouch(1, mousePointerId2, x, y);
            AddMouseTouch(2, mousePointerId3, x, y);
        }

        private void ProcessMouseWheel()
        {
            // if the mouse is not setup or the user doesn't want the mouse treated as touches, return right away
            if (!Input.mousePresent || !TreatMousePointerAsFinger)
            {
                return;
            }

            // the mouse wheel will act as a rotate and pinch / zoom
            const float threshold = 100.0f;
            const float deltaModifier = 0.025f;
            Vector2 delta = Input.mouseScrollDelta;
            float scrollDelta = (delta.y == 0.0f ? delta.x : delta.y) * deltaModifier;

            // add type 1 = moved, 2 = begin, 3 = ended, 4 = none
            int addType1 = 4;
            int addType2 = 4;

            // left or right control initial down means begin
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
            {
                addType1 = 2;
            }
            // left or right control still down means move
            else if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                pinchScale += scrollDelta;
                addType1 = 1;
            }
            // left or right control initial up means end
            else if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl))
            {
                addType1 = 3;
            }

            // left or right shift initial down means begin
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                addType2 = 2;
            }
            // left or right shift still down means move
            else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                rotateAngle += scrollDelta;
                addType2 = 1;
            }
            // left or right shift initial up means end
            else if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
            {
                addType2 = 3;
            }

            // use the minimum add type so that moves are preferred over begins and begins are preferred over ends
            int addType = Mathf.Min(addType1, addType2);

            // no begins, moves or ends, set defaults and end
            if (addType == 4)
            {
                pinchScale = 1.0f;
                rotateAngle = 0.0f;
                return;
            }

            // calculate rotation
            float x = Input.mousePosition.x;
            float y = Input.mousePosition.y;
            float xRot1 = x - threshold;
            float yRot1 = y;
            float xRot2 = x + threshold;
            float yRot2 = y;
            float distance = threshold * pinchScale;
            xRot1 = x - distance;
            yRot1 = y;
            xRot2 = x + distance;
            yRot2 = y;
            RotateAroundPoint(ref xRot1, ref yRot1, x, y, rotateAngle);
            RotateAroundPoint(ref xRot2, ref yRot2, x, y, rotateAngle);

#if DEBUG

            if (scrollDelta != 0.0f)
            {
                //Debug.LogFormat("Mouse delta: {0}", scrollDelta);
            }

#endif

            // calculate rotation and zoom based on mouse values
            if (addType == 1)
            {
                // moved
                rotatePinch1 = new GestureTouch(int.MaxValue - 5, xRot1, yRot1, rotatePinch1.X, rotatePinch1.Y, 0.0f, xRot1, yRot1);
                rotatePinch2 = new GestureTouch(int.MaxValue - 6, xRot2, yRot2, rotatePinch2.X, rotatePinch2.Y, 0.0f, xRot2, yRot2);
                touchesMoved.Add(rotatePinch1);
                touchesMoved.Add(rotatePinch2);
            }
            else if (addType == 2)
            {
                // begin
                rotatePinch1 = new GestureTouch(int.MaxValue - 5, xRot1, yRot1, xRot1, yRot1, 0.0f, xRot1, yRot1);
                rotatePinch2 = new GestureTouch(int.MaxValue - 6, xRot2, yRot2, xRot2, yRot2, 0.0f, xRot1, yRot1);
                touchesBegan.Add(rotatePinch1);
                touchesBegan.Add(rotatePinch2);
            }
            else if (addType == 3)
            {
                // end
                touchesEnded.Add(rotatePinch1);
                touchesEnded.Add(rotatePinch2);
            }
        }

        private bool GameObjectMatchesPlatformSpecificView(List<GameObject> list, GestureRecognizer r)
        {
            GameObject platformSpecificView = r.PlatformSpecificView as GameObject;

            if ((platformSpecificView == null && EventSystem.current == null) ||
                // HACK: If the platform specific view is a Canvas, always match
                (platformSpecificView != null && platformSpecificView.GetComponent<Canvas>() != null))
            {
                return true;
            }
            else if (list.Count == 0)
            {
                return (platformSpecificView == null);
            }
            foreach (GameObject obj in list)
            {
                if (obj == platformSpecificView)
                {
                    return true;
                }
                else
                {
                    // if we have a collider and no platform specific view, count as a match
                    bool hasCollider = (obj != null && (obj.GetComponent<Collider2D>() != null || obj.GetComponent<Collider>() != null));
                    if (hasCollider && platformSpecificView == null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private ICollection<GestureTouch> FilterTouchesBegan(List<GestureTouch> touches, GestureRecognizer r)
        {
            // in order to begin, touches must match the platform specific view
            List<GameObject> gameObjects;
            filteredTouches.Clear();
            foreach (GestureTouch t in touches)
            {
                if (!gameObjectsForTouch.TryGetValue(t.Id, out gameObjects) || GameObjectMatchesPlatformSpecificView(gameObjects, r))
                {
                    filteredTouches.Add(t);
                }
            }
            return filteredTouches;
        }

        private void CleanupPassThroughObjects()
        {
            if (PassThroughObjects == null)
            {
                PassThroughObjects = new List<GameObject>();
            }
            for (int i = PassThroughObjects.Count - 1; i >= 0; i--)
            {
                if (PassThroughObjects[i] == null)
                {
                    PassThroughObjects.RemoveAt(i);
                }
            }
        }

        private void ResetState(bool clearGestures)
        {
            if (clearGestures)
            {
                gestures.Clear();
            }
            else
            {
                foreach (GestureRecognizer gesture in gestures)
                {
                    gesture.Reset();
                }
            }
            touchesBegan.Clear();
            touchesMoved.Clear();
            touchesEnded.Clear();
            gameObjectsForTouch.Clear();
            captureRaycastResults.Clear();
            filteredTouches.Clear();
            touches.Clear();
            previousTouchPositions.Clear();
            rotateAngle = 0.0f;
            pinchScale = 1.0f;
            rotatePinch1 = new GestureTouch();
            rotatePinch2 = new GestureTouch();
            isTouchDown.Clear();
            for (int i = 0; i < mousePrev.Length; i++)
            {
                mousePrev[i] = new KeyValuePair<float, float>(float.MinValue, float.MinValue);
            }
            for (int i = 0; i < isMouseDown.Length; i++)
            {
                isMouseDown[i] = false;
            }

            // cleanup deleted pass through objects
            for (int i = PassThroughObjects.Count - 1; i >= 0; i--)
            {
                if (PassThroughObjects[i] == null)
                {
                    PassThroughObjects.RemoveAt(i);
                }
            }
        }

        private void SetupDefaultPassThroughComponents()
        {
            componentTypesToDenyPassThrough.Add(typeof(Scrollbar));
            componentTypesToDenyPassThrough.Add(typeof(Button));
            componentTypesToDenyPassThrough.Add(typeof(Dropdown));
            componentTypesToDenyPassThrough.Add(typeof(Toggle));
            componentTypesToDenyPassThrough.Add(typeof(Slider));
            componentTypesToDenyPassThrough.Add(typeof(InputField));

            componentTypesToIgnorePassThrough.Add(typeof(Text));
        }

        private void SceneManagerSceneLoaded(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.LoadSceneMode arg1)
        {
            ResetState(ClearGesturesOnLevelLoad);
        }

        private void Awake()
        {
            if (singleton != null && singleton != this)
            {

#if UNITY_EDITOR

                Debug.LogError("Multiple fingers scripts found, there should only be one. When you access FingersScript.Instance, the prefab will be loaded automatically from the resources folder if it isn't already in the scene. If you are dragging the prefab into your scenes, then every scene needs to have the prefab dragged in, otherwise no scene should have the prefab dragged in and it will load automatically when you call FingersScript.Instance.");

#endif

                DestroyImmediate(gameObject, true);
                return;
            }
            singleton = this;

            // setup DPI, using a default value if it cannot be determined
            DeviceInfo.PixelsPerInch = (int)Screen.dpi;
            if (DeviceInfo.PixelsPerInch > 0)
            {
                DeviceInfo.UnitMultiplier = DeviceInfo.PixelsPerInch;
            }
            else
            {
                // pick a sensible dpi since we don't know the actual DPI
                DeviceInfo.UnitMultiplier = DeviceInfo.PixelsPerInch = DefaultDPI;
            }

            // set the main thread callback so gestures can callback after a delay
            GestureRecognizer.MainThreadCallback = (float delay, System.Action callback) =>
            {
                StartCoroutine(MainThreadCallback(delay, callback));
            };

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneManagerSceneLoaded;
            ResetState();
            Input.multiTouchEnabled = true;
            Input.simulateMouseWithTouches = SimulateMouseWithTouches;
            SetupDefaultPassThroughComponents();

#if UNITY_EDITOR

            if (EventSystem.current == null)
            {
                Debug.LogWarning("FingersScript highly recommends an active EventSystem be in your scene. Simply add a Unity UI element and it will setup a canvas and event system.");
            }

#endif

        }

        private void Update()
        {
            // turn on the canvas to see touches - don't do this unless you are debugging as it can mess up other canvases
            if (gameObject.transform.childCount > 0 && gameObject.transform.GetChild(0).GetComponent<Canvas>() != null)
            {
                gameObject.transform.GetChild(0).gameObject.SetActive(ShowTouches);
            }

            // cleanup pass through objects
            CleanupPassThroughObjects();

            // clear out all touches for each phase
            touchesBegan.Clear();
            touchesMoved.Clear();
            touchesEnded.Clear();

            // process touches and mouse
            ProcessTouches();
            ProcessMouseButtons();
            ProcessMouseWheel();

            // Debug.LogFormat("B: {0}, M: {1}, E: {2}", touchesBegan.Count, touchesMoved.Count, touchesEnded.Count);

            // keep track of game objects and touches
            foreach (GestureTouch t in touchesBegan)
            {
                PopulateGameObjectsForTouch(t.Id, t.X, t.Y);
            }

            // for each gesture, process the touches
            // copy to temp list in case gestures are added during the callbacks
            gesturesTemp.AddRange(gestures);
            foreach (GestureRecognizer r in gesturesTemp)
            {
                r.ProcessTouchesBegan(FilterTouchesBegan(touchesBegan, r));
                r.ProcessTouchesMoved(touchesMoved);
                r.ProcessTouchesEnded(touchesEnded);
            }
            gesturesTemp.Clear();

            // remove any game objects that are no longer being touched
            foreach (GestureTouch t in touchesEnded)
            {
                gameObjectsForTouch.Remove(t.Id);
            }

            // clear touches
            touches.Clear();

            // add all the touches
            touches.AddRange(touchesBegan);
            touches.AddRange(touchesMoved);
            touches.AddRange(touchesEnded);
        }

        private void LateUpdate()
        {
            if (ShowTouches && TouchCircles != null && TouchCircles.Length != 0)
            {
                int index = 0;
                foreach (GestureTouch t in Touches)
                {
                    GameObject obj = TouchCircles[index++];
                    obj.SetActive(true);
                    obj.transform.position = new Vector3(t.X, t.Y);
                }
                while (index < TouchCircles.Length)
                {
                    TouchCircles[index++].gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (singleton == this)
            {
                singleton = null;
            }
        }

        /// <summary>
        /// Add a gesture to the fingers script. This gesture will give callbacks when it changes state.
        /// </summary>
        /// <param name="gesture">Gesture to add</param>
        /// <return>True if the gesture was added, false if the gesture was already added</return>
        public bool AddGesture(GestureRecognizer gesture)
        {
            if (gesture == null || gestures.Contains(gesture))
            {
                return false;
            }
            gestures.Add(gesture);
            return true;
        }

        /// <summary>
        /// Remove a gesture from the script. The gesture will no longer give callbacks.
        /// </summary>
        /// <param name="gesture">Gesture to remove</param>
        /// <returns>True if the gesture was removed, false if it was not in the script</returns>
        public bool RemoveGesture(GestureRecognizer gesture)
        {
            return gestures.Remove(gesture);
        }

        /// <summary>
        /// Reset state - all touches and tracking are cleared, gestures are maintained
        /// </summary>
        public void ResetState()
        {
            ResetState(false);
        }

        /// <summary>
        /// Convert rect transform to screen space
        /// </summary>
        /// <param name="transform">Rect transform</param>
        /// <returns>Screen space rect</returns>
        public static Rect RectTransformToScreenSpace(RectTransform transform)
        {
            Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
            float x = transform.position.x - (size.x * 0.5f);
            float y = transform.position.y - (size.y * 0.5f);
            return new Rect(x, y, size.x, size.y);
        }

        /// <summary>
        /// Gets a collection of the current touches
        /// </summary>
        public ICollection<GestureTouch> Touches { get { return touches; } }

        /// <summary>
        /// Optional handler to determine whether a game object will pass through or not.
        /// Null handler gets default gesture capture handling.
        /// Non-null handler that returns null gets default handling.
        /// Non-null handler that returns true captures the gesture.
        /// Non-null handler that returns false passes the gesture through.
        /// </summary>
        public System.Func<GameObject, bool?> CaptureGestureHandler;

        /// <summary>
        /// A set of component types that will stop the gesture from passing through. By default includes UI components like Button, Dropdown, etc.
        /// You can add additional component types if you like, but you should not remove items from this set or clear the set.
        /// </summary>
        public HashSet<System.Type> ComponentTypesToDenyPassThrough { get { return componentTypesToDenyPassThrough; } }

        /// <summary>
        /// A set of component types that will be ignored for purposes of pass through checking. By default includes the Text UI component.
        /// You can add additional component types if you like, but you should not remove items from this set or clear the set.
        /// </summary>
        public HashSet<System.Type> ComponentTypesToIgnorePassThrough { get { return componentTypesToIgnorePassThrough; } }

        /// <summary>
        /// Shared static instance of fingers script that lives forever - the prefab MUST exist in a resources folder!
        /// Note that you can still add the prefab to your scene if you prefer to do it that way, in which case the
        /// singleton will be re-created each time a scene loads.
        /// </summary>
        public static FingersScript Instance
        {
            get
            {
                if (singleton == null)
                {
                    GameObject prefab = GameObject.Instantiate(Resources.Load("FingersScriptPrefab") as GameObject);
                    GameObject.DontDestroyOnLoad(prefab);
                    singleton = prefab.GetComponent<FingersScript>();
                }
                return singleton;
            }
        }

        /// <summary>
        /// Check whether Instance is not null without it actually creating a prefab if needed
        /// </summary>
        public static bool HasInstance { get { return singleton != null; } }
    }
}
