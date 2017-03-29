using System.Collections.Generic;
using UnityEngine;

namespace BundleChecker.ResoucreAttribute
{
    /// <summary>
    /// 资源公共属性
    /// </summary>
    public sealed class ResourceGlobalProperty
    {
        public const string Name = "Name";
        public const string ResType = "ResType";
        public const string AssetBundles = "Bundles";
    }
    /// <summary>
    /// 资源基础
    /// </summary>
    public abstract class ABaseResource
    {
        private List<ResPropertyGUI> attributeGUI = new List<ResPropertyGUI>();

        protected ResoucresBean mainAsset;
        public ABaseResource(ResoucresBean res)
        {
            this.mainAsset = res;
        }

        public void AddProperty(string property, float guiWidth = 0)
        {
            ResPropertyGUI propertyGui = new ResPropertyGUI();
            propertyGui.PropertyName = property;
            propertyGui.GuiWidth = guiWidth;

            attributeGUI.Add(propertyGui);
        }


        public void SetPropertyGUI(ResPropertyGUI[] propertys)
        {
            this.attributeGUI.Clear();
            attributeGUI.AddRange(propertys);
        }


        public void OnGUI()
        {
            foreach (ResPropertyGUI rpgui in attributeGUI)
            {
                if (rpgui.GuiWidth > 0)
                {
                    if (rpgui.PropertyName == ResourceGlobalProperty.AssetBundles)
                    {
                        GUILayout.BeginVertical();
                        int column = 2;
                        int endIndex = 0;
                        for (int i = 0, maxCount = mainAsset.IncludeBundles.Count; i < maxCount; i++)
                        {
                            EditorBundleBean depBundle = mainAsset.IncludeBundles[i];
                            if (i % column == 0)
                            {
                                endIndex = i + column - 1;
                                GUILayout.BeginHorizontal(GUILayout.MaxWidth(rpgui.GuiWidth));
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
                    }else
                        GUILayout.Label(getPropertyValue(rpgui.PropertyName) , GUILayout.MinWidth(rpgui.GuiWidth));
                }
                else
                {
                    GUILayout.Label(getPropertyValue(rpgui.PropertyName));
                }
            }
        }

        protected virtual string getPropertyValue(string property)
        {
            if (property == ResourceGlobalProperty.Name) return this.mainAsset.Name;
            if (property == ResourceGlobalProperty.ResType) return this.mainAsset.ResourceType;

            return string.Empty;
        }
    }
}