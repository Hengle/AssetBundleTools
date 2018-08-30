using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetBundleBuilder
{
    public class AssetBundleBuilderWindow : EditorWindow
    {

        private enum EToolbar
        {
            Home , Setting
        }

        private EToolbar toolbarIndex = EToolbar.Home;

        private Styles styles;

        //left 
        private float letfGUIWidth = 250;
        private int sdkConfigIndex;
        private int autoBuildIndex;  //autobuild
        private int buildingIndex;   //building

        //right
        private SearchField m_SearchField;
        private Texture2D iconReflush;
        private AssetTreeView treeView;
        //[SerializeField]
        private TreeViewState treeViewState; // Serialized in the window layout file so it survives assembly reloading
        //[SerializeField]
        private MultiColumnHeaderState multiColumnHeaderState;

        private AssetTreeModel treeModel;

        //other
        private AssetBuildRuleManager rulManger;

        private AssetBundleBuilder builder;

        [MenuItem("[Build]/Asset Bundle Builder")]
        public static void ShowWindow()
        {
            AssetBundleBuilderWindow window = EditorWindow.GetWindow<AssetBundleBuilderWindow>("Bundle Builder");
            window.Show();
        }


        Rect multiColumnTreeViewRect
        {
            get { return new Rect(letfGUIWidth + 10, 70, position.width - letfGUIWidth - 20, position.height - 120); }
        }

        Rect searchbarRect
        {
            get { return new Rect(letfGUIWidth + 10, 25f, position.width - letfGUIWidth - 20, 20f); }
        }

        #region Unity Standard API

        private void OnEnable()
        {
            styles = new Styles();
            rulManger = AssetBuildRuleManager.Instance;
            builder = new AssetBundleBuilder();

            iconReflush = EditorGUIUtility.FindTexture("TreeEditor.Refresh");

            this.initTreeView(); 
        }


        private void initTreeView()
        {
            treeViewState = new TreeViewState();

            List<AssetElement> treeEles = new List<AssetElement>();
            treeEles.Add(new AssetElement("root" , -1,0));

            treeModel = new AssetTreeModel(treeEles);
            treeModel.modelChanged += onTreeModelChanged;

            var headerState = AssetTreeView.CreateDefaultMultiColumnHeaderState(multiColumnTreeViewRect.width);
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(multiColumnHeaderState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(multiColumnHeaderState, headerState);
            multiColumnHeaderState = headerState;

            MultiColumnHeader multiColumnHeader = new MultiColumnHeader(headerState);
            multiColumnHeader.ResizeToFit();

            treeView = new AssetTreeView(treeViewState, multiColumnHeader, treeModel);
            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += treeView.SetFocusAndEnsureSelectedItem;
                

            
        }

        private void onTreeModelChanged()
        {
            treeView.Reload();
        }


        private void OnGUI()
        {
            drawToolbar();

            GUILayout.BeginHorizontal();
            
            GUILayout.BeginVertical(GUI.skin.box ,GUILayout.Width(letfGUIWidth));
            drawLeftCenterGUI();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(position.width - letfGUIWidth));
            drawRightCenterGUI();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            drawBottomGUI();
        }


        private void OnDestroy()
        {
            builder.OnDestroy();
            rulManger.OnDestroy();
            
        }

        #endregion


        #region ----draw gui----


        private void drawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string[] enumNames = Enum.GetNames(typeof(EToolbar));
            EToolbar[] enumValues = (EToolbar[])Enum.GetValues(typeof(EToolbar));

            for (int i = 0; i < enumNames.Length - 1; i++)
            {
                if (GUILayout.Toggle(toolbarIndex == enumValues[i], enumNames[i], EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    toolbarIndex = enumValues[i];
                    
                }
            }

            GUILayout.Toolbar(0, new[] {""}, EditorStyles.toolbar, GUILayout.ExpandWidth(true));

            if (GUILayout.Toggle(enumNames[(int)toolbarIndex].Equals(enumNames[enumNames.Length - 1]), enumNames[enumNames.Length - 1], EditorStyles.toolbarButton , GUILayout.Width(100)))
            {
                toolbarIndex = enumValues[enumNames.Length - 1];
            }

            EditorGUILayout.EndHorizontal();
        }


        private void drawLeftCenterGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Toggle(false, "Configs", EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Asset Version" , GUILayout.MaxWidth(100));
            GUI.color = Color.gray;
            GUILayout.TextField("0.0.1");
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("App   Version", GUILayout.MaxWidth(100));
            GUI.color = Color.gray;
            GUILayout.TextField("0.0.1");
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("SDK Config", GUILayout.Width(100)))
            {
                Debug.Log("编辑打开！！！！");
            }
            GUI.backgroundColor = Color.red;
            sdkConfigIndex = EditorGUILayout.Popup(sdkConfigIndex, builder.NameSDKs, GUILayout.MaxWidth(160));
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Development Build", builder.IsBuildDev ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.MaxWidth(160));
            builder.IsBuildDev = EditorGUILayout.Toggle(builder.IsBuildDev, GUILayout.MaxWidth(30));
            GUILayout.EndHorizontal();
            if (!this.builder.IsBuildDev)
            {
                this.builder.IsAutoConnectProfile = false;
                this.builder.IsScriptDebug = false;
            }

            EditorGUI.BeginDisabledGroup(!this.builder.IsBuildDev);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Script Debugging", this.builder.IsScriptDebug ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.MaxWidth(160));
            this.builder.IsScriptDebug = EditorGUILayout.Toggle(this.builder.IsScriptDebug, GUILayout.MaxWidth(30));
            GUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Autoconnect Profile", this.builder.IsAutoConnectProfile ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.MaxWidth(160));
            this.builder.IsAutoConnectProfile = EditorGUILayout.Toggle(this.builder.IsAutoConnectProfile, GUILayout.MaxWidth(30));
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            
//            EditorGUILayout.BeginHorizontal();
//            EditorGUILayout.LabelField("Config Table", GUILayout.MaxWidth(160));
//            this.builder.IsDebug = EditorGUILayout.Toggle(this.builder.IsDebug, GUILayout.MaxWidth(30));
//            GUILayout.EndHorizontal();
//
//            EditorGUILayout.BeginHorizontal();
//            EditorGUILayout.LabelField("Lua Script", GUILayout.MaxWidth(160));
//            this.builder.IsDebug = EditorGUILayout.Toggle(this.builder.IsDebug, GUILayout.MaxWidth(30));
//            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Debug Build"))
            {
                builder.OnClickDebugBuild(true);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Buildings", GUILayout.Width(100));
            buildingIndex = EditorGUILayout.Popup(buildingIndex, styles.BuildContents);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Build"))
            {
                builder.OnClickBuild(styles.BuildingOpts[buildingIndex], styles.BuildPackageOpts[buildingIndex]);
            }

            GUILayout.Space(10);
            //GUILayout.Label("", "IN Title");
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Auto Build", GUILayout.Width(100));
            autoBuildIndex = EditorGUILayout.Popup(autoBuildIndex, styles.OnekeyBuilds);
            
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Go"))
            {
                string rootPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../"));
                string filePath = EditorUtility.SaveFilePanel("Tip", rootPath, PlayerSettings.productName, BuilderPreference.AppExtension.Substring(1));
                if (!string.IsNullOrEmpty(filePath))
                {
                    builder.OnClickAutoBuild(filePath , styles.AutoBuilds[autoBuildIndex]);
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(5);
            GUILayout.Label("", "IN Title");
            GUILayout.Space(-5);

            GUILayout.BeginVertical(GUILayout.MaxHeight(this.position.height * 0.5f));
            drawBundleRulePropertys();
            GUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制Bundle Rule 详细配置
        /// </summary>
        private void drawBundleRulePropertys()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Toggle(true , "Bunld Rule Propertys" , EditorStyles.toolbarButton , GUILayout.MaxWidth(120));
            GUILayout.Toolbar(0, new[] { "" }, EditorStyles.toolbar, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            AssetElement lastClickItem = treeModel.Find(this.treeViewState.lastClickedID);

            if (lastClickItem != null && lastClickItem.BuildRule != null)
            {
                AssetBuildRule bundleRule = lastClickItem.BuildRule;

                EditorGUIUtility.labelWidth = 120;

                if (EditorGUILayout.Toggle("Preload", bundleRule.LoadType == ELoadType.PreLoad))
                {
                    bundleRule.LoadType = ELoadType.PreLoad;
                }

                bundleRule.PackageType = (PackageAssetType)EditorGUILayout.EnumPopup("Package Type" , bundleRule.PackageType);

                if (bundleRule.PackageType == PackageAssetType.OutPackage)
                {
                    //非整包资源
                    bundleRule.DownloadOrder = EditorGUILayout.IntField("Download Order", bundleRule.DownloadOrder);
                }

//                GUILayout.BeginHorizontal();
//                GUILayout.Label("Path", GUILayout.Width(50));
//                EditorGUILayout.TextField(bundleRule.Path);
//                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.TextArea(bundleRule.Path, GUILayout.MaxWidth(letfGUIWidth - 10), GUILayout.MaxHeight(40));
            }
            
        }

        private void drawRightCenterGUI()
        {
            GUILayoutOption largeButtonWidth = GUILayout.MaxWidth(120);
            GUILayoutOption nomaleButtonWidth = GUILayout.MaxWidth(80);
            GUILayoutOption miniButtonWidth = GUILayout.MaxWidth(30);
            m_SearchField.OnGUI(searchbarRect , "");

            GUILayout.Space(30);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Options:", nomaleButtonWidth))
            {
                treeView.Toggle = !treeView.Toggle;
            }
            
            if (GUILayout.Button(iconReflush, miniButtonWidth))
            {
                treeModel.AddChildrens(treeView.GetSelection());
            }                
            

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear All AB", GUI.skin.button , largeButtonWidth))
            {
                BuildUtil.ClearAssetBundleName();
                AssetDatabase.Refresh();
            }

            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            Rect treeViewRect = multiColumnTreeViewRect;
            this.treeView.OnGUI(treeViewRect);

            GUILayout.Space(treeViewRect.height + 10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(">>", miniButtonWidth))
            {
                treeView.ExpandAll();
            }
            
            if (GUILayout.Button("+", GUI.skin.button, miniButtonWidth))
            {
                this.addNewRootFolder();
            }

            if (GUILayout.Button("-", GUI.skin.button, miniButtonWidth))
            {
                this.treeModel.RemoveSelectElement();
            }

            if (GUILayout.Button("<<", miniButtonWidth))
            {
                treeView.CollapseAll();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Save", GUI.skin.button, nomaleButtonWidth))
            {
                treeModel.Save();
            }
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
        }


        private void drawBottomGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            GUILayout.Toggle(false, "Version：0.0.1", EditorStyles.toolbarButton, GUILayout.MaxWidth(100));
            EditorGUILayout.EndHorizontal();
        }

        #endregion


        /// <summary>
        /// 添加新的根目录
        /// </summary>
        private void addNewRootFolder()
        {
            string path = EditorUtility.OpenFolderPanel("Add Root Folder", "Assets", "");
            if (string.IsNullOrEmpty(path)) return;

            string relativePath = path.Replace(Application.dataPath, "Assets").Replace('\\', '/');
//            ABData data = new ABData(null, relativePath, "", 0, 0, 0, 0, false, false, 0, false, 0, 0, false);
//            ABData.datas.Add(relativePath, data);

            treeModel.AddRoot(relativePath);

            treeView.Reload();

            //        ABData.datas.Clear();
            //        string[] pathes = Directory.GetDirectories(path, "*.*", SearchOption.TopDirectoryOnly);
            //        for (int i = 0; i < pathes.Length; ++i)
            //        {
            //            var abPath = "Assets" + Path.GetFullPath(pathes[i]).Replace(Path.GetFullPath(Application.dataPath), "").Replace('\\', '/');
            //            var data = new ABData(null, abPath, "", 0, 0, 0, 0, false, false, 0, false, 0, 0, false);
            //            ABData.datas.Add(abPath, data);
            //        }
        }



        
    }

}


