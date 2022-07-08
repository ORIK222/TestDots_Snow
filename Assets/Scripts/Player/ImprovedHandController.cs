using UnityEngine;
//using UnityEngine.InputSystem;

namespace Core.Player
{
    public class ImprovedHandController : MonoBehaviour
    {
        /*[SerializeField] InputActionReference controllerActionGrip;
        [SerializeField] InputActionReference controllerActionTrigger;
        private Animator _handAnimator;

        private void Awake()
        {
            controllerActionGrip.action.performed += GripPress;
            controllerActionGrip.action.canceled += GripPress;
            controllerActionTrigger.action.performed += TriggerPress;
            controllerActionTrigger.action.canceled += TriggerPress;
            _handAnimator = GetComponent<Animator>();
        }

        private void TriggerPress(InputAction.CallbackContext obj) =>
            _handAnimator.SetFloat("Trigger", obj.ReadValue<float>());

        private void GripPress(InputAction.CallbackContext obj) =>
            _handAnimator.SetFloat("Grip", obj.ReadValue<float>());*/
        
    }
}
