// GENERATED FILE — DO NOT EDIT
using UnityEngine;

namespace Version_5
{
    public class InitializeUI_UIManager : MonoBehaviour
    {
        void Update()
        {
            if (UIManagerStateStorage.Get(GameObject.Find("UIManager")) == UIManagerStateEnum.Initialized)
            {
                UserAlgorithms.SetupUISystem(GameObject.Find("UIManager"));
            }
        }
    }
}
