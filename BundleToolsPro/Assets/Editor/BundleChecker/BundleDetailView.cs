using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BundleChecker
{
    /// <summary>
    /// AssetBundle包内详细
    /// </summary>
    public class BundleDetailView
    {
        private EditorBundleBean curBundle;

        private GUIStyle titleLabStyle = new GUIStyle();

        private int curTabIndex = 0;

        private string selectAsset ="";

        public void OnGUI()
        {
            if (curBundle == null) return;

            titleLabStyle.fontSize = 25;
            titleLabStyle.fontStyle = FontStyle.Bold;
            titleLabStyle.richText = true;
            GUILayout.Label(string.Format("<color=white>AssetBundle:<color=green>{0}</color></color>" , curBundle.BundleName) , titleLabStyle);

            drawTitle();
            
            if (curTabIndex == 0)
            {
                drawAllAssets();
            }else if (curTabIndex == 1)
                drawTextureAssets();
            else if (curTabIndex == 2)
                drawMaterialAssets();
            else if (curTabIndex == 3)
                drawMeshAssets();
            else if (curTabIndex == 4)
                drawShaderAssets();
        }

        private void drawTitle()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(curTabIndex == 0, "所有资源", "ButtonLeft")) curTabIndex = 0;
            if (GUILayout.Toggle(curTabIndex == 1, "Texture", "ButtonMid")) curTabIndex = 1;
            if (GUILayout.Toggle(curTabIndex == 2, "Matrial", "ButtonMid")) curTabIndex = 2;
            if (GUILayout.Toggle(curTabIndex == 3, "Mesh", "ButtonMid")) curTabIndex = 3;
            if (GUILayout.Toggle(curTabIndex == 4, "Shader", "ButtonRight")) curTabIndex = 4;
            GUILayout.EndHorizontal();
        }

        #region ----------所有资源--------------
        private void drawAllAssets()
        {
            drawAllAssetTitle();
            NGUIEditorTools.DrawSeparator();

            List<ResoucresBean> resList = curBundle.GetAllAssets();
            int indexRow = 0;
            foreach (ResoucresBean res in resList)
            {
                indexRow++;
                GUI.backgroundColor = indexRow % 2 == 0 ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
                GUI.backgroundColor = Color.white;

                GUI.color = selectAsset == res.Name ? Color.green : Color.white;
                if (GUILayout.Button(res.Name, EditorStyles.label))
                {
                    selectAsset = res.Name;
                }
                GUI.color = Color.white;
                GUILayout.Label(res.ResourceType , GUILayout.Width(100));
                GUILayout.Toggle(false,"", GUILayout.Width(100));
                GUILayout.EndHorizontal();
            }
        }

        private void drawAllAssetTitle()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Asset名称");
            GUILayout.Label("类型" , GUILayout.Width(100));
            GUILayout.Label("是否冗余" , GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }
        #endregion


        #region --------------绘制Texture分页--------------------
        private void drawTextureAssets()
        {
            drawTextureAssetsTitle();
            NGUIEditorTools.DrawSeparator();

        }

        private void drawTextureAssetsTitle()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("贴图名称", GUILayout.MinWidth(150));
            GUILayout.Label("AB数量", GUILayout.MinWidth(80));
            GUILayout.Label("宽度", GUILayout.MinWidth(80));
            GUILayout.Label("高度", GUILayout.MinWidth(80));
            GUILayout.Label("内存占用", GUILayout.MinWidth(100));
            GUILayout.Label("压缩格式", GUILayout.MinWidth(100));
            GUILayout.Label("MipMap", GUILayout.MinWidth(80));
            GUILayout.Label("Read/Write", GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
        }
        #endregion


        private void drawMaterialAssets()
        {
            
        }

        private void drawMeshAssets()
        {
            
        }

        private void drawShaderAssets()
        {
            
        }

        public void SetCurrentBundle(EditorBundleBean bundle)
        {
            this.curBundle = bundle;
        }
    }
}