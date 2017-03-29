using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BundleChecker
{
    /// <summary>
    /// AssetBundle资源总览
    /// </summary>
    public class ABOverview
    {

        private Vector2 scrollPos = Vector2.zero;

        private int indexRow;
        private int loadIndex;
        private float loadCount;

        private enum EView
        {
            ALLAsset , RedundancyAssets , MissingAsset
        }

        private EView curView = EView.ALLAsset;
        //冗余的bundle资源
        private Dictionary<string , EditorBundleBean> redundancyDic = new Dictionary<string, EditorBundleBean>();
        public void OnGUI()
        {
            string curFolder = CurFolderRoot;
            Dictionary<string, EditorBundleBean> bundles = ABMainChecker.MainChecker.BundleList;
            if (curFolder != Application.dataPath && bundles.Count ==0)
                findAllBundles();

            NGUIEditorTools.DrawHeader("检测AssetBundle");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Asset Bundles", GUILayout.Width(120));
            
            if (curFolder.StartsWith(Application.dataPath))
            {
                curFolder = curFolder.Replace(Application.dataPath, "Assets");
            }
            GUILayout.TextField(curFolder);
            if (GUILayout.Button("..." , GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFolderPanel("Select", CurFolderRoot , "");
                CurFolderRoot = path;
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Go Check" , GUILayout.Height(30)))
            {
                this.findAllBundles();
            }

            //Overview
            NGUIEditorTools.DrawSeparator();
            drawOverview(bundles);

            switch (curView)
            {
                    case EView.ALLAsset:
                    drawAllAssetBundle();
                    break;
                    case EView.RedundancyAssets:
                    drawAllRedundancyAsset();
                    break;
                    case EView.MissingAsset:
                    drawMissingAsset();
                    break;
            }
        }

        /// <summary>
        /// 总览
        /// </summary>
        private void drawOverview(Dictionary<string, EditorBundleBean> bundles)
        {
            ABMainChecker mainCheckr = ABMainChecker.MainChecker;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(string.Format("总资源数：{0}" , mainCheckr.BundleList.Count) , GUILayout.Height(50)))
            {
                scrollPos = Vector2.zero;
                curView = EView.ALLAsset;
                
            }

            if (GUILayout.Button(string.Format("冗余资源数：{0}", redundancyDic.Count), GUILayout.Height(50)))
            {
                scrollPos = Vector2.zero;
                curView = EView.RedundancyAssets;
            }

            if (GUILayout.Button(string.Format("丢失AB数：{0}", mainCheckr.MissingRes.Count), GUILayout.Height(50)))
            {
                scrollPos = Vector2.zero;
                selectAsset = "";
                curView = EView.MissingAsset;
            }
            GUILayout.EndHorizontal();
        }

        #region --------------All AssetBundle------------------
        
        private void drawAllAssetBundle()
        {
            //all assets
            NGUIEditorTools.DrawHeader("All AssetBundle");

            GUILayout.BeginHorizontal();
            GUILayout.Toggle(false, "AssetBundle 名称", "ButtonLeft", GUILayout.Width(200));
            GUILayout.Toggle(false, "依赖数量", "ButtonMid", GUILayout.Width(80));
            GUILayout.Toggle(false, "具体依赖文件", "ButtonMid");
            GUILayout.Toggle(false, "详细", "ButtonRight", GUILayout.Width(80));
            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            indexRow = 0;

            foreach (EditorBundleBean bundle in ABMainChecker.MainChecker.BundleList.Values)
            {
                drawRowBundle(bundle);
            }
            GUILayout.EndScrollView();
        }

        private void drawRowBundle(EditorBundleBean bundle)
        {
            indexRow ++;
            GUI.backgroundColor = indexRow % 2 == 0 ? Color.white : new Color(0.8f, 0.8f, 0.8f);
            GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
            GUI.backgroundColor = Color.white;
            //名称
            GUILayout.Label(bundle.BundleName , GUILayout.Width(200));
            //依赖数量
            List<EditorBundleBean> dependencies = bundle.GetAllDependencies();
            GUILayout.Label(dependencies.Count.ToString() , GUILayout.Width(80));
            //具体的ab名称
            GUILayout.BeginVertical();
            int column = Mathf.Max( 1, (int)((ABMainChecker.MainChecker.Width - 380)/150));
            int endIndex = 0;
            for (int i = 0 , maxCount = dependencies.Count ; i < maxCount; i++)
            {
                EditorBundleBean depBundle = dependencies[i];
                if (i % column == 0)
                {
                    endIndex = i + column - 1;
                    GUILayout.BeginHorizontal();
                }
                if (GUILayout.Button(depBundle.BundleName, GUILayout.Width(150)))
                {
                    ABMainChecker.MainChecker.DetailBundleView.SetCurrentBundle(depBundle);
                }
                if (i == endIndex)
                {
                    endIndex = 0;
                    GUILayout.EndHorizontal();
                }
            }
            if (endIndex != 0) GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            //查询
            GUILayout.Space(15);
            if (GUILayout.Button("GO" , GUILayout.Width(50) , GUILayout.Height(25)))
            {
                ABMainChecker.MainChecker.DetailBundleView.SetCurrentBundle(bundle);
            }
            GUILayout.Space(15);
            GUILayout.EndHorizontal();
        }
        #endregion

        #region --------------Redundancy Asset------------------
        /// <summary>
        /// 冗余的资源
        /// </summary>
        private void drawAllRedundancyAsset()
        {
            //all assets
            NGUIEditorTools.DrawHeader("All Redundancy AssetBundle");

            GUILayout.BeginHorizontal();
            GUILayout.Toggle(false, "AssetBundle 名称", "ButtonLeft", GUILayout.Width(200));
            GUILayout.Toggle(false, "Mesh", "ButtonMid");
            GUILayout.Toggle(false, "Material", "ButtonMid");
            GUILayout.Toggle(false, "Texture", "ButtonMid");
            GUILayout.Toggle(false, "Shader", "ButtonMid");
            GUILayout.Toggle(false, "详细", "ButtonRight", GUILayout.Width(80));
            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            indexRow = 0;
            foreach (EditorBundleBean bundle in redundancyDic.Values)
            {
                drawRowRedundancyAsset(bundle);
            }
            GUILayout.EndScrollView();
        }

        private void drawRowRedundancyAsset(EditorBundleBean bundle)
        {
            indexRow++;
            GUI.backgroundColor = indexRow % 2 == 0 ? Color.white : new Color(0.8f, 0.8f, 0.8f);
            GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
            GUI.backgroundColor = Color.white;
            //名称
            GUILayout.Label(bundle.BundleName, GUILayout.Width(200));
            //Mesh
            int count = getAssetDedundancyCount(bundle, EResoucresTypes.MeshType);
            GUILayout.Label(count.ToString());
            //material
            count = getAssetDedundancyCount(bundle, EResoucresTypes.MatrialType);
            GUILayout.Label(count.ToString());
            //Texture
            count = getAssetDedundancyCount(bundle, EResoucresTypes.TextureType);
            GUILayout.Label(count.ToString());
            //Shader
            count = getAssetDedundancyCount(bundle, EResoucresTypes.ShaderType);
            GUILayout.Label(count.ToString());
            //查询
            GUILayout.Space(15);
            if (GUILayout.Button("GO", GUILayout.Width(50), GUILayout.Height(25)))
            {
                ABMainChecker.MainChecker.DetailBundleView.SetCurrentBundle(bundle);
            }
            GUILayout.Space(15);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 冗余资源被反复引用的次数
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="resType"></param>
        /// <returns></returns>
        private int getAssetDedundancyCount(EditorBundleBean bundle, string resType)
        {
            int count = 0;
            List<ResoucresBean> resList = bundle.GetAllAssets();
            foreach (ResoucresBean res in resList)
            {
                if (resType == res.ResourceType) count ++;
            }
            return count;
        }
        #endregion


        #region ----------------------丢失的Bundle资源----------------------------------------

        private string selectAsset = "";
        private void drawMissingAsset()
        {
            //all assets
            NGUIEditorTools.DrawHeader("All Missing AssetBundle");

            GUILayout.BeginHorizontal();
            GUILayout.Toggle(false, "Asset 名称", "ButtonLeft", GUILayout.Width(200));
            GUILayout.Toggle(false, "类型", "ButtonMid" , GUILayout.Width(100));
            GUILayout.Toggle(false, "路径", "ButtonRight");
            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            indexRow = 0;
            List<ResoucresBean> missingResList = ABMainChecker.MainChecker.MissingRes;
            foreach (ResoucresBean res in missingResList)
            {
                indexRow++;
                GUI.backgroundColor = indexRow % 2 == 0 ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
                GUI.backgroundColor = Color.white;

                bool isCheck = false;
                //名称
                GUI.color = selectAsset == res.Name ? Color.green : Color.white;
                isCheck = GUILayout.Button(res.Name, EditorStyles.label, GUILayout.Width(200));
                //type
                GUILayout.Label(res.ResourceType , GUILayout.Width(100));
                //Path
                isCheck = (GUILayout.Button(res.AssetPath , EditorStyles.label) ? true : isCheck);
                GUI.color = Color.white;
                if (isCheck)
                {
                    selectAsset = res.Name;
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(res.AssetPath);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        #endregion
        /// <summary>
        /// 查找指定目录的Bundles
        /// </summary>
        private void findAllBundles()
        {
            string rootPath = CurFolderRoot;
            if (!Directory.Exists(rootPath)) return;

            string[] fileArr = Directory.GetFiles(rootPath, "*" + ABMainChecker.AssetBundleSuffix, SearchOption.AllDirectories);
            Dictionary<string , EditorBundleBean> bundleDic = ABMainChecker.MainChecker.BundleList;
            bundleDic.Clear();

            EditorUtility.DisplayProgressBar("查找中", "正在查找文件...", 1.0f / fileArr.Length);
            loadCount = (float)fileArr.Length;
            loadIndex = 0;

            string dataPath = Application.dataPath;
            for (int i = 0 , maxCount = fileArr.Length; i < maxCount; i++)
            {
                string abPath = fileArr[i].Replace("\\", "/");
                if (!bundleDic.ContainsKey(abPath))
                {
                    EditorBundleBean bundleBean = new EditorBundleBean(abPath);
                    bundleDic[bundleBean.BundlePath] = bundleBean;
                    loadAssetBundle(bundleBean);
                }
            }
            EditorUtility.DisplayProgressBar("分析中", "分析冗余", 1.0f);

            Dictionary<string, ResoucresBean> resDic = ABMainChecker.MainChecker.ResourceDic;
            ResoucresBean[] resArr = new ResoucresBean[resDic.Count];
            resDic.Values.CopyTo(resArr , 0);

            redundancyDic.Clear();
            foreach (ResoucresBean res in resArr)
            {
                res.CheckDependencies();

                if (res.IncludeBundles.Count <= 1) continue;

                foreach (EditorBundleBean bundle in res.IncludeBundles)
                {
                    redundancyDic[bundle.BundleName] = bundle;
                }
            }
            EditorUtility.ClearProgressBar();
        }


        private void loadAssetBundle(EditorBundleBean bundle)
        {
            loadIndex++;
            EditorUtility.DisplayProgressBar("分析中", "分析AssetBundle资源...", loadIndex / loadCount);

            string manifest = string.Concat(bundle.BundlePath, ".manifest");
            manifest = File.ReadAllText(manifest);
            string[] manifestInfoArr = manifest.Split('\n');

            //查找包含资源
            string[] bundInfo = getBundleInfo(manifestInfoArr, "Assets:");
            List<ResoucresBean> allAssets = bundle.GetAllAssets();

            ABMainChecker mainCheck = ABMainChecker.MainChecker;
            foreach (string assetPath in bundInfo)
            {
                string assetName = Path.GetFileName(assetPath);
                ResoucresBean rb = null;
                if (!mainCheck.ResourceDic.TryGetValue(assetName , out rb))
                {
                    rb = new ResoucresBean(assetPath);
                    mainCheck.ResourceDic[assetName] = rb;
                }

                if(!rb.IncludeBundles.Contains(bundle))
                    rb.IncludeBundles.Add(bundle);

                allAssets.Add(rb);
            }

            //查找依赖
            bundInfo = getBundleInfo(manifestInfoArr, "Dependencies:");
            Dictionary<string, EditorBundleBean> bundles = ABMainChecker.MainChecker.BundleList;
            List<EditorBundleBean> depBundles = bundle.GetAllDependencies();
            foreach (string assetPath in bundInfo)
            {
                EditorBundleBean depBundle = null;
                if (!bundles.TryGetValue(assetPath, out depBundle))
                {
                    depBundle = new EditorBundleBean(assetPath);
                    bundles[assetPath] = depBundle;
                    loadAssetBundle(depBundle);
                }

                //依赖记录
                if (!depBundles.Contains(depBundle))
                    depBundles.Add(depBundle);

                //被依赖
                List<EditorBundleBean> beDepBundles = depBundle.GetBedependencies();
                if (!beDepBundles.Contains(bundle))
                    beDepBundles.Add(bundle);
            }
        }


        private string[] getBundleInfo(string[] manifestArr, string key)
        {
            bool isStart = false;
            List<string> infos = new List<string>();
            foreach (string str in manifestArr)
            {

                if (isStart)
                {
                    if (!str.StartsWith("-")) break;
                    infos.Add(str.Replace("-","").Trim());
                }

                if (str.StartsWith(key))
                {
                    isStart = true;
                }
            }
            return infos.ToArray();
        }

        public string CurFolderRoot
        {
            get
            {
                return EditorPrefs.GetString("ABChecker_OverView_defultFolder", Application.dataPath);
            }
            set
            {
                EditorPrefs.SetString("ABChecker_OverView_defultFolder", value);
            }
        }
    }



}