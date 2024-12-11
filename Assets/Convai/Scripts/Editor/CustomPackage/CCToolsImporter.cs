#if !CC_TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Rendering;

namespace Convai.Scripts.Editor.CustomPackage
{
    [InitializeOnLoad]
    public static class CCToolsImporter
    {
        private const string CC_TOOLS_SYMBOL = "CC_TOOLS";
        private const string URP_URL = "https://github.com/soupday/cc_unity_tools_URP.git";
        private const string HDRP_URL = "https://github.com/soupday/cc_unity_tools_HDRP.git";
        private const string BASE_URL = "https://github.com/soupday/cc_unity_tools_3D.git";
        private const int MAX_RETRY_ATTEMPTS = 5;
        private static int _retryCount = 0;

        static CCToolsImporter()
        {
            EditorApplication.update += CheckAndInstallCCTools;
        }

        private static void CheckAndInstallCCTools()
        {
            // Ensure we only attempt to check/install once
            EditorApplication.update -= CheckAndInstallCCTools;

            // Check if the package is already installed
            var listRequest = Client.List(true);
            while (!listRequest.IsCompleted) { }

            if (IsPackageAlreadyInstalled(listRequest))
            {
                Debug.Log("CC Tools package is already installed. Adding define symbol.");
                AddCCToolsDefineSymbol();
                return;
            }

            // If not installed, proceed with installation
            TryInstallCCTools();
        }

        private static bool IsPackageAlreadyInstalled(ListRequest listRequest)
        {
            return listRequest.Status == StatusCode.Success &&
                   listRequest.Result.Any(package => package.packageId.Contains("cc_unity_tools"));
        }

        private static void TryInstallCCTools()
        {
            // Determine appropriate package URL based on render pipeline
            string packageUrl = DeterminePackageUrl();

            // Start package installation asynchronously
            AddRequest addRequest = Client.Add(packageUrl);

            // Use a coroutine-friendly approach to handle installation
            EditorApplication.update += () => HandlePackageInstallation(addRequest);
        }

        private static void HandlePackageInstallation(AddRequest addRequest)
        {
            // Check if the request is not completed
            if (!addRequest.IsCompleted)
            {
                return;
            }

            // Remove the update method to prevent multiple executions
            EditorApplication.update -= () => HandlePackageInstallation(addRequest);

            switch (addRequest.Status)
            {
                case StatusCode.Success:
                    Debug.Log("CC Tools has been installed successfully.");
                    AddCCToolsDefineSymbol();

                    if (DeterminePackageUrl() == URP_URL)
                    {
                        ConvaiCustomPackageInstaller convaiCustomPackageInstaller = new ConvaiCustomPackageInstaller();
                        convaiCustomPackageInstaller.InstallConvaiURPConverter();
                    }
                    break;
                case StatusCode.Failure:
                    HandleInstallationFailure(addRequest);
                    break;
                case StatusCode.InProgress:
                    Debug.Log("CC Tools installation is in progress.");
                    break;
                default:
                    Debug.LogWarning("Unexpected status during CC Tools installation.");
                    break;
            }
        }

        private static void HandleInstallationFailure(AddRequest addRequest)
        {
            Debug.LogError($"CC Tools installation failed: {addRequest.Error.message}");

            // Implement a retry mechanism
            if (_retryCount < MAX_RETRY_ATTEMPTS)
            {
                _retryCount++;
                Debug.Log($"Retrying CC Tools installation. Attempt {_retryCount} of {MAX_RETRY_ATTEMPTS}");

                // Wait a short time before retrying to reduce chances of conflict
                EditorApplication.delayCall += () => { TryInstallCCTools(); };
            }
            else
            {
                Debug.LogError("Maximum retry attempts reached. CC Tools installation failed.");
            }
        }

        private static string DeterminePackageUrl()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
                return BASE_URL;

            string renderPipelineName = GraphicsSettings.currentRenderPipeline.GetType().Name;

            return renderPipelineName == "UniversalRenderPipelineAsset"
                ? URP_URL
                : HDRP_URL;
        }

        private static void AddCCToolsDefineSymbol()
        {
            foreach (BuildTarget target in Enum.GetValues(typeof(BuildTarget)))
            {
                BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);

                if (group == BuildTargetGroup.Unknown)
                    continue;

                NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(group);
                List<string> symbols = PlayerSettings.GetScriptingDefineSymbols(namedTarget)
                    .Split(';')
                    .Select(d => d.Trim())
                    .ToList();

                if (!symbols.Contains(CC_TOOLS_SYMBOL))
                    symbols.Add(CC_TOOLS_SYMBOL);

                PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", symbols.ToArray()));
            }
        }
    }
}
#endif