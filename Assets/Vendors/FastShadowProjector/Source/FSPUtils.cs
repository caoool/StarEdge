using UnityEngine;
using System.Collections;

namespace FastShadowProjector
{
	public static class FSPUtils
	{
		public static void CalculateFrustumPlanes(Plane[] planes, Camera camera)
		{
			var cameraMatrix = camera.projectionMatrix * camera.worldToCameraMatrix;

			CalculateFrustumPlanesNonAlloc(planes, cameraMatrix);
		}

		public static Vector3 Get3PlaneConcurrencePoint(Plane planeA, Plane planeB, Plane planeC)
		{
			var pointA = -planeA.normal * planeA.distance;
			var pointB = -planeB.normal * planeB.distance;
			var pointC = -planeC.normal * planeC.distance;

			var det = Determinant3x3(planeA.normal, planeB.normal, planeC.normal);

			var conPoint = (1f / det) * 
						   (Vector3.Dot(pointA, planeA.normal) * Vector3.Cross(planeB.normal, planeC.normal) +
							Vector3.Dot(pointB, planeB.normal) * Vector3.Cross(planeC.normal, planeA.normal) +
							Vector3.Dot(pointC, planeC.normal) * Vector3.Cross(planeA.normal, planeB.normal));

			return conPoint;
		}

		public static float Determinant3x3(Vector3 col1, Vector3 col2, Vector3 col3)
		{
			var det = (col1.x * col2.y * col3.z) +
					  (col2.x * col3.y * col1.z) +
					  (col3.x * col1.y * col2.z) -
					  (col3.x * col2.y * col1.z) -
					  (col1.x * col3.y * col2.z) -
					  (col2.x * col1.y * col3.z);

			return det;
		}

		static System.Action<Plane[], Matrix4x4> _calculateFrustumPlanes_Imp;
		static void CalculateFrustumPlanesNonAlloc(Plane[] planes, Matrix4x4 worldToProjectMatrix)
		{
			if (planes == null) throw new System.ArgumentNullException("planes");
			if (planes.Length < 6) throw new System.ArgumentException("Output array must be at least 6 in length.", "planes");

			if (_calculateFrustumPlanes_Imp == null)
			{
				var meth = typeof(GeometryUtility).GetMethod("Internal_ExtractPlanes", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new System.Type[] { typeof(Plane[]), typeof(Matrix4x4) }, null);
				if (meth == null) throw new System.Exception("Failed to reflect internal method. Your Unity version may not contain the presumed named method in GeometryUtility.");

				_calculateFrustumPlanes_Imp = System.Delegate.CreateDelegate(typeof(System.Action<Plane[], Matrix4x4>), meth) as System.Action<Plane[], Matrix4x4>;
				if(_calculateFrustumPlanes_Imp == null) throw new System.Exception("Failed to reflect internal method. Your Unity version may not contain the presumed named method in GeometryUtility.");
			}

			_calculateFrustumPlanes_Imp(planes, worldToProjectMatrix);
		}
	}
}
