using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{
    
    public class AssetBuildRuleManager
    {
        // key:path value : assetbuildrule
        private ABConfigs configs;

        private static AssetBuildRuleManager instance;


        public AssetBuildRule[] Rules
        {
            get
            {
                if (configs == null) return null;
                return configs.Rules;
            }
        }

        public static AssetBuildRuleManager Instance
        {
            get
            {
                if(instance == null)
                    instance = new AssetBuildRuleManager();
                return instance;
            }
        }

        private AssetBuildRuleManager() { }


        public void LoadConifg()
        {
            string configPath = BuilderPreference.DEFAULT_CONFIG_NAME;
            if (!File.Exists(configPath)) return;

            configs = AssetDatabase.LoadAssetAtPath<ABConfigs>(configPath);
            
        }


        public void SaveConfig(AssetBuildRule[] rules)
        {
            if (rules == null || rules.Length <= 0) return;

            string defaultConfigPath = BuilderPreference.DEFAULT_CONFIG_NAME;

            if (File.Exists(defaultConfigPath)) File.Delete(defaultConfigPath);

            configs = ScriptableObject.CreateInstance<ABConfigs>();
            configs.Rules = rules;

            AssetDatabase.CreateAsset(configs , defaultConfigPath);

//            Debug.Log("<color=#2fd95b>Save Success !</color>");
        }
    }
}