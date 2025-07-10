using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Enbug.Billing.Editor
{
    public static class EnbugBillingEditor
    {
        private class LibraryInfo
        {
            public string[] PluginPaths;
        }

        private const string ConfigPath = "Assets/Resources/enbug_billing.asset";
        private const string AndroidLibraryPath = "Packages/io.enbug.billing/Plugins/Android";

        public static PlatformInfo CurrentPlatformInfo
        {
            get
            {
                var config = AssetDatabase.LoadAssetAtPath<PlatformInfo>(ConfigPath);
                if (!config)
                {
                    config = ScriptableObject.CreateInstance<PlatformInfo>();
                    var dir = Path.GetDirectoryName(ConfigPath)!;
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                        AssetDatabase.Refresh();
                    }

                    AssetDatabase.CreateAsset(config, ConfigPath);
                }

                return config;
            }
        }

        private static readonly Dictionary<AppStore, LibraryInfo> AndroidLibraries = new()
        {
            [AppStore.GooglePlay] = new LibraryInfo
            {
                PluginPaths = new[] { "GooglePlayBillingWrapper.androidlib" },
            },
        };

        public static void SetAppStore(AppStore appStore)
        {
            var config = CurrentPlatformInfo;

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                appStore = AppStore.AppleAppStore;
            }
            else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                if (!AndroidLibraries.ContainsKey(appStore))
                {
                    appStore = AppStore.GooglePlay;
                }
            }
            else
            {
                appStore = AppStore.Unknown;
            }

            config.appStore = appStore;

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                foreach (var (mapping, libraryInfo) in AndroidLibraries)
                {
                    var enabled = mapping == appStore;

                    foreach (var aarPath in libraryInfo.PluginPaths)
                    {
                        var aarFullPath = Path.Combine(AndroidLibraryPath, aarPath);
                        var importer = (AssetImporter.GetAtPath(aarFullPath) as PluginImporter)!;
                        importer.SetCompatibleWithPlatform(BuildTarget.Android, enabled);
                        importer.SaveAndReimport();
                    }
                }
            }

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}