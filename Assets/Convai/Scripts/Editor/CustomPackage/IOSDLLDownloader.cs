using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using Convai.Scripts.Runtime.LoggerSystem;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Convai.Scripts.Editor.CustomPackage
{
    /// <summary>
    ///     Editor window for downloading and extracting iOS DLL from a specified URL.
    /// </summary>
    public class iOSDLLDownloader
    {
        private const string DOWNLOAD_ENDPOINT_URL = "https://api.convai.com/user/downloadAsset";
        private const string RELATIVE_PATH = "Convai/Plugins/gRPC/Grpc.Core/runtimes";
        private static string _targetDirectory;

        /// <summary>
        ///     Attempts to download the iOS DLL if it doesn't already exist.
        /// </summary>
        public static void TryToDownload()
        {
            if (CheckFileExistence()) return;
            ConvaiLogger.DebugLog("<color=lime>The iOS DLL download has started...</color>", ConvaiLogger.LogCategory.Editor);
            DownloadAndExtract(GetTargetDirectory());
        }

        /// <summary>
        ///     Coroutine to download and extract the ZIP file from the specified URL.
        /// </summary>
        /// <param name="outputPath">Directory to extract the contents to.</param>
        /// <returns></returns>
        private static void DownloadAndExtract(string outputPath)
        {
            try
            {
                string downloadURL = GetDownloadURL();

                if (downloadURL == null)
                    ConvaiLogger.Error("Failed to get download URL. Please check the API key and try again.", ConvaiLogger.LogCategory.Editor);

                using UnityWebRequest webRequest = UnityWebRequest.Get(downloadURL);
                webRequest.SendWebRequest();

                while (!webRequest.isDone)
                {
                    float progress = webRequest.downloadProgress;
                    EditorUtility.DisplayProgressBar("Downloading required iOS DLL...",
                        "Please wait for the download to finish and do not close Unity. " + (int)(progress * 100) + "%", progress);
                }

                EditorUtility.ClearProgressBar();

                if (webRequest.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
                {
                    ConvaiLogger.Error($"Error downloading file: {webRequest.error}", ConvaiLogger.LogCategory.Editor);
                }
                else
                {
                    byte[] results = webRequest.downloadHandler.data;
                    string zipPath = Path.Combine(Path.GetTempPath(), "downloaded.zip");
                    File.WriteAllBytes(zipPath, results);
                    ExtractZipFile(zipPath, outputPath);
                    File.Delete(zipPath);
                    ConvaiLogger.Info($"Downloaded and extracted to {outputPath}" + "/ios/libgrpc.a", ConvaiLogger.LogCategory.Editor);

                    // Refresh the asset database to make sure the new files are recognized
                    AssetDatabase.Refresh();
                }
            }
            catch (Exception e)
            {
                ConvaiLogger.Error(e.Message, ConvaiLogger.LogCategory.Editor);
                throw;
            }
        }

        /// <summary>
        ///     Retrieves the download URL from Convai API.
        /// </summary>
        /// <returns>The download URL or null.</returns>
        private static string GetDownloadURL()
        {
            if (!ConvaiAPIKeySetup.GetAPIKey(out string apiKey)) return null;

            string body = @"{""service_name"": ""unity-builds"",""version"":""ios""}";

            WebRequest request = WebRequest.Create(DOWNLOAD_ENDPOINT_URL);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("CONVAI-API-KEY", apiKey);

            using (StreamWriter streamWriter = new(request.GetRequestStream()))
            {
                streamWriter.Write(body);
            }

            using (WebResponse response = request.GetResponse())
            using (Stream dataStream = response.GetResponseStream())
            using (StreamReader reader = new(dataStream))
            {
                JObject responseJson = JObject.Parse(reader.ReadToEnd());
                return (string)responseJson["download_link"];
            }
        }

        /// <summary>
        ///     Extracts the contents of a ZIP file to the specified output folder.
        /// </summary>
        /// <param name="zipFilePath">Path to the ZIP file.</param>
        /// <param name="outputFolder">Directory to extract the contents to.</param>
        private static void ExtractZipFile(string zipFilePath, string outputFolder)
        {
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
            {
                float totalEntries = archive.Entries.Count;
                float currentEntry = 0;

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string fullPath = Path.Combine(outputFolder, entry.FullName);

                    // Ensure the directory exists
                    string directoryName = Path.GetDirectoryName(fullPath);
                    if (!Directory.Exists(directoryName))
                        if (directoryName != null)
                            Directory.CreateDirectory(directoryName);

                    // Extract the entry to the output directory
                    entry.ExtractToFile(fullPath, true);

                    // Update the progress bar
                    currentEntry++;
                    float progress = currentEntry / totalEntries;
                    EditorUtility.DisplayProgressBar("Extracting", $"Extracting file {entry.Name}...", progress);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        ///     Gets the target directory for extracting the files.
        /// </summary>
        /// <returns>Target directory path.</returns>
        private static string GetTargetDirectory()
        {
            _targetDirectory = Path.Combine(Application.dataPath, RELATIVE_PATH);
            if (!Directory.Exists(_targetDirectory)) Directory.CreateDirectory(_targetDirectory);
            return _targetDirectory;
        }

        /// <summary>
        ///     Checks if the iOS DLL file already exists.
        /// </summary>
        /// <returns>True if the file exists, otherwise false.</returns>
        private static bool CheckFileExistence()
        {
            string fullPath = Path.Combine(Application.dataPath, RELATIVE_PATH + "/ios/libgrpc.a");
            bool fileExists = File.Exists(fullPath);
            if (fileExists) ConvaiLogger.DebugLog("iOS DLL already exists. No need to download.", ConvaiLogger.LogCategory.Editor);

            return fileExists;
        }
    }
}