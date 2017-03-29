using System;
using System.Collections.Generic;
using System.ComponentModel;
using BundleChecker.ResoucreAttribute;
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

        private Vector2 scrollPos = Vector2.zero;
        public BundleDetailView()
        {
            titleLabStyle.alignment = TextAnchor.MiddleCenter;
            titleLabStyle.fontSize = 25;
            titleLabStyle.fontStyle = FontStyle.Bold;
            titleLabStyle.richText = true;
        }

        public void OnGUI()
        {
            if (curBundle == null) return;

            GUILayout.Label(string.Format("<color=white>[AssetBundle]<color=green>{0}</color></color>" , curBundle.BundleName) , titleLabStyle);

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
            GUILayout.BeginHorizontal();
            GUILayout.Label("Asset名称" , GUILayout.Width(150));
            GUILayout.Label("类型", GUILayout.Width(100));
            GUILayout.Label("所属AssetBundle");
            GUILayout.Label("是否冗余", GUILayout.Width(100));
            GUILayout.EndHorizontal();

            NGUIEditorTools.DrawSeparator();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            List<ResoucresBean> resList = curBundle.GetAllAssets();
            int indexRow = 0;
            foreach (ResoucresBean res in resList)
            {
                indexRow++;
                GUI.backgroundColor = indexRow % 2 == 0 ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
                GUI.backgroundColor = Color.white;

                GUI.color = selectAsset == res.Name ? Color.green : Color.white;
                if (GUILayout.Button(res.Name, EditorStyles.label , GUILayout.Width(150)))
                {
                    selectAsset = res.Name;
                }
                GUI.color = Color.white;
                GUILayout.Label(res.ResourceType , GUILayout.Width(100));

                //具体的ab名称                
                int column = Mathf.Max(1, (int)((ABMainChecker.MainChecker.Width - 380) / 150));
                if (res.IncludeBundles.Count > 1)
                {
                    GUILayout.BeginVertical();
                    int endIndex = 0;
                    for (int i = 0, maxCount = res.IncludeBundles.Count; i < maxCount; i++)
                    {
                        EditorBundleBean depBundle = res.IncludeBundles[i];
                        if (i%column == 0)
                        {
                            endIndex = i + column;
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

                    GUILayout.Space(15);
                    if (GUILayout.Button("GO" , GUILayout.Width(80) , GUILayout.Height(25)))
                    {
                        ABMainChecker.MainChecker.AssetView.SetResoucre(res);
                    }
                    GUILayout.Space(15);
                }
                
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
                drawDepdencieBundle();
                GUILayout.Space(10);
                drawBeDepdencieBundle();
            GUILayout.EndHorizontal();

        }

        private Vector2 depScrollPos = Vector2.zero;
        /// <summary>
        /// 依赖的Bundle
        /// </summary>
        private void drawDepdencieBundle()
        {
            float width = ABMainChecker.MainChecker.Width*0.5f - 10;
            GUILayout.BeginVertical(GUILayout.Width(width));
            NGUIEditorTools.DrawHeader("依赖的AssetBundle");
            depScrollPos = GUILayout.BeginScrollView(depScrollPos);

            int indexRow = 0;
            foreach (EditorBundleBean depBundle in curBundle.GetAllDependencies())
            {
                indexRow++;
                GUI.backgroundColor = indexRow % 2 == 0 ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
                GUI.backgroundColor = Color.white;

                //Name
                GUILayout.Label(depBundle.BundleName);

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private Vector2 beDepScrollPos = Vector2.zero;
        /// <summary>
        /// 被依赖的Bundle
        /// </summary>
        private void drawBeDepdencieBundle()
        {
            float width = ABMainChecker.MainChecker.Width * 0.5f - 10;
            GUILayout.BeginVertical(GUILayout.Width(width));
            NGUIEditorTools.DrawHeader("被其它AssetBundle依赖");
            beDepScrollPos = GUILayout.BeginScrollView(beDepScrollPos);

            int indexRow = 0;
            foreach (EditorBundleBean bundle in curBundle.GetBedependencies())
            {
                indexRow++;
                GUI.backgroundColor = indexRow % 2 == 0 ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
                GUI.backgroundColor = Color.white;

                //Name
                GUILayout.Label(bundle.BundleName);

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        #endregion


        #region --------------绘制Texture分页--------------------

        private ResPropertyGUI[] texPropertys = new[]
        {
            new ResPropertyGUI{ PropertyName = ResourceGlobalProperty.Name, GuiWidth = 150},
            new ResPropertyGUI{ PropertyName = TextureAttribute.WIDTH, GuiWidth = 80},
            new ResPropertyGUI{ PropertyName = TextureAttribute.HEIGHT, GuiWidth = 80},
            new ResPropertyGUI{ PropertyName = TextureAttribute.MEMORYSIZE, GuiWidth = 100},
            new ResPropertyGUI{ PropertyName = TextureAttribute.FORMAT, GuiWidth = 150},
            new ResPropertyGUI{ PropertyName = TextureAttribute.MIPMAP, GuiWidth = 80},
            new ResPropertyGUI{ PropertyName = TextureAttribute.READWRITE, GuiWidth = 80},
            new ResPropertyGUI{ PropertyName = ResourceGlobalProperty.AssetBundles, GuiWidth = 380},
        };
        /// <summary>
        /// 贴图信息
        /// </summary>
        private void drawTextureAssets()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("贴图名称", GUILayout.MinWidth(150));
            GUILayout.Label("宽度", GUILayout.MinWidth(80));
            GUILayout.Label("高度", GUILayout.MinWidth(80));
            GUILayout.Label("内存占用", GUILayout.MinWidth(100));
            GUILayout.Label("压缩格式", GUILayout.MinWidth(150));
            GUILayout.Label("MipMap", GUILayout.MinWidth(80));
            GUILayout.Label("Read/Write", GUILayout.MinWidth(80));
            GUILayout.Label("AB数量", GUILayout.MinWidth(380));
            GUILayout.EndHorizontal();

            NGUIEditorTools.DrawSeparator();

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            int indexRow = 0;
            foreach (ResoucresBean res in curBundle.GetAllAssets())
            {
                if(res.ResourceType != EResoucresTypes.TextureType) continue;

                indexRow++;
                GUI.backgroundColor = indexRow % 2 == 0 ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
                GUI.backgroundColor = Color.white;

                if (res.RawRes == null)
                {
                    GUI.color = Color.red;
                    GUILayout.Label(res.Name);
                    GUI.color = Color.white;
                }
                else
                {
                    res.RawRes.SetPropertyGUI(texPropertys);
                    res.RawRes.OnGUI();
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
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
            ABMainChecker.MainChecker.SetCurrentView(ABMainChecker.EView.BundleDetailView);
        }
    }
}