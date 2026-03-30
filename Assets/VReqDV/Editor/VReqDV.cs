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

public class MainMenu : EditorWindow
{
    private static MainMenu window;
    [MenuItem("Window/VReqDV")]
    public static void ShowWindow()
    {
        window = GetWindow<MainMenu>("VReqDV");
    }

    private onScreenState screenState;

    public BehaviorList behaviorSpecifications;
    private ArticleList objectSpecifications;
    public BehaviorList compareBehaviorSpecifications;
    private ArticleList compareObjectSpecifications;

    private Vector2 scrollPositionObject;
    private Vector2 scrollPositionObjectCompare;

    private int selected_display_component = 0;
    private string[] version_list;
    private static string[] versionSpecs;
    private int compare_version = 0;
    private bool editingEnabled = true;

    private void Initialize()
    {
        versionSpecs = Directory.GetDirectories("Assets/VReqDV/specifications");
        screenState.total_versions = versionSpecs.Length;
        version_list = new string[screenState.total_versions + 1];

        for (int i = 1; i <= screenState.total_versions; i++)
        {
            version_list[i] = i.ToString();
        }
    }

    private GUIStyle setFont(int x)
    {
        GUIStyle customLabel = new GUIStyle(EditorStyles.label);
        customLabel.fontSize = x;
        return customLabel;
    }

    private void OnEnable()
    {
        screenState = new onScreenState();
        window = this;

        Initialize();

        // Restore version selection that may have been lost during domain reload
        int savedVersion = EditorPrefs.GetInt("VReqDV_CurrentVersion", 1);
        if (savedVersion >= 1 && savedVersion <= screenState.total_versions)
            screenState.curr_version = savedVersion;

        int savedCompare = EditorPrefs.GetInt("VReqDV_CompareVersion", 0);
        if (savedCompare >= 0 && savedCompare <= screenState.total_versions)
            compare_version = savedCompare;
    }

    private void OnGUI()
    {
        Initialize();
        if (screenState.total_versions == 0)
        {
            GUILayout.Label("To start using VReqDV to track your project versions, upload the project specifications in a new version, or save the contents of the current scene to a new version.");
            if (GUILayout.Button("Save Version"))
            {
                screenState.total_versions++;
                screenState.curr_version = screenState.total_versions;
                SaveVersion(-1, screenState.curr_version);
                SaveSceneToPrefab(screenState.curr_version);
                window.Repaint();
            }
        }

        else
        {
            // Show the current version specifications
            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Version: " + screenState.curr_version, setFont(14));
            GUILayout.Label("Total Versions: " + screenState.total_versions, setFont(14), GUILayout.Width(400));

            if(GUILayout.Button("Save New Version", GUILayout.Width(200)))
            {
                screenState.total_versions++;
                SaveVersion(screenState.curr_version, screenState.total_versions);
                screenState.curr_version = screenState.total_versions;
                SaveSceneToPrefab(screenState.curr_version);
                window.Repaint();
            }
            if(GUILayout.Button("Save to Current Version", GUILayout.Width(200)))
            {
                SaveVersion(screenState.curr_version, screenState.curr_version);
                SaveSceneToPrefab(screenState.curr_version);
                window.Repaint();
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            // GUILayout.BeginHorizontal();
            // GUILayout.Label("Editing Enabled: " + editingEnabled, setFont(14));
            // GUILayout.Label("NOTE: If editing is enabled, compare versions will not work!", setFont(14), GUILayout.Width(750));
            // if(GUILayout.Button("Enable/Disable Form Editing", GUILayout.Width(200)))
            // {
            //     editingEnabled = !editingEnabled;
            // }
            // GUILayout.EndHorizontal();

            // EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Change Current Version:", setFont(12));
            int newVersion = EditorGUILayout.Popup(screenState.curr_version, version_list, GUILayout.Width(100));
            if (newVersion != screenState.curr_version)
            {
                screenState.curr_version = newVersion;
                EditorPrefs.SetInt("VReqDV_CurrentVersion", screenState.curr_version);
            }
            if(GUILayout.Button("Display Mock-up", GUILayout.Width(200)))
            {
                // Save version to EditorPrefs before triggering reload
                EditorPrefs.SetInt("VReqDV_CurrentVersion", screenState.curr_version);
                ClearObjects();
                OpenScene(screenState.curr_version);
                string dir_path = $"Assets/VReqDV/ScenePrefabs/version_{screenState.curr_version}";
                if(!Directory.Exists(dir_path))
                    SaveSceneToPrefab(screenState.curr_version);
            }

            GUILayout.Label("Compare with Version:", setFont(12));
            
            int newCompare = EditorGUILayout.Popup(compare_version, version_list, GUILayout.Width(100));
            if (newCompare != compare_version)
            {
                compare_version = newCompare;
                EditorPrefs.SetInt("VReqDV_CompareVersion", compare_version);
            }

            // EditorGUI.BeginDisabledGroup(editingEnabled);
            // if(GUILayout.Button("Display Comparison", GUILayout.Width(200)))
            // {
            //     CompareHandler.ComparisonSideBySide(screenState.curr_version, compare_version);
            // }
            // EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            try
            {
                string file = "Assets/VReqDV/specifications/version_" + screenState.curr_version + "/article.json";
                string objectData = File.ReadAllText(file);
                objectSpecifications = JsonConvert.DeserializeObject<ArticleList>(objectData);
            }
            catch (FileNotFoundException)
            {
                objectSpecifications = new ArticleList { articles = new List<Article> { new Article { _objectname = "Error", _slabel = "File not found" } } };
            }
            catch (JsonException)
            {
                objectSpecifications = new ArticleList { articles = new List<Article> { new Article { _objectname = "Error", _slabel = "Failed to parse JSON" } } };
            }

            try
            {
                string file = "Assets/VReqDV/specifications/version_" + screenState.curr_version + "/behavior.json";
                string behaviorData = File.ReadAllText(file);
                behaviorSpecifications = JsonConvert.DeserializeObject<BehaviorList>(behaviorData);
            }
            catch (FileNotFoundException)
            {
                behaviorSpecifications = new BehaviorList { behaviors = new List<BehaviorRule> { new BehaviorRule { Id = "Error - File not found" } } };
            }
            catch (JsonException)
            {
                behaviorSpecifications = new BehaviorList { behaviors = new List<BehaviorRule> { new BehaviorRule { Id = "Error - Failed to parse JSON" } } };
            }

            if (compare_version != 0)
            {
                try
                {
                    string file = "Assets/VReqDV/specifications/version_" + compare_version + "/article.json";
                    string objectData = File.ReadAllText(file);
                    compareObjectSpecifications = JsonConvert.DeserializeObject<ArticleList>(objectData);
                }
                catch (FileNotFoundException)
                {
                    compareObjectSpecifications = new ArticleList { articles = new List<Article> { new Article { _objectname = "Error", _slabel = "File not found" } } };
                }
                catch (JsonException)
                {
                    compareObjectSpecifications = new ArticleList { articles = new List<Article> { new Article { _objectname = "Error", _slabel = "Failed to parse JSON" } } };
                }

                try
                {
                    string file = "Assets/VReqDV/specifications/version_" + compare_version + "/behavior.json";
                    string behaviorData = File.ReadAllText(file);
                    compareBehaviorSpecifications = JsonConvert.DeserializeObject<BehaviorList>(behaviorData);
                }
                catch (FileNotFoundException)
                {
                    compareBehaviorSpecifications = new BehaviorList { behaviors = new List<BehaviorRule> { new BehaviorRule { Id = "Error - File not found" } } };
                }
                catch (JsonException)
                {
                    compareBehaviorSpecifications = new BehaviorList { behaviors = new List<BehaviorRule> { new BehaviorRule { Id = "Error - Failed to parse JSON" } } };
                }
            }

            string[] list = new string[] { "Articles", "Behaviors" };
            selected_display_component = EditorGUILayout.Popup("Select Component", selected_display_component, list);

            GUILayout.BeginHorizontal();

            bool formContentChanged = false;

            // Scrollable area for object data
            scrollPositionObject = EditorGUILayout.BeginScrollView(scrollPositionObject, GUILayout.Height(position.height - 80), GUILayout.Width(position.width / 2));

            EditorGUI.BeginChangeCheck();
            if (objectSpecifications != null && objectSpecifications.articles != null)
            {
                if (list[selected_display_component] == "Articles")
                {
                    DisplayArticleForm(objectSpecifications.articles, screenState.curr_version);
                }
            }
            if (behaviorSpecifications != null && behaviorSpecifications.behaviors != null)
            {
                if (list[selected_display_component] == "Behaviors")
                {
                    DisplayBehaviorForm(behaviorSpecifications.behaviors, screenState.curr_version);
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                formContentChanged = true;
            }
            EditorGUILayout.EndScrollView();

            // compare with
            if (compare_version != 0)
            {
                scrollPositionObjectCompare = EditorGUILayout.BeginScrollView(scrollPositionObjectCompare, GUILayout.Height(position.height - 80), GUILayout.Width(position.width / 2));

                if (compareObjectSpecifications != null && compareObjectSpecifications.articles != null)
                {
                    if (list[selected_display_component] == "Articles")
                        DisplayArticleForm(compareObjectSpecifications.articles, compare_version);
                }
                if (compareBehaviorSpecifications != null && compareBehaviorSpecifications.behaviors != null)
                {
                    if (list[selected_display_component] == "Behaviors")
                        DisplayBehaviorForm(compareBehaviorSpecifications.behaviors, compare_version);
                }
                EditorGUILayout.EndScrollView();
            }
            GUILayout.EndHorizontal();
            
            if (editingEnabled && formContentChanged)
            {
                string json1 = JsonConvert.SerializeObject(objectSpecifications, Formatting.Indented);
                string filePath1 = $"Assets/VReqDV/specifications/version_{screenState.curr_version}/article.json";
                File.WriteAllText(filePath1, json1);
                AssetDatabase.Refresh();
                ClearObjects();
                OpenScene(screenState.curr_version);
                SaveSceneToPrefab(screenState.curr_version);
            }
        }
    }

    private void OpenScene(int version_no)
    {
        string folder_path = "Assets/VReqDV/specifications/version_" + version_no;
        if (Directory.Exists(folder_path))
        {
            var inheritanceMap = ObjectHandler.CreateObjects(folder_path);
            
            // Flag for BehaviorAttacher to pick up after compilation
            EditorPrefs.SetString("VReqDV_PendingVersion", version_no.ToString());
            
            BehaviorCodeGenerator.CreateBehaviors(folder_path, inheritanceMap);
        }
        else
        {
            Debug.Log("Version Directory not found");
        }
    }

    private void SaveSceneToPrefab(int version)
    {
        string prefabDirectory = $"Assets/VReqDV/ScenePrefabs/version_{version}";
        if (!Directory.Exists(prefabDirectory))
        {
            Directory.CreateDirectory(prefabDirectory);
        }

        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject rootObject in rootObjects)
        {
            string prefabPath = Path.Combine(prefabDirectory, rootObject.name + ".prefab");
            PrefabUtility.SaveAsPrefabAsset(rootObject, prefabPath);
        }

        Debug.Log($"Scene saved as a prefab in version {version}.");
    }

    private void ClearObjects()
    {
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Debug.Log($"ClearObjects(): Active Scene: {activeScene.name}");
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>(true); // includeInactive=true

        foreach (GameObject obj in allObjects)
        {
            if (obj.scene != activeScene || obj.transform.parent != null)
                continue;

            // Skip if this object has XRInteractionManager or XROrigin component
            if (obj.name == "XR Interaction Manager" || obj.name == "XR Origin (XR Rig)" || obj.name == "EventSystem")
                continue;

            // Skip if this object has Main Camera or Directional Light component
            if (obj.name == "Main Camera" || obj.name == "Directional Light")
                continue;   

            // deleting root objects automatically removes children too
            if (obj.scene == activeScene && obj.transform.parent == null)
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
        // GameObject mainCamera = new GameObject("Main Camera");
        // mainCamera.AddComponent<Camera>();
        // mainCamera.tag = "MainCamera";
        // mainCamera.transform.position = new Vector3(0, 1, -10);

        // GameObject directionalLight = new GameObject("Directional Light");
        // Light lightComp = directionalLight.AddComponent<Light>();
        // lightComp.type = LightType.Directional;
        // directionalLight.transform.position = new Vector3(0, 3, 0);
        // directionalLight.transform.rotation = Quaternion.Euler(50, -30, 0);
    }

    
    private void DisplayArticleForm(List<Article> articles, int ver_no)
    {
        GUILayout.Label("Articles Specifications - Version " + ver_no, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        foreach (var article in articles)
        {
            if (article._objectname == "Main Camera" || article._objectname == "Directional Light")
                continue;

            EditorGUILayout.LabelField("Object Name:", article._objectname, EditorStyles.boldLabel);
            article._objectname = EditorGUILayout.TextField("Object Name:", article._objectname);
            article._sid = EditorGUILayout.TextField("SID:", article._sid);
            article._slabel = EditorGUILayout.TextField("Label:", article._slabel);
            article._IsHidden = EditorGUILayout.IntField("Is Hidden:", article._IsHidden);
            article._enumcount = EditorGUILayout.IntField("Enum Count:", article._enumcount);
            article._Is3DObject = EditorGUILayout.IntField("Is 3D Object:", article._Is3DObject);
            article.HasChild = EditorGUILayout.IntField("Has Child:", article.HasChild);
            if(article.context_img_source != null)
                article.context_img_source = EditorGUILayout.TextField("Asset Path:", article.context_img_source);
            else
                article.shape = EditorGUILayout.TextField("Shape:", article.shape);

            // Lighting
            if (article.lighting != null)
            {
                EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
                article.lighting.CastShadow = EditorGUILayout.TextField("Cast Shadow:", article.lighting.CastShadow);
                article.lighting.ReceiveShadow = EditorGUILayout.TextField("Receive Shadow:", article.lighting.ReceiveShadow);
                article.lighting.ContributeGlobalIlumination = EditorGUILayout.TextField("Contribute Global Illumination:", article.lighting.ContributeGlobalIlumination);
            }

            if (article.Transform_initialpos != null)
            {
                EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);
                article.Transform_initialpos.x = EditorGUILayout.TextField("x position: ", article.Transform_initialpos.x);
                article.Transform_initialpos.y = EditorGUILayout.TextField("y position: ", article.Transform_initialpos.y);
                article.Transform_initialpos.z = EditorGUILayout.TextField("z position: ", article.Transform_initialpos.z);
            }
            if (article.Transform_objectscale != null)
            {
                EditorGUILayout.LabelField("Object Scale", EditorStyles.boldLabel);
                article.Transform_objectscale.x = EditorGUILayout.TextField("x scale: ", article.Transform_objectscale.x);
                article.Transform_objectscale.y = EditorGUILayout.TextField("y scale: ", article.Transform_objectscale.y);
                article.Transform_objectscale.z = EditorGUILayout.TextField("z scale: ", article.Transform_objectscale.z);
            }
            if (article.Transform_initialrotation != null)
            {
                EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
                article.Transform_initialrotation.x = EditorGUILayout.TextField("x rotation: ", article.Transform_initialrotation.x);
                article.Transform_initialrotation.y = EditorGUILayout.TextField("y rotation: ", article.Transform_initialrotation.y);
                article.Transform_initialrotation.z = EditorGUILayout.TextField("z rotation: ", article.Transform_initialrotation.z);
            }
            if (article.XRRigidObject != null)
            {
                EditorGUILayout.LabelField("XR Rigid Object", EditorStyles.boldLabel);
                article.XRRigidObject.value = EditorGUILayout.TextField("Is XR Rigid Object: ", article.XRRigidObject.value);
                article.XRRigidObject.mass = EditorGUILayout.TextField("Mass ", article.XRRigidObject.mass);
                article.XRRigidObject.dragfriction = EditorGUILayout.TextField("Drag ", article.XRRigidObject.dragfriction);
                article.XRRigidObject.angulardrag = EditorGUILayout.TextField("Angular Drag ", article.XRRigidObject.angulardrag);
                article.XRRigidObject.Isgravityenable = EditorGUILayout.TextField("Use Gravity ", article.XRRigidObject.Isgravityenable);
                article.XRRigidObject.IsKinematic = EditorGUILayout.TextField("Is Kinematic ", article.XRRigidObject.IsKinematic);
                article.XRRigidObject.CanInterpolate = EditorGUILayout.TextField("Interpolate ", article.XRRigidObject.CanInterpolate);
                article.XRRigidObject.CollisionPolling = EditorGUILayout.TextField("Collision Detection ", article.XRRigidObject.CollisionPolling);
            }

            if (article.Interaction != null)
            {
                EditorGUILayout.LabelField("Interaction", EditorStyles.boldLabel);
                article.Interaction.XRGrabInteractable = EditorGUILayout.TextField("XR Grab Interactable: ", article.Interaction.XRGrabInteractable);
                
                // Display interaction mask layers as a comma-separated editable field
                string layersStr = article.Interaction.XRInteractionMaskLayer != null ? string.Join(", ", article.Interaction.XRInteractionMaskLayer) : "";
                string newLayersStr = EditorGUILayout.TextField("Interaction Mask Layers: ", layersStr);
                if (newLayersStr != layersStr)
                {
                    article.Interaction.XRInteractionMaskLayer = new List<string>();
                    foreach (string layer in newLayersStr.Split(','))
                    {
                        string trimmed = layer.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                            article.Interaction.XRInteractionMaskLayer.Add(trimmed);
                    }
                }

                article.Interaction.TrackPosition = EditorGUILayout.TextField("Track Position: ", article.Interaction.TrackPosition);
                article.Interaction.TrackRotation = EditorGUILayout.TextField("Track Rotation: ", article.Interaction.TrackRotation);
                article.Interaction.Throw_Detach = EditorGUILayout.TextField("Throw On Detach: ", article.Interaction.Throw_Detach);
            }


            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
    }

    private void DisplayBehaviorForm(List<BehaviorRule> behaviors, int version)
    {
        GUILayout.Label("Behavior Specifications - Version " + version, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        foreach (var rule in behaviors)
        {
            EditorGUILayout.LabelField("ID: ", rule.Id, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Event: ", rule.Event);
            EditorGUILayout.LabelField("Source: ", rule.Source);

            if (rule.Precondition != null)
            {
                EditorGUILayout.LabelField("Precondition:", EditorStyles.boldLabel);
                DisplayConditionNode(rule.Precondition);
            }

            if (rule.Action != null)
            {
                EditorGUILayout.LabelField("Action:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Algorithm: ", rule.Action.runAlgorithm);
                if (rule.Action.@params != null)
                {
                    foreach (var param in rule.Action.@params)
                    {
                        EditorGUILayout.LabelField($"  {param.Key}: ", param.Value);
                    }
                }
            }

            if (rule.Postcondition != null)
            {
                EditorGUILayout.LabelField("Postcondition:", EditorStyles.boldLabel);
                DisplayConditionNode(rule.Postcondition);
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }
    }

    private void DisplayConditionNode(ConditionNode node, int indentLevel = 0)
    {
        string indent = new string(' ', indentLevel * 4);
        
        if (node.all != null && node.all.Count > 0)
        {
            EditorGUILayout.LabelField($"{indent}ALL:");
            foreach (var child in node.all) DisplayConditionNode(child, indentLevel + 1);
        }
        
        if (node.any != null && node.any.Count > 0)
        {
            EditorGUILayout.LabelField($"{indent}ANY:");
            foreach (var child in node.any) DisplayConditionNode(child, indentLevel + 1);
        }

        if (node.equals != null && node.equals.Count >= 2)
        {
             EditorGUILayout.LabelField($"{indent}{node.equals[0]} == {node.equals[1]}");
        }

        if (!string.IsNullOrEmpty(node.runAlgorithm))
        {
             EditorGUILayout.LabelField($"{indent}Run: {node.runAlgorithm}");
             if (node.@params != null)
             {
                 foreach (var param in node.@params)
                 {
                     EditorGUILayout.LabelField($"{indent}  {param.Key}: {param.Value}");
                 }
             }
        }
    }

    public void SaveVersion(int prev_version_no, int version_no)
    {
        // Reload objectSpecifications from disk to ensure we have the latest data
        // (OnGUI button handlers run before objectSpecifications is loaded each frame)
        Debug.Log($"[SaveVersion] Called with prev_version_no={prev_version_no}, version_no={version_no}");
        if (prev_version_no > 0)
        {
            try
            {
                string file = "Assets/VReqDV/specifications/version_" + prev_version_no + "/article.json";
                string data = File.ReadAllText(file);
                Debug.Log($"[SaveVersion] Read {data.Length} chars from {file}");
                
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) =>
                    {
                        Debug.LogError($"[SaveVersion] JSON Error: {args.ErrorContext.Error.Message} at path: {args.ErrorContext.Path}");
                        args.ErrorContext.Handled = true;
                    }
                };
                objectSpecifications = JsonConvert.DeserializeObject<ArticleList>(data, settings);
                Debug.Log($"[SaveVersion] After reload: {objectSpecifications?.articles?.Count ?? 0} articles");
                if (objectSpecifications?.articles != null)
                {
                    foreach (var a in objectSpecifications.articles)
                        Debug.Log($"[SaveVersion]   - '{a._objectname}'");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveVersion] Could not reload article.json for version {prev_version_no}: {e.Message}");
            }
        }
        else
        {
            Debug.Log($"[SaveVersion] Skipped reload (prev_version_no={prev_version_no})");
        }

        // Lookup for existing metadata
        Dictionary<string, Article> existingArticles = new Dictionary<string, Article>();
        if (objectSpecifications != null && objectSpecifications.articles != null)
        {
            foreach (var art in objectSpecifications.articles)
            {
                if (!string.IsNullOrEmpty(art._objectname) && !existingArticles.ContainsKey(art._objectname))
                    existingArticles[art._objectname] = art;
            }
        }
        Debug.Log($"[SaveVersion] objectSpecifications has {objectSpecifications?.articles?.Count ?? 0} articles. existingArticles keys: [{string.Join(", ", existingArticles.Keys)}]");

        List<Article> newArticles = new List<Article>();
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "Main Camera" || obj.name == "Directional Light" || obj.name == "XR Interaction Manager" || obj.name == "XR Origin (XR Rig)" || obj.name == "EventSystem")
                continue;

            // Skip children of protected objects (e.g. XR Origin's Camera Offset, controllers, etc.)
            bool isChildOfProtected = false;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (parent.name == "XR Interaction Manager" || parent.name == "XR Origin (XR Rig)" || parent.name == "EventSystem" || parent.name == "Camera Offset" || parent.name == "LeftHand Controller" || parent.name == "RightHand Controller")
                {
                    isChildOfProtected = true;
                    break;
                }
                parent = parent.parent;
            }
            if (isChildOfProtected) continue;

            Debug.Log($"[SaveVersion] Processing: '{obj.name}', found in existing: {existingArticles.ContainsKey(obj.name)}");

            // Helper to get scene data (returns Dictionary<string,object> with numeric values)
            var posData = HF.GetTransformInitialPosition(obj);
            var rotData = HF.GetTransformInitialRotation(obj);
            var scaleData = HF.GetTransformObjectScale(obj);
            var rigidData = HF.GetXRRigidObject(obj);

            Article art = new Article();
            art._objectname = obj.name;

            // 1. Populate Metadata from existing (or defaults)
            if (existingArticles.ContainsKey(obj.name))
            {
                Article existing = existingArticles[obj.name];
                art._sid = existing._sid;
                art._slabel = existing._slabel;
                art.source = existing.source;
                art._IsHidden = existing._IsHidden;
                art._enumcount = existing._enumcount;
                art._Is3DObject = existing._Is3DObject;
                art.HasChild = existing.HasChild;
                art.Children = existing.Children;
                art.states = existing.states;
                art.Interaction = existing.Interaction;
                art.context_img_source = existing.context_img_source;
                
            }
            else
            {
                // Defaults for new objects
                art.HasChild = 0;
                art._IsHidden = 0;
                art._enumcount = 0;
                art._Is3DObject = 1; // Assume 3D?
            }

            // 2. Populate Scene Data (Transforms, Physics, Shape)
            
            // Shape — check if object is a prefab instance first
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
            if (!string.IsNullOrEmpty(prefabPath))
            {
                art.context_img_source = prefabPath;
                art.shape = null;
            }
            else if (obj.GetComponent<MeshFilter>() && obj.GetComponent<MeshFilter>().sharedMesh)
                art.shape = obj.GetComponent<MeshFilter>().sharedMesh.name;
            else
                art.shape = "empty";

            // Transforms - Convert numbers to strings
            art.Transform_initialpos = new TransformData 
            { 
                x = posData["x"].ToString(), 
                y = posData["y"].ToString(), 
                z = posData["z"].ToString() 
            };
            art.Transform_initialrotation = new TransformData 
            { 
                x = rotData["x"].ToString(), 
                y = rotData["y"].ToString(), 
                z = rotData["z"].ToString() 
            };
            art.Transform_objectscale = new TransformData 
            { 
                x = scaleData["x"].ToString(), 
                y = scaleData["y"].ToString(), 
                z = scaleData["z"].ToString() 
            };

            // XR Rigid Object
            art.XRRigidObject = new XRRigidObject
            {
                value = rigidData["value"].ToString(),
                mass = rigidData["mass"].ToString(),
                dragfriction = rigidData["dragfriction"].ToString(),
                angulardrag = rigidData["angulardrag"].ToString(),
                Isgravityenable = rigidData["Isgravityenable"].ToString().ToLower(), // JSON often uses lowercase true/false
                IsKinematic = rigidData["IsKinematic"].ToString().ToLower(),
                CanInterpolate = rigidData["CanInterpolate"].ToString(),
                CollisionPolling = rigidData["CollisionPolling"].ToString()
            };

            // Interaction
            art.Interaction = HF.GetInteraction(obj);

            newArticles.Add(art);
        }

        // Construct final ArticleList
        ArticleList finalList = new ArticleList { articles = newArticles };
        string json_new_format = JsonConvert.SerializeObject(finalList, Formatting.Indented);

        // Save
        string directory_path = "Assets/VReqDV/specifications/version_" + version_no;
        if (!Directory.Exists(directory_path))
            Directory.CreateDirectory(directory_path);

        File.WriteAllText(directory_path + "/article.json", json_new_format);
        

        Debug.Log("Saved version " + version_no);

        // Copy behavior.json and UserAlgorithms.cs from previous version if they exist
        if (prev_version_no > 0)
        {
            string prev_directory_path = "Assets/VReqDV/specifications/version_" + prev_version_no;
            
            // 1. Copy behavior.json
            string prev_behavior_path = prev_directory_path + "/behavior.json";
            string new_behavior_path = directory_path + "/behavior.json";
            if (File.Exists(prev_behavior_path) && !File.Exists(new_behavior_path))
            {
                File.Copy(prev_behavior_path, new_behavior_path);
                Debug.Log($"Saved behavior.json from Version {prev_version_no} to Version {version_no}");
            }

            // 2. Copy and Update UserAlgorithms.cs
            string prev_algo_path = prev_directory_path + "/UserAlgorithms.cs";
            string new_algo_path = directory_path + "/UserAlgorithms.cs";
            if (File.Exists(prev_algo_path) && !File.Exists(new_algo_path))
            {
                string content = File.ReadAllText(prev_algo_path);
                // Replace namespace
                content = content.Replace($"namespace Version_{prev_version_no}", $"namespace Version_{version_no}");
                File.WriteAllText(new_algo_path, content);
                Debug.Log($"Saved and updated UserAlgorithms.cs from Version {prev_version_no} to Version {version_no}");
            }
        }
        
        // Refresh internal state if saving to current ver
        if(version_no == screenState.curr_version)
        {
             objectSpecifications = finalList;
        }
    }



}