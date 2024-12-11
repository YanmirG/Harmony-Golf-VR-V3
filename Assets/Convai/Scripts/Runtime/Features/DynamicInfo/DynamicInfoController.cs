using Service;
using UnityEngine;

namespace Convai.Scripts.Runtime.Features
{
    public class DynamicInfoController : MonoBehaviour
    {
        public DynamicInfoConfig DynamicInfoConfig { get; private set; }

        private void Awake()
        {
            DynamicInfoConfig = new DynamicInfoConfig();
        }

        public void SetDynamicInfo(string info)
        {
            DynamicInfoConfig.Text = info;
        }

        public void AddDynamicInfo(string info)
        {
            DynamicInfoConfig.Text += info;
        }

        public void ClearDynamicInfo()
        {
            DynamicInfoConfig.Text = "";
        }
    }
}