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
            //生成Bundle中的Asset资源的AB映射
            BuildBundleNameMapFile();

            yield return null;

            //计录AB文件的原始信息,用于后续的下载更新匹配
            this.RecordFileRealSize();
            
        }
        /// <summary>
        /// 建立ab映射文件
        /// </summary>
        private void BuildBundleNameMapFile()
        {
            string savePath = BuilderPreference.BUILD_PATH + "/bundles/bundlemap.ab";
            StringBuilder sb = new StringBuilder();
            
            foreach (AssetMap asset in Builder.AssetMaps.Values)
            {
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

            Builder.AddBuildLog(sb.ToString());

            BuildUtil.SwapPathDirectory(savePath);

            File.WriteAllBytes(savePath, Crypto.Encode(Riverlake.Encoding.GetBytes(sb.ToString())));
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
        }
    }
}