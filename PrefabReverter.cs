#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tools.Editor
{
    /// <summary>
    /// The purpose of this class is to revert prefab overrides that pop-up when retargeting an unpacked
    /// prefab after a large amount of art changes are made
    /// </summary>
    public class PrefabReverter : EditorWindow
    {
        private const float WINDOWWIDTH = 400;
        private const float WINDOWHEIGHT = 360;
        private readonly List<GameObject> objectBlacklist = new();
        private GameObject prefabRoot;

        private Vector2 scrollPosition = Vector2.zero;

        [MenuItem(itemName: "Utilities/Prefab Reverter")]
        public static void OpenWindow()
        {
            PrefabReverter window = CreateWindow<PrefabReverter>("Prefab Reverter");
            window.minSize = new Vector2(WINDOWWIDTH, WINDOWHEIGHT);
            window.maxSize = new Vector2(WINDOWWIDTH, WINDOWHEIGHT);
        }

        private void OnGUI()
        {
            GameObject newObject = null;
            newObject = (GameObject)EditorGUILayout.ObjectField(
                "Add to Blacklist",
                newObject,
                typeof(GameObject),
                true);

            // Add Object to blacklist and refresh GUI
            if (newObject)
            {
                objectBlacklist.Add(newObject);
                Repaint();
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Blacklist");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Display list of blacklisted objects
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(250));
            for (int i = 0; i < objectBlacklist.Count; i++)
            {
                objectBlacklist[i] = (GameObject)EditorGUILayout.ObjectField(
                    i.ToString(),
                    objectBlacklist[i],
                    typeof(GameObject),
                    true);
            }

            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Clear Blacklist"))
            {
                objectBlacklist.Clear();
                Repaint();
            }

            prefabRoot = (GameObject)EditorGUILayout.ObjectField(
                "Select Prefab Root",
                prefabRoot,
                typeof(GameObject),
                true);

            if (prefabRoot)
            {
                if (GUILayout.Button("Revert Prefab Overrides"))
                {
                    RevertPrefabs(prefabRoot.transform);
                }
            }
        }

        /// <summary>
        /// Recursively iterate through all child gameobjects of root parent and revert
        /// prefab overrides. This ignores the objects included in Blacklist
        /// </summary>
        /// <param name="parent">Parent Gameobject</param>
        private void RevertPrefabs(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != null && !objectBlacklist.Contains(child.gameObject))
                {
                    // Check if object is a part of prefab
                    Transform prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(child);
                    if (!prefabParent)
                    {
                        Debug.Log(child.name + " is not attached to prefab!");
                        continue;
                    }

                    // Revert Transform
                    PrefabUtility.RevertObjectOverride(child, InteractionMode.UserAction);

                    // Revert MeshRenderer
                    if (child.TryGetComponent<MeshRenderer>(out MeshRenderer renderer))
                    {
                        PrefabUtility.RevertObjectOverride(renderer, InteractionMode.UserAction);
                    }

                    // Revert MeshFilter
                    if(child.TryGetComponent<MeshFilter>(out MeshFilter filter))
                    {
                        PrefabUtility.RevertObjectOverride(filter, InteractionMode.UserAction);
                    }
                }

                RevertPrefabs(child);
            }
        }
    }
}
#endif