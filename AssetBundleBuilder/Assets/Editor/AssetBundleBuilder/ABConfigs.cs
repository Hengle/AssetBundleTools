using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleBuilder
{
    [Serializable]
    public class AssetBuildRule
    {
        public string Path = "Assets"; // eg:Assets/xxxx

        public string AssetBundleName;
        
        public int Order;
        /// <summary>
        /// 文件过滤类型
        /// </summary>
        public FileType FileFilterType = FileType.Folder;

        /// <summary>
        /// 打包方式
        /// </summary>
        public int BuildType = 1;

        /// <summary>
        /// 加载类型
        /// </summary>
        public ELoadType LoadType;
        /// <summary>
        /// 资源包类型
        /// </summary>
        public PackageAssetType PackageType;
        
        /// <summary>
        /// 下载顺序
        /// </summary>
        public int DownloadOrder;

        [NonSerialized]
        public AssetBuildRule[] Childrens;
        

        public void AddChild(AssetBuildRule childRule)
        {
            List<AssetBuildRule> list = new List<AssetBuildRule>();
            if(Childrens != null)   list.AddRange(Childrens);

            if (list.Contains(childRule)) return;

            list.Add(childRule);

            Childrens = list.ToArray();
        }


        public void RemoveChild(AssetBuildRule rule)
        {
            if (Childrens == null) return;

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

            if (rule.Childrens == null || rule.BuildType < 0) return;

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