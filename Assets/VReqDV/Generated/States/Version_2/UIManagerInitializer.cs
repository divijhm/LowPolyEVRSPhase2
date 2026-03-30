// GENERATED FILE — DO NOT EDIT
using UnityEngine;

namespace Version_2
{
    public class UIManagerInitializer : MonoBehaviour
    {
        public UIManagerStateEnum initialState = UIManagerStateEnum.Initialized;

        void Awake()
        {
            UIManagerStateStorage.Register(gameObject, initialState);
        }
    }
}
