using Convai.Scripts.Runtime.UI;

namespace Convai.Scripts.Runtime.Addons
{
    public class NotificationSystemActiveStatusHandler : ActiveStatusHandler
    {
        protected override void UISaveLoadSystem_OnLoad()
        {
            // Retrieve the saved notification system activation status.
            bool newValue = UISaveLoadSystem.Instance.NotificationSystemActiveStatus;

            // Update the UI and internal status based on the loaded value.
            OnStatusChange(newValue);
            _activeStatusToggle.isOn = newValue;
        }

        protected override void UISaveLoadSystem_OnSave()
        {
            // Save the current notification system activation status.
            UISaveLoadSystem.Instance.NotificationSystemActiveStatus = _activeStatusToggle.isOn;
        }

        /// <summary>
        ///     Set the activation status of the notification system.
        /// </summary>
        /// <param name="value"> The new activation status. </param>
        public override void OnStatusChange(bool value)
        {
            // Call the NotificationSystemHandler to update the activation status.
            NotificationSystemHandler.Instance.SetNotificationSystemActiveStatus(value);
        }
    }
}

// Handles the activation status of the notification system based on Settings Panel Toggle.