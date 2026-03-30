using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class BehaviorCodeGenerator
{
    public static void CreateBehaviors(string directory_path, Dictionary<string, List<string>> inheritanceMap = null)
    {
        string jsonData = File.ReadAllText(directory_path + "/behavior.json");

        // Parse JSON → runtime rules
        var behaviorRules = BehaviorJsonParser.Parse(jsonData);

        // Extract version from directory path (e.g., "version_16" -> "Version_16")
        string dirName = new DirectoryInfo(directory_path).Name;
        string version = char.ToUpper(dirName[0]) + dirName.Substring(1);

        List<BehaviorRule> finalRules = new List<BehaviorRule>();

        foreach (var rule in behaviorRules.behaviors)
        {
            // Hybrid Inheritance & Actor Expansion
            // 1. Start with explicit actors
            HashSet<string> targetActors = new HashSet<string>();
            if (rule.Actors != null)
            {
                foreach(var actor in rule.Actors) targetActors.Add(actor);
            }
            // Fallback for legacy JSONs that might still use "source" (though we mapped it, Classes.cs handles the reverse mapping if we really wanted to, but let's trust Actors list for now)

            // 2. Expand via Inheritance Map (Implicit)
            // If any of the targetActors have children in the inheritance map, add them too.
            // We iterate a copy of the list so we can modify the HashSet
            var initialActors = new List<string>(targetActors);
            if (inheritanceMap != null)
            {
                foreach (var actor in initialActors)
                {
                    if (inheritanceMap.ContainsKey(actor))
                    {
                        foreach (string derivedObj in inheritanceMap[actor])
                        {
                            targetActors.Add(derivedObj);
                        }
                    }
                }
            }

            // 3. Generate for each Unique Actor
            foreach (string actorName in targetActors)
            {
                BehaviorRule newRule = DeepCopyRule(rule);
                
                // Append ActorName to ID to avoid collision if multiple actors use same behavior template
                
                newRule.Id = rule.Id + "_" + actorName;
                newRule.Source = actorName; // This sets the Actors[0] too due to our compat property, but mainly for logic usage
                
                // 4. Smart Substitution
                // Replace "Self" -> actorName
                ReplaceParams(newRule.Precondition, "Self", actorName);
                ReplaceParams(newRule.Action, "Self", actorName);
                if (newRule.Postcondition != null) ReplaceParams(newRule.Postcondition, "Self", actorName);

                // Legacy Substitution: Replace the "Primary Source" (first actor in definitions) with current actorName
                // This handles cases where user wrote "Pin_1" explicitly in the condition but meant "Self" (implicit self).
                // Only do this if we are not processing the primary source itself (though replacing string with same string is harmless).
                string primarySource = (rule.Actors != null && rule.Actors.Count > 0) ? rule.Actors[0] : null;
                if (!string.IsNullOrEmpty(primarySource))
                {
                     ReplaceParams(newRule.Precondition, primarySource, actorName);
                     ReplaceParams(newRule.Action, primarySource, actorName);
                     if (newRule.Postcondition != null) ReplaceParams(newRule.Postcondition, primarySource, actorName);
                }

                finalRules.Add(newRule);
            }
        }
        
        // Update the list with expanded rules
        behaviorRules.behaviors = finalRules;

        // Create/Clear target directory
        string targetDir = "Assets/VReqDV/Generated/Behaviors/" + version;
        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, true);
        }
        Directory.CreateDirectory(targetDir);

        foreach (var rule in behaviorRules.behaviors)
        {
            GenerateBehavior(rule, targetDir, version);
        }
        
        // GenerateLoader(behaviorRules, "Assets/VReqDV/Generated/Behaviors/" + version, version);

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    private static BehaviorRule DeepCopyRule(BehaviorRule rule)
    {
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(rule);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<BehaviorRule>(json);
    }

    private static void ReplaceParams(ConditionNode node, string oldName, string newName)
    {
        if (node == null) return;

        if (node.all != null) foreach (var child in node.all) ReplaceParams(child, oldName, newName);
        if (node.any != null) foreach (var child in node.any) ReplaceParams(child, oldName, newName);
        
        if (node.equals != null)
        {
            for (int i = 0; i < node.equals.Count; i++)
            {
                node.equals[i] = node.equals[i].Replace(oldName, newName);
            }
        }

        if (node.@params != null)
        {
            var keys = new List<string>(node.@params.Keys);
            foreach (var key in keys)
            {
                node.@params[key] = node.@params[key].Replace(oldName, newName);
            }
        }
    }

    private static void ReplaceParams(ActionNode node, string oldName, string newName)
    {
        if (node == null) return;
        
        if (node.@params != null)
        {
            var keys = new List<string>(node.@params.Keys);
            foreach (var key in keys)
            {
                node.@params[key] = node.@params[key].Replace(oldName, newName);
            }
        }
    }

    public static void GenerateBehavior(
        BehaviorRule rule,
        string outputFolder,
        string version
    )
    {
        Debug.Log($"Generating behavior for file: {rule.Id} (Version: {version})");
        Directory.CreateDirectory(outputFolder);

        string code;
        switch (rule.Event)
        {
            case "OnCondition":
                code = GenerateOnCondition(rule, version);
                break;
            case "OnXRInteraction":
                code = GenerateXRInteractableEvent(rule, version);
                break;
            default:
                code = GenerateOnStateChange(rule, version);
                break;
        }

        File.WriteAllText(
            Path.Combine(outputFolder, $"{rule.Id}.cs"),
            code
        );
    }

    // ------------------------------------------------------------
    // ON CONDITION TEMPLATE (Polling)
    // ------------------------------------------------------------
    private static string GenerateOnCondition(BehaviorRule rule, string version)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// GENERATED FILE — DO NOT EDIT");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine($"namespace {version}");
        sb.AppendLine("{");
        sb.AppendLine($"    public class {rule.Id} : MonoBehaviour");
        sb.AppendLine("    {");
        sb.AppendLine("        void Update()");
        sb.AppendLine("        {");
        sb.AppendLine($"            if ({GenerateCondition(rule.Precondition)})");
        sb.AppendLine("            {");
        
        string args = GetParamsString(rule.Action?.@params);
        sb.AppendLine($"                UserAlgorithms.{rule.ActionAlgorithm}({args});");
        
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // ------------------------------------------------------------
    // ON STATE CHANGE TEMPLATE (Subscription)
    // ------------------------------------------------------------
    private static string GenerateOnStateChange(BehaviorRule rule, string version)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// GENERATED FILE — DO NOT EDIT");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine($"namespace {version}");
        sb.AppendLine("{");
        sb.AppendLine($"    public class {rule.Id} : MonoBehaviour");
        sb.AppendLine("    {");
        sb.AppendLine("        void OnEnable()");
        sb.AppendLine("        {");
        sb.AppendLine($"            {rule.Source}StateStorage.OnStateChanged += Handle;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        void OnDisable()");
        sb.AppendLine("        {");
        sb.AppendLine($"            {rule.Source}StateStorage.OnStateChanged -= Handle;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        void Handle(GameObject obj, {rule.Source}StateEnum newState)");
        sb.AppendLine("        {");
        sb.AppendLine($"            if ({GenerateCondition(rule.Precondition)})");
        sb.AppendLine("            {");
        
        string args = GetParamsString(rule.Action?.@params);
        sb.AppendLine($"                UserAlgorithms.{rule.ActionAlgorithm}({args});");
        
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // ------------------------------------------------------------
    // ON XR INTERACTION TEMPLATE (Event-driven)
    // ------------------------------------------------------------
    
    private static string ExtractXRInteraction(ConditionNode node)
    {
        if (node == null) return null;
        if (!string.IsNullOrEmpty(node.XRinteraction)) return node.XRinteraction;
        
        if (node.all != null)
        {
            foreach (var child in node.all)
            {
                var res = ExtractXRInteraction(child);
                if (res != null) return res;
            }
        }
        
        if (node.any != null)
        {
            foreach (var child in node.any)
            {
                var res = ExtractXRInteraction(child);
                if (res != null) return res;
            }
        }
        
        return null;
    }

    private static string GenerateXRInteractableEvent(BehaviorRule rule, string version)
    {
        // Recursively find XRinteraction in the precondition tree
        string interactionType = ExtractXRInteraction(rule.Precondition);

        // Map interactionType to XRI event name and args type
        string eventName;
        string argsType;
        switch (interactionType?.ToLower())
        {
            case "grab":
            case "select":
                eventName = "selectEntered";
                argsType = "SelectEnterEventArgs";
                break;
            case "grabexit":
            case "selectexit":
                eventName = "selectExited";
                argsType = "SelectExitEventArgs";
                break;
            case "activate":
                eventName = "activated";
                argsType = "ActivateEventArgs";
                break;
            case "deactivate":
                eventName = "deactivated";
                argsType = "DeactivateEventArgs";
                break;
            case "hover":
                eventName = "hoverEntered";
                argsType = "HoverEnterEventArgs";
                break;
            case "hoverexit":
                eventName = "hoverExited";
                argsType = "HoverExitEventArgs";
                break;
            case "focus":
                eventName = "focusEntered";
                argsType = "FocusEnterEventArgs";
                break;
            case "focusexit":
                eventName = "focusExited";
                argsType = "FocusExitEventArgs";
                break;
            default:
                Debug.LogWarning($"Unknown XRinteraction '{interactionType}' — defaulting to activated");
                eventName = "activated";
                argsType = "ActivateEventArgs";
                break;
        }

        var sb = new StringBuilder();

        sb.AppendLine("// GENERATED FILE — DO NOT EDIT");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.XR.Interaction.Toolkit;");
        sb.AppendLine();
        sb.AppendLine($"namespace {version}");
        sb.AppendLine("{");
        sb.AppendLine($"    public class {rule.Id} : MonoBehaviour");
        sb.AppendLine("    {");
        sb.AppendLine("        private XRBaseInteractable _interactable;");
        sb.AppendLine();
        sb.AppendLine("        void OnEnable()");
        sb.AppendLine("        {");
        sb.AppendLine("            _interactable = GetComponent<XRBaseInteractable>();");
        sb.AppendLine($"            if (_interactable != null) _interactable.{eventName}.AddListener(OnInteraction);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        void OnDisable()");
        sb.AppendLine("        {");
        sb.AppendLine($"            if (_interactable != null) _interactable.{eventName}.RemoveListener(OnInteraction);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        void OnInteraction({argsType} args)");
        sb.AppendLine("        {");

        // Generate precondition check
        string condition = GenerateCondition(rule.Precondition);
        if (condition != "true")
        {
            sb.AppendLine($"            if ({condition})");
            sb.AppendLine("            {");
            string args2 = GetParamsString(rule.Action?.@params);
            sb.AppendLine($"                UserAlgorithms.{rule.ActionAlgorithm}({args2});");
            sb.AppendLine("            }");
        }
        else
        {
            string args2 = GetParamsString(rule.Action?.@params);
            sb.AppendLine($"            UserAlgorithms.{rule.ActionAlgorithm}({args2});");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // ------------------------------------------------------------
    // CONDITION GENERATOR (runtime ConditionNode)
    // ------------------------------------------------------------
    private static string GenerateCondition(ConditionNode condition)
    {
        if (condition == null)
        {
            return "true";
        }

        if (condition.all != null && condition.all.Count > 0)
        {
            var parts = new List<string>();
            foreach (var c in condition.all)
            {
                parts.Add(GenerateCondition(c));
            }
            return "(" + string.Join(" && ", parts) + ")";
        }

        if (condition.any != null && condition.any.Count > 0)
        {
            var parts = new List<string>();
            foreach (var c in condition.any)
            {
                parts.Add(GenerateCondition(c));
            }
            return "(" + string.Join(" || ", parts) + ")";
        }

        if (!string.IsNullOrEmpty(condition.left) &&
            !string.IsNullOrEmpty(condition.right))
        {
            if (condition.left.EndsWith(".state"))
            {
                string objName = condition.left.Substring(0, condition.left.LastIndexOf(".state"));
                string stateValue = Pascal(condition.right);
                return $"{objName}StateStorage.Get(GameObject.Find(\"{objName}\")) == {objName}StateEnum.{stateValue}";
            }
            return $"{condition.left} == {condition.right}";
        }

        if (!string.IsNullOrEmpty(condition.runAlgorithm))
        {
            string args = GetParamsString(condition.@params);
            return $"UserAlgorithms.{condition.runAlgorithm}({args})";
        }

        return "true";
    }

    private static string GetParamsString(Dictionary<string, string> parameters)
    {
        if (parameters != null && parameters.ContainsKey("obj"))
        {
            return $"GameObject.Find(\"{parameters["obj"]}\")";
        }
        return "";
    }

    private static string Pascal(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToUpper(value[0]) + value.Substring(1);
    }

}
