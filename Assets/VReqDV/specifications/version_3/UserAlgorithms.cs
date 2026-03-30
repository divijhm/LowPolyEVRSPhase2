using UnityEngine;
using VReqDV;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.EventSystems;

namespace Version_3
{
    /// <summary>
    /// VR Injection Administration Guide - Version 2 Extended
    /// 8-card interactive training with angle alignment, insertion tracking, and plunger feedback
    /// </summary>
    public static class UserAlgorithms
    {
        // ─── UI System State ──────────────────────────────────────────────────────
        private static Canvas mainCanvas = null;
        private static GameObject uiManagerContext = null;
        private static CanvasGroup[] cardPanels = new CanvasGroup[8];
        private static int currentCardIndex = 0;
        private static Button nextButton = null;
        private static Button backButton = null;
        private static Text controllerAngleText = null;
        private static Text angleStatusText = null;
        private static Text insertionFeedbackText = null;
        private static Text plungerFeedbackText = null;
        private static bool uiInitialized = false;

        // ─── Angle Configuration ──────────────────────────────────────────────────
        private const float ANGLE_TOLERANCE = 25f;
        private const float TARGET_ANGLE_X = 0f;
        private const float TARGET_ANGLE_Y = 0f;
        private const float TARGET_ANGLE_Z = 0f;

        // ─── Insertion Tracking ───────────────────────────────────────────────────
        private static Vector3 gripStartPosition = Vector3.zero;
        private static bool isGripPressed = false;
        private static bool wasGripPressed = false;
        private const float INSERTION_PERFECT_MIN = 0.08f;
        private const float INSERTION_PERFECT_MAX = 0.15f;
        private const float INSERTION_MAX = 0.25f;

        // ─── Plunger Tracking ─────────────────────────────────────────────────────
        private static Vector3 plungerStartPosition = Vector3.zero;
        private static bool wasIndexPressed = false;
        private static bool wasRightIndexPressed = false;
        private static float plungerStartTime = 0f;
        private const float PLUNGER_PERFECT_TIME = 3f;
        private const float PLUNGER_PERFECT_MIN = 0.03f;
        private const float PLUNGER_PERFECT_MAX = 0.08f;

        // ─── Card Data ────────────────────────────────────────────────────────────
        private static readonly string[] SyringeTypes = { "Big", "Small", "Wide" };
        private static readonly string[] AdminTypes = { "ID", "SC", "IV", "IM" };
        private static readonly string[] CardTitles = {
            "Select Syringe Type",
            "Select Administration Type",
            "Fill the Syringe",
            "Check for Air Bubbles",
            "Controller Angle Display",
            "Angle Alignment",
            "Needle Insertion",
            "Plunger Press"
        };
        private static readonly string[] CardDescriptions = {
            "Choose the type of syringe:",
            "Select the injection type:",
            "Steps: Draw medication, Measure dosage, Keep sterile",
            "Check for bubbles: Hold upright, Tap to dislodge, Expel air",
            "Position left controller to see real-time angles.",
            "Align angles to green, then press GRIP to insert.",
            "Insert needle into skin. Press and hold GRIP.",
            "Press INDEX FINGER to push plunger. Control speed."
        };

        private static string selectedSyringe = "";
        private static string selectedAdminType = "";

        // ═════════════════════════════════════════════════════════════════════════

        public static void SetupUISystem(GameObject obj)
        {
            if (uiInitialized) return;
            uiManagerContext = obj;

            GameObject staleCanvas = GameObject.Find("InjectionGuideCanvas");
            if (staleCanvas != null)
            {
                Object.Destroy(staleCanvas);
            }

            EnsureXRUIEventSystem();
            EnsureControllerRayInteractors();

            // Create WorldSpace Canvas for XR interaction
            GameObject canvasGO = new GameObject("InjectionGuideCanvas");
            canvasGO.transform.SetParent(null, true);
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1920, 1080);

            Camera xrCam = Camera.main;
            if (xrCam != null)
            {
                Vector3 topRightPos = xrCam.transform.position + (xrCam.transform.forward * 1.25f) + (xrCam.transform.right * 0.65f) + (xrCam.transform.up * 0.14f);
                canvasGO.transform.position = topRightPos;
                canvasGO.transform.rotation = Quaternion.LookRotation(xrCam.transform.position - topRightPos, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
            }
            else
            {
                canvasGO.transform.position = new Vector3(1.3f, 1.5f, 1.5f);
                canvasGO.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            }

            canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();

            // Container panel
            GameObject containerGO = new GameObject("UIContainer");
            containerGO.transform.SetParent(canvasGO.transform, false);
            RectTransform containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.offsetMin = new Vector2(20, -420);
            containerRect.offsetMax = new Vector2(420, -20);

            Image containerImage = containerGO.AddComponent<Image>();
            containerImage.color = new Color(0, 0, 0, 0.9f);

            // Create 8 card panels
            for (int i = 0; i < 8; i++)
            {
                GameObject cardGO = new GameObject($"Card{i + 1}");
                cardGO.transform.SetParent(containerGO.transform, false);
                RectTransform cardRect = cardGO.AddComponent<RectTransform>();
                cardRect.anchorMin = Vector2.zero;
                cardRect.anchorMax = Vector2.one;
                cardRect.offsetMin = Vector2.zero;
                cardRect.offsetMax = Vector2.zero;

                CanvasGroup cardGroup = cardGO.AddComponent<CanvasGroup>();
                cardGroup.alpha = (i == 0) ? 1f : 0f;
                cardGroup.interactable = (i == 0);
                cardGroup.blocksRaycasts = (i == 0);
                cardPanels[i] = cardGroup;

                CreateCardContent(cardGO, i);
            }

            // Navigation buttons
            GameObject buttonsContainer = new GameObject("ButtonsContainer");
            buttonsContainer.transform.SetParent(containerGO.transform, false);
            RectTransform buttonsRect = buttonsContainer.AddComponent<RectTransform>();
            buttonsRect.anchorMin = new Vector2(0, 0);
            buttonsRect.anchorMax = new Vector2(1, 0);
            buttonsRect.offsetMin = new Vector2(0, 0);
            buttonsRect.offsetMax = new Vector2(0, 50);

            HorizontalLayoutGroup buttonsLayout = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 10;
            buttonsLayout.childForceExpandWidth = true;
            buttonsLayout.childForceExpandHeight = true;
            buttonsLayout.padding = new RectOffset(10, 10, 5, 5);

            backButton = CreateXRButton(buttonsContainer, "Back");
            nextButton = CreateXRButton(buttonsContainer, "Next");

            backButton.onClick.AddListener(() => HandleBackNavigation(obj));
            nextButton.onClick.AddListener(() => HandleNextNavigation(obj));

            RefreshButtons();
            uiInitialized = true;
            StateAccessor.SetState("UIManager", "card1_syringeSelection", GetUIManagerContext(obj), "Version_3");
            Debug.Log("[VR Injection] UI initialized");
        }

        // ─── Card Creation ────────────────────────────────────────────────────────

        private static void CreateCardContent(GameObject cardGO, int cardIndex)
        {
            // Title
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(cardGO.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(10, -40);
            titleRect.offsetMax = new Vector2(-10, 0);

            Text titleText = titleGO.AddComponent<Text>();
            titleText.text = CardTitles[cardIndex];
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.4f, 0.78f, 1f);
            titleText.alignment = TextAnchor.UpperLeft;
            ConfigureTextFit(titleText, 16, 10, 18);

            // Content area
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(cardGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(10, 50);
            contentRect.offsetMax = new Vector2(-10, -60);

            // Card-specific content
            switch (cardIndex)
            {
                case 0: CreateSyringeSelectionCard(contentGO); break;
                case 1: CreateAdminTypeCard(contentGO); break;
                case 4: CreateControllerAnglesCard(contentGO); break;
                case 5: CreateAngleAlignmentCard(contentGO); break;
                case 6: CreateInsertionCard(contentGO); break;
                case 7: CreatePlungerCard(contentGO); break;
                default:
                    GameObject descGO = new GameObject("Description");
                    descGO.transform.SetParent(contentGO.transform, false);
                    RectTransform descRect = descGO.AddComponent<RectTransform>();
                    descRect.anchorMin = Vector2.zero;
                    descRect.anchorMax = Vector2.one;
                    Text descText = descGO.AddComponent<Text>();
                    descText.text = CardDescriptions[cardIndex];
                    descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    descText.fontSize = 12;
                    descText.color = new Color(0.8f, 0.8f, 0.8f);
                    descText.alignment = TextAnchor.UpperLeft;
                    descText.horizontalOverflow = HorizontalWrapMode.Wrap;
                    descText.verticalOverflow = VerticalWrapMode.Truncate;
                    ConfigureTextFit(descText, 12, 9, 14);
                    break;
            }
        }

        private static void CreateSyringeSelectionCard(GameObject contentGO)
        {
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(contentGO.transform, false);
            Text labelText = labelGO.AddComponent<Text>();
            labelText.text = "Select Syringe:";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.color = Color.white;
            ConfigureTextFit(labelText, 12, 9, 14);

            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(0f, -28f);
            labelRect.offsetMax = new Vector2(0f, 0f);

            GameObject optionsContainer = new GameObject("SyringeOptions");
            optionsContainer.transform.SetParent(contentGO.transform, false);
            RectTransform optionsRect = optionsContainer.AddComponent<RectTransform>();
            optionsRect.anchorMin = new Vector2(0f, 1f);
            optionsRect.anchorMax = new Vector2(1f, 1f);
            optionsRect.offsetMin = new Vector2(0f, -170f);
            optionsRect.offsetMax = new Vector2(0f, -40f);

            VerticalLayoutGroup layout = optionsContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            for (int i = 0; i < SyringeTypes.Length; i++)
            {
                string option = SyringeTypes[i];
                Button optionButton = CreateXRButton(optionsContainer, option);
                optionButton.onClick.AddListener(() => selectedSyringe = option);
            }

            selectedSyringe = SyringeTypes[0];
        }

        private static void CreateControllerAnglesCard(GameObject contentGO)
        {
            GameObject angleGO = new GameObject("ControllerAngles");
            angleGO.transform.SetParent(contentGO.transform, false);
            RectTransform angleRect = angleGO.AddComponent<RectTransform>();
            angleRect.anchorMin = Vector2.zero;
            angleRect.anchorMax = Vector2.one;
            angleRect.offsetMin = Vector2.zero;
            angleRect.offsetMax = Vector2.zero;

            controllerAngleText = angleGO.AddComponent<Text>();
            controllerAngleText.text = "X: 0.0°\nY: 0.0°\nZ: 0.0°";
            controllerAngleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            controllerAngleText.fontSize = 12;
            controllerAngleText.color = Color.white;
            controllerAngleText.alignment = TextAnchor.UpperLeft;
            controllerAngleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            controllerAngleText.verticalOverflow = VerticalWrapMode.Truncate;
            controllerAngleText.supportRichText = true;
            ConfigureTextFit(controllerAngleText, 12, 9, 14);
        }

        private static void CreateAdminTypeCard(GameObject contentGO)
        {
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(contentGO.transform, false);
            Text labelText = labelGO.AddComponent<Text>();
            labelText.text = "Select Administration Type:";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.color = Color.white;
            ConfigureTextFit(labelText, 12, 9, 14);

            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(0f, -28f);
            labelRect.offsetMax = new Vector2(0f, 0f);

            GameObject optionsContainer = new GameObject("AdminOptions");
            optionsContainer.transform.SetParent(contentGO.transform, false);
            RectTransform optionsRect = optionsContainer.AddComponent<RectTransform>();
            optionsRect.anchorMin = new Vector2(0f, 1f);
            optionsRect.anchorMax = new Vector2(1f, 1f);
            optionsRect.offsetMin = new Vector2(0f, -220f);
            optionsRect.offsetMax = new Vector2(0f, -40f);

            VerticalLayoutGroup layout = optionsContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            for (int i = 0; i < AdminTypes.Length; i++)
            {
                string option = AdminTypes[i];
                Button optionButton = CreateXRButton(optionsContainer, option);
                optionButton.onClick.AddListener(() => selectedAdminType = option);
            }

            selectedAdminType = AdminTypes[0];
        }

        private static void CreateAngleAlignmentCard(GameObject contentGO)
        {
            GameObject angleGO = new GameObject("AngleDisplay");
            angleGO.transform.SetParent(contentGO.transform, false);
            RectTransform angleRect = angleGO.AddComponent<RectTransform>();
            angleRect.anchorMin = Vector2.zero;
            angleRect.anchorMax = Vector2.one;

            angleStatusText = angleGO.AddComponent<Text>();
            angleStatusText.text = "Align angles...";
            angleStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            angleStatusText.fontSize = 11;
            angleStatusText.color = Color.white;
            angleStatusText.alignment = TextAnchor.UpperLeft;
            angleStatusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            angleStatusText.verticalOverflow = VerticalWrapMode.Truncate;
            angleStatusText.supportRichText = true;
            ConfigureTextFit(angleStatusText, 11, 8, 13);
        }

        private static void CreateInsertionCard(GameObject contentGO)
        {
            GameObject feedbackGO = new GameObject("Feedback");
            feedbackGO.transform.SetParent(contentGO.transform, false);
            RectTransform feedbackRect = feedbackGO.AddComponent<RectTransform>();
            feedbackRect.anchorMin = Vector2.zero;
            feedbackRect.anchorMax = Vector2.one;

            insertionFeedbackText = feedbackGO.AddComponent<Text>();
            insertionFeedbackText.text = "Hold GRIP to insert...";
            insertionFeedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            insertionFeedbackText.fontSize = 12;
            insertionFeedbackText.color = Color.white;
            insertionFeedbackText.alignment = TextAnchor.UpperLeft;
            insertionFeedbackText.horizontalOverflow = HorizontalWrapMode.Wrap;
            insertionFeedbackText.verticalOverflow = VerticalWrapMode.Truncate;
            insertionFeedbackText.supportRichText = true;
            ConfigureTextFit(insertionFeedbackText, 12, 9, 14);
        }

        private static void CreatePlungerCard(GameObject contentGO)
        {
            GameObject feedbackGO = new GameObject("Feedback");
            feedbackGO.transform.SetParent(contentGO.transform, false);
            RectTransform feedbackRect = feedbackGO.AddComponent<RectTransform>();
            feedbackRect.anchorMin = Vector2.zero;
            feedbackRect.anchorMax = Vector2.one;

            plungerFeedbackText = feedbackGO.AddComponent<Text>();
            plungerFeedbackText.text = "Press INDEX to plunge...";
            plungerFeedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            plungerFeedbackText.fontSize = 12;
            plungerFeedbackText.color = new Color(0.2f, 1f, 0.2f);
            plungerFeedbackText.alignment = TextAnchor.UpperLeft;
            plungerFeedbackText.horizontalOverflow = HorizontalWrapMode.Wrap;
            plungerFeedbackText.verticalOverflow = VerticalWrapMode.Truncate;
            plungerFeedbackText.supportRichText = true;
            ConfigureTextFit(plungerFeedbackText, 12, 9, 14);
        }

        // ─── XR Button Creation ───────────────────────────────────────────────────

        private static Button CreateXRButton(GameObject parentGO, string text)
        {
            GameObject btnGO = new GameObject($"{text}Button");
            btnGO.transform.SetParent(parentGO.transform, false);
            RectTransform btnRect = btnGO.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(100, 40);

            LayoutElement layoutElem = btnGO.AddComponent<LayoutElement>();
            layoutElem.preferredWidth = 100;
            layoutElem.preferredHeight = 40;

            Image btnImage = btnGO.AddComponent<Image>();
            btnImage.color = new Color(0.27f, 0.51f, 0.71f);

            Button btn = btnGO.AddComponent<Button>();

            // XR Interactable
            XRSimpleInteractable interactable = btnGO.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener((SelectEnterEventArgs args) => btn.onClick.Invoke());

            // Collider for raycasting
            BoxCollider collider = btnGO.AddComponent<BoxCollider>();
            collider.size = new Vector3(btnRect.sizeDelta.x, btnRect.sizeDelta.y, 20f);
            collider.center = new Vector3(0f, 0f, 0f);

            // Text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            Text btnText = textGO.AddComponent<Text>();
            btnText.text = text;
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 12;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            ConfigureTextFit(btnText, 12, 9, 14);

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return btn;
        }

        // ─── Navigation ───────────────────────────────────────────────────────────

        public static void HandleNextNavigation(GameObject obj)
        {
            if (currentCardIndex < 7)
            {
                HideCard(currentCardIndex);
                currentCardIndex++;
                ShowCard(currentCardIndex);
                RefreshButtons();
                StateAccessor.SetState("UIManager", GetStateForCard(currentCardIndex), GetUIManagerContext(obj), "Version_3");
                Debug.Log($"[VR Injection] → Card {currentCardIndex + 1}");
            }
        }

        public static void HandleBackNavigation(GameObject obj)
        {
            if (currentCardIndex > 0)
            {
                HideCard(currentCardIndex);
                currentCardIndex--;
                ShowCard(currentCardIndex);
                RefreshButtons();
                StateAccessor.SetState("UIManager", GetStateForCard(currentCardIndex), GetUIManagerContext(obj), "Version_3");
                Debug.Log($"[VR Injection] ← Card {currentCardIndex + 1}");
            }
        }

        private static void ShowCard(int idx)
        {
            if (idx >= 0 && idx < 8)
            {
                cardPanels[idx].alpha = 1f;
                cardPanels[idx].interactable = true;
                cardPanels[idx].blocksRaycasts = true;
            }
        }

        private static void HideCard(int idx)
        {
            if (idx >= 0 && idx < 8)
            {
                cardPanels[idx].alpha = 0f;
                cardPanels[idx].interactable = false;
                cardPanels[idx].blocksRaycasts = false;
            }
        }

        private static void RefreshButtons()
        {
            if (backButton != null) backButton.interactable = (currentCardIndex > 0);
            if (nextButton != null) nextButton.interactable = (currentCardIndex < 7);
        }

        private static string GetStateForCard(int idx)
        {
            return idx switch
            {
                0 => "card1_syringeSelection",
                1 => "card2_adminType",
                2 => "card3_fillInstructions",
                3 => "card4_checkBubbles",
                4 => "card5_controllerAngles",
                5 => "card6_angleAlignment",
                6 => "card7_needleInsertion",
                7 => "card8_plungerPress",
                _ => "card1_syringeSelection"
            };
        }

        // ─── Angle Tracking ───────────────────────────────────────────────────────

        public static void DisplayLeftControllerAngles(GameObject obj)
        {
            HandleRightIndexNextClick(obj);
            if (currentCardIndex != 4 || controllerAngleText == null) return;

            GameObject leftCtrl = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Left Controller");
            if (leftCtrl == null) return;

            Vector3 angles = leftCtrl.transform.eulerAngles;
            float x = NormalizeAngle(angles.x);
            float y = NormalizeAngle(angles.y);
            float z = NormalizeAngle(angles.z);

            bool xOk = Mathf.Abs(x - TARGET_ANGLE_X) <= ANGLE_TOLERANCE;
            bool yOk = Mathf.Abs(y - TARGET_ANGLE_Y) <= ANGLE_TOLERANCE;
            bool zOk = Mathf.Abs(z - TARGET_ANGLE_Z) <= ANGLE_TOLERANCE;

            controllerAngleText.text =
                $"Target Range\n" +
                $"X: {TARGET_ANGLE_X - ANGLE_TOLERANCE:F1}° to {TARGET_ANGLE_X + ANGLE_TOLERANCE:F1}°\n" +
                $"Y: {TARGET_ANGLE_Y - ANGLE_TOLERANCE:F1}° to {TARGET_ANGLE_Y + ANGLE_TOLERANCE:F1}°\n" +
                $"Z: {TARGET_ANGLE_Z - ANGLE_TOLERANCE:F1}° to {TARGET_ANGLE_Z + ANGLE_TOLERANCE:F1}°\n\n" +
                $"Live Angles\n" +
                $"<color={(xOk ? "lime" : "red")}>X: {x:F1}°</color>\n" +
                $"<color={(yOk ? "lime" : "red")}>Y: {y:F1}°</color>\n" +
                $"<color={(zOk ? "lime" : "red")}>Z: {z:F1}°</color>";
        }

        public static void UpdateAngleAlignment(GameObject obj)
        {
            HandleRightIndexNextClick(obj);
            if (currentCardIndex != 5 || angleStatusText == null) return;

            GameObject leftCtrl = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Left Controller");
            if (leftCtrl == null) return;

            Vector3 angles = leftCtrl.transform.eulerAngles;
            float x = NormalizeAngle(angles.x);
            float y = NormalizeAngle(angles.y);
            float z = NormalizeAngle(angles.z);

            bool xOk = Mathf.Abs(x - TARGET_ANGLE_X) <= ANGLE_TOLERANCE;
            bool yOk = Mathf.Abs(y - TARGET_ANGLE_Y) <= ANGLE_TOLERANCE;
            bool zOk = Mathf.Abs(z - TARGET_ANGLE_Z) <= ANGLE_TOLERANCE;

            angleStatusText.text =
                $"Target Range\n" +
                $"X: {TARGET_ANGLE_X - ANGLE_TOLERANCE:F1}° to {TARGET_ANGLE_X + ANGLE_TOLERANCE:F1}°\n" +
                $"Y: {TARGET_ANGLE_Y - ANGLE_TOLERANCE:F1}° to {TARGET_ANGLE_Y + ANGLE_TOLERANCE:F1}°\n" +
                $"Z: {TARGET_ANGLE_Z - ANGLE_TOLERANCE:F1}° to {TARGET_ANGLE_Z + ANGLE_TOLERANCE:F1}°\n\n" +
                $"Live Angles\n" +
                $"<color={(xOk ? "lime" : "red")}>X: {x:F1}°</color>\n" +
                $"<color={(yOk ? "lime" : "red")}>Y: {y:F1}°</color>\n" +
                $"<color={(zOk ? "lime" : "red")}>Z: {z:F1}°</color>";

            if (xOk && yOk && zOk)
            {
                angleStatusText.text += "\n\n<color=lime>✓ ALIGNED!\nPress GRIP to insert</color>";
                CheckGripForInsertion(obj);
            }
        }

        private static void CheckGripForInsertion(GameObject obj)
        {
            ActionBasedController leftCtrl = FindLeftController();
            if (leftCtrl == null || leftCtrl.selectAction.action == null) return;

            bool gripNow = leftCtrl.selectAction.action.IsPressed();
            if (gripNow && !wasGripPressed && currentCardIndex == 5)
            {
                HideCard(5);
                currentCardIndex = 6;
                ShowCard(6);
                RefreshButtons();
                gripStartPosition = leftCtrl.transform.position;
                StateAccessor.SetState("UIManager", "card7_needleInsertion", GetUIManagerContext(obj), "Version_3");
                Debug.Log("[VR Injection] Starting insertion...");
            }
            wasGripPressed = gripNow;
        }

        // ─── Insertion Tracking ───────────────────────────────────────────────────

        public static void TrackNeedleInsertion(GameObject obj)
        {
            HandleRightIndexNextClick(obj);
            if (currentCardIndex != 6 || insertionFeedbackText == null) return;

            ActionBasedController leftCtrl = FindLeftController();
            if (leftCtrl == null || leftCtrl.selectAction.action == null) return;

            bool gripNow = leftCtrl.selectAction.action.IsPressed();

            if (gripNow)
            {
                isGripPressed = true;
                float depth = Vector3.Distance(gripStartPosition, leftCtrl.transform.position);
                depth = Mathf.Clamp(depth, 0, INSERTION_MAX);

                string status = "TOO LITTLE";
                Color color = Color.red;

                if (depth >= INSERTION_PERFECT_MIN && depth <= INSERTION_PERFECT_MAX)
                {
                    status = "PERFECT";
                    color = new Color(0.2f, 1f, 0.2f);
                }
                else if (depth > INSERTION_PERFECT_MAX)
                {
                    status = "TOO MUCH";
                    color = Color.red;
                }

                insertionFeedbackText.color = color;
                insertionFeedbackText.text = $"<b>{status}</b>\n\nDepth: {depth * 100:F1}cm\nTarget: 8-15cm\n\nPress INDEX to plunge";
                CheckIndexForPlunger(obj);
            }
            else if (wasGripPressed)
            {
                isGripPressed = false;
            }

            wasGripPressed = gripNow;
        }

        private static void CheckIndexForPlunger(GameObject obj)
        {
            ActionBasedController leftCtrl = FindLeftController();
            if (leftCtrl == null || leftCtrl.activateAction.action == null) return;

            bool indexNow = leftCtrl.activateAction.action.IsPressed();
            if (indexNow && !wasIndexPressed && isGripPressed)
            {
                HideCard(6);
                currentCardIndex = 7;
                ShowCard(7);
                RefreshButtons();
                plungerStartPosition = leftCtrl.transform.position;
                plungerStartTime = Time.time;
                StateAccessor.SetState("UIManager", "card8_plungerPress", GetUIManagerContext(obj), "Version_3");
                Debug.Log("[VR Injection] Starting plunger...");
            }
            wasIndexPressed = indexNow;
        }

        // ─── Plunger Tracking ─────────────────────────────────────────────────────

        public static void TrackPlungerPress(GameObject obj)
        {
            HandleRightIndexNextClick(obj);
            if (currentCardIndex != 7 || plungerFeedbackText == null) return;

            ActionBasedController leftCtrl = FindLeftController();
            if (leftCtrl == null || leftCtrl.activateAction.action == null) return;

            bool indexNow = leftCtrl.activateAction.action.IsPressed();

            if (indexNow)
            {
                float elapsed = Time.time - plungerStartTime;
                float distance = Vector3.Distance(plungerStartPosition, leftCtrl.transform.position);

                string status = "TOO SLOW";
                Color color = Color.red;

                if (elapsed >= PLUNGER_PERFECT_TIME * 0.8f && elapsed <= PLUNGER_PERFECT_TIME * 1.2f &&
                    distance >= PLUNGER_PERFECT_MIN && distance <= PLUNGER_PERFECT_MAX)
                {
                    status = "PERFECT";
                    color = new Color(0.2f, 1f, 0.2f);
                }
                else if (elapsed < PLUNGER_PERFECT_TIME * 0.8f)
                {
                    status = "TOO FAST";
                    color = Color.red;
                }

                plungerFeedbackText.color = color;
                plungerFeedbackText.text = $"<b>{status}</b>\n\nTime: {elapsed:F1}s / {PLUNGER_PERFECT_TIME}s\nDist: {distance * 100:F1}cm";
            }
            else if (wasIndexPressed)
            {
                plungerFeedbackText.text = "<color=lime>Complete!\nNext button ready.</color>";
                plungerFeedbackText.color = new Color(0.2f, 1f, 0.2f);
            }

            wasIndexPressed = indexNow;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private static ActionBasedController FindLeftController()
        {
            GameObject go = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Left Controller");
            return go != null ? go.GetComponent<ActionBasedController>() : null;
        }

        private static ActionBasedController FindRightController()
        {
            GameObject go = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Right Controller");
            return go != null ? go.GetComponent<ActionBasedController>() : null;
        }

        private static void HandleRightIndexNextClick(GameObject obj)
        {
            if (nextButton == null || !nextButton.interactable) return;

            ActionBasedController rightCtrl = FindRightController();
            if (rightCtrl == null || rightCtrl.activateAction.action == null) return;

            bool rightIndexNow = rightCtrl.activateAction.action.IsPressed();
            if (rightIndexNow && !wasRightIndexPressed)
            {
                nextButton.onClick.Invoke();
            }
            wasRightIndexPressed = rightIndexNow;
        }

        private static void ConfigureTextFit(Text text, int preferredSize, int minSize, int maxSize)
        {
            text.fontSize = preferredSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static GameObject GetUIManagerContext(GameObject fallback)
        {
            if (uiManagerContext != null)
            {
                return uiManagerContext;
            }
            return fallback;
        }

        private static void EnsureXRUIEventSystem()
        {
            EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                eventSystem = esGo.AddComponent<EventSystem>();
            }

            XRUIInputModule xrInputModule = eventSystem.GetComponent<XRUIInputModule>();
            if (xrInputModule == null)
            {
                eventSystem.gameObject.AddComponent<XRUIInputModule>();
            }

            StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                Object.Destroy(standalone);
            }
        }

        private static void EnsureControllerRayInteractors()
        {
            string[] controllerPaths =
            {
                "XR Origin (XR Rig)/Camera Offset/Left Controller",
                "XR Origin (XR Rig)/Camera Offset/Right Controller"
            };

            for (int i = 0; i < controllerPaths.Length; i++)
            {
                GameObject controller = GameObject.Find(controllerPaths[i]);
                if (controller == null) continue;

                if (controller.GetComponent<XRRayInteractor>() == null)
                {
                    controller.AddComponent<XRRayInteractor>();
                }
            }
        }
    }
}
