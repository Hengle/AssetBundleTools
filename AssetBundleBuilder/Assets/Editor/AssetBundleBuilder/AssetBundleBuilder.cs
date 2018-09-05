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
        private AssetBundleBuilderWindow mainWindow;

        private Queue<ABuilding> enumerators = new Queue<ABuilding>();

        private IEnumerator curEnumerator;
        private ABuilding curBuilding;

        //当前选择的SDK
        public int ActiveSdkIndex;
        private List<SDKConfig> sdkConfigs = new List<SDKConfig>();
        public string[] NameSDKs;

        public bool IsDebug; //本地调试
        public bool IsBuildDev;
        public bool IsScriptDebug;
        public bool IsAutoConnectProfile;

        public string ApkSavePath;  //apk/ipa安装包
        
        public Dictionary<string, AssetMap> AssetMaps;

        private GameVersion gameVersion;
        private int apkVersion;
        
        private int totalWeight;
        private float finishWeight;

        private DateTime startTime;

        private List<string> buildLog = new List<string>();

        public float Progress { get; set; }
        /// <summary>
        /// 总计消耗时间
        /// </summary>
        public double UseTime { get; private set; }

        public GameVersion GameVersion
        {
            get { return gameVersion; }
        }

        public int ApkVersion
        {
            get { return apkVersion; }
        }


        public SDKConfig CurrentConfigSDK
        {
            get { return sdkConfigs[ActiveSdkIndex]; }
        }

        public int BuildingCount
        {
            get { return enumerators.Count; }
        }

        public List<string> BuildLog
        {
            get { return buildLog; }
        }


        public AssetBundleBuilder(AssetBundleBuilderWindow window)
        {
            mainWindow = window;

            readGameVersion();

            loadSDKConfig();
        }

        /// <summary>
        /// 初始化打包流程
        /// </summary>
        public void InitAutoBuilding(int packageBuildings)
        {
            this.clear();

            this.AddBuilding(new SvnUpdateBuilding());
            this.AddBuilding(new AssetBundleBinding());
            this.AddBuilding(new LuaBuilding());
            this.AddBuilding(new AssetConfigBuilding());
            this.AddBuilding(new CompressBuilding());
            this.AddBuilding(new SvnCommitBuilding());

            if(isEnableLayer(packageBuildings, (int)PackageBuildings.SubPackage))
                this.AddBuilding(new SubPackageBuilding(true));

            if (isEnableLayer(packageBuildings, (int) PackageBuildings.FullPackage))
            {
                bool isForceUpdate = isEnableLayer(packageBuildings, (int) PackageBuildings.ForceUpdate);
                this.AddBuilding(new FullPackageBuilding(true , isForceUpdate));
            }
            this.AddBuilding(new CDNBuilding());
        }

        public void InitBuilding(bool isDebug)
        {
            this.clear();

            this.IsDebug = true;
            this.AddBuilding(new AssetBundleBinding());
            this.AddBuilding(new LuaBuilding());
            this.AddBuilding(new AssetConfigBuilding());

            if (!isDebug)
            {
                this.AddBuilding(new CompressBuilding());
                this.AddBuilding(new SubPackageBuilding(true));
            }
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
                BuildUtil.SwapPathDirectory(versionPath);

                File.Copy(destVersionPath, versionPath, true);
                gameVersion = GameVersion.CreateVersion(File.ReadAllText(versionPath));
            }
            else
                gameVersion = GameVersion.CreateVersion(Application.version);

            // 读取游戏版本号
            apkVersion = 0;
            destVersionPath = BuilderPreference.VERSION_PATH + "/apk_version.txt";
            if (File.Exists(destVersionPath))
            {
                var ver = File.ReadAllText(destVersionPath);
                apkVersion = Convert.ToInt32(ver);
            }
                
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
        public void OnClickDebugBuild(bool isDebug)
        {
            this.InitBuilding(isDebug);
            this.StartBuild();
        }

        /// <summary>
        /// 点击打包分包
        /// </summary>
        public void OnClickBuild(int buildings , int packageBuildings)
        {
            clear();

            if (isBuilding(buildings , Buildings.Assetbundle))
            {
                this.AddBuilding(new AssetBundleBinding());
            }

            if (isBuilding(buildings, Buildings.Lua))
                this.AddBuilding(new LuaBuilding());

            if (isBuilding(buildings, Buildings.Assetbundle) || isBuilding(buildings, Buildings.Lua))
                this.AddBuilding(new AssetConfigBuilding());

            if (isBuilding(buildings, Buildings.Compress))
                this.AddBuilding(new CompressBuilding());

            if(isBuilding(buildings, Buildings.SubPackage))
                this.AddBuilding(new SubPackageBuilding(isEnableLayer(packageBuildings , (int)PackageBuildings.BuildApp)));

            if(isBuilding(buildings , Buildings.FullPackage))
                this.AddBuilding(new FullPackageBuilding(false , false));

            this.StartBuild();
        }

        private bool isBuilding (int buildings , Buildings eBuilding)
        {
            return (buildings & (int)eBuilding) != 0;
        }


        private bool isEnableLayer(int mask, int layer)
        {
            return (mask & (int)layer) != 0;
        }

        /// <summary>
        /// 点击一键生成
        /// </summary>
        public void OnClickAutoBuild(string packagePath , int packageBuildings)
        {
            InitAutoBuilding(packageBuildings);
            ApkSavePath = packagePath;
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
            buildLog.Clear();
            Progress = 0;
            
            curBuilding = enumerators.Dequeue();
            curEnumerator = curBuilding.OnBuilding();

            this.mainWindow.SetPanelState(EToolbar.Building);
            startTime = DateTime.Now;

            EditorCoroutines.StartCoroutine(this.onUpdateBuilding(), this.mainWindow);
        }


        public void CanleBuild()
        {
           
            EditorCoroutines.StopAllCoroutines(this.mainWindow);
            
            Debug.LogWarning("!!!-----Canle Build------!!" + enumerators.Count);
        }


        public void AddBuildLog(string log)
        {
            buildLog.Add(log);
            Debug.Log(log);
        }


        public IEnumerator onUpdateBuilding()
        {
            using (new LockAssemblies())
            {
                while (true)
                {
                    if (curEnumerator == null)
                    {
                        clear();
                        Progress = 1f;
                        TimeSpan ts = DateTime.Now - startTime;
                        UseTime = ts.TotalMilliseconds;

                        Debug.Log("<color=green>Build finish!</color>");
                        yield break;
                    }

                    if (!curEnumerator.MoveNext())
                    {
                        finishWeight += curBuilding.Weight;

                        if (enumerators.Count > 0)
                        {
                            curBuilding = enumerators.Dequeue();
                            curEnumerator = curBuilding.OnBuilding();

                            if (curEnumerator == null)
                                Debug.LogError("Wtf building enumrator is null ");
                        }
                        else
                        {
                            curEnumerator = null;
                            curBuilding = null;
                        }
                    }

                    if (curBuilding != null)
                    {
                        Progress = (finishWeight + curBuilding.Progress * curBuilding.Weight) / totalWeight;
                    }

                    yield return null;
                }
            }
        }

        #region Build SDK Config

        /// <summary>
        /// 加载SDK配置
        /// </summary>
        private void loadSDKConfig()
        {
            sdkConfigs.Clear();
            string root = BuilderPreference.SDK_CONFIG_PATH;
            string[] files = Directory.GetFiles(root, "*.json", SearchOption.TopDirectoryOnly);

            NameSDKs = new string[files.Length];
            for (int i = 0; i < files.Length; ++i)
            {
                var config = SDKConfig.LoadSDKConfig(File.ReadAllText(files[i]));
                sdkConfigs.Add(config);
                NameSDKs[i] = config.show_name;
            }
        }

        private void SaveConfig()
        {
//            XmlDocument doc = new XmlDocument();
//            if (File.Exists(DEFAULT_CONFIG_NAME)) File.Delete(DEFAULT_CONFIG_NAME);
//            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
//            doc.AppendChild(dec);
//            var attr = doc.CreateElement("ABConfig");
//            doc.AppendChild(attr);
//            attr.SetAttribute("lastConfigIndex", lastSelectConfigIndex.ToString());
//            foreach (var val in ABData.datas.Values)
//            {
//                val.SerializeToXml(attr);
//            }

//            doc.Save(DEFAULT_CONFIG_NAME);
//            AssetDatabase.Refresh();
//            Debug.Log("<color=#2fd95b>Save Success !</color>");
        }

        #endregion

        private void clear()
        {
            IsDebug = false;
            this.enumerators.Clear();
            totalWeight = 0;
            finishWeight = 0;
        }


        public void OnDestroy()
        {
            
        }
    }
}