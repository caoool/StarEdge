using UnityEngine;
using System.Collections;

namespace FastShadowProjector
{
	public static class FSPDebugUtils
	{
		public static void DrawCameraFrustumPlanes(Camera camera)
		{
			Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);

			var nearPointA = FSPUtils.Get3PlaneConcurrencePoint(planes[4], planes[0], planes[3]);
			var nearPointB = FSPUtils.Get3PlaneConcurrencePoint(planes[4], planes[1], planes[3]);
			var nearPointC = FSPUtils.Get3PlaneConcurrencePoint(planes[4], planes[1], planes[2]);
			var nearPointD = FSPUtils.Get3PlaneConcurrencePoint(planes[4], planes[0], planes[2]);

			Gizmos.DrawLine(nearPointA, nearPointB);
			Gizmos.DrawLine(nearPointB, nearPointC);
			Gizmos.DrawLine(nearPointC, nearPointD);
			Gizmos.DrawLine(nearPointD, nearPointA);

			var farPointA = FSPUtils.Get3PlaneConcurrencePoint(planes[5], planes[0], planes[3]);
			var farPointB = FSPUtils.Get3PlaneConcurrencePoint(planes[5], planes[1], planes[3]);
			var farPointC = FSPUtils.Get3PlaneConcurrencePoint(planes[5], planes[1], planes[2]);
			var farPointD = FSPUtils.Get3PlaneConcurrencePoint(planes[5], planes[0], planes[2]);

			Gizmos.DrawLine(farPointA, farPointB);
			Gizmos.DrawLine(farPointB, farPointC);
			Gizmos.DrawLine(farPointC, farPointD);
			Gizmos.DrawLine(farPointD, farPointA);

			Gizmos.DrawLine(nearPointA, farPointA);
			Gizmos.DrawLine(nearPointB, farPointB);
			Gizmos.DrawLine(nearPointC, farPointC);
			Gizmos.DrawLine(nearPointD, farPointD);
		}
	}
}
