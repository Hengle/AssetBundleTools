using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{
    public class AssetBundleBuilder
    {
        private Queue<ABuilding> enumerators = new Queue<ABuilding>();

        private IEnumerator curEnumerator;
        private ABuilding curBuilding;

        //当前选择的SDK
        public SDKConfig SDKConfig;
        public int ActiveSdkIndex;

        public bool IsDebug; //本地调试
        public bool IsBuildDev;
        public bool IsScriptDebug;
        public bool IsAutoConnectProfile;

        public string ApkSavePath;  //apk/ipa安装包
        // build 
        public AutoBuildType AutoBuild;

        
        public Dictionary<string, AssetMap> AssetMaps;

        private GameVersion gameVersion;
        private int apkVersion;


        public float Progress
        {
            get { return totalWeight <= 0 ? 0 : finishWeight /totalWeight; }
        }

        public GameVersion GameVersion
        {
            get { return gameVersion; }
        }

        public int ApkVersion
        {
            get { return apkVersion; }
        }

        private int totalWeight;
        private float finishWeight;
        
        private StringBuilder buildLog;



        public AssetBundleBuilder()
        {
            readGameVersion();

            // 读取游戏版本号
            string destVersionPath = BuilderPreference.VERSION_PATH + "/apk_version.txt";
            if (File.Exists(destVersionPath))
            {
                var ver = File.ReadAllText(destVersionPath);
                apkVersion = Convert.ToInt32(ver);
            }
            else
                apkVersion = 0;
        }

        /// <summary>
        /// 初始化打包流程
        /// </summary>
        public void InitAutoBuilding()
        {
            this.clear();

            this.AddBuilding(new SvnUpdateBuilding());
            this.AddBuilding(new AssetBundleBinding());
            this.AddBuilding(new LuaBuilding());
            this.AddBuilding(new CompressBuilding());
            this.AddBuilding(new PackageBuilding());
            this.AddBuilding(new SvnCommitBuilding());
            this.AddBuilding(new CDNBuilding());
        }

        public void InitBuilding()
        {
            this.clear();

//            this.AddBuilding(new AssetBundleBinding());
//            this.AddBuilding(new LuaBuilding());
//            this.AddBuilding(new CompressBuilding());
            this.AddBuilding(new PackageBuilding());
        }

        /// <summary>
        /// 读取游戏版本号
        /// </summary>
        private void readGameVersion()
        {
            // 拷贝资源版本号
#if UNITY_IOS
            string destVersionPath = BuilderPreference.VERSION_PATH + "/version_ios.txt";
#else
            string destVersionPath = BuilderPreference.VERSION_PATH + "/version.txt";
#endif
            if (!File.Exists(destVersionPath))
                destVersionPath = BuilderPreference.ASSET_PATH + "/version.txt";

            if (File.Exists(destVersionPath))
            {
                var versionPath = BuilderPreference.BUILD_PATH + "/version.txt";
                File.Copy(destVersionPath, versionPath, true);
                gameVersion = GameVersion.CreateVersion(File.ReadAllText(versionPath));
            }
            else
                gameVersion = GameVersion.CreateVersion(Application.version);
        }

        /// <summary>
        /// 保存写入游戏版本数据
        /// </summary>
        public void SaveVersion()
        {
            string resVersion = gameVersion.ToString();
#if UNITY_IOS
            string resVersionPath = BuilderPreference.VERSION_PATH + "/version_ios.txt";
#else
            string resVersionPath = BuilderPreference.VERSION_PATH + "/version.txt";
#endif
            BuildUtil.SwapPathDirectory(resVersionPath);

            File.WriteAllText(resVersionPath, resVersion.ToString());
        }

        #region View Button Click

        /// <summary>
        /// 点击打包本地测试
        /// </summary>
        public void OnClickBuildLocalDebug()
        {
            this.IsDebug = true;

        }

        /// <summary>
        /// 点击打包分包
        /// </summary>
        public void OnClickBuildSubPackage()
        {
            
        }

        /// <summary>
        /// 点击打包Xls配置表
        /// </summary>
        public void OnClickBuildXls()
        {
            
        }

        /// <summary>
        /// 点击打包Lua脚本
        /// </summary>
        public void OnClickBuildLua()
        {
            
        }

        /// <summary>
        /// 点击生成分包资源
        /// </summary>
        public void OnClickBuildSubPackageAssets()
        {
            
        }

        /// <summary>
        /// 点击生成整包资源
        /// </summary>
        public void OnClickBuildAllPackageAssets()
        {
            
        }
        
        /// <summary>
        /// 点击一键生成
        /// </summary>
        public void OnClickAutoBuild()
        {
            this.StartBuild();
        }

        #endregion


        public void AddBuilding(ABuilding building)
        {
            building.Builder = this;
            enumerators.Enqueue(building);
            totalWeight += building.Weight;
        }


        public void StartBuild()
        {
            if (buildLog == null)
                buildLog = new StringBuilder();
            buildLog.Length = 0;

            curBuilding = enumerators.Dequeue();
            curEnumerator = curBuilding.OnBuilding();

            EditorApplication.update += onUpdateBuilding;
        }


        public void CanleBuild()
        {
            clear();
            EditorApplication.update -= onUpdateBuilding;
        }


        public void AddBuildLog(string log)
        {
            buildLog.AppendLine(log);
            Debug.Log(log);
        }


        private void onUpdateBuilding()
        {
            if (curEnumerator == null)
            {
                CanleBuild();
                return;
            }

            if (!curEnumerator.MoveNext())
            {
                finishWeight += curBuilding.Weight;
                curEnumerator = null;

                if (enumerators.Count > 0)
                {
                    curBuilding = enumerators.Dequeue();
                    curEnumerator = curBuilding.OnBuilding();
                }
                else
                {
                    Debug.Log("<color=green>Build success!</color>");
                }
            }
        }


        private void clear()
        {
            this.enumerators.Clear();
            totalWeight = 0;
            finishWeight = 0;
            ApkSavePath = String.Empty;
        }
    }
}