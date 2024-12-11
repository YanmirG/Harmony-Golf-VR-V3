using UnityEngine;
using UnityEngine.UI;

namespace Convai.Scripts.Runtime.UI
{
    /// <summary>
    ///     Base class to handle different toggle status, including loading and saving
    /// </summary>
    public class ActiveStatusHandler : MonoBehaviour
    {
        [SerializeField] protected Toggle _activeStatusToggle;

        private void Awake()
        {
            // Subscribe to the toggle's value change event.
            _activeStatusToggle.onValueChanged.AddListener(OnStatusChange);
        }

        /// <summary>
        ///     Subscribe to events when this component is enabled.
        /// </summary>
        private void OnEnable()
        {
            // Subscribe to the event when saved data is loaded.
            UISaveLoadSystem.Instance.OnLoad += UISaveLoadSystem_OnLoad;

            // Subscribe to the event when data is saved.
            UISaveLoadSystem.Instance.OnSave += UISaveLoadSystem_OnSave;
        }

        /// <summary>
        ///     Unsubscribe from events when this component is disabled.
        /// </summary>
        private void OnDisable()
        {
            // Subscribe to the event when saved data is loaded.
            UISaveLoadSystem.Instance.OnLoad -= UISaveLoadSystem_OnLoad;

            // Subscribe to the event when data is saved.
            UISaveLoadSystem.Instance.OnSave -= UISaveLoadSystem_OnSave;
        }

        /// <summary>
        ///     Event handler for when saved data is loaded.
        /// </summary>
        protected virtual void UISaveLoadSystem_OnLoad()
        {
        }

        /// <summary>
        ///     Event handler for when data is saved.
        /// </summary>
        protected virtual void UISaveLoadSystem_OnSave()
        {
        }

        /// <summary>
        ///     Set the activation status
        /// </summary>
        /// <param name="value"> The new activation status. </param>
        public virtual void OnStatusChange(bool value)
        {
        }
    }
}