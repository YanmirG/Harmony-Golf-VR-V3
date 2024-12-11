using UnityEngine;

namespace Convai.Scripts.Runtime.UI
{
    public class ConvaiLogoHandler : MonoBehaviour
    {
        [SerializeField] private GameObject convaiLogo;

        private void Start()
        {
            ChangeLogo(false);
        }

        private void OnEnable()
        {
            ChatUIBase.UIStatusChange += ChangeLogo;
            UIAppearanceSettings.UIStatusChange += ChangeLogo;
        }

        private void OnDisable()
        {
            ChatUIBase.UIStatusChange -= ChangeLogo;
            UIAppearanceSettings.UIStatusChange -= ChangeLogo;
        }

        private void ChangeLogo(bool isHidden)
        {
            convaiLogo.SetActive(!isHidden);
        }
    }
}