using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{
    public class BuilderPreference
    {
        public const string DEFAULT_CONFIG_NAME = "Assets/ABConfig.asset";
        public const string VARIANT_V1 = "ab";
        public const string VARIANT_UNTIY3D = "unity3d";

        private static string tempAssetPath;
        private static string assetsPath;
        private static string versionPath;
        private static string buildPath;
        private static string streamAssetPath;

        //排除文件
        public static HashSet<string> ExcludeFiles = new HashSet<string>{ ".cs", ".dll" , ".ttf"};
        /// <summary>
        /// 生成映射文件目录,在treeview加一个标识
        /// </summary>
        public static HashSet<string> BundleMapFile = new HashSet<string>()
        {
            "Assets/Res","Assets/Scenes/Level"
        }; 

        public static BuildAssetBundleOptions BuildBundleOptions = BuildAssetBundleOptions.DeterministicAssetBundle | 
                                                             BuildAssetBundleOptions.UncompressedAssetBundle;

        public static string BUILD_PATH
        {
            get
            {
                if (string.IsNullOrEmpty(buildPath))
                {
                    buildPath = "Assets/AssetBundle/" + PlatformTargetFolder;
                    buildPath = buildPath.Replace("\\", "/");
                }
                return buildPath;
            }
        }


        public static string TEMP_ASSET_PATH
        {
            get
            {
                if (string.IsNullOrEmpty(tempAssetPath))
                {
                    tempAssetPath = Path.Combine(Application.dataPath, "../../tempAssets/" + PlatformTargetFolder);
                    tempAssetPath = Path.GetFullPath(tempAssetPath).Replace("\\", "/");
                }
                return tempAssetPath;
            }
        }


        public static string ASSET_PATH
        {
            get
            {
                if (string.IsNullOrEmpty(assetsPath))
                {
                    assetsPath = Path.Combine(Application.dataPath, "../../assets/" + PlatformTargetFolder);
                    assetsPath = Path.GetFullPath(assetsPath).Replace("\\", "/");
                }
                return assetsPath;
            }
        }


        public static string VERSION_PATH
        {
            get
            {
                if (string.IsNullOrEmpty(versionPath))
                {
                    versionPath = ASSET_PATH + "/version";
                }
                return versionPath;
            }
        }


        public static string StreamingAssetsPlatormPath
        {
            get
            {
                if (string.IsNullOrEmpty(streamAssetPath))
                {
                    streamAssetPath = "Assets/StreamingAssets/" + PlatformTargetFolder;
                    streamAssetPath = streamAssetPath.Replace("\\", "/");
                }
                return streamAssetPath;
            }
        }

        public static string PlatformTargetFolder
        {
            get
            {
                if (EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
                    return "Android";
                if (EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
                    return "iOS";
                return "Win";
            }
        }

        public static string AppExtension
        {
            get
            {
                if (EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
                    return ".apk";
                if (EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
                    return ".ipa";
                return ".exe";
            }
        }
    }
}