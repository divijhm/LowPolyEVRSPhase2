using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using Newtonsoft.Json;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;
using HF = HelperFunctions;
using UnityEngine.XR.Interaction.Toolkit;

public class ObjectHandler
{
    public static Dictionary<string, List<string>> CreateObjects(string directory_path)
    {
        string jsonData = File.ReadAllText(directory_path + "/article.json");
        ArticleList objectDataList = JsonConvert.DeserializeObject<ArticleList>(jsonData);

        // Create lookup dictionary
        Dictionary<string, Article> articleMap = new Dictionary<string, Article>();
        
        // Map for Source -> Derived mapping
        Dictionary<string, List<string>> inheritanceMap = new Dictionary<string, List<string>>();

        foreach (var art in objectDataList.articles)
        {
            if (!string.IsNullOrEmpty(art._objectname) && !articleMap.ContainsKey(art._objectname))
            {
                articleMap.Add(art._objectname, art);
            }
            
            // Build inheritance map
            if (!string.IsNullOrEmpty(art.source))
            {
                if (!inheritanceMap.ContainsKey(art.source))
                {
                    inheritanceMap[art.source] = new List<string>();
                }
                inheritanceMap[art.source].Add(art._objectname);
            }
        }

        // Resolve inheritance
        foreach (Article art in objectDataList.articles)
        {
            if (!string.IsNullOrEmpty(art.source) && articleMap.ContainsKey(art.source))
            {
                Article sourceArt = articleMap[art.source];
                
                if (string.IsNullOrEmpty(art.shape)) art.shape = sourceArt.shape;
                if (art.Transform_initialpos == null) art.Transform_initialpos = sourceArt.Transform_initialpos;
                if (art.Transform_initialrotation == null) art.Transform_initialrotation = sourceArt.Transform_initialrotation;
                if (art.Transform_objectscale == null) art.Transform_objectscale = sourceArt.Transform_objectscale;
                if (art.XRRigidObject == null) art.XRRigidObject = sourceArt.XRRigidObject;
                if (art.Interaction == null) art.Interaction = sourceArt.Interaction;
                if (art.states == null) art.states = sourceArt.states;
                if (string.IsNullOrEmpty(art.context_img_source)) art.context_img_source = sourceArt.context_img_source;
                
                // Copy other fields if needed
                if (art.HasChild == 0 && sourceArt.HasChild != 0)
                {
                    art.HasChild = sourceArt.HasChild;
                    if (art.Children == null) art.Children = sourceArt.Children;
                }
            }
        }

        foreach (Article objectData in objectDataList.articles)
        {
            // Debug.Log(objectData._objectname);
            GameObject go = null;
            if(objectData.HasChild != 0)
            {
                go = new GameObject(objectData._objectname);
                List<GameObject> objectsToGroup = new List<GameObject>();

                foreach (string name in objectData.Children)
                {
                    GameObject obj = GameObject.Find(name);
                    if (obj != null)
                    {
                        objectsToGroup.Add(obj);
                    }
                    else
                    {
                        Debug.LogWarning("GameObject not found: " + name);
                    }
                }
                Vector3 center = Vector3.zero;
                foreach (GameObject obj in objectsToGroup)
                {
                    center += obj.transform.position;
                }
                center /= objectsToGroup.Count;
                go.transform.position = center;
                foreach (GameObject obj in objectsToGroup)
                {
                    obj.transform.SetParent(go.transform);
                }
            }
            else if(objectData.context_img_source != null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(objectData.context_img_source);
                if(prefab != null)
                {
                    // go = UnityEngine.Object.Instantiate(prefab);
                    go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    go.name = objectData._objectname;
                    if(go.GetComponent<BoxCollider>() == null)
                        go.AddComponent<BoxCollider>();
                }
                else
                {
                    Debug.LogWarning("Asset not found at: " + objectData.context_img_source);
                    go = GameObject.CreatePrimitive(HF.GetPrimitiveTypeByString(objectData.shape));
                    go.name = objectData._objectname;
                }
            }
            else
            {
                go = GameObject.CreatePrimitive(HF.GetPrimitiveTypeByString(objectData.shape));
                go.name = objectData._objectname;
            }

            if(objectData.shape != "empty")
            {
                go.transform.position = new Vector3(
                    float.Parse(objectData.Transform_initialpos.x),
                    float.Parse(objectData.Transform_initialpos.y),
                    float.Parse(objectData.Transform_initialpos.z)
                );

                go.transform.rotation = Quaternion.Euler(
                    float.Parse(objectData.Transform_initialrotation.x),
                    float.Parse(objectData.Transform_initialrotation.y),
                    float.Parse(objectData.Transform_initialrotation.z)
                );

                go.transform.localScale = new Vector3(
                    float.Parse(objectData.Transform_objectscale.x),
                    float.Parse(objectData.Transform_objectscale.y),
                    float.Parse(objectData.Transform_objectscale.z)
                );
            }

            if(objectData.XRRigidObject.value == "1" && go.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = go.AddComponent<Rigidbody>();
                rb.mass = float.Parse(objectData.XRRigidObject.mass);
                rb.drag = float.Parse(objectData.XRRigidObject.dragfriction);
                rb.angularDrag = float.Parse(objectData.XRRigidObject.angulardrag);
                rb.useGravity = bool.Parse(objectData.XRRigidObject.Isgravityenable);
                rb.isKinematic = bool.Parse(objectData.XRRigidObject.IsKinematic);

                switch (objectData.XRRigidObject.CollisionPolling)
                {
                    case "discrete":
                        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                        break;
                    case "continuous":
                        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                        break;
                    case "continuous-dynamic":
                        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                        break;
                    case "continuous-speculative":
                        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                        break;
                    default:
                        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                        break;
                }

                switch (int.Parse(objectData.XRRigidObject.CanInterpolate))
                {
                    case 1:
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                        break;
                    case 2:
                        rb.interpolation = RigidbodyInterpolation.Extrapolate;
                        break;
                    default:
                        rb.interpolation = RigidbodyInterpolation.None;
                        break;
                }
            }

            // Apply XRGrabInteractable from Interaction data
            if (objectData.Interaction != null && objectData.Interaction.XRGrabInteractable == "true")
            {
                XRGrabInteractable grabInteractable = go.GetComponent<XRGrabInteractable>();
                if (grabInteractable == null)
                    grabInteractable = go.AddComponent<XRGrabInteractable>();

                // Wire up the Interaction Manager from the scene
                UnityEngine.XR.Interaction.Toolkit.XRInteractionManager manager = UnityEngine.Object.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
                if (manager != null)
                {
                    grabInteractable.interactionManager = manager;
                }

                grabInteractable.movementType = XRGrabInteractable.MovementType.Kinematic;
                grabInteractable.trackPosition = objectData.Interaction.TrackPosition == "true";
                grabInteractable.trackRotation = objectData.Interaction.TrackRotation == "true";
                grabInteractable.throwOnDetach = objectData.Interaction.Throw_Detach == "true";

                // Apply interaction layer mask from layer indices or names
                if (objectData.Interaction.XRInteractionMaskLayer != null && objectData.Interaction.XRInteractionMaskLayer.Count > 0)
                {
                    int maskValue = 0;
                    foreach (string layerIdent in objectData.Interaction.XRInteractionMaskLayer)
                    {
                        // Try parsing as integer first
                        if (int.TryParse(layerIdent, out int idx) && idx >= 0 && idx < 32)
                        {
                            maskValue |= (1 << idx);
                        }
                        else
                        {
                            // Try parsing as string name
                            // Unity layer names are case-sensitive. Capitalize first letter as fallback for "default"
                            string fixedLayerIdent = layerIdent;
                            if (layerIdent.ToLower() == "default") fixedLayerIdent = "Default";

                            int layerIdx = InteractionLayerMask.NameToLayer(fixedLayerIdent);
                            if (layerIdx != -1)
                            {
                                maskValue |= (1 << layerIdx);
                            }
                            else
                            {
                                Debug.LogWarning($"[VReqDV] Unknown Interaction Layer: {layerIdent}");
                            }
                        }
                    }
                    InteractionLayerMask layerMask = grabInteractable.interactionLayers;
                    layerMask.value = maskValue;
                    grabInteractable.interactionLayers = layerMask;
                }
            }

            // Extract version from directory path
            string dirName = new DirectoryInfo(directory_path).Name;
            string version = char.ToUpper(dirName[0]) + dirName.Substring(1); // e.g., "Version_16"

            StateCodeGenerator.GenerateStateSystem(objectData._objectname, objectData.states, version);
        }
        return inheritanceMap;
    }

}