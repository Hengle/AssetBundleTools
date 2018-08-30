using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{

    public class AssetBuildRuleManager
    {
        private AssetBuildRule[] rootRules;

        private static AssetBuildRuleManager instance;


        public AssetBuildRule[] Rules
        {
            get
            {
                return rootRules;
            }
        }

        public static AssetBuildRuleManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new AssetBuildRuleManager();
                return instance;
            }
        }

        private AssetBuildRuleManager() { }


        public void LoadConifg()
        {
            string configPath = BuilderPreference.DEFAULT_CONFIG_NAME;
            if (!File.Exists(configPath)) return;

            ABConfigs configs = AssetDatabase.LoadAssetAtPath<ABConfigs>(configPath);

            AssetBuildRule[] configRules = configs.Rules;
            Array.Sort(configRules, (x, y) => x.Path.Length.CompareTo(y.Path.Length));

            List<AssetBuildRule> rootRuleList = new List<AssetBuildRule>();

            for (int i = 0; i < configRules.Length; i++)
            {
                bool hasParent = false;
                for (int j = 0; j < rootRuleList.Count; j++)
                {
                    hasParent = findParentRecursive(configRules[i], rootRuleList[j]);
                    if (hasParent) break;
                }

                if (!hasParent)
                    rootRuleList.Add(configRules[i]);
            }

            this.rootRules = rootRuleList.ToArray();
        }


        private bool findParentRecursive(AssetBuildRule buildRule, AssetBuildRule parent)
        {
            if (buildRule.Equals(parent) || !buildRule.Path.StartsWith(parent.Path + "/")) return false;

            bool result = false;
            if (parent.Childrens != null)
            {
                foreach (AssetBuildRule child in parent.Childrens)
                {
                    result = findParentRecursive(buildRule, child);
                    if (result) break;
                }
            }

            if (!result)
                parent.AddChild(buildRule);

            return true;
        }


        public void SaveConfig(AssetBuildRule[] rules)
        {
            if (rules == null || rules.Length <= 0) return;

            string defaultConfigPath = BuilderPreference.DEFAULT_CONFIG_NAME;

            if (File.Exists(defaultConfigPath)) File.Delete(defaultConfigPath);

            this.rootRules = rules;

            Dictionary<string, AssetBuildRule> ruleMap = new Dictionary<string, AssetBuildRule>();

            for (int i = 0; i < rules.Length; i++)
            {
                List<AssetBuildRule> treeList = rules[i].TreeToList();
                for (int j = 0; j < treeList.Count; j++)
                {
                    ruleMap[treeList[j].Path] = treeList[j];
                }
            }

            AssetBuildRule[] ruleArr = new AssetBuildRule[ruleMap.Count];
            ruleMap.Values.CopyTo(ruleArr, 0);

            Array.Sort(ruleArr, (x, y) => x.Path.CompareTo(y.Path));

            ABConfigs configs = ScriptableObject.CreateInstance<ABConfigs>();
            configs.Rules = ruleArr;

            if (File.Exists(defaultConfigPath)) AssetDatabase.DeleteAsset(defaultConfigPath);

            AssetDatabase.CreateAsset(configs, defaultConfigPath);

            //            Debug.Log("<color=#2fd95b>Save Success !</color>");
        }


        public void OnDestroy()
        {
            instance = null;
            this.rootRules = null;
        }
    }
}