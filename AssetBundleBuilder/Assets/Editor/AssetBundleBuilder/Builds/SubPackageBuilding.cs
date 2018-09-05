using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace AssetBundleBuilder
{
    /// <summary>
    /// 分包资源处理
    /// </summary>
    public class SubPackageBuilding : PackageBuilding
    {
        public SubPackageBuilding(bool buildApp) : base(buildApp)
        {
        }

        public override IEnumerator OnBuilding()
        {
            //打包测试包
            CopyPackableFiles();
            yield return null;

            CompressWithZSTD(1024 * 1024 * 10);


            Builder.AddBuildLog("<SubPackage Building>Compress Zstd Finished...");

            yield return null;

            if (isBuildApp)
            {
                BuildApp(false, false);

                Builder.AddBuildLog("<SubPackage Building>Build App  Finished...");                
            }
        }

        /// <summary>
        /// 复制分包资源到StreamingAssets目录下
        /// </summary>
        void CopyPackableFiles()
        {
            string targetPath = BuilderPreference.StreamingAssetsPlatormPath;
            if (Directory.Exists(targetPath)) Directory.Delete(targetPath, true);
            Directory.CreateDirectory(targetPath);

            string bundlePath = BuilderPreference.BUILD_PATH;

            //拷贝StreamAsset目录中的资源
            AssetBuildRule[] rules = AssetBuildRuleManager.Instance.Rules;
            Dictionary<string, AssetBuildRule> ruleMap = new Dictionary<string, AssetBuildRule>();

            for (int i = 0; i < rules.Length; i++)
            {
                List<AssetBuildRule> ruleList = rules[i].TreeToList();
                for (int j = 0; j < ruleList.Count; j++)
                {
                    ruleMap[ruleList[j].AssetBundleName] = ruleList[j];
                }
            }

            //只拷贝整包类型的文件
            foreach (AssetBuildRule bundleRule in ruleMap.Values)
            {
                if (bundleRule.PackageType != PackageAssetType.InPackage) continue;

                string assetBundleName = BuildUtil.FormatBundleName(bundleRule);

                string buildBundlePath = string.Concat(bundlePath, "/", assetBundleName, BuilderPreference.VARIANT_V1);

                if (!File.Exists(buildBundlePath)) continue;

                string streamBundlePath = string.Concat(targetPath, "/", assetBundleName, BuilderPreference.VARIANT_V1);

                BuildUtil.SwapPathDirectory(streamBundlePath);

                File.Copy(buildBundlePath, streamBundlePath);
            }

            Action<List<string>, string> copyFiles = (filePaths, rootPath) =>
            {
                for (int i = 0; i < filePaths.Count; i++)
                {
                    string relativePath = filePaths[i];
                    string streamBundlePath = relativePath.Replace(rootPath, targetPath);

                    BuildUtil.SwapPathDirectory(streamBundlePath);

                    File.Copy(relativePath, streamBundlePath);
                }
            };

            HashSet<string> includeExtensions = new HashSet<string>() { ".ab", ".unity3d", ".txt", ".conf", ".pb", ".bytes" };
            //拷贝bundle配置目录的配置文件
            string[] copyTargetPaths = new[]
            {
                string.Concat(bundlePath, "/files.txt"),
                string.Concat(bundlePath , "/bundlemap.ab")
            };
            List<string> files = new List<string>(copyTargetPaths);
            copyFiles(files, bundlePath);

            //拷贝Lua目录代码
            string luaBundlePath = string.Concat(bundlePath, "/lua");
            files = BuildUtil.SearchIncludeFiles(luaBundlePath, SearchOption.AllDirectories, includeExtensions);
            copyFiles(files, luaBundlePath);
            
            Builder.AddBuildLog("<Sub Package Building>Copy sub package files ...");

            AssetDatabase.Refresh();
        }
    }
}