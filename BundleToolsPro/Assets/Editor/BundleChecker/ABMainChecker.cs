using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BundleChecker
{
    /// <summary>
    /// AssetBundle检测工具主入口
    /// </summary>
    public class ABMainChecker : EditorWindow
    {
        public const string AssetBundleSuffix = ".ab";
        //排除文件
        public static HashSet<string> ExcludeFiles = new HashSet<string>(new []{".cs"});   

        private ABOverview overview = new ABOverview();
        
        private BundleDetailView bundleDetailView = new BundleDetailView();

        private AssetDistributeView assetView = new AssetDistributeView();

        public enum EView
        {
            OverView, BundleDetailView , AssetDistributeView
        }

        private EView curView = EView.OverView;

        public static ABMainChecker MainChecker;

        public Dictionary<string , EditorBundleBean> BundleList = new Dictionary<string, EditorBundleBean>();
        /// <summary>
        /// 资源池
        /// </summary>
        public Dictionary<string , ResoucresBean> ResourceDic = new Dictionary<string, ResoucresBean>();
        /// <summary>
        /// 丢失的资源
        /// </summary>
        public List<ResoucresBean> MissingRes = new List<ResoucresBean>();
         
        [MenuItem("AB冗余检测/Bundle Checker")]
        public static void ShowChecker()
        {
            MainChecker = EditorWindow.GetWindow<ABMainChecker>();
        }


        
        void OnGUI()
        {
            switch (curView)
            {
                    case EView.OverView:
                    overview.OnGUI();
                    break;
                default:
                    if (GUILayout.Button("< Back" , GUILayout.Width(100) , GUILayout.Height(30)))
                    {
                        curView = EView.OverView;
                    }
                    break;
            }

            switch (curView)
            {
                case EView.BundleDetailView:
                    bundleDetailView.OnGUI();
                    break;
                case EView.AssetDistributeView:
                    assetView.OnGUI();
                    break;
            }
        }

        public BundleDetailView DetailBundleView { get { return bundleDetailView;} }

        public AssetDistributeView AssetView { get { return assetView; } }

        public void SetCurrentView(EView view)
        {
            this.curView = view;
        }

        public float Width
        {
            get { return MainChecker.position.width; }
        }

        public float Height
        {
            get { return MainChecker.position.height;}
        }
    }
}