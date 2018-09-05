using System;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{
    public class AssetBundleBuilderWindow : EditorWindow
    {
        private EToolbar toolbarIndex = EToolbar.Home;
        
        private AssetBundleBuilder builder;

        private APanel curPanel;
        private APanel[] panels;

        public AssetBundleBuilder Builder
        {
            get { return builder; }
        }
        
        [MenuItem("[Build]/Asset Bundle Builder")]
        public static void ShowWindow()
        {
            AssetBundleBuilderWindow window = EditorWindow.GetWindow<AssetBundleBuilderWindow>("Bundle Builder");
            window.Show();
        }

        #region Unity Standard API

        private void OnEnable()
        {
            if (builder != null) return;

            builder = new AssetBundleBuilder(this);

            panels = new APanel[3];
            panels[0] = new BundleSettingPanel(this);
            panels[1] = new DetailBuildingPanel(this);
            panels[2] = new SettingPanel(this);

            curPanel = panels[0];
            curPanel.OnInit();
        }



        private void OnGUI()
        {
            drawToolbar();

            if(curPanel != null)
                curPanel.OnGUI();

            GUILayout.FlexibleSpace();
            drawBottomGUI();
        }


        private void OnDestroy()
        {
            builder.OnDestroy();
            
            curPanel.OnDestroy();
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


            if (curPanel != panels[(int)toolbarIndex])
            {
                curPanel = panels[(int)toolbarIndex];
                if(!curPanel.IsInited)
                    curPanel.OnInit();
            }

            EditorGUILayout.EndHorizontal();
        }




        private void drawBottomGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            GUILayout.Toggle(false, "Version：0.0.1", EditorStyles.toolbarButton, GUILayout.MaxWidth(100));
            EditorGUILayout.EndHorizontal();
        }

        #endregion


        public void SetPanelState(EToolbar toolbar)
        {
            this.toolbarIndex = toolbar;
        }
    }

}


