using UnityEngine;
using VReqDV;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

namespace Version_2
{
    /// <summary>
    /// VR Injection Administration Guide - Version 2
    /// 
    /// This system provides an interactive tutorial for learning injection administration.
    /// It includes:
    /// - Card-based UI system in top-left corner
    /// - Dropdown selection for syringe types
    /// - Radio buttons for injection administration types
    /// - Real-time display of left controller angles
    /// - Navigation between 5 cards
    /// </summary>
    public static class UserAlgorithms
    {
        // ─── UI System State ───────────────────────────────────────────────────────

        private static Canvas mainCanvas = null;
        private static CanvasGroup[] cardPanels = new CanvasGroup[5];
        private static int currentCardIndex = 0; // 0-4 for cards 1-5
        private static Button nextButton = null;
        private static Button backButton = null;
        private static Text controllerAngleText = null;
        private static Dropdown syringeDropdown = null;
        private static bool uiInitialized = false;

        // ─── Card Data ───────────────────────────────────────────────────────────

        private static readonly string[] SyringeTypes = { "Big", "Small", "Wide" };
        private static readonly string[] AdminTypes = { "ID (Intradermal)", "SC (Subcutaneous)", "IV (Intravenous)", "IM (Intramuscular)" };
        private static readonly string[] CardTitles = {
            "Select Syringe Type",
            "Select Administration Type",
            "Fill the Syringe",
            "Check for Air Bubbles",
            "Controller Angle Display"
        };
        private static readonly string[] CardDescriptions = {
            "Choose the type of syringe you will be using:",
            "Select the injection administration type:",
            "Steps to fill the syringe:\n1. Draw medication into syringe\n2. Measure correct dosage\n3. Keep syringe sterile\n4. Tap to continue when ready",
            "Check for air bubbles in the syringe:\n1. Hold syringe upright\n2. Tap to dislodge bubbles\n3. Expel air through needle\n4. Tap to continue when ready",
            "Position your left controller and observe the real-time rotation angles.\nThis indicates the insertion angle for the injection."
        };

        private static string selectedSyringe = "";
        private static string selectedAdminType = "";

        // ─── Setup UI System ───────────────────────────────────────────────────────

        public static void SetupUISystem(GameObject obj)
        {
            if (uiInitialized)
                return;

            // Create main Canvas
            GameObject canvasGO = new GameObject("InjectionGuideCanvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            GraphicRaycaster raycaster = canvasGO.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;

            // Create main container panel (top-left)
            GameObject containerGO = new GameObject("UIContainer");
            containerGO.transform.SetParent(canvasGO.transform, false);
            RectTransform containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(0, 1);
            containerRect.offsetMin = new Vector2(20, -420);
            containerRect.offsetMax = new Vector2(420, -20);
            containerRect.sizeDelta = new Vector2(400, 400);

            Image containerImage = containerGO.AddComponent<Image>();
            containerImage.color = new Color(0, 0, 0, 0.8f);
            
            LayoutElement containerLayout = containerGO.AddComponent<LayoutElement>();
            containerLayout.preferredWidth = 400;
            containerLayout.preferredHeight = 400;

            // Create 5 card panels
            for (int i = 0; i < 5; i++)
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

                // Card content
                CreateCardContent(cardGO, i);
            }

            // Create navigation buttons (Bottom of container)
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

            // Back Button
            backButton = CreateButton(buttonsContainer, "BackButton", "Back", 0);
            backButton.onClick.AddListener(() => HandleBackNavigation(obj));

            // Next Button
            nextButton = CreateButton(buttonsContainer, "NextButton", "Next", 1);
            nextButton.onClick.AddListener(() => HandleNextNavigation(obj));

            // Update button visibility
            UpdateNavigationButtons();

            uiInitialized = true;
            StateAccessor.SetState("UIManager", "card1_syringeSelection", obj, "Version_2");

            Debug.Log("[VR Injection Guide] UI System initialized. Starting with Card 1.");
        }

        // ─── Create Card Content ───────────────────────────────────────────────────

        private static void CreateCardContent(GameObject cardGO, int cardIndex)
        {
            // Create title
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

            // Create content area
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(cardGO.transform, false);
            RectTransform contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(10, 50);
            contentRect.offsetMax = new Vector2(-10, -60);

            Image contentImage = contentGO.AddComponent<Image>();
            contentImage.color = new Color(0, 0, 0, 0.5f);

            LayoutGroup contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            ((VerticalLayoutGroup)contentLayout).spacing = 10;
            ((VerticalLayoutGroup)contentLayout).padding = new RectOffset(10, 10, 10, 10);
            ((VerticalLayoutGroup)contentLayout).childForceExpandWidth = true;
            ((VerticalLayoutGroup)contentLayout).childForceExpandHeight = true;

            // Card-specific content
            switch (cardIndex)
            {
                case 0: // Syringe Selection
                    CreateSyringeSelectionCard(contentGO);
                    break;
                case 1: // Admin Type
                    CreateAdminTypeCard(contentGO);
                    break;
                case 2: // Fill Instructions
                    CreateInstructionsCard(contentGO, cardIndex);
                    break;
                case 3: // Check Bubbles
                    CreateInstructionsCard(contentGO, cardIndex);
                    break;
                case 4: // Controller Angles
                    CreateControllerAnglesCard(contentGO);
                    break;
            }

            // Add description text
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
        }

        // ─── Syringe Selection Card ───────────────────────────────────────────────

        private static void CreateSyringeSelectionCard(GameObject contentGO)
        {
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(contentGO.transform, false);
            Text labelText = labelGO.AddComponent<Text>();
            labelText.text = "Select Syringe:";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.color = Color.white;

            GameObject dropdownGO = new GameObject("SyringeDropdown");
            dropdownGO.transform.SetParent(contentGO.transform, false);
            RectTransform dropdownRect = dropdownGO.AddComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(300, 30);

            Image dropdownImage = dropdownGO.AddComponent<Image>();
            dropdownImage.color = new Color(0.2f, 0.2f, 0.2f);

            syringeDropdown = dropdownGO.AddComponent<Dropdown>();
            syringeDropdown.options.Clear();
            foreach (string syringe in SyringeTypes)
            {
                syringeDropdown.options.Add(new Dropdown.OptionData(syringe));
            }
            syringeDropdown.value = 0;
            syringeDropdown.onValueChanged.AddListener((int index) =>
            {
                selectedSyringe = SyringeTypes[index];
                Debug.Log($"[VR Injection] Selected Syringe: {selectedSyringe}");
            });

            selectedSyringe = SyringeTypes[0];
        }

        // ─── Admin Type Card ───────────────────────────────────────────────────────

        private static void CreateAdminTypeCard(GameObject contentGO)
        {
            for (int i = 0; i < AdminTypes.Length; i++)
            {
                GameObject buttonGO = new GameObject($"AdminTypeButton{i}");
                buttonGO.transform.SetParent(contentGO.transform, false);
                RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(300, 30);

                Image buttonImage = buttonGO.AddComponent<Image>();
                buttonImage.color = new Color(0.3f, 0.3f, 0.3f);

                Button button = buttonGO.AddComponent<Button>();
                int index = i;
                button.onClick.AddListener(() =>
                {
                    selectedAdminType = AdminTypes[index];
                    Debug.Log($"[VR Injection] Selected Admin Type: {selectedAdminType}");
                });

                Text buttonText = new GameObject("Text").AddComponent<Text>();
                buttonText.transform.SetParent(buttonGO.transform, false);
                buttonText.text = AdminTypes[i];
                buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                buttonText.fontSize = 12;
                buttonText.color = Color.white;
                buttonText.alignment = TextAnchor.MiddleCenter;

                RectTransform textRect = buttonText.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }
        }

        // ─── Instructions Card ─────────────────────────────────────────────────────

        private static void CreateInstructionsCard(GameObject contentGO, int cardIndex)
        {
            // Instructions are shown in the description text - no additional content needed
            // This space could be used for images or diagrams in future versions
        }

        // ─── Controller Angles Card ───────────────────────────────────────────────

        private static void CreateControllerAnglesCard(GameObject contentGO)
        {
            GameObject angleDisplayGO = new GameObject("AngleDisplay");
            angleDisplayGO.transform.SetParent(contentGO.transform, false);
            RectTransform angleRect = angleDisplayGO.AddComponent<RectTransform>();
            angleRect.anchorMin = Vector2.zero;
            angleRect.anchorMax = Vector2.one;

            controllerAngleText = angleDisplayGO.AddComponent<Text>();
            controllerAngleText.text = "Waiting for controller input...";
            controllerAngleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            controllerAngleText.fontSize = 14;
            controllerAngleText.fontStyle = FontStyle.Bold;
            controllerAngleText.color = new Color(0.2f, 1f, 0.2f);
            controllerAngleText.alignment = TextAnchor.UpperLeft;
            controllerAngleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            controllerAngleText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        // ─── Create Button Helper ──────────────────────────────────────────────────

        private static Button CreateButton(GameObject parentGO, string name, string text, int order)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parentGO.transform, false);
            RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(100, 40);

            LayoutElement layoutElement = buttonGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 100;
            layoutElement.preferredHeight = 40;

            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.27f, 0.51f, 0.71f);

            Button button = buttonGO.AddComponent<Button>();

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);

            Text buttonText = textGO.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 12;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        // ─── Navigation ───────────────────────────────────────────────────────────

        public static void HandleNextNavigation(GameObject obj)
        {
            if (currentCardIndex < 4)
            {
                HideCard(currentCardIndex);
                currentCardIndex++;
                ShowCard(currentCardIndex);
                UpdateNavigationButtons();
                
                string newState = GetStateForCard(currentCardIndex);
                StateAccessor.SetState("UIManager", newState, obj, "Version_2");
                Debug.Log($"[VR Injection] Navigated to Card {currentCardIndex + 1}");
            }
        }

        public static void HandleBackNavigation(GameObject obj)
        {
            if (currentCardIndex > 0)
            {
                HideCard(currentCardIndex);
                currentCardIndex--;
                ShowCard(currentCardIndex);
                UpdateNavigationButtons();
                
                string newState = GetStateForCard(currentCardIndex);
                StateAccessor.SetState("UIManager", newState, obj, "Version_2");
                Debug.Log($"[VR Injection] Navigated to Card {currentCardIndex + 1}");
            }
        }

        private static void ShowCard(int index)
        {
            if (index >= 0 && index < 5)
            {
                cardPanels[index].alpha = 1f;
                cardPanels[index].interactable = true;
                cardPanels[index].blocksRaycasts = true;
            }
        }

        private static void HideCard(int index)
        {
            if (index >= 0 && index < 5)
            {
                cardPanels[index].alpha = 0f;
                cardPanels[index].interactable = false;
                cardPanels[index].blocksRaycasts = false;
            }
        }

        private static void UpdateNavigationButtons()
        {
            if (backButton != null)
                backButton.interactable = (currentCardIndex > 0);
            
            if (nextButton != null)
                nextButton.interactable = (currentCardIndex < 4);
        }

        private static string GetStateForCard(int index)
        {
            return index switch
            {
                0 => "card1_syringeSelection",
                1 => "card2_adminType",
                2 => "card3_fillInstructions",
                3 => "card4_checkBubbles",
                4 => "card5_controllerAngles",
                _ => "card1_syringeSelection"
            };
        }

        // ─── Controller Angle Display ──────────────────────────────────────────────

        public static void DisplayLeftControllerAngles(GameObject obj)
        {
            if (controllerAngleText == null || currentCardIndex != 4)
                return;

            // Find the left controller
            GameObject leftController = GameObject.Find("XR Origin (XR Rig)/Camera Offset/Left Controller");
            if (leftController == null)
            {
                controllerAngleText.text = "Left Controller not found in scene!";
                return;
            }

            Vector3 eulerAngles = leftController.transform.eulerAngles;

            // Normalize angles to -180 to 180 range for readability
            float x = NormalizeAngle(eulerAngles.x);
            float y = NormalizeAngle(eulerAngles.y);
            float z = NormalizeAngle(eulerAngles.z);

            controllerAngleText.text = $"LEFT CONTROLLER ANGLES\n\nX (Pitch): {x:F1}°\nY (Yaw): {y:F1}°\nZ (Roll): {z:F1}°\n\nUse these angles to guide insertion angle.";
        }

        private static float NormalizeAngle(float angle)
        {
            if (angle > 180f)
                angle -= 360f;
            return angle;
        }
    }
}
