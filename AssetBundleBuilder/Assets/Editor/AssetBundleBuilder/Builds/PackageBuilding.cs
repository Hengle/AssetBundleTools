using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Riverlake.Crypto;
using UnityEditor;
using UnityEngine;
using ZstdNet;

namespace AssetBundleBuilder
{
    /// <summary>
    /// 分包阶段，打包资源完成后，将不同资源拷贝到对应包体对应的目录
    /// </summary>
    public class PackageBuilding : ABuilding
    {
        public PackageBuilding() : base(20)
        {

        }

        public override IEnumerator OnBuilding()
        {
            return base.OnBuilding();
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
            HashSet<string> includeExtensions = new HashSet<string>() { ".ab", ".unity3d", ".txt", ".conf", ".pb", ".bytes" };
            string[] files = Directory.GetFiles(bundlePath, "*.*", SearchOption.AllDirectories)
                .Where(s => includeExtensions.Contains(Path.GetExtension(s).ToLower())).ToArray();

            int buildlePathLength = bundlePath.Length + 1;
            for (int i = 0; i < files.Length; ++i)
            {
                string fileName = Path.GetFileName(files[i]);
                if (fileName == "tempsizefile.txt" || fileName == "luamd5.txt") continue;

                //ABPackHelper.ShowProgress("Copying files...", (float)i / (float)files.Length);
                string streamFilePath = files[i].Replace("\\", "/").Replace(bundlePath , targetPath);
                if (!Directory.Exists(streamFilePath))  Directory.CreateDirectory(streamFilePath);

//                var file = tempStr.ToLower();
//                if (file.EndsWith(".ab"))
//                {
//                    bool copyed = false;
//                    //todo ab Type Map is null
////                    foreach (var key in abTypeMaps.Keys)
////                    {
////                        var value = abTypeMaps[key.ToLower()].Split('.');
////                        if ((file.Contains(key) && Convert.ToInt32(value[0]) == 0)
////                            || file.Contains(LuaConst.osDir.ToLower() + ".ab")
////                            || file.Contains("bundlemap.ab") || file.Contains("shader.ab")
////                            || file.Contains("font.ab") || file.Contains("scene_public.ab"))
////                        {
////                            copyed = true;
////                            File.Copy(files[i], targetPath + "/" + tempStr);
////                            break;
////                        }
////                    }
//                    if (!copyed && file.Contains("_effect", StringComparison.OrdinalIgnoreCase))
//                    {
//                        File.Copy(files[i], targetPath + "/" + tempStr);
//                    }
//                }
//                else
//                {
//                    File.Copy(files[i], targetPath + "/" + tempStr);
//                }
            }
//            AssetDatabase.Refresh();
            //ABPackHelper.ShowProgress("", 1);
            CompressWithZSTD(1024 * 1024 * 10);
        }

        private void CopyAllBundles()
        {
            string targetPath = BuilderPreference.StreamingAssetsPlatormPath;
            if (Directory.Exists(targetPath))   Directory.Delete(targetPath, true);
            Directory.CreateDirectory(targetPath);


            string buildPath = BuilderPreference.BUILD_PATH;
            HashSet<string> withExtensions = new HashSet<string>() { ".ab", ".unity3d", ".txt", ".conf", ".pb", ".bytes" };
            string[] files = Directory.GetFiles(targetPath, "*.*", SearchOption.AllDirectories)
                .Where(s => withExtensions.Contains(Path.GetExtension(s).ToLower())).ToArray();

//            ABPackHelper.ShowProgress("", 0);
            int buildPathLength = buildPath.Length + 1; 
            for (int i = 0; i < files.Length; ++i)
            {
                string fileName = Path.GetFileName(files[i]);
                if (fileName == "tempsizefile.txt" || fileName == "luamd5.txt") continue;
                //ABPackHelper.ShowProgress("Copying files...", (float)i / (float)files.Length);

                var tempStr = files[i].Replace("\\", "/").Substring(buildPathLength);
                var dirs = tempStr.Split('/');
                var tempDir = targetPath;
                for (int j = 0; j < dirs.Length - 1; ++j)
                {
                    tempDir += "/" + dirs[j];
                    if (!Directory.Exists(tempDir))
                        Directory.CreateDirectory(tempDir);
                }
                var file = ABPackHelper.GetRelativeAssetsPath(files[i]);
                bool needCopy = true;
                //todo abTypeMap is null
//                foreach (var key in abTypeMaps.Keys)
//                {
//                    var value = abTypeMaps[key.ToLower()].Split('.');
//                    if (file.Contains(key) && Convert.ToInt32(value[0]) == 3)
//                    {
//                        needCopy = false;
//                        break;
//                    }
//                }
                if (needCopy) File.Copy(files[i], targetPath + "/" + tempStr);
            }
//            AssetDatabase.Refresh();
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
                var abFileInfos = dirs[i].GetFiles("*.*", SearchOption.AllDirectories);
                for (int j = 0; j < abFileInfos.Length; ++j)
                {
                    if (abFileInfos[j].FullName.EndsWith(".meta")) continue;
                    var fileName = ABPackHelper.GetRelativeAssetsPath(abFileInfos[j].FullName);
                    var data = File.ReadAllBytes(fileName);
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
                    if (curSize == 0) allFiles.Add(tmpIndex, new List<string>());
                    allFiles[tmpIndex].Add(fileName);
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
            ABPackHelper.ShowProgress("Finished...", 1);
            for (int i = 0; i < dirs.Length; ++i)
            {
                if (dirs[i].Name == "lua") continue;
                Directory.Delete(dirs[i].FullName, true);
            }
            AssetDatabase.Refresh();

            // 对合并后的文件进行压缩
            ABPackHelper.ShowProgress("Hold on...", 0);
            var pakFiles = Directory.GetFiles(outPutPath, "*.tmp", SearchOption.AllDirectories);
            for (int i = 0; i < pakFiles.Length; ++i)
            {
                var savePath = string.Format("{0}/{1}.bin", outPutPath, Path.GetFileNameWithoutExtension(pakFiles[i]));
                ABPackHelper.ShowProgress("compress with zstd...", (float)i / (float)pakFiles.Length);
                var fileName = ABPackHelper.GetRelativeAssetsPath(pakFiles[i]);
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
            ABPackHelper.ShowProgress("Finished...", 1);

            // 生成包体第一次进入游戏解压缩配置文件
            StringBuilder builder = new StringBuilder();
            string[] allfiles = Directory.GetFiles(outPutPath, "*.*", SearchOption.AllDirectories);
            for (int i = 0; i < allfiles.Length; ++i)
            {
                if (allfiles[i].EndsWith(".meta")) continue;
                if (allfiles[i].EndsWith("datamap.ab")) continue;

                var fileName = allfiles[i].Replace("\\", "/").Substring(pathLength);
                builder.Append(fileName);
                builder.Append('|');
                builder.Append(MD5.ComputeHashString(allfiles[i]));
                builder.Append("\n");
            }

            var packFlistPath = outPutPath + "/packlist.txt";
            if (File.Exists(packFlistPath)) File.Delete(packFlistPath);
            File.WriteAllText(packFlistPath, builder.ToString());
//            AssetDatabase.Refresh();
        }


        void BuildApp(bool packAllRes, bool forceUpdate)
        {
            var option = BuildOptions.None;
            if (Builder.IsDebug) option |= BuildOptions.AllowDebugging;
            if (Builder.IsBuildDev) option |= BuildOptions.Development;
            if (Builder.IsAutoConnectProfile) option |= BuildOptions.ConnectWithProfiler;

//            var temps = Apk_Save_Path.Replace("\\", "/").Split('/');
//            if ((ABPackHelper.GetBuildTarget() == BuildTarget.Android
//                || ABPackHelper.GetBuildTarget() == BuildTarget.StandaloneWindows64
//                || ABPackHelper.GetBuildTarget() == BuildTarget.StandaloneWindows)
//                && sdkConfig != null)
//            {
//                string lastChannel = string.Empty;
//                for (int i = 0; i < sdkConfig.items.Count; ++i)
//                {
//                    StringBuilder final_path = new StringBuilder();
//                    for (int j = 0; j < temps.Length - 1; ++j)
//                    {
//                        final_path.Append(temps[j] + "/");
//                    }
//                    var item = sdkConfig.items[i];
//                    if (item.need_subpack == 0 && !packAllRes) continue;
//                    if (ABPackHelper.GetBuildTarget() == BuildTarget.StandaloneWindows64 || ABPackHelper.GetBuildTarget() == BuildTarget.StandaloneWindows)
//                    {
//                        final_path.Append(DateTime.Now.ToString("yyyyMMdd") + "/");
//                        if (!Directory.Exists(final_path.ToString())) Directory.CreateDirectory(final_path.ToString());
//                        final_path.Append(item.game_name + "_v");
//                    }
//                    else
//                    {
//                        if (packAllRes)
//                        {
//                            if (item.development == 1)
//                            {
//                                option |= BuildOptions.Development;
//                                final_path.Append(item.game_name + DateTime.Now.ToString("yyyyMMdd") + "_allpack_dev_v");
//                            }
//                            else if (item.use_sdk == 1)
//                                final_path.Append(item.game_name + DateTime.Now.ToString("yyyyMMdd") + "_allpack_sdk_v");
//                            else
//                                final_path.Append(item.game_name + DateTime.Now.ToString("yyyyMMdd") + "_allpack_test_v");
//                        }
//                        else
//                        {
//                            if (item.development == 1)
//                            {
//                                option |= BuildOptions.Development;
//                                final_path.Append(item.game_name + DateTime.Now.ToString("yyyyMMdd") + "_subpack_dev_v");
//                            }
//                            else if (item.use_sdk == 1)
//                                final_path.Append(item.game_name + DateTime.Now.ToString("yyyyMMdd") + "_subpack_sdk_v");
//                            else
//                                final_path.Append(item.game_name + DateTime.Now.ToString("yyyyMMdd") + "_subpack_test_v");
//                        }
//                    }
//                    //final_path.Append(gameVersion.ToString());
//                    if (ABPackHelper.GetBuildTarget() == BuildTarget.Android)
//                    {
//                        final_path.Append(".apk");
//                        if (File.Exists(final_path.ToString())) File.Delete(final_path.ToString());
//                        // 写入并保存sdk启用配置
//                        item.CopyConfig();
//                        item.CopySDK();
//                        item.SetPlayerSetting(sdkConfig.splash_image);
//                        item.SaveSDKConfig();
//                        //item.SplitAssets(sdkConfig.split_assets);
//                        if (item.update_along == 0 && forceUpdate)
//                        {
//                            if (Directory.Exists(Application.streamingAssetsPath)) Directory.Delete(Application.streamingAssetsPath, true);
//                        }
//                    }
//                    else if (ABPackHelper.GetBuildTarget() == BuildTarget.StandaloneWindows64 || ABPackHelper.GetBuildTarget() == BuildTarget.StandaloneWindows)
//                    {
//                        final_path.Append(".exe");
//                        if (Directory.Exists(final_path.ToString())) Directory.Delete(final_path.ToString(), true);
//                        item.CopyConfig();
//                    }
//                    AssetDatabase.Refresh();
//                    BuildPipeline.BuildPlayer(ABPackHelper.GetBuildScenes(), final_path.ToString(), ABPackHelper.GetBuildTarget(), option);
//                    AssetDatabase.Refresh();
//                    item.ClearSDK();
//
//                    SVNHelper.UpdateAll();
//                }
//            }
//            else if (ABPackHelper.GetBuildTarget() == BuildTarget.iOS)
//            {
//                // 在上传目录新建一个ios_check.txt文件用于判断当前包是否出于提审状态
//                string checkFile = ABPackHelper.ASSET_PATH + LuaConst.osDir + "/ios_check.txt";
//                if (File.Exists(checkFile)) File.Delete(checkFile);
//                File.WriteAllText(checkFile, "1");
//
//                XCConfigItem configItem = XCConfigItem.ParseXCConfig(XCodePostProcess.config_path);
//                if (configItem != null)
//                {
//                    PlayerSettings.applicationIdentifier = configItem.bundleIdentifier;
//                    PlayerSettings.productName = configItem.product_name;
//                    configItem.CopyConfig();
//                }
//                IOSGenerateHelper.IOSConfusing();
//                AssetDatabase.Refresh();
//                BuildPipeline.BuildPlayer(ABPackHelper.GetBuildScenes(), Apk_Save_Path, ABPackHelper.GetBuildTarget(), option);
//                AssetDatabase.Refresh();
//            }

            Resources.UnloadUnusedAssets();
            GC.Collect();

            Debug.Log("<color=green>Build success!</color>");
        }

    }
}