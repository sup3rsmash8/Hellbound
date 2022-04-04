using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using JTools;

namespace SmashysFramework
{
    public static class Mathffs
    {
        // Part of the OverlapComponents function.
        private static Collider[] _overlapResults = new Collider[8];
        //IF MARIO_64
        private static readonly int _charLayer = LayerMask.NameToLayer("Character");
        //ENDIF
        public static Quaternion LookOnSurface(Vector3 lookDirection, Vector3 normal)
        {
            Vector3 right = Vector3.Cross(normal, lookDirection).normalized;
            Vector3 forward = Vector3.Cross(right, normal);
            return Quaternion.LookRotation(forward, normal);
        }

        public static Vector3 ForwardSurface(Vector3 lookDirection, Vector3 normal)
        {
            Vector3 right = Vector3.Cross(normal, lookDirection).normalized;
            Vector3 forward = Vector3.Cross(right, normal);

            return forward;
        }

        /// <summary>
        /// <br>Converts a character's GravityRotation to face the direction of a wall normal.</br>
        /// <br>Primarily intended for IGravitable.GravityRotation, but works just as well for transform.rotation on objects that don't inherit it.</br>
        /// </summary>
        /// <returns></returns>
        public static Quaternion WallNormalToGravityRotation(Quaternion gravityRotation, Vector3 wallNormal)
        {
            //Quaternion q = gravityRotation * Quaternion.FromToRotation(gravityRotation * Vector3.forward, wallNormal);
            //q.eulerAngles = new Vector3(0, q.y, 0);
            gravityRotation.eulerAngles += new Vector3(0, Vector3.Angle(gravityRotation * Vector3.forward, wallNormal), 0);

            return gravityRotation;
        }

        /// <summary>
        /// Computes and stores all components of type T touching or inside the sphere into
        /// the provided buffer. 
        /// <br>Components will only be detected if a collider is attached to them. (Alternatively, 
        /// if the collider belongs to an ICollisionModule, the function will search its root object
        /// as well.)</br>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static int SphereOverlapComponents<T>(Vector3 position, float radius, T[] results, int layerMask, Collider ignoreCollider = null, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal) where T : class
        {
            // Resize cache if smaller than possible results.
            if (results.Length > _overlapResults.Length)
                _overlapResults = new Collider[_overlapResults.Length];

            int resultCount = 0;
            bool hasCheckedIgnore = false;

            for (int i = 0; i < Physics.OverlapSphereNonAlloc(position, radius, _overlapResults, layerMask); i++)
            {
                if (!hasCheckedIgnore && _overlapResults[i] == ignoreCollider)
                {
                    hasCheckedIgnore = true;
                    continue;
                }

                T component;

                if (_overlapResults[i].TryGetComponent(out component))
                {
                    results[resultCount] = component;
                    resultCount++;
                }
                // IF MARIO_64
                //else if (_overlapResults[i].gameObject.layer == _charLayer && _overlapResults[i].transform.GetComponentInParent<ICollisionModule>() != null)
                //{
                //    // This once again assumes the Collider_Root structure, where the actor's actual
                //    // collision is stored as a child object to a child object to the actor.
                //    component = _overlapResults[i].transform.parent.GetComponentInParent<T>();
                //    if (component != null)
                //    {
                //        results[resultCount] = component;
                //        resultCount++;
                //    }
                //}
                // ENDIF
            }

            return resultCount;
        }

        /// <summary>
        /// Basically like Transform.RotateAround, except the rotation is done with a quaternion, and the result can
        /// be applied to Vector3s and Quaternions without the need of a transform. Rotates rotation by rotationChange
        /// such that an object would seemingly rotate around point.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="point"></param>
        /// <param name="rotationChange"></param>
        public static void RotateAroundPoint(ref Vector3 position, ref Quaternion rotation, Vector3 point, float angle, Vector3 axis)
        {
            Vector3 delta = Quaternion.Inverse(rotation) * (point - position);

            rotation *= Quaternion.AngleAxis(angle, axis);

            delta = rotation * delta;

            position = point - delta;
        }

        /// <summary>
        /// Normalizes a euler angle, i.e. fits it into the interval -180 < x < 180.
        /// </summary>
        /// <param name="eulerAngle"></param>
        /// <returns></returns>
        public static float NormalizeEuler(float eulerAngle)
        {
            return -Mathf.DeltaAngle(eulerAngle, 0);
        }

        public static Vector3 NormalizedEulerAngles(Vector3 eulerAngles)
        {
            return new Vector3(NormalizeEuler(eulerAngles.x), NormalizeEuler(eulerAngles.y), NormalizeEuler(eulerAngles.z));
        }

        public static float CosD(float d) => Mathf.Cos(Mathf.Deg2Rad * d);

        public static float SinD(float d) => Mathf.Sin(Mathf.Deg2Rad * d);

        public static float TanD(float d) => Mathf.Tan(Mathf.Deg2Rad * d);

        public static float AcosD(float f) => Mathf.Acos(f) * Mathf.Rad2Deg;

        public static float AsinD(float f) => Mathf.Asin(f) * Mathf.Rad2Deg;

        public static float AtanD(float f) => Mathf.Atan(f) * Mathf.Rad2Deg;

        public static float Atan2D(float y, float x) => Mathf.Atan2(y, x) * Mathf.Rad2Deg;

        public static float Atan2D(Vector2 yx) => Mathf.Atan2(yx.y, yx.x) * Mathf.Rad2Deg;

        /// <summary>
        /// Calculates the angles of a two-joint 2D inverse kinematic system based on two lengths and a pole-to-target distance.
        /// </summary>
        /// <param name="innerLength">The length of the joint closest to the root pole.</param>
        /// <param name="outerLength">The length of the joint closest to the target.</param>
        /// <param name="targetDistance">The magnitude of the target -> pole vector.</param>
        /// <param name="innerAngle">The resulting angle for the inner joint.</param>
        /// <param name="outerAngle">The resulting angle for the outer joint.</param>
        public static void CalculateIKAngles(float innerLength, float outerLength, float targetDistance, out float innerAngle, out float outerAngle)
        {
            innerLength = Mathf.Clamp(innerLength, 0, Mathf.Infinity);
            outerLength = Mathf.Clamp(outerLength, 0, Mathf.Infinity);

            float totalLength = innerLength + outerLength;

            if (targetDistance >= totalLength)
            {
                innerAngle = 0;
                outerAngle = 0;
                return;
            }

            float atan = Atan2D(0, targetDistance);

            innerAngle = atan - LawOfCosines(outerLength, targetDistance, innerLength);
            outerAngle = 180f - LawOfCosines(targetDistance, outerLength, innerLength);
        }

        /// <summary>
        /// Returns in degrees an internal angle of a triangle.
        /// </summary>
        /// <param name="a">The segment BC.</param>
        /// <param name="b">The segment AC.</param>
        /// <param name="c">The segment AB.</param>
        public static float LawOfCosines(float a, float b, float c)
        {
            float result = (float)((b * b) + (c * c) - (a * a)) / (2f * b * c);

            return AcosD(result);
        }

        public enum Axis3D
        {
            X,
            Y,
            Z
        }
    }

}
