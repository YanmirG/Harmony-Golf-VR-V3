namespace Convai.Scripts.Runtime.UI
{
    // Handles the activation status of the transcript UI based on Settings Panel Toggle.
    public class TranscriptUIActiveStatusHandler : ActiveStatusHandler
    {
        protected override void UISaveLoadSystem_OnLoad()
        {
            // Retrieve the saved notification system activation status.
            bool newValue = UISaveLoadSystem.Instance.TranscriptUIActiveStatus;

            // Update the UI and internal status based on the loaded value.
            OnStatusChange(newValue);
            _activeStatusToggle.isOn = newValue;
        }

        /// <summary>
        ///     Set the activation status of the notification system.
        /// </summary>
        /// <param name="value"> The new activation status. </param>
        public override void OnStatusChange(bool value)
        {
            // Save the current transcript UI activation status.
            UISaveLoadSystem.Instance.TranscriptUIActiveStatus = _activeStatusToggle.isOn;

            IChatUI currentImplementation = ConvaiChatUIHandler.Instance.GetCurrentUI();

            // Activate or Deactivate Toggle Values based on the toggle value
            if (value)
                currentImplementation.ActivateUI();
            else
                currentImplementation.DeactivateUI();
        }
    }
}