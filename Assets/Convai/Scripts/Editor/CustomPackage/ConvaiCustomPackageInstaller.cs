#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Convai.Scripts.Editor.CustomPackage
{
    public class ConvaiCustomPackageInstaller : IActiveBuildTargetChanged
    {
        #region Constants

        // XR Package Paths
        private const string AR_PACKAGE_PATH = "Assets/Convai/Custom Packages/ConvaiARUpgrader.unitypackage";
        private const string VR_PACKAGE_PATH = "Assets/Convai/Custom Packages/ConvaiVRUpgrader.unitypackage";
        private const string MR_PACKAGE_PATH = "Assets/Convai/Custom Packages/ConvaiMRUpgrader.unitypackage";
        private const string XR_PACKAGE_PATH = "Assets/Convai/Custom Packages/ConvaiXR.unitypackage";

        private const string XR_PREFAB_PATH = "Assets/Convai/ConvaiXR/Prefabs/Convai Essentials - XR.prefab";

        // Other Package Paths
        private const string IOS_BUILD_PACKAGE_PATH = "Assets/Convai/Custom Packages/ConvaiiOSBuild.unitypackage";
        private const string URP_CONVERTER_PACKAGE_PATH = "Assets/Convai/Custom Packages/ConvaiURPConverter.unitypackage";
        private const string TMP_PACKAGE_PATH = "Assets/Convai/Custom Packages/ConvaiCustomTMP.unitypackage";

        #endregion

        #region Fields

        private SetupTypes _currentSetup;
        private List<string> _setupSteps;
        private int _totalSetupSteps;
        private int _currentStep = 0;
        private string _currentStepDescription = "";
        private bool _assembliesLocked = false;

        private BuildTarget _selectedARPlatform;
        private InstallationType _installationType;

        public int callbackOrder { get; }

        #endregion

        #region Enums

        private enum SetupTypes
        {
            AR,
            VR,
            MR,
            iOS,
            NormalUnityPackage
        }

        private enum InstallationType
        {
            Automatic,
            Manual
        }

        #endregion

        #region Constructor

        public ConvaiCustomPackageInstaller()
        {
        }

        public ConvaiCustomPackageInstaller(VisualElement root = null)
        {
            if (root != null)
            {
                InitializeUI(root);
            }
        }

        #endregion

        #region UI Initialization

        private void InitializeUI(VisualElement root)
        {
            root.Q<Button>("install-ar-package").clicked += () => StartPackageInstall(SetupTypes.AR);
            root.Q<Button>("install-vr-package").clicked += () => StartPackageInstall(SetupTypes.VR);
            root.Q<Button>("install-mr-package").clicked += () => StartPackageInstall(SetupTypes.MR);
            root.Q<Button>("install-ios-build-package").clicked += () =>
            {
                InitializeSetupSteps(SetupTypes.iOS);
                InstallConvaiPackage(IOS_BUILD_PACKAGE_PATH);
                TryToDownloadiOSDLL();
            };
            root.Q<Button>("install-urp-converter").clicked += () =>
            {
                InitializeSetupSteps(SetupTypes.NormalUnityPackage);
                InstallConvaiPackage(URP_CONVERTER_PACKAGE_PATH);
                EditorUtility.ClearProgressBar();
            };
            root.Q<Button>("convai-custom-tmp-package").clicked += () =>
            {
                InitializeSetupSteps(SetupTypes.NormalUnityPackage);
                InstallConvaiPackage(TMP_PACKAGE_PATH);
                EditorUtility.ClearProgressBar();
            };
        }

        #endregion

        #region Build Target Change Handling

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            if (newTarget == BuildTarget.iOS) TryToDownloadiOSDLL();
        }

        #endregion

        #region Package Installation

        private async void StartPackageInstall(SetupTypes setupType)
        {
            _currentSetup = setupType;

            InitializeSetupSteps(setupType);

            if (setupType == SetupTypes.AR)
            {
                if (!await ConfirmARPlatform()) return;
                if (!await ConfirmAutomaticInstallation(SetupTypes.AR)) return;
            }
            else
            {
                if (!await ConfirmInstallationType(setupType)) return;
            }

            InitializeSetupSteps(setupType);
            _currentStep = 0;

            ConvaiLogger.DebugLog($"Installation of {setupType} package has started... This process may take 3-5 minutes.", ConvaiLogger.LogCategory.Editor);

            LockAssemblies();

            try
            {
                await HandlePackageInstall();
            }
            catch (Exception e)
            {
                ConvaiLogger.Error($"An error occurred during package installation: {e.Message}", ConvaiLogger.LogCategory.Editor);
            }
            finally
            {
                UnlockAssemblies();
                EditorUtility.ClearProgressBar();
                ConvaiLogger.DebugLog($"Convai {setupType} setup process completed.", ConvaiLogger.LogCategory.Editor);
            }
        }

        private void InitializeSetupSteps(SetupTypes setupType)
        {
            _setupSteps = new List<string>();
            if (setupType == SetupTypes.AR && _selectedARPlatform != EditorUserBuildSettings.activeBuildTarget)
            {
                _setupSteps.Add($"Change build platform to {_selectedARPlatform}");
            }
            else if ((setupType == SetupTypes.VR || setupType == SetupTypes.MR) &&
                     _installationType == InstallationType.Automatic &&
                     EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                _setupSteps.Add("Change build platform to Android");
            }

            switch (setupType)
            {
                case SetupTypes.AR:
                    if (_installationType == InstallationType.Automatic)
                    {
                        _setupSteps.AddRange(_selectedARPlatform == BuildTarget.Android
                            ? new[] { "Universal Render Pipeline (URP)", "ARCore", "Convai AR Package", "Convai URP Converter" }
                            : new[] { "Universal Render Pipeline (URP)", "ARKit", "Convai iOS DLL", "Convai iOS Build Package", "Convai AR Package", "Convai URP Converter", });
                    }
                    else
                    {
                        _setupSteps.Add("Convai AR Package");
                    }

                    break;
                case SetupTypes.VR:
                    if (_installationType == InstallationType.Automatic)
                    {
                        _setupSteps.AddRange(new[] { "Universal Render Pipeline (URP)", "OpenXR", "XR Interaction Toolkit", "Convai VR Package", "Convai URP Converter" });
                    }
                    else
                    {
                        _setupSteps.Add("Convai VR Package");
                    }

                    break;
                case SetupTypes.MR:
                    if (_installationType == InstallationType.Automatic)
                    {
                        _setupSteps.AddRange(new[] { "Universal Render Pipeline (URP)", "XR Management", "Oculus XR Plugin", "Convai URP Converter", "Meta XR SDK All", "Convai MR Package" });
                    }
                    else
                    {
                        _setupSteps.Add("Convai MR Package");
                    }

                    break;
                case SetupTypes.iOS:
                    _setupSteps.AddRange(new[] { "Convai iOS Build Package", "Convai iOS DLL" });
                    break;
                case SetupTypes.NormalUnityPackage:
                    _setupSteps.Add("Convai Custom Package");
                    break;
            }

            _totalSetupSteps = _setupSteps.Count;
        }

        private async Task HandlePackageInstall()
        {
            switch (_currentSetup)
            {
                case SetupTypes.AR:
                    await HandleARPackageInstall();
                    break;
                case SetupTypes.VR:
                    await HandleVRPackageInstall();
                    break;
                case SetupTypes.MR:
                    await HandleMRPackageInstall();
                    break;
            }
        }

        private async Task HandleARPackageInstall()
        {
            ChangeBuildTarget(_selectedARPlatform);
            await Task.Delay(TimeSpan.FromSeconds(5));
            await InitializeURPSetup();

            if (_selectedARPlatform == BuildTarget.Android)
            {
                await InitializeARCoreSetup();
            }
            else if (_selectedARPlatform == BuildTarget.iOS)
            {
                await InitializeARKitSetup();
                TryToDownloadiOSDLL();
                InstallConvaiPackage(IOS_BUILD_PACKAGE_PATH);
            }

            InstallConvaiPackage(AR_PACKAGE_PATH);
            InstallConvaiPackage(URP_CONVERTER_PACKAGE_PATH);
        }

        private async Task HandleVRPackageInstall()
        {
            if (_installationType == InstallationType.Automatic)
            {
                ChangeBuildTarget(BuildTarget.Android);
                await Task.Delay(TimeSpan.FromSeconds(5));
                await InitializeURPSetup();
                await InitializeOpenXRSetup();
                await InitializeXRInteractionToolkitSetup();
                InstallConvaiPackage(VR_PACKAGE_PATH);
                InstallConvaiPackage(URP_CONVERTER_PACKAGE_PATH);
            }
            else
            {
                InstallConvaiPackage(XR_PACKAGE_PATH);
                await TryToInstantiateXRPrefab();
            }
        }

        private async Task HandleMRPackageInstall()
        {
            if (_installationType == InstallationType.Automatic)
            {
                ChangeBuildTarget(BuildTarget.Android);
                await InitializeURPSetup();
                await InitializeXRManagementSetup();
                await InitializeOculusXRSetup();
                InstallConvaiPackage(URP_CONVERTER_PACKAGE_PATH);
                await InitializeMetaXRSDKAllSetup();
                InstallConvaiPackage(MR_PACKAGE_PATH);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            else
            {
                InstallConvaiPackage(XR_PACKAGE_PATH);
                await TryToInstantiateXRPrefab();
            }
        }

        #endregion

        #region Package Initialization

        private async Task InitializePackageSetup(string packageName, string stepDescription)
        {
            UpdateProgressBar(stepDescription);
            if (IsPackageInstalled(packageName))
            {
                IncrementProgress($"{packageName} is already installed. Skipping...");
                return;
            }

            ConvaiLogger.DebugLog($"{packageName} Package Installation Request sent to Package Manager.", ConvaiLogger.LogCategory.Editor);
            AddRequest request = Client.Add(packageName);

            while (!request.IsCompleted)
            {
                await Task.Delay(100);
            }

            if (request.Status == StatusCode.Success)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                IncrementProgress($"Successfully installed: {packageName}");
            }
            else
            {
                ConvaiLogger.Error($"Failed to install {packageName}: {request.Error.message}", ConvaiLogger.LogCategory.Editor);
            }
        }

        private Task InitializeURPSetup() => InitializePackageSetup("com.unity.render-pipelines.universal", "Installing Universal Render Pipeline (URP)");
        private Task InitializeARCoreSetup() => InitializePackageSetup("com.unity.xr.arcore@5.1.4", "Installing ARCore");
        private Task InitializeARKitSetup() => InitializePackageSetup("com.unity.xr.arkit@5.1.4", "Installing ARKit");
        private Task InitializeOpenXRSetup() => InitializePackageSetup("com.unity.xr.openxr@1.10.0", "Installing OpenXR");
        private Task InitializeOculusXRSetup() => InitializePackageSetup("com.unity.xr.oculus", "Installing Oculus XR Plugin");
        private Task InitializeMetaXRSDKAllSetup() => InitializePackageSetup("com.meta.xr.sdk.all", "Installing Meta XR SDK");
        private Task InitializeXRManagementSetup() => InitializePackageSetup("com.unity.xr.management", "Installing XR Management");
        private Task InitializeXRInteractionToolkitSetup() => InitializePackageSetup("com.unity.xr.interaction.toolkit@2.5.4", "Installing XR Interaction Toolkit");

        #endregion

        #region Package Installation Methods

        private void InstallConvaiPackage(string packagePath)
        {
            string packageName = Path.GetFileNameWithoutExtension(packagePath);
            UpdateProgressBar($"Installing {packageName}");

            ConvaiLogger.DebugLog($"Importing: {packageName}", ConvaiLogger.LogCategory.Editor);

            AssetDatabase.ImportPackage(packagePath, false);

            IncrementProgress($"{packageName} Custom Unity Package Installation Completed.");
        }

        private async Task TryToInstantiateXRPrefab()
        {
            if (_installationType != InstallationType.Manual || _currentSetup == SetupTypes.AR) return;

            ConvaiLogger.DebugLog("Attempting to instantiate XR Prefab...", ConvaiLogger.LogCategory.Editor);

            const int maxAttempts = 30;
            const int delayBetweenAttempts = 1000; // 1 second

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                await DelayWithEditorAvailableCheck(delayBetweenAttempts);

                if (TryInstantiatePrefab(out GameObject prefab))
                {
                    ConvaiLogger.DebugLog($"Convai XR Prefab Instantiated on attempt {attempt + 1}.", ConvaiLogger.LogCategory.Editor);
                    return;
                }
            }

            ConvaiLogger.Error($"Failed to load XR Prefab at path: {XR_PREFAB_PATH} after {maxAttempts} attempts.", ConvaiLogger.LogCategory.Editor);
        }

        private bool TryInstantiatePrefab(out GameObject prefab)
        {
            AssetDatabase.Refresh();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XR_PREFAB_PATH);

            if (prefab != null)
            {
                PrefabUtility.InstantiatePrefab(prefab);
                return true;
            }

            return false;
        }

        private static void TryToDownloadiOSDLL() => iOSDLLDownloader.TryToDownload();

        private async Task<bool> ConfirmARPlatform()
        {
            string[] options = { "Android", "iOS" };
            int choice = await DisplayDialogComplex("Select AR Platform", "Choose the target platform for AR development:", options);

            if (choice == -1 || choice == 1) return false; // User cancelled

            _selectedARPlatform = choice == 0 ? BuildTarget.Android : BuildTarget.iOS;
            return true;
        }

        private async Task<bool> ConfirmInstallationType(SetupTypes setupType)
        {
            string[] options = { "Automatic", "Manual" };
            string message = $"Choose the installation type for {setupType}:\n\n" +
                             "Automatic Installation: All necessary packages will be installed, and your project settings will be changed to prepare for building. This option is suitable for new projects. " +
                             "Using it on existing projects may pose risks and could lead to errors in your current setup.\n\n" +
                             "Manual Installation: Only the Convai XR package will be installed, and your project settings will remain unchanged. This option is suitable for those who already have an existing XR project.";

            int choice = await DisplayDialogComplex($"Select {setupType} Installation Type", message, options);

            if (choice == -1 || choice == 1) return false; // User cancelled

            _installationType = choice == 0 ? InstallationType.Automatic : InstallationType.Manual;

            if (_installationType == InstallationType.Automatic)
            {
                return await ConfirmAutomaticInstallation(setupType);
            }

            return true;
        }

        private async Task<bool> ConfirmAutomaticInstallation(SetupTypes setupType)
        {
            InitializeSetupSteps(setupType);

            string packageList = string.Join("\n", _setupSteps.Select(p => $"- {p}"));

            string message = $"Automatic installation for {setupType} will change your project settings, import the following packages, and perform the following steps: \n\n" +
                             $"{packageList}\n\n" +
                             "* This process cannot be undone.\n\n" +
                             "** Recommended for new projects.\n\n" +
                             "Are you sure you want to proceed with the automatic installation?";

            int choice = await DisplayDialogComplex("Confirm Automatic Installation", message, new[] { "Yes, proceed", "No, cancel" });

            return choice == 0;
        }

        private void ChangeBuildTarget(BuildTarget target)
        {
            UpdateProgressBar($"Changing Build Target to {target}");

            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                ConvaiLogger.DebugLog($"Build Target Platform is being Changed to {target}...", ConvaiLogger.LogCategory.Editor);

                BuildTargetGroup targetGroup = target == BuildTarget.Android ? BuildTargetGroup.Android : BuildTargetGroup.iOS;
                bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);

                if (switched)
                {
                    IncrementProgress($"Build Target changed to {target}");
                }
                else
                {
                    IncrementProgress($"Failed to Change Build Target to {target}");
                }
            }
            else
            {
                IncrementProgress($"Build Target is already set to {target}");
            }
        }

        public void InstallConvaiURPConverter()
        {
            InitializeSetupSteps(SetupTypes.NormalUnityPackage);
            InstallConvaiPackage(URP_CONVERTER_PACKAGE_PATH);
            EditorUtility.ClearProgressBar();
        }
        #endregion

        #region Utility Methods

        private static bool IsPackageInstalled(string packageName) =>
            PackageInfo.GetAllRegisteredPackages().Any(packageInfo => packageInfo.name == packageName);

        private void LockAssemblies()
        {
            if (!_assembliesLocked)
            {
                EditorApplication.LockReloadAssemblies();
                _assembliesLocked = true;
                ConvaiLogger.DebugLog("Assemblies locked for package installation.", ConvaiLogger.LogCategory.Editor);
            }
        }

        private void UnlockAssemblies()
        {
            if (_assembliesLocked)
            {
                EditorApplication.UnlockReloadAssemblies();
                _assembliesLocked = false;
                ConvaiLogger.DebugLog("Assemblies unlocked after package installation.", ConvaiLogger.LogCategory.Editor);
            }
        }

        private async Task DelayWithEditorAvailableCheck(int delayBetweenAttempts)
        {
            await Task.Delay(delayBetweenAttempts);

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ConvaiLogger.DebugLog("Waiting for Unity to finish compiling or updating...", ConvaiLogger.LogCategory.Editor);
                await DelayWithEditorAvailableCheck(delayBetweenAttempts); // Recursive call to wait again
            }
        }

        private Task<int> DisplayDialogComplex(string title, string message, string[] options)
        {
            return Task.FromResult(EditorUtility.DisplayDialogComplex(title, message, options[0], "Cancel", options[1]));
        }

        #endregion

        #region Progress Management

        private void UpdateProgressBar(string stepDescription)
        {
            _currentStepDescription = stepDescription;
            float progress = (float)_currentStep / _totalSetupSteps;
            int progressPercentage = Mathf.RoundToInt(progress * 100);
            string title = $"Convai {_currentSetup} Setup Progress";
            string info = $"Step {_currentStep + 1} of {_totalSetupSteps}: {_currentStepDescription} ({progressPercentage}% Complete)";

            EditorUtility.DisplayProgressBar(title, info, progress);
        }

        private void IncrementProgress(string completedStepDescription)
        {
            _currentStep++;
            ConvaiLogger.DebugLog(completedStepDescription, ConvaiLogger.LogCategory.Editor);
            if (_currentStep < _totalSetupSteps)
            {
                UpdateProgressBar(_setupSteps[_currentStep]);
            }
        }

        #endregion
    }
}
#endif