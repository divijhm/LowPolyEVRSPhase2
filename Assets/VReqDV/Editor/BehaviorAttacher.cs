using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

public class BehaviorAttacher
{
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        Debug.Log("[VReqDV] OnScriptsReloaded fired.");
        // Check if we have a pending attachment job
        if (EditorPrefs.HasKey("VReqDV_PendingVersion"))
        {
            string versionStr = EditorPrefs.GetString("VReqDV_PendingVersion");
            Debug.Log($"[VReqDV] Found pending attachment for Version {versionStr}");
            
            string version = "Version_" + versionStr;
            EditorPrefs.DeleteKey("VReqDV_PendingVersion"); // Clear flag

            AttachBehaviors(versionStr, version);
        }
    }

    private static void AttachBehaviors(string versionNumber, string versionNamespace)
    {
        string directory_path = $"Assets/VReqDV/specifications/version_{versionNumber}";
        if (!Directory.Exists(directory_path))
        {
            Debug.LogError($"[VReqDV] Specifications directory not found: {directory_path}");
            return;
        }

        string jsonData = File.ReadAllText(directory_path + "/behavior.json");
        var behaviorRules = BehaviorJsonParser.Parse(jsonData);

        string articleJson = File.ReadAllText(directory_path + "/article.json");
        ArticleList articleList = JsonConvert.DeserializeObject<ArticleList>(articleJson);
        Dictionary<string, List<string>> inhMap = new Dictionary<string, List<string>>();
        foreach (var art in articleList.articles)
        {
             if (!string.IsNullOrEmpty(art.source))
             {
                 if (!inhMap.ContainsKey(art.source)) inhMap[art.source] = new List<string>();
                 inhMap[art.source].Add(art._objectname);
             }
        }

        List<BehaviorRule> finalRules = new List<BehaviorRule>();

        foreach (var rule in behaviorRules.behaviors)
        {
             // Hybrid Inheritance logic matching Generator
            HashSet<string> targetActors = new HashSet<string>();
            if (rule.Actors != null)
            {
                foreach(var actor in rule.Actors) targetActors.Add(actor);
            }

            // Expand via Inheritance Map
            var initialActors = new List<string>(targetActors);
            foreach (var actor in initialActors)
            {
                if (inhMap.ContainsKey(actor))
                {
                    foreach (string derivedObj in inhMap[actor])
                    {
                        targetActors.Add(derivedObj);
                    }
                }
            }

            foreach (string actorName in targetActors)
            {
                BehaviorRule newRule = DeepCopyRule(rule);
                newRule.Id = rule.Id + "_" + actorName; // Matching Generator's naming convention
                newRule.Source = actorName;
                finalRules.Add(newRule);
            }
        }

        int count = 0;
        foreach (var rule in finalRules)
        {
            string className = $"{versionNamespace}.{rule.Id}";
            string initializerName = $"{versionNamespace}.{rule.Source}Initializer";

            // If this is an XR interaction, attach prerequisites before the behavior script
            if (rule.Event == "OnXRInteraction")
            {
                EnsureXRInteractable(rule.Source);
            }

            // Attach Behavior
            AttachComponent(rule.Source, className);
            
            // Attach Initializer (State Machine Storage)
            AttachComponent(rule.Source, initializerName);
            
            count++;
        }
        
        Debug.Log($"[VReqDV] Successfully attached {count} behaviors/initializers for {versionNamespace}.");
    }

    private static void EnsureXRInteractable(string objName)
    {
        GameObject obj = GameObject.Find(objName);
        if (obj == null) return;

        UnityEngine.XR.Interaction.Toolkit.XRBaseInteractable interactable = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRBaseInteractable>();
        
        if (interactable == null)
        {
            Debug.Log($"[VReqDV] {objName} requested OnXRInteraction but has no interactable. Attaching XRSimpleInteractable.");
            
            // Require a collider for the raycast to hit
            if (obj.GetComponent<Collider>() == null)
            {
                obj.AddComponent<BoxCollider>();
            }

            interactable = obj.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable>();

            // Wire up the Interaction Manager from the scene
            UnityEngine.XR.Interaction.Toolkit.XRInteractionManager manager = UnityEngine.Object.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            if (manager != null)
            {
                interactable.interactionManager = manager;
            }

            // Mark the object dirty so Unity saves the newly attached components in the scene
            EditorUtility.SetDirty(obj);
        }
    }

    private static void AttachComponent(string objName, string typeName)
    {
        GameObject obj = GameObject.Find(objName);
        if (obj == null) 
        {
            Debug.LogWarning($"[VReqDV] Could not find GameObject: {objName}");
            return;
        }

        Type type = Type.GetType(typeName + ", Assembly-CSharp"); 
        if (type == null)
        {
             type = Type.GetType(typeName);
        }

        if (type != null)
        {
            Debug.Log($"[VReqDV] Found Type: {type.FullName} for Object: {objName}");
            if (obj.GetComponent(type) == null)
            {
                obj.AddComponent(type);
                Debug.Log($"[VReqDV] Attached {type.Name} to {objName}");
            }
            else
            {
                Debug.Log($"[VReqDV] {type.Name} already attached to {objName}");
            }
        }
        else
        {
            Debug.LogError($"[VReqDV] FAILED to find Type: {typeName}");    
        }
    }

    private static BehaviorRule DeepCopyRule(BehaviorRule rule)
    {
        string json = JsonConvert.SerializeObject(rule);
        return JsonConvert.DeserializeObject<BehaviorRule>(json);
    }
}
