using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using Newtonsoft.Json;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;

public class CompareHandler
{
    public static void ComparisonSideBySide(int v1, int v2)
    {
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SetActiveScene(newScene);

        string[] prefabPaths1 = AssetDatabase.FindAssets("t:Prefab", new string[] { $"Assets/VReqDV/ScenePrefabs/version_{v1}" });
        string[] prefabPaths2 = AssetDatabase.FindAssets("t:Prefab", new string[] { $"Assets/VReqDV/ScenePrefabs/version_{v2}" });

        float offset = 0;

        foreach (string prefabPath in prefabPaths1)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance != null)
            {
                instance.transform.position += new Vector3(offset, 0, 0);
            }
        }

        offset += 24; // Separate the versions

        foreach (string prefabPath in prefabPaths2)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance != null)
            {
                instance.transform.position += new Vector3(offset, 0, 0);
            }
        }

        Debug.Log($"Comparison scene created for versions {v1} and {v2}.");
    }
}