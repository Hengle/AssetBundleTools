using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleBuilder
{
    [Serializable]
    public class AssetBuildRule
    {
        public string AssetName;

        public string Path; // eg:Assets/xxxx

        public string AssetBundleName;
        
        public int Order;
        /// <summary>
        /// 文件过滤类型
        /// </summary>
        public FileType FileFilterType;
        /// <summary>
        /// 加载类型
        /// </summary>
        public ELoadType LoadType;
        /// <summary>
        /// 资源包类型
        /// </summary>
        public PackageAssetType BuildType;
        /// <summary>
        /// 下载顺序
        /// </summary>
        public int DownloadOrder;

        public AssetBuildRule[] Childrens;
        

        public void AddChild(AssetBuildRule childRule)
        {
            List<AssetBuildRule> list = new List<AssetBuildRule>();
            if(Childrens != null)   list.AddRange(Childrens);

            list.Add(childRule);

            Childrens = list.ToArray();
        }


        public void RemoveChild(AssetBuildRule rule)
        {
            List<AssetBuildRule> list = new List<AssetBuildRule>(Childrens);
            list.Remove(rule);
            Childrens = list.ToArray();
        }

        public List<AssetBuildRule> TreeToList()
        {
            List<AssetBuildRule> buildRules = new List<AssetBuildRule>();

            TreeToList(this, buildRules);

            return buildRules;
        }

        public void TreeToList(AssetBuildRule rule, List<AssetBuildRule> buildRules)
        {
            buildRules.Add(rule);

            foreach (AssetBuildRule childRule in rule.Childrens)
                TreeToList(childRule, buildRules);
        }
    }

    [Serializable]
    public class ABConfigs : ScriptableObject
    {
        public AssetBuildRule[] Rules;

    }
}