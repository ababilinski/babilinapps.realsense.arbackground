using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace BabilinApps.RealSense.Downloader.Editor
{

    /// <summary>
    /// Runs when the asset database reloads to check if the Intel RealSense package has been imported
    /// </summary>
    [InitializeOnLoad]
    internal static class Autorun
    {
        private const string REALSENSE_DLL_PATH_KEY = "REALSENSE_DLL_PATH";
        private const string REALSENSE_DEFINES = "REALSENSE";
        private static bool cachedDllPathLoaded = false;
        private static bool cachedDllPathEmpty = true;
        private static string _dllPath = "";



        /// <summary>
        /// Check if the current define symbols contain a definition
        /// </summary>
        public static bool ContainsDefineSymbol(string symbol)
        {
            string definesString =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            List<string> allDefines = definesString.Split(';').ToList();
            return allDefines.Contains(symbol);
        }


        private static string EditorPrefKey
        {
            get
            {
                var projectKey = PlayerSettings.companyName + "." + PlayerSettings.productName;
                var path = Path.GetFullPath(Application.dataPath);
                return $"{REALSENSE_DLL_PATH_KEY}_[{projectKey}]-[{path}]";
            }
        }
        /// <summary>
        /// Add define symbol as soon as Unity gets done compiling.
        /// </summary>
        public static void AddDefineSymbol(string symbol)
        {
            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            List<string> allDefines = definesString.Split(';').ToList();
            if (allDefines.Contains(symbol))
            {
                Debug.LogWarning($"Add Defines Ignored. Symbol [{symbol}] already exists.");
                return;
            }

            allDefines.Add(symbol);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                                                             string.Join(";", allDefines.ToArray()));
        }

        /// <summary>
        /// Remove define symbol as soon as Unity gets done compiling.
        /// </summary>
        public static void RemoveDefineSymbol(string symbol)
        {
            string definesString =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            List<string> allDefines = definesString.Split(';').ToList();
            if (!allDefines.Contains(symbol))
            {
                Debug.LogWarning($"Remove Defines Ignored. Symbol [{symbol}] does not exists.");

            }
            else
            {
                allDefines.Remove(symbol);

                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                                                                 string.Join(";", allDefines.ToArray()));
            }

        }

        static void OnEditorApplicationUpdate()
        {
          
            var editorIsBusy = (AssetDatabase.IsAssetImportWorkerProcess() || EditorApplication.isCompiling || EditorApplication.isUpdating);
            Debug.Log("editorIsBusy: " + editorIsBusy);
            if (editorIsBusy)
            {
                return;
            }

            AskToDownload();
            EditorApplication.update -= OnEditorApplicationUpdate;
        }

        static void AskToDownload()
        {
            if (!PackageDownloader.ShouldOpenDownloadWindow)
            {
                return;
            }
            if (!cachedDllPathLoaded)
            {

                _dllPath = EditorPrefs.GetString(EditorPrefKey, "");
                if (!string.IsNullOrWhiteSpace(_dllPath))
                {
                    cachedDllPathEmpty = false;
                }

                cachedDllPathLoaded = true;
            }

            bool hasFileAndSymbol = false;
            if (!cachedDllPathEmpty && File.Exists(_dllPath))
            {
                if (!ContainsDefineSymbol(REALSENSE_DEFINES))
                {
                    string[] files = Directory.GetFiles(Application.dataPath, "*.dll", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        if (file.Contains("RealSense.dll"))
                        {
                            _dllPath = file;
                            hasFileAndSymbol = true;
                            AddDefineSymbol(REALSENSE_DEFINES);
                            break;
                        }
                    }
                }
                else
                {
                    hasFileAndSymbol = true;
                }
            }
            else
            {
                if (ContainsDefineSymbol(REALSENSE_DEFINES))
                {
                    RemoveDefineSymbol(REALSENSE_DEFINES);
                }

                string[] files = Directory.GetFiles(Application.dataPath, "*.dll", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (file.Contains("RealSense.dll"))
                    {
                        _dllPath = file;
                        hasFileAndSymbol = true;
                        AddDefineSymbol(REALSENSE_DEFINES);
                        break;
                    }
                }
            }

            if (hasFileAndSymbol)
            {
                EditorPrefs.SetString(EditorPrefKey, _dllPath);
                cachedDllPathEmpty = false;

            }
            else
            {
                PackageDownloader.OpenPluginNotFoundOptions();
            }
        }

        static Autorun()
        {

            EditorApplication.update += OnEditorApplicationUpdate;

        }


    }

}