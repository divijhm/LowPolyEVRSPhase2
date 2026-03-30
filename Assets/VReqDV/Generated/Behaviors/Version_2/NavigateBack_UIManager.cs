// GENERATED FILE — DO NOT EDIT
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace Version_2
{
    public class NavigateBack_UIManager : MonoBehaviour
    {
        private XRBaseInteractable _interactable;

        void OnEnable()
        {
            _interactable = GetComponent<XRBaseInteractable>();
            if (_interactable != null) _interactable.selectEntered.AddListener(OnInteraction);
        }

        void OnDisable()
        {
            if (_interactable != null) _interactable.selectEntered.RemoveListener(OnInteraction);
        }

        void OnInteraction(SelectEnterEventArgs args)
        {
            UserAlgorithms.HandleBackNavigation(GameObject.Find("UIManager"));
        }
    }
}
