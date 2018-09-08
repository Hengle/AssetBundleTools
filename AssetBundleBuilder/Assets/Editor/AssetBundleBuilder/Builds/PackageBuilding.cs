using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Riverlake.Crypto;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ZstdNet;

namespace AssetBundleBuilder
{
    /// <summary>
    /// 资源分包阶段，打包资源完成后，将不同资源拷贝到对应包体对应的目录
    /// </summary>
    public abstract class PackageBuilding : ABuilding
    {
        protected bool isBuildApp;

        protected int compressIndex, compressCount;

        public PackageBuilding(bool buildApp) : base(20)
        {
            this.isBuildApp = buildApp;
        }

        /// <summary>
        /// 压缩StreamAsset目录的资源
        /// </summary>
        /// <param name="maxFileSize"></param>
        protected void CompressWithZSTD(long maxFileSize)
        {
            string outPutPath = BuilderPreference.StreamingAssetsPlatormPath;

            var dirInfo = new DirectoryInfo(outPutPath);
            var dirs = dirInfo.GetDirectories();

            Dictionary<int, List<string>> allFiles = new Dictionary<int, List<string>>();
            // data原始包控制在10M左右
            long curSize = 0;
            int tmpIndex = 0;
            for (int i = 0; i < dirs.Length; ++i)
            {
                if (dirs[i].Name == "lua") continue;

                var abFileInfos = BuildUtil.SearchFiles(dirs[i].FullName, SearchOption.AllDirectories);

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
            Dictionary<string, byte[]> mergeFiles = new Dictionary<string, byte[]>();

            foreach (var key in allFiles.Keys)
            {
                var tmpName = "data" + key;
#if UNITY_IOS
            tmpName = IOSGenerateHelper.RenameResFileWithRandomCode(tmpName);
#endif
                var savePath = string.Format("{0}/{1}.tmp", outPutPath, tmpName);
                //                ABPackHelper.ShowProgress("Streaming data...", (float)index++ / (float)allFiles.Count);
                using (MemoryStream fs = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(fs))
                    {
                        List<string> filePaths = allFiles[key];
                        for (int i = 0; i < filePaths.Count; ++i)
                        {
                            var bytes = File.ReadAllBytes(filePaths[i]);
                            var abName = filePaths[i].Substring(pathLength);
                            writer.Write(abName);
                            writer.Write(bytes.Length);
                            writer.Write(bytes);
                        }
                    }
                    mergeFiles[savePath] = fs.ToArray();
                }
            }
            //            ABPackHelper.ShowProgress("Finished...", 1);

            //删除子目录
            for (int i = 0; i < dirs.Length; ++i)
            {
                if (dirs[i].Name == "lua") continue;
                Directory.Delete(dirs[i].FullName, true);
            }

            AssetDatabase.Refresh();

            // 对合并后的文件进行压缩
            Builder.AddBuildLog("<Copresss zstd> compress with zstd...");
            compressIndex = 0;
            compressCount = mergeFiles.Count;

            foreach (string filePath in mergeFiles.Keys)
            {
                ThreadPool.QueueUserWorkItem(onThreadCompress, new object[] { filePath, outPutPath, mergeFiles[filePath] });
            }

        }

        /// <summary>
        /// 生成包体第一次进入游戏解压缩配置文件
        /// </summary>
        protected void genPacklistFile()
        {
            StringBuilder builder = new StringBuilder();
            string outPutPath = BuilderPreference.StreamingAssetsPlatormPath;

            List<string> allfiles = BuildUtil.SearchFiles(outPutPath, SearchOption.AllDirectories);

            int pathLength = outPutPath.Length + 1;
            for (int i = 0; i < allfiles.Count; ++i)
            {
                if (allfiles[i].EndsWith("datamap.ab")) continue;

                var relativePath = allfiles[i].Substring(pathLength);
                string md5 = MD5.ComputeHashString(allfiles[i]);

                builder.AppendLine(string.Format("{0}|{1}", relativePath, md5));
            }

            var packFlistPath = outPutPath + "/packlist.txt";
            File.WriteAllText(packFlistPath, builder.ToString());
        }


        protected void onThreadCompress(object fileInfos)
        {
            object[] fileInfoArr = (object[])fileInfos;
            string filePath = (string)fileInfoArr[0];
            string outPutPath = (string)fileInfoArr[1];
            byte[] fileBytes = (byte[])fileInfoArr[2];

            var savePath = string.Format("{0}/{1}.bin", outPutPath, Path.GetFileNameWithoutExtension(filePath));

            using (var compressFs = new FileStream(savePath, FileMode.CreateNew))
            {
                using (var compressor = new Compressor(new CompressionOptions(CompressionOptions.MaxCompressionLevel)))
                {
                    var bytes = compressor.Wrap(fileBytes);
#if UNITY_IOS
                        bytes = Crypto.Encode(bytes);
#endif
                    compressFs.Write(bytes, 0, bytes.Length);
                }
            }
            compressIndex++;
        }


        protected string BuildApp(bool packAllRes, bool forceUpdate)
        {
            Builder.AddBuildLog("Build App Start !... packAllRes:" + packAllRes + ",force update:" + forceUpdate);

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
                        final_path = string.Concat(dir, "/", fileName, "_", time, flag, Builder.GameVersion.ToString(), ".apk");
                        if (File.Exists(final_path)) File.Delete(final_path);
                        // 写入并保存sdk启用配置
//                        item.CopyConfig();
//                        item.CopySDK();
//                        item.SetPlayerSetting(curSdkConfig.splash_image);
//                        item.SaveSDKConfig();
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

                        item.CopyConfig();
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
            HashSet<string> validSceneNames = new HashSet<string>(new[] { "startscene", "updatescene", "preloadingscene",
                                                                           "createrolescene", "preloginscene", "preloadingchunkscene" });
            List<string> names = new List<string>();
            foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            {
                if (e == null || !e.enabled) continue;

                string name = Path.GetFileNameWithoutExtension(e.path).ToLower();
                if (validSceneNames.Contains(name))
                    names.Add(e.path);
            }

            if (names.Count <= 0)
                Debug.LogError("Cant find build scene !");

            return names.ToArray();
        }
    }
}