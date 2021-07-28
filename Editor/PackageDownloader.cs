using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;


namespace BabilinApps.RealSense.ARBackground.Editor
{
/// <summary>
/// Unofficial Package Downloader - that downloads the official RealSense asset package from github 
/// </summary>

public class PackageDownloader : EditorWindow
{
    private enum PackageDownloaderState
    {
        None,
        StartDownload,
        IsDownloading,
        Error,

    }

    private static string _downloadLocation => Path.Combine(Application.temporaryCachePath, "Intel.RealSense.unitypackage");
    private const string GITHUB_DOWNLOAD_URL = "https://github.com/IntelRealSense/librealsense/releases/latest/download/Intel.RealSense.unitypackage";


#region Package Not Found Text
    private const string PACKAGE_NOT_FOUND_TITLE = "RealSense Package Not Found";
    private const string PACKAGE_NOT_FOUND_MESSAGE = "The RealSense package was not not found in your project. Would you like to download it?";
    private const string PACKAGE_NOT_FOUND_OK = "Yes";
    private const string PACKAGE_NOT_FOUND_CANCEL = "No";
    private const string PACKAGE_NOT_FOUND_ALT = "No, Dont't Ask Again";
#endregion

#region Failed To Download Text
    private const string FAILED_TO_DOWNLOAD_TITLE = "Error Downloading Package";
    private const string FAILED_TO_DOWNLOAD_MESSAGE = "Error {0}.\nWould you like to try again?";
    private const string FAILED_TO_DOWNLOAD_OK = "Yes";
    private const string FAILED_TO_DOWNLOAD_CANCEL = "No";
    private const string FAILED_TO_DOWNLOAD_ALT = "Download Manually";

#endregion

#region Progress Bar
    private const string DOWNLOADING_TITLE = "Downloading Package";
    private const string DOWNLOADING_TITLE_MESSAGE = "Downloading the newest version of the RealSenseSDK2.0 Unity Package";
#endregion

    public static bool ShouldOpenDownloadWindow => EditorPrefs.GetInt(EditorPrefKey, 0) <1;
    public static string EditorPrefKey
    {
        get
        {
            var projectKey = PlayerSettings.companyName + "." + PlayerSettings.productName;
            var path = Path.GetFullPath(Application.dataPath);
            return $"REALSENSE_PLUGIN_SEARCH_OPTION_[{projectKey}]-[{path}]";
        }
    }

    private static PackageDownloaderState _packageDownloaderState;

    private static UnityWebRequest _unityWebRequest;


    [MenuItem("RealSense/Download Package From Github")]
    public static void DownloadRealSensePackageMenuItem()
    {

        if (_packageDownloaderState == PackageDownloaderState.None)
        {
            _packageDownloaderState = PackageDownloaderState.StartDownload;
            EditorApplication.update += OnEditorApplicationUpdate;

        }
    }

    static void OnEditorApplicationUpdate()
    {

     
            switch (_packageDownloaderState)
            {
                case PackageDownloaderState.None:
                    EditorApplication.update -= OnEditorApplicationUpdate;
                    break;
                case PackageDownloaderState.StartDownload:
                    StartDownloadState();
                    break;
                case PackageDownloaderState.IsDownloading:
                    IsDownloadingState();
                    break;
                case PackageDownloaderState.Error:
                    ErrorState();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    static void StartDownloadState()
    {
        if (_packageDownloaderState == PackageDownloaderState.StartDownload || _packageDownloaderState == PackageDownloaderState.None)
        {
            EditorUtility.ClearProgressBar();
            _unityWebRequest?.Dispose();
            _unityWebRequest = new UnityWebRequest(GITHUB_DOWNLOAD_URL) {downloadHandler = new DownloadHandlerBuffer(), timeout = 10};
            Debug.Log($"Downloaded File to [{_downloadLocation}]");
            var webRequest = _unityWebRequest.SendWebRequest();

            webRequest.completed += (e) =>
                                    {
                                        if (_unityWebRequest.result != UnityWebRequest.Result.Success)
                                        {
                                            Debug.LogError($"[Unity Web Request Error]: {_unityWebRequest.error} |{_unityWebRequest.result}");
                                            _packageDownloaderState = PackageDownloaderState.Error;
                                        }
                                        else
                                        {
                                            EditorUtility.ClearProgressBar();
                                            // Or retrieve results as binary data
                                            byte[] results = _unityWebRequest.downloadHandler.data;
                                            File.WriteAllBytes(_downloadLocation, results);

                                        }

                                        EditorUtility.ClearProgressBar();
                                        _packageDownloaderState = PackageDownloaderState.None;
                                        AssetDatabase.ImportPackage(_downloadLocation, true);
                                        AssetDatabase.Refresh();
                                        _unityWebRequest.Dispose();
                                    };
            _packageDownloaderState = PackageDownloaderState.IsDownloading;
        }
    }

    static void ErrorState()
    {
        EditorUtility.ClearProgressBar();
        int failedToDownloadOption = EditorUtility.DisplayDialogComplex(FAILED_TO_DOWNLOAD_TITLE, string.Format(FAILED_TO_DOWNLOAD_MESSAGE,_unityWebRequest.error),
                                                                        FAILED_TO_DOWNLOAD_OK, FAILED_TO_DOWNLOAD_CANCEL, FAILED_TO_DOWNLOAD_ALT);

        switch (failedToDownloadOption)
        {
            case 0: //Try Again
                _packageDownloaderState = PackageDownloaderState.StartDownload;
                break;
            case 1: //Stop
                _packageDownloaderState = PackageDownloaderState.None;
                break;
            case 2: //Download Manually
                _packageDownloaderState = PackageDownloaderState.None;
                Help.BrowseURL(GITHUB_DOWNLOAD_URL);
                break;
        }

        _unityWebRequest.Dispose();
    }

    static void IsDownloadingState()
    {
        if (!_unityWebRequest.isDone)
        {
            EditorUtility.DisplayProgressBar(DOWNLOADING_TITLE, DOWNLOADING_TITLE_MESSAGE, _unityWebRequest.downloadProgress);
            return;
        }

       
    }


    /// <summary>
    /// Called when the RealSense assembly is not found by AutoRun.cs
    /// </summary>
    public static void OpenPluginNotFoundOptions()
    {

        var lookForPlugin = EditorPrefs.GetInt(EditorPrefKey, 0);
        if (lookForPlugin < 1)
        {
            int pluginNotFoundOption = EditorUtility.DisplayDialogComplex(PACKAGE_NOT_FOUND_TITLE, PACKAGE_NOT_FOUND_MESSAGE,
                                                                          PACKAGE_NOT_FOUND_OK, PACKAGE_NOT_FOUND_CANCEL, PACKAGE_NOT_FOUND_ALT);
            switch (pluginNotFoundOption)
            {
                case 0: //Download
                    if (_packageDownloaderState == PackageDownloaderState.None)
                    {
                        _packageDownloaderState = PackageDownloaderState.StartDownload;
                        EditorApplication.update += OnEditorApplicationUpdate;
                    }
                    break;
                case 1: //Close
                    break;
                case 2: //Close, Dont't Ask Again
                    EditorPrefs.SetInt(EditorPrefKey, 1);
                    break;
                
            }
        }
    }




    }
}