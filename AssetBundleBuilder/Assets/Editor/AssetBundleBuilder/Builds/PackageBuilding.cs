using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Riverlake.Crypto;
using UnityEditor;
using UnityEngine;
using ZstdNet;

namespace AssetBundleBuilder
{
    /// <summary>
    /// 资源分包阶段，打包资源完成后，将不同资源拷贝到对应包体对应的目录
    /// </summary>
    public class PackageBuilding : ABuilding
    {
        private int packageBuildings = -1;

        public PackageBuilding(int packages) : base(20)
        {
            packageBuildings = packages;
        }

        /// <summary>
        /// 是否激活对应类型的生成
        /// </summary>
        /// <returns></returns>
        private bool IsPackageBuilding(PackageBuildings building)
        {
            return (packageBuildings & (int)building) != 0;
        }

        public override IEnumerator OnBuilding()
        {
            if (IsPackageBuilding(PackageBuildings.SubPackage))
            {
                //打包测试包
                
                CopyPackableFiles();

                if(IsPackageBuilding(PackageBuildings.BuildApp))
                    BuildApp(false, false);
            }

            yield return null;

            bool isBuildFullPackage = IsPackageBuilding(PackageBuildings.FullPackage);
            if (isBuildFullPackage)
            {
                CopyAllBundles();

                if (IsPackageBuilding(PackageBuildings.BuildApp))
                {
                    bool isForceUpdatePackage = IsPackageBuilding(PackageBuildings.ForceUpdate);
                    string appPath = BuildApp(true, isForceUpdatePackage);

                    EditorUtility.RevealInFinder(appPath);

                    ResetConfig();
                }
            }
        }

        /// <summary>
        /// 复制分包资源到StreamingAssets目录下
        /// </summary>
        void CopyPackableFiles()
        {
            string targetPath = BuilderPreference.StreamingAssetsPlatormPath;
            if (Directory.Exists(targetPath))   Directory.Delete(targetPath, true);
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
                if(bundleRule.PackageType != PackageAssetType.InPackage)  continue;

                string assetBundleName = BuildUtil.FormatBundleName(bundleRule);

                string buildBundlePath = string.Concat(bundlePath,"/", assetBundleName , BuilderPreference.VARIANT_V1);

                if(!File.Exists(buildBundlePath))   continue;

                string streamBundlePath = string.Concat(targetPath, "/", assetBundleName, BuilderPreference.VARIANT_V1);

                BuildUtil.SwapPathDirectory(streamBundlePath);

                File.Copy(buildBundlePath , streamBundlePath);
            }

            Action<List<string>, string> copyFiles = (filePaths , rootPath) =>
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
            string bundleConfigPath = string.Concat(bundlePath, "/bundles");
            List<string> files = BuildUtil.SearchIncludeFiles(bundleConfigPath, SearchOption.AllDirectories, includeExtensions);
            copyFiles(files, bundleConfigPath);

            //拷贝Lua目录代码
            string luaBundlePath = string.Concat(bundlePath, "/lua");
            files = BuildUtil.SearchIncludeFiles(luaBundlePath, SearchOption.AllDirectories, includeExtensions);
            copyFiles(files, luaBundlePath);

            AssetDatabase.Refresh();

            Builder.AddBuildLog("[end]Copy sub package files ...");
            CompressWithZSTD(1024 * 1024 * 10);
        }

        private void CopyAllBundles()
        {
            string targetPath = BuilderPreference.StreamingAssetsPlatormPath;
            if (Directory.Exists(targetPath))   Directory.Delete(targetPath, true);
            Directory.CreateDirectory(targetPath);
            
            string buildPath = BuilderPreference.BUILD_PATH;
            HashSet<string> withExtensions = new HashSet<string>() { ".ab", ".unity3d", ".txt", ".conf", ".pb", ".bytes" };
            List<string> files = BuildUtil.SearchIncludeFiles(buildPath, SearchOption.AllDirectories , withExtensions);

            Builder.AddBuildLog("Copy all bundle ...");
//            int buildPathLength = buildPath.Length + 1; 
            for (int i = 0; i < files.Count; ++i)
            {
                string fileName = Path.GetFileName(files[i]);
                if (fileName == "tempsizefile.txt" || fileName == "luamd5.txt") continue;
                //ABPackHelper.ShowProgress("Copying files...", (float)i / (float)files.Length);
                
                string streamBundlePath = files[i].Replace(buildPath, targetPath);

                BuildUtil.SwapPathDirectory(streamBundlePath);
                
                File.Copy(files[i], streamBundlePath);
            }
            AssetDatabase.Refresh();
//            ABPackHelper.ShowProgress("", 1);
            CompressWithZSTD(1024 * 1024 * 5);
        }

        /// <summary>
        /// 压缩StreamAsset目录的资源
        /// </summary>
        /// <param name="maxFileSize"></param>
        void CompressWithZSTD(long maxFileSize)
        {
            string outPutPath = BuilderPreference.StreamingAssetsPlatormPath;
//            ABPackHelper.ShowProgress("Hold on...", 0);
            var dirInfo = new DirectoryInfo(outPutPath);
            var dirs = dirInfo.GetDirectories();
            Dictionary<int, List<string>> allFiles = new Dictionary<int, List<string>>();
            // data原始包控制在10M左右
            long curSize = 0;
            int tmpIndex = 0;
            for (int i = 0; i < dirs.Length; ++i)
            {
                if (dirs[i].Name == "lua") continue;
                var abFileInfos = BuildUtil.SearchFiles(dirs[i].FullName , SearchOption.AllDirectories);
                for (int j = 0; j < abFileInfos.Count; ++j)
                {
                    var relativePath = abFileInfos[j];
                    var data = new FileInfo(relativePath);
                    if (data.Length >= maxFileSize)
                    {
                        curSize = 0;
                        tmpIndex++;
                    }
                    else if (curSize >= maxFileSize)
                    {
                        curSize = 0;
                        tmpIndex++;
                    }

                    if (curSize == 0)
                        allFiles.Add(tmpIndex, new List<string>());

                    allFiles[tmpIndex].Add(relativePath);
                    curSize += data.Length;
                }
            }
            int index = 0;

            // 合并生成的bundle文件，合成10M左右的小包(二进制)
            int pathLength = outPutPath.Length + 1;
            foreach (var key in allFiles.Keys)
            {
                var tmpName = "data" + key;
#if UNITY_IOS
            tmpName = IOSGenerateHelper.RenameResFileWithRandomCode(tmpName);
#endif
                var savePath = string.Format("{0}/{1}.tmp", outPutPath, tmpName);
//                ABPackHelper.ShowProgress("Streaming data...", (float)index++ / (float)allFiles.Count);
                using (var fs = new FileStream(savePath, FileMode.CreateNew))
                {
                    using (var writer = new BinaryWriter(fs))
                    {
                        for (int i = 0; i < allFiles[key].Count; ++i)
                        {
                            var bytes = File.ReadAllBytes(allFiles[key][i]);
                            var abName = allFiles[key][i].Substring(pathLength);
                            writer.Write(abName);
                            writer.Write(bytes.Length);
                            writer.Write(bytes);
                        }
                    }
                }
            }
//            ABPackHelper.ShowProgress("Finished...", 1);
            for (int i = 0; i < dirs.Length; ++i)
            {
                if (dirs[i].Name == "lua") continue;
                Directory.Delete(dirs[i].FullName, true);
            }
            AssetDatabase.Refresh();

            // 对合并后的文件进行压缩
            Builder.AddBuildLog("compress with zstd...");
            var pakFiles = Directory.GetFiles(outPutPath, "*.tmp", SearchOption.AllDirectories);
            for (int i = 0; i < pakFiles.Length; ++i)
            {
                var savePath = string.Format("{0}/{1}.bin", outPutPath, Path.GetFileNameWithoutExtension(pakFiles[i]));
//                ABPackHelper.ShowProgress("compress with zstd...", (float)i / (float)pakFiles.Length);
                var fileName = BuildUtil.RelativePaths(pakFiles[i]);
                using (var compressFs = new FileStream(savePath, FileMode.CreateNew))
                {
                    using (var compressor = new Compressor(new CompressionOptions(CompressionOptions.MaxCompressionLevel)))
                    {
                        var bytes = compressor.Wrap(File.ReadAllBytes(fileName));
#if UNITY_IOS
                        bytes = Crypto.Encode(bytes);
#endif
                        compressFs.Write(bytes, 0, bytes.Length);
                    }
                }
                File.Delete(fileName);
            }
            AssetDatabase.Refresh();
            
            // 生成包体第一次进入游戏解压缩配置文件
            StringBuilder builder = new StringBuilder();
            List<string> allfiles = BuildUtil.SearchFiles(outPutPath, SearchOption.AllDirectories);
            for (int i = 0; i < allfiles.Count; ++i)
            {
                if (allfiles[i].EndsWith("datamap.ab")) continue;

                var relativePath = allfiles[i].Substring(pathLength);
                string md5 = MD5.ComputeHashString(allfiles[i]);

                builder.AppendLine(string.Format("{0}|{1}" ,relativePath , md5));
            }

            var packFlistPath = outPutPath + "/packlist.txt";
            File.WriteAllText(packFlistPath, builder.ToString());
//            AssetDatabase.Refresh();

            Builder.AddBuildLog("Compress Zstd Finished...");
        }


        private string BuildApp(bool packAllRes, bool forceUpdate)
        {
            Builder.AddBuildLog("Build App Start !...");
            var option = BuildOptions.None;
            if (Builder.IsDebug) option |= BuildOptions.AllowDebugging;
            if (Builder.IsBuildDev) option |= BuildOptions.Development;
            if (Builder.IsAutoConnectProfile) option |= BuildOptions.ConnectWithProfiler;
            
            string dir = Path.GetDirectoryName(Builder.ApkSavePath);
            string fileName = Path.GetFileNameWithoutExtension(Builder.ApkSavePath);
            string time = DateTime.Now.ToString("yyyyMMdd");
            string flag = string.Empty;

            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            string final_path = string.Empty;

            if (buildTarget != BuildTarget.iOS)
            {
                SDKConfig curSdkConfig = Builder.CurrentConfigSDK;
                for (int i = 0; i < curSdkConfig.items.Count; i++)
                {
                    var item = Builder.CurrentConfigSDK.items[i];

                    BuildOptions targetOptions = option;
                    if (item.development == 1)
                    {
                        targetOptions |= BuildOptions.Development;
                        flag = packAllRes ? "_allpack_dev_v" : "_subpack_dev_v";
                    }
                    else if (item.use_sdk == 1)
                        flag = packAllRes ? "_allpack_sdk_v" : "_subpack_sdk_v";
                    else
                        flag = packAllRes ? "_allpack_test_v" : "_subpack_test_v";

                    
                    if (buildTarget == BuildTarget.Android)
                    {
                        final_path = string.Concat(dir,"/", fileName, "_" ,time, flag, Builder.GameVersion.ToString(), ".apk");
                        if (File.Exists(final_path)) File.Delete(final_path);
                        // 写入并保存sdk启用配置
//                        item.CopyConfig();
                        item.CopySDK();
                        item.SetPlayerSetting(curSdkConfig.splash_image);
                        item.SaveSDKConfig();
                        //item.SplitAssets(sdkConfig.split_assets);
                        if (item.update_along == 0 && forceUpdate)
                        {
                            if (Directory.Exists(Application.streamingAssetsPath))
                                Directory.Delete(Application.streamingAssetsPath, true);
                        }
                    }
                    else if (buildTarget == BuildTarget.StandaloneWindows64 || buildTarget == BuildTarget.StandaloneWindows)
                    {
                        final_path = string.Concat(dir, "/", fileName, "_", time, flag, Builder.GameVersion.ToString(), ".exe");
                        if (Directory.Exists(final_path)) Directory.Delete(final_path, true);

//                        item.CopyConfig();
                    }
                    AssetDatabase.Refresh();

                    BuildUtil.SwapPathDirectory(final_path);

                    BuildPipeline.BuildPlayer(GetBuildScenes(), final_path, buildTarget, targetOptions);
                    item.ClearSDK();
                }

            }
            else if (buildTarget == BuildTarget.iOS)
            {
                // 在上传目录新建一个ios_check.txt文件用于判断当前包是否出于提审状态
                string checkFile = BuilderPreference.ASSET_PATH + "/ios_check.txt";
                if (File.Exists(checkFile)) File.Delete(checkFile);
                File.WriteAllText(checkFile, "1");

                XCConfigItem configItem = XCConfigItem.ParseXCConfig(XCodePostProcess.config_path);
                if (configItem != null)
                {
                    PlayerSettings.applicationIdentifier = configItem.bundleIdentifier;
                    PlayerSettings.productName = configItem.product_name;
                    configItem.CopyConfig();
                }
//                IOSGenerateHelper.IOSConfusing();
                AssetDatabase.Refresh();
                BuildPipeline.BuildPlayer(GetBuildScenes(), Builder.ApkSavePath, buildTarget, option);
            }

            Resources.UnloadUnusedAssets();
            GC.Collect();
            
            Builder.AddBuildLog("[end]Build App Finish !...");
            return final_path;
        }


        public string[] GetBuildScenes()
        {
            HashSet<string> validSceneNames = new HashSet<string>(new [] { "startscene", "updatescene", "preloadingscene",
                                                                           "createrolescene", "preloginscene", "preloadingchunkscene" });
            List<string> names = new List<string>();
            foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            {
                if (e == null || !e.enabled)  continue;
                
                string name = Path.GetFileNameWithoutExtension(e.path).ToLower();
                if (validSceneNames.Contains(name))
                    names.Add(e.path);
            }

            if(names.Count <= 0)
                Debug.LogError("Cant find build scene !");

            return names.ToArray();
        }

        void ResetConfig()
        {
            string resources_path = "Assets/Resources/";
            if (File.Exists(resources_path + "config1.tmp"))
            {
                File.WriteAllText(resources_path + "config.txt", File.ReadAllText(resources_path + "config1.tmp"));
                File.Delete(resources_path + "config1.tmp");
                AssetDatabase.Refresh();
            }
        }
    }
}