using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SmashysFramework;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace MainGame.Algorithms
{
    /// <summary>
    /// Class that helps applying two-joint inverse kinematics to characters' legs.
    /// </summary>
    public class LegIK : MonoBehaviour
    {
        #region Cache
        private bool _didHit = false;
        private RaycastHit _ikHit;
        #endregion

        #region MethodsAndProperties
        public bool IsValid()
        {
            return _innerJoint && _outerJoint && _extremityJoint && _modelTransform;
        }

        /// <summary>
        /// Returns the distance between the leg joint and the knee joint.
        /// </summary>
        public float InnerLength
        {
            get
            {
                if (!IsValid())
                    return 0;

                float result = _outerJoint.localPosition.y;

                return Mathf.Abs(result);
            }
        }

        /// <summary>
        /// Returns the distance between the knee joint and the foot joint.
        /// </summary>
        public float OuterLength
        {
            get
            {
                if (!IsValid())
                    return 0;

                float result = _extremityJoint.localPosition.y;

                return Mathf.Abs(result);
            }
        }

        /// <summary>
        /// Returns whether or not the IK leg has hit something.
        /// </summary>
        public bool DidHit() => _didHit;

        /// <summary>
        /// Returns whether or not the IK leg has hit something. This
        /// variant also returns information about what it hit.
        /// </summary>
        public bool DidHit(out RaycastHit hit)
        {
            if (_didHit)
            {
                hit = _ikHit;
            }
            else
            {
                hit = new RaycastHit();
            }

            return _didHit;
        }

        public bool SetIKActive { set => _active = value; }
        #endregion

        #region InspectorFields
        [SerializeField]
        private bool _active = true;

        [SerializeField]
        private LayerMask _layerMask;

#if UNITY_EDITOR
        [SerializeField]
        private bool _isDebug = false;
#endif

        [Space]

        //[SerializeField, Tooltip("The local axis direction along which the joints are aligned down to the extremity.")]
        //private Mathffs.Axis3D _alignmentAxis = Mathffs.Axis3D.Y;

        //[SerializeField, Tooltip("The local axis around which the joints will rotate.")]
        //private Mathffs.Axis3D _rotationAxis = Mathffs.Axis3D.X;

        [SerializeField, Tooltip("The joint closest to the torso. This is the transform you normally place this component on.")]
        private Transform _innerJoint;

        [SerializeField, Tooltip("The joint connected to the other end of the inner joint.")]
        private Transform _outerJoint;

        [SerializeField, Tooltip("The joint connected to the end of the outer joint." +
            "When collision is detected, will always try to maintain a rotation perpendicular of the surface it touches.")]
        private Transform _extremityJoint;

        [SerializeField, Tooltip("The transform of the model this leg belongs to.")]
        private Transform _modelTransform;

        [Space]

        [SerializeField, Tooltip("The thickness of the sole, if it's not aligned with the extremity joint.")]
        private float _soleSize = 0.01f;
        #endregion

        protected virtual void LateUpdate()
        {
            ProcessIK();
        }

        private void OnDrawGizmos()
        {
            //Handles.Draw
        }

        protected void ProcessIK()
        {
            if (!IsValid() || !_active)
            {
                return;
            }

            Vector3 start = _innerJoint.position;
            Vector3 end = _extremityJoint.position + _extremityJoint.forward * _soleSize;

#if UNITY_EDITOR
            if (_isDebug)
                Debug.DrawLine(start, end, new Color(0.5f, 0, 0));
#endif

            _didHit = Physics.Linecast(start, end, out RaycastHit hit, _layerMask);

            if (_didHit)
            {
                // Get the two angles using the law of cosines.
                Mathffs.CalculateIKAngles(InnerLength, OuterLength + _soleSize, hit.distance, out float innerAngle, out float outerAngle);

                // Prevent divisions by zero.
                if (float.IsNaN(innerAngle) || float.IsNaN(outerAngle))
                {
                    _didHit = false;
                    return;
                }

                _ikHit = hit;

                // Save foot rotation until after the leg rotation
                // has been calculated.
                Quaternion extremityRot = _extremityJoint.rotation;

                float fromToAngle;
                float newXAngle;

                // Due to eulerAngles in Unity being inconsistent, adding and
                // subtracting will be based on the dot product.
                float deltaAngle = Vector3.Dot(-_modelTransform.up, _innerJoint.up);
                if (deltaAngle >= 0)
                {
                    // First, add offset that points in the direction of the foot.
                    fromToAngle = 90 - Mathffs.AcosD(Vector3.Dot(-_modelTransform.up, Vector3.Cross(_modelTransform.right, (end - start).normalized)));
                    fromToAngle = -fromToAngle;

                    // THEN we add the IK angle.
                    newXAngle = fromToAngle + innerAngle;
                }
                else
                {
                    // First, add offset that points in the direction of the foot.
                    fromToAngle = 90 + Mathffs.AcosD(Vector3.Dot(-_modelTransform.up, Vector3.Cross(_modelTransform.right, (end - start).normalized)));
                    fromToAngle = -fromToAngle;

                    // THEN we add the IK angle.
                    newXAngle = fromToAngle - innerAngle;
                }

                Vector3 euler = _innerJoint.localEulerAngles;
                _innerJoint.localEulerAngles = new Vector3(newXAngle, euler.y, euler.z);

                // Even for forearms this problem occurs,
                // so we must use the dot product here as well.
                if (Vector3.Dot(_innerJoint.up, _outerJoint.up) < 0)
                    deltaAngle = 180 - outerAngle;
                else
                    deltaAngle = outerAngle;

                euler = _outerJoint.localEulerAngles;
                _outerJoint.localEulerAngles = new Vector3(deltaAngle, euler.y, euler.z);

                _extremityJoint.rotation = extremityRot;

#if UNITY_EDITOR
                if (_isDebug)
                    Debug.DrawLine(start, hit.point, Color.red);
#endif
            }

#if UNITY_EDITOR
            if (_isDebug)
            {
                Debug.DrawLine(_innerJoint.position, _outerJoint.position, Color.gray);
                Debug.DrawLine(_outerJoint.position, _extremityJoint.position + _extremityJoint.forward * _soleSize, Color.gray);
                if (!_didHit)
                    Debug.DrawLine(start, end, Color.red);
            }
#endif
        }

        protected Vector3 LocalRotateJointAroundAxis(Vector3 jointLocalEulerAngles, Mathffs.Axis3D axis, float angles)
        {
            switch (axis)
            {
                default:
                case Mathffs.Axis3D.X:
                    jointLocalEulerAngles.x = angles;
                    break;

                case Mathffs.Axis3D.Y:
                    jointLocalEulerAngles.y = angles;
                    break;

                case Mathffs.Axis3D.Z:
                    jointLocalEulerAngles.z = angles;
                    break;
            }

            return jointLocalEulerAngles;
        }

#if UNITY_EDITOR

        [ContextMenu("AutoConfigureIK")]
        protected void AutoConfigureIK()
        {
            _innerJoint = transform;

            if (_innerJoint)
            {
                _outerJoint = _innerJoint.GetChild(0);

                if (_outerJoint)
                {
                    _extremityJoint = _outerJoint.GetChild(0);

                    if (!_extremityJoint)
                    {
                        _innerJoint = null;
                        _outerJoint = null;
                        _extremityJoint = null;
                        Debug.LogError("Could not auto-configure IK: There is no extremity joint connected to the outer joint.");
                        return;
                    }
                }
                else
                {
                    _innerJoint = null;
                    _outerJoint = null;
                    _extremityJoint = null;
                    Debug.LogError("Could not auto-configure IK: There is no outer joint connected to the inner joint.");
                    return;
                }
            }
        }
#endif
    }
}

