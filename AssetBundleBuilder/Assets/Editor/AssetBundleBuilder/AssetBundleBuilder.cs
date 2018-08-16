using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace AssetBundleBuilder
{
    public class AssetBundleBuilder
    {
        private Queue<ABuilding> enumerators = new Queue<ABuilding>();

        private IEnumerator curEnumerator;
        private ABuilding curBuilding;

        public bool IsDebug; //本地调试
        public bool IsBuildDev;
        public bool IsScriptDebug;
        public bool IsAutoConnectProfile;

        public BuildType BuildAssetType;

        public Dictionary<string, AssetMap> AssetMaps;

        public float Progress
        {
            get { return totalWeight <= 0 ? 0 : finishWeight /totalWeight; }
        }

        private int totalWeight;
        private float finishWeight;
        
        private StringBuilder buildLog;

        /// <summary>
        /// 初始化打包流程
        /// </summary>
        public void InitBuilding()
        {
            this.AddBuilding(new SvnUpdateBuilding());
            this.AddBuilding(new AssetBundleBinding());
            this.AddBuilding(new CompressBuilding());
            this.AddBuilding(new PackageBuilding());
            this.AddBuilding(new SvnCommitBuilding());
            this.AddBuilding(new CDNBuilding());
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
            curBuilding = enumerators.Dequeue();
            curEnumerator = curBuilding.OnBuilding();
            if(buildLog == null)
                buildLog = new StringBuilder();
            buildLog.Length = 0;
            EditorApplication.update += onUpdateBuilding;
        }


        public void CanleBuild()
        {
            
            totalWeight = 0;
            EditorApplication.update -= onUpdateBuilding;
        }


        public void AddBuildLog(string log)
        {
            buildLog.AppendLine(log);
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
                    
            }
        }
    }
}