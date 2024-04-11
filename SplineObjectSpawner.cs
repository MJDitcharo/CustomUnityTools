using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;
using System.Collections;
using System;

namespace Tools
{
    /// <summary>
    /// This class provides debugging features for visualizing object spawning points and rotations along a spline.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(SplineContainer))]
    public class SplineObjectSpawner : MonoBehaviour
    {
        private SplineContainer _spline;

        [SerializeField]
        private bool isEnabled = false;

        [SerializeField, Header("Disable when not being used"), Tooltip("Rebuilds Geometry in real time. Disable when not being used.")]
        private bool isDynamicRebuild = false;

        [Header("DebugObject Fields")]
        [SerializeField]
        private GameObject objectParent;

        [SerializeField]
        private GameObject objectInstance;

        [SerializeField]
        private Vector3 rotationOffset;

        // Minimum changed to 1 to prevent potential divide-by-zero.
        [SerializeField, Range(1, 500)]
        private int objectCount = 1;

        private void OnDrawGizmosSelected()
        {
            if (!_spline)
            {
                if (!TryGetComponent<SplineContainer>(out _spline))
                {
                    Debug.LogError("SplineContainer component not found on the object.");
                    return;
                }
            }

            float step = 1.0f / objectCount;
            for (float i = 0; i <= 1.0f; i += step)
            {
                if (!_spline.Evaluate(i, out float3 position, out float3 tangent, out float3 up))
                {
                    // Skip this iteration if the spline evaluation fails.
                    continue;
                }

                DrawGizmos(position, tangent, up);
            }

            // Check if Dynamic Rebuild is enabled and there are no null references
            if (isDynamicRebuild && ErrorLog()) 
            {
                RebuildObjects();
            }
        }

        private void OnValidate()
        {
            if (!ErrorLog())
            {
                return;
            }

            if (isEnabled)
            {
                StartCoroutine(SetPoolActive(true));
                StartCoroutine(UpdateObjects());
            } else
            {
                StartCoroutine(SetPoolActive(false));
            }
        }

        /// <summary>
        /// Draws gizmos in the scene view for visualizing position and orientation.
        /// </summary>
        /// <param name="position">The position along the spline.</param>
        /// <param name="tangent">The tangent of the spline at the current position, indicating direction.</param>
        /// <param name="up">The up vector relative to the spline's current position and orientation.</param>
        private void DrawGizmos(float3 position, float3 tangent, float3 up)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(position, 0.005f);

            // Calculate the rotation from the tangent and up direction.
            Quaternion lookAtRot = Quaternion.LookRotation(math.normalize(tangent), up);

            // Combine it with the additional rotation.
            Quaternion finalRot = lookAtRot * Quaternion.Euler(rotationOffset);

            // Convert position to Vector3.
            Vector3 pos = new Vector3(position.x, position.y, position.z);

            // Calculate world directions based on the final rotation.
            Vector3 worldUp = pos + finalRot * Vector3.up * 0.05f;
            Vector3 worldLeft = pos + finalRot * -Vector3.left * 0.05f;
            Vector3 worldForward = pos + finalRot * Vector3.forward * 0.05f;

            // Draw the direction lines.
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pos, worldUp);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(pos, worldLeft);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(pos, worldForward);
        }

        /// <summary>
        /// Handles placing the generated objects alongside the spline using the built in Evaluate method. 
        /// After the object is placed properly, a local rotation offset is then applied to it.
        /// </summary>
        private void RebuildObjects()
        {
            if (!ErrorLog())
                return;

            // Position objects
            for (int i = 0; i < objectParent.transform.childCount; i++)
            {
                // Evaluate returns the postion, tangent, and up vector at the point on the spline from 0 to 1
                bool result = _spline.Evaluate(i / (float)objectCount, out float3 position, out float3 tangent, out float3 up);
                if (!result)
                {
                    Debug.LogWarning("Debug Evaluate failed");
                }

                Transform currObject = objectParent.transform.GetChild(i);
                currObject.position = position;
                currObject.LookAt(position + tangent, up);
                currObject.Rotate(rotationOffset, Space.Self);
                currObject.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Destroys excess objects when the object count is less than the generated child count
        /// </summary>
        private void DestroyObjects()
        {
            int childCount = objectParent.transform.childCount - 1;
            for (int i = childCount; i >= objectCount; i--)
            {
                Transform currObject = objectParent.transform.GetChild(i);
                if (currObject)
                {
                    DestroyImmediate(currObject.gameObject);
                }
            }
        }

        /// <summary>
        /// Creates new objects based on the difference between the object count and currently generated children objects
        /// </summary>
        private void CreateObjects()
        {
            int tempChildren = objectParent.transform.childCount;
            for (int i = 0; i < objectCount - tempChildren; i++)
            {
                Instantiate(objectInstance, objectParent.transform);
            }
        }

        /// <summary>
        /// Core update coroutine that handles the logic for Instantiating, Placing, and Destroying objects along the spline. 
        /// The coroutine is used to get around OnValidate errors/warnings
        /// </summary>
        private IEnumerator UpdateObjects()
        {
            yield return new WaitForSeconds(0);

            CreateObjects();
            RebuildObjects();
            DestroyObjects();
        }

        /// <summary>
        /// A coroutine used to disable/enable parent object. 
        /// The coroutine is used to get around OnValidate errors/warnings
        /// </summary>
        /// <param name="isActive">True to enable parent</param>
        private IEnumerator SetPoolActive(bool isActive)
        {
            yield return new WaitForSeconds(0);
            objectParent.SetActive(isActive);
        }

        /// <summary>
        /// General error log to catch any null refs if they happen and Log errors when necessary.
        /// </summary>
        /// <returns>True if there are no errors. False if certain components are null or if the application is playing</returns>
        private bool ErrorLog()
        {
            if(!_spline)
            {
                Debug.LogWarning("Spline Component is null. Make sure Spline component is attached and Gizmos are enabled");
                return false;
            }

            if(!objectInstance)
            {
                Debug.Log("Object Instance is null. Make sure to set the Object Instance in the editor");
                return false;
            }

            if(!objectParent)
            {
                Debug.Log("Object Parent is null. Make sure to set the Object Parent in the editor");
                return false;
            }

            if(Application.isPlaying)
            {
                return false;
            }

            return true;
        }
    }
}
