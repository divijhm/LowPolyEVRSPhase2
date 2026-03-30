// GENERATED FILE — DO NOT EDIT
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Version_2
{
    public static class UIManagerStateStorage
    {
        private static Dictionary<GameObject, UIManagerStateEnum> stateTable = new();

        public static event Action<GameObject, UIManagerStateEnum> OnStateChanged;

        public static void Register(GameObject obj, UIManagerStateEnum initialState)
        {
            if (!stateTable.ContainsKey(obj))
                stateTable.Add(obj, initialState);
        }

        public static UIManagerStateEnum Get(GameObject obj) => stateTable[obj];

        public static bool IsInitialized(GameObject obj) => stateTable[obj] == UIManagerStateEnum.Initialized;
        public static bool IsCard1_syringeSelection(GameObject obj) => stateTable[obj] == UIManagerStateEnum.Card1_syringeSelection;
        public static bool IsCard2_adminType(GameObject obj) => stateTable[obj] == UIManagerStateEnum.Card2_adminType;
        public static bool IsCard3_fillInstructions(GameObject obj) => stateTable[obj] == UIManagerStateEnum.Card3_fillInstructions;
        public static bool IsCard4_checkBubbles(GameObject obj) => stateTable[obj] == UIManagerStateEnum.Card4_checkBubbles;
        public static bool IsCard5_controllerAngles(GameObject obj) => stateTable[obj] == UIManagerStateEnum.Card5_controllerAngles;

        public static void SetInitialized(GameObject obj) => SetState(obj, UIManagerStateEnum.Initialized);
        public static void SetCard1_syringeSelection(GameObject obj) => SetState(obj, UIManagerStateEnum.Card1_syringeSelection);
        public static void SetCard2_adminType(GameObject obj) => SetState(obj, UIManagerStateEnum.Card2_adminType);
        public static void SetCard3_fillInstructions(GameObject obj) => SetState(obj, UIManagerStateEnum.Card3_fillInstructions);
        public static void SetCard4_checkBubbles(GameObject obj) => SetState(obj, UIManagerStateEnum.Card4_checkBubbles);
        public static void SetCard5_controllerAngles(GameObject obj) => SetState(obj, UIManagerStateEnum.Card5_controllerAngles);

        private static void SetState(GameObject obj, UIManagerStateEnum newState)
        {
            if (stateTable[obj] != newState)
            {
                stateTable[obj] = newState;
                OnStateChanged?.Invoke(obj, newState);
            }
        }
    }
}
