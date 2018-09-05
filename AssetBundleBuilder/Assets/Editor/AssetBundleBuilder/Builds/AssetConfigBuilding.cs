using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Riverlake.Crypto;

namespace AssetBundleBuilder
{
    /// <summary>
    /// 资源配置处理，在打包AssetBundle后，更新资源的加载/更新配置
    /// </summary>
    public class AssetConfigBuilding : ABuilding
    {
        public AssetConfigBuilding() : base(10)
        {
        }


        public override IEnumerator OnBuilding()
        {
            BuildFileIndex();

            yield return null;

            //生成Bundle中的Asset资源的AB映射
            BuildBundleNameMapFile();

            yield return null;

            //计录AB文件的原始信息,用于后续的下载更新匹配
            this.RecordFileRealSize();
            
        }


        private void BuildFileIndex()
        {
            Builder.AddBuildLog("<Asset Config Building> start Build File Index ....");

            string resPath = BuilderPreference.BUILD_PATH + "/";
            //----------------------创建文件列表-----------------------
            string newFilePath = resPath + "files.txt";
            if (File.Exists(newFilePath)) File.Delete(newFilePath);

            string tempSizeFile = resPath + "tempsizefile.txt";
            Dictionary<string, string> assetTypeDict = new Dictionary<string, string>();
            if (File.Exists(tempSizeFile))
            {
                var sizeFileContent = File.ReadAllText(tempSizeFile);
                var temps = sizeFileContent.Split('\n');
                for (int i = 0; i < temps.Length; ++i)
                {
                    if (!string.IsNullOrEmpty(temps[i]))
                    {
                        var temp = temps[i].Split('|');
                        if (temp.Length != 2 && temp.Length != 3) throw new System.IndexOutOfRangeException();

                        var assetType = temp[1];
                        if (temp.Length == 3) assetType += "|" + temp[2];

                        assetTypeDict.Add(temp[0], assetType);
                        //UpdateProgress(i, temps.Length, temps[i]);
                    }
                }
                //            EditorUtility.ClearProgressBar();
            }

            List<string> includeFiles = BuildUtil.SearchFiles(resPath, SearchOption.AllDirectories);
            HashSet<string> excludeSuffxs = new HashSet<string>() { ".DS_Store", ".manifest" };  //排除文件

            BuildUtil.SwapPathDirectory(newFilePath);

            using (FileStream fs = new FileStream(newFilePath, FileMode.CreateNew))
            {
                StreamWriter sw = new StreamWriter(fs);
                for (int i = 0; i < includeFiles.Count; i++)
                {
                    string file = includeFiles[i];
                    string ext = Path.GetExtension(file);

                    if (excludeSuffxs.Contains(ext) || file.EndsWith("apk_version.txt") || file.Contains("tempsizefile.txt") || file.Contains("luamd5.txt")) continue;

                    string md5 = MD5.ComputeHashString(file);
                    int size = (int)new FileInfo(file).Length;
                    string value = file.Replace(resPath, string.Empty).ToLower();
                    if (assetTypeDict.ContainsKey(value))
                        sw.WriteLine("{0}|{1}|{2}|{3}", value, md5, size, assetTypeDict[value]);
                    else
                        sw.WriteLine("{0}|{1}|{2}", value, md5, size);
                    //            UpdateProgress(i, includeFiles.Count, file);

                }
                sw.Close();
            }
            //        EditorUtility.ClearProgressBar();

            Builder.AddBuildLog("<Asset Config Building> Build File Index end ....");
        }
        /// <summary>
        /// 建立ab映射文件
        /// </summary>
        private void BuildBundleNameMapFile()
        {
            string savePath = BuilderPreference.BUILD_PATH + "/bundlemap.ab";
            StringBuilder sb = new StringBuilder();
            
            foreach (AssetMap asset in Builder.AssetMaps.Values)
            {
                if(!asset.IsBinding)    continue;

                int rootLength = 0;
                foreach (string rootFolder in BuilderPreference.BundleMapFile)
                {
                    if (asset.AssetPath.StartsWith(rootFolder))
                    {
                        rootLength = rootFolder.Length;
                        break;
                    }
                }

                if(rootLength <= 0)   continue;

                string assetName = asset.AssetPath.Substring(rootLength + 1);
                if (asset.Rule.FileFilterType == FileType.Scene)
                    assetName = Path.GetFileName(asset.AssetPath);

                string abName = BuildUtil.FormatBundleName(asset.Rule);
                int preload = asset.Rule.LoadType == ELoadType.PreLoad ? 1 : 0;

                string str = string.Format("{0}|{1}.{2}|{3}", assetName.Split('.')[0].ToLower(), abName, BuilderPreference.VARIANT_V1, preload);
                sb.AppendLine(str);
            }

            
            if (File.Exists(savePath)) File.Delete(savePath);
            
            BuildUtil.SwapPathDirectory(savePath);
            
            File.WriteAllBytes(savePath, Crypto.Encode(Riverlake.Encoding.GetBytes(sb.ToString())));

            Builder.AddBuildLog("<Asset Config Building> Gen bundle name map file !");
        }



        
        /// <summary>
        /// 记录压缩前文件大小和md5码,以及资源类型（是否包含于整包,是否是补丁资源）
        /// </summary>
        private void RecordFileRealSize()
        {
            string buildPath = BuilderPreference.BUILD_PATH;
            StringBuilder sb = new StringBuilder();

            AssetBuildRule[] rules = AssetBuildRuleManager.Instance.Rules;
            Dictionary<string , AssetBuildRule> ruleMap = new Dictionary<string, AssetBuildRule>();

            for (int i = 0; i < rules.Length; i++)
            {
                List<AssetBuildRule> ruleList = rules[i].TreeToList();
                for (int j = 0; j < ruleList.Count; j++)
                {
                    ruleMap[ruleList[j].AssetBundleName] = ruleList[j];
                }
            }

            foreach (AssetBuildRule bundle in ruleMap.Values)
            {
                // fileName|AssetType:value|DownloadOrder:value
                // todo AssetType 暂时没支持
                string format = string.Format("{0}|AssetType:{1}|DownloadOrder:{2}", 
                                              BuildUtil.FormatBundleName(bundle), 0,  bundle.DownloadOrder);
                sb.AppendLine(format);
            }

            // save temp size file
            string savePath = buildPath + "/tempsizefile.txt";
            if (File.Exists(savePath)) File.Delete(savePath);
            File.WriteAllText(savePath, sb.ToString());

            Builder.AddBuildLog("<Asset Config Building>Record file real size");
        }
    }
}