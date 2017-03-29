using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BundleChecker
{
    /// <summary>
    /// 资源分布页
    /// </summary>
    public class AssetDistributeView
    {
        private Vector2 scrollPos = Vector2.zero;
        private int indexRow;

        private ResoucresBean curRes;
       
        private GUIStyle titleLabStyle = new GUIStyle();
        public AssetDistributeView()
        {
            titleLabStyle.alignment = TextAnchor.MiddleCenter;
            titleLabStyle.fontSize = 25;
            titleLabStyle.fontStyle = FontStyle.Bold;
            titleLabStyle.richText = true;
        }
        public void OnGUI()
        {
            GUILayout.Label(string.Format("<color=white>[{1}]<color=green>{0}</color></color>", curRes.Name , curRes.ResourceType), titleLabStyle);

            if(curRes.Dependencies.Count > 0)
                drawDependencieAsset();

            NGUIEditorTools.DrawSeparator();

            drawAllBundles();
        }


        public void SetResoucre(ResoucresBean res)
        {
            curRes = res;

            ABMainChecker.MainChecker.SetCurrentView(ABMainChecker.EView.AssetDistributeView);
        }
        /// <summary>
        /// 冗余的资源
        /// </summary>
        private void drawAllBundles()
        {
            //all assets
            NGUIEditorTools.DrawHeader("All AssetBundle");

            GUILayout.BeginHorizontal();
            GUILayout.Toggle(false, "所属AssetBundle名称", "ButtonLeft");
            //GUILayout.Toggle(false, "依赖数量", "ButtonMid", GUILayout.Width(80));
            //GUILayout.Toggle(false, "所属AssetBundle文件", "ButtonMid");
            GUILayout.Toggle(false, "详细", "ButtonRight", GUILayout.Width(100));
            GUILayout.EndHorizontal();

            foreach (EditorBundleBean bundle in curRes.IncludeBundles)
            {
                drawBundle(bundle);
            }
        }

        private void drawBundle(EditorBundleBean bundle)
        {
            indexRow++;
            GUI.backgroundColor = indexRow % 2 == 0 ? Color.white : new Color(0.8f, 0.8f, 0.8f);
            GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
            GUI.backgroundColor = Color.white;
            //名称
            GUILayout.Label(bundle.BundleName);
            
            //查询
            GUILayout.Space(15);
            if (GUILayout.Button("GO", GUILayout.Width(50), GUILayout.Height(25)))
            {
                ABMainChecker.MainChecker.DetailBundleView.SetCurrentBundle(bundle);
                
            }
            GUILayout.Space(15);
            GUILayout.EndHorizontal();
        }

        #region -------------------依赖的资源----------------------------

        private void drawDependencieAsset()
        {
            NGUIEditorTools.DrawHeader("Dependencies");

            int column = 3;
            int columnWidth = Mathf.Max( 1, (int)(ABMainChecker.MainChecker.Width - 30)/ column);

            GUILayout.BeginVertical();
            int endIndex = 0;
            string missingStr = "{0}(missing)";
            int i = 0;
            foreach (ResoucresBean depRes in curRes.Dependencies.Values)
            {
                if (i % column == 0)
                {
                    endIndex = i + column - 1;
                    GUILayout.BeginHorizontal();
                }

                if (depRes.IsMissing)
                {
                    GUI.backgroundColor = Color.red ;
                    GUILayout.Button(string.Format(missingStr, depRes.Name), GUILayout.Width(columnWidth));
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    if (GUILayout.Button(depRes.Name, GUILayout.Width(columnWidth)))
                    {
                        ABMainChecker.MainChecker.AssetView.SetResoucre(depRes);
                    }
                }

                GUILayout.Space(5);

                if (i == endIndex)
                {
                    endIndex = 0;
                    GUILayout.EndHorizontal();
                }
                i ++;
            }
            if (endIndex != 0) GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        

        #endregion

    }
}