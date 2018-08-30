using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.CustomTreeView;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetBundleBuilder
{
    public class AssetTreeView : TreeViewWithTreeModel<AssetElement>
    {
        const float kRowHeights = 28f;
        const float kToggleWidth = 20f;

        static Texture2D[] icons =
        {
            EditorGUIUtility.FindTexture ("Folder Icon"),
            EditorGUIUtility.FindTexture ("Textrue Icon"),
            EditorGUIUtility.FindTexture ("PreMatSphere"),

            EditorGUIUtility.FindTexture ("AudioSource Icon"),
            EditorGUIUtility.FindTexture ("Animation Icon"),
            EditorGUIUtility.FindTexture ("AnimatorController Icon"),
            EditorGUIUtility.FindTexture ("PrefabModel Icon"),
            EditorGUIUtility.FindTexture ("Shader Icon"),
            EditorGUIUtility.FindTexture ("PrefabNormal Icon"),
            EditorGUIUtility.FindTexture ("ScriptableObject Icon"),
            EditorGUIUtility.FindTexture ("UnityLogo"),
            EditorGUIUtility.FindTexture ("Font Icon"),
            EditorGUIUtility.FindTexture ("GameObject Icon"),
            EditorGUIUtility.FindTexture ("Camera Icon"),
            EditorGUIUtility.FindTexture ("Windzone Icon"),
            EditorGUIUtility.FindTexture ("GameObject Icon")
        };

        private Texture2D iconIgnore = EditorGUIUtility.FindTexture("TreeEditor.Trash");
        private GUIStyle miniButton;

        public bool Toggle;

        private AssetTreeModel assetTreeModel;
        public AssetTreeView(TreeViewState state, TreeModel<AssetElement> model) : 
            base(state, model)
        {

        }

        public AssetTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader, TreeModel<AssetElement> model) : 
            base(state, multiColumnHeader, model)
        {
            assetTreeModel = model as AssetTreeModel;

            rowHeight = kRowHeights;
            columnIndexForTreeFoldouts = 1;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI
            extraSpaceBeforeIconAndLabel = kToggleWidth;
            multiColumnHeader.sortingChanged += OnSortingChanged;
            
            Reload();
        }


        void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SortIfNeeded(rootItem, GetRows());
        }

        void SortIfNeeded(TreeViewItem root, IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
                return;

            if (multiColumnHeader.sortedColumnIndex == -1)
            {
                return; // No column to sort for (just use the order the data are in)
            }

            // Sort the roots of the existing tree items
            //SortByMultipleColumns();
            TreeToList(root, rows);
            Repaint();
        }

        public void TreeToList(TreeViewItem root, IList<TreeViewItem> result)
        {
            if (root == null)
                throw new NullReferenceException("root");
            if (result == null)
                throw new NullReferenceException("result");

            result.Clear();

            if (root.children == null)
                return;

            Stack<TreeViewItem> stack = new Stack<TreeViewItem>();
            for (int i = root.children.Count - 1; i >= 0; i--)
                stack.Push(root.children[i]);

            while (stack.Count > 0)
            {
                TreeViewItem current = stack.Pop();
                result.Add(current);

                if (current.hasChildren && current.children[0] != null)
                {
                    for (int i = current.children.Count - 1; i >= 0; i--)
                    {
                        stack.Push(current.children[i]);
                    }
                }
            }
        }
        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (TreeViewItem<AssetElement>)args.item;
//            GUIStyle gs = EditorStyles.textField;
//            gs.fontSize = 12;
//            gs.fixedHeight = 20;
//            GUIStyle label = EditorStyles.label;
//            label.fontSize = 13;
//            label.fixedHeight = 25;
//
//            GUIStyle boldLabel = EditorStyles.boldLabel;
//            boldLabel.fontSize = 13;
//            boldLabel.fixedHeight = 25;
//
//            GUIStyle popup = EditorStyles.popup;
//            popup.fontSize = 13;
//            popup.fixedHeight = 20;
            miniButton = EditorStyles.miniButton;
            miniButton.fontSize = 13;
            miniButton.fixedHeight = 20;

            AssetElement parentItem = item.data.parent as AssetElement;
            bool isTogetherDisable = false;
            if(item.data.FileType == FileType.Folder)
                isTogetherDisable = (parentItem.BuildRule.BuildType & (int)BundleBuildType.TogetherFolders) != 0;
            else
                isTogetherDisable = (parentItem.BuildRule.BuildType & (int)BundleBuildType.TogetherFiles) != 0;

            bool isIgnore = item.data.BuildRule.BuildType == 0 && parentItem.BuildRule.BuildType == 0;

            bool isDisable = isTogetherDisable || isIgnore;

            EditorGUI.BeginDisabledGroup(isDisable);

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (AssetTreeHeader)args.GetColumn(i), ref args);
            }

            EditorGUI.EndDisabledGroup();
        }



        
        void CellGUI(Rect cellRect, TreeViewItem<AssetElement> item, AssetTreeHeader column, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);

            AssetElement element = item.data;
            AssetBuildRule buildRule = element.BuildRule;
            
            switch (column)
            {
                case AssetTreeHeader.Icon:
                    {
//                        EditorGUI.BeginDisabledGroup(!item.data.IsBuild);
                    if(buildRule.BuildType == 0)
                        GUI.DrawTexture(cellRect, iconIgnore, ScaleMode.ScaleToFit);
                    else
                        GUI.DrawTexture(cellRect, icons[GetIconByIndex(item)], ScaleMode.ScaleToFit);
//                        EditorGUI.EndDisabledGroup();

                    }
                    break;
                //case MyColumns.Icon2:
                //    {

                //        GUI.DrawTexture(cellRect, s_TestIcons[GetIcon2Index(item)], ScaleMode.ScaleToFit);
                //    }
                //    break;

                case AssetTreeHeader.AssetName:
                    {
                        if (!Toggle)
                        {
                            args.rowRect = cellRect;
                            base.RowGUI(args);
                        }
                        else
                        {
                            // Do toggle
                            Rect toggleRect = cellRect;
                            toggleRect.x += GetContentIndent(item);
                            toggleRect.width = kToggleWidth;

                            if (toggleRect.xMax < cellRect.xMax)
                            {

    //                            EditorGUI.BeginDisabledGroup(false);
                                bool beforeToggle = item.data.Toggle;
                                //                            item.data.IsBuild = EditorGUI.Toggle(toggleRect, item.data.IsBuild); // hide when outside cell rect
                                item.data.Toggle = EditorGUI.Toggle(toggleRect, item.data.Toggle);
                                if (item.data.Toggle != beforeToggle)
                                {
    //                                if (element.FileType == FileType.Folder)
    //                                {
    //                                    if (item.data.Before != item.data.IsBuild)
    //                                    {
                                    if (item.data.children != null)
                                    {
                                        for (int i = 0; i < item.data.children.Count; i++)
                                        {
                                            item.data.children[i].Toggle = item.data.Toggle;
                                        }
                                    }
    //                                    }

    //                                    item.data.Before = item.data.IsBuild;
    //                                }
                                }
    //                            EditorGUI.EndDisabledGroup();

                            }
    //                        EditorGUI.BeginDisabledGroup(false);//!item.data.IsBuild

                            // Default icon and label
                            args.rowRect = cellRect;
                            base.RowGUI(args);
    //                        EditorGUI.EndDisabledGroup();                            
                        }
                    }
                    break;
                case AssetTreeHeader.NameAB:
//                    EditorGUI.BeginChangeCheck();
                    EditorGUI.TextField(cellRect, buildRule.AssetBundleName); //buildRule.AssetBundleName = 
                    break;
                case AssetTreeHeader.Order:
                    int newOrder = EditorGUI.IntField(cellRect, buildRule.Order);
                    if (newOrder != buildRule.Order)
                        buildRule.Order = Math.Min(newOrder, BuildUtil.GetFileOrder(buildRule.FileFilterType) + 999);
                    break;
                case AssetTreeHeader.File:
                    if (element.FileType == FileType.Folder)
                    {
                        FileType fileFilterType = (FileType) EditorGUI.EnumPopup(cellRect, buildRule.FileFilterType);
                        if (fileFilterType != buildRule.FileFilterType)
                        {
                            buildRule.FileFilterType = fileFilterType;
                            buildRule.Order = BuildUtil.GetFileOrder(fileFilterType);

                            //刷新子结点
                            assetTreeModel.AddChildrens(new []{element.id});
                        }
                    }
                    else
                    {
                        EditorGUI.LabelField(cellRect, buildRule.FileFilterType.ToString());
                    }
                    break;
                case AssetTreeHeader.Build:
                    int buildIndex = Array.FindIndex(Styles.BundleBuildEnums, ev => ev == buildRule.BuildType);
                    int newBuildIndex = EditorGUI.Popup(cellRect, buildIndex, Styles.BundleBuildOptions);
                    if (newBuildIndex != buildIndex)
                    {
                        buildRule.BuildType = Styles.BundleBuildEnums[newBuildIndex];
                        this.assetTreeModel.ReflushChildrenRecursive(element);                            
                    }
                    break;
//                case AssetTreeHeader.Ignore:
//                    buildRule.BuildType = (PackageAssetType)EditorGUI.EnumPopup(cellRect, buildRule.BuildType);
//                    break;
            }
        }

        int GetIconByIndex(TreeViewItem<AssetElement> item)
        {
            if ((int)item.data.FileType > icons.Length)
            {
                return 0;
            }

            return (int)(item.data.FileType);
        }


        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(EditorGUIUtility.FindTexture("FilterByType"), "Lorem ipsum dolor sit amet, consectetur adipiscing elit. "),
                    contextMenuText = "Type",
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 30,
                    minWidth = 30,
                    maxWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                //new MultiColumnHeaderState.Column
                //{
                //    headerContent = new GUIContent(EditorGUIUtility.FindTexture("FilterByType"), "Sed hendrerit mi enim, eu iaculis leo tincidunt at."),
                //    contextMenuText = "Type",
                //    headerTextAlignment = TextAlignment.Center,
                //    sortedAscending = true,
                //    sortingArrowAlignment = TextAlignment.Right,
                //    width = 30,
                //    minWidth = 30,
                //    maxWidth = 60,
                //    autoResize = false,
                //    allowToggleVisibility = true
                //},
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Asset Name"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
//                    width = 250,
//                    minWidth = 100,
                    autoResize = true,
                    allowToggleVisibility = false
                },

                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("AssetBundle Name", "资源的 AssetbundleName 名，设置了之后会打包成ab资源"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 200,
                    minWidth = 150,
                    autoResize = false,
                    allowToggleVisibility = true
                },

                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Order", "设置资源的打包顺序"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 80,
                    minWidth = 60,

                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("File", "资源对应工程里面的对象"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 100,
                    minWidth = 80,
                    autoResize = false
                },
//                new MultiColumnHeaderState.Column
//                {
//                    headerContent = new GUIContent("Load", "资源加载的类型设置，0 :无，1：预加载"),
//                    headerTextAlignment = TextAlignment.Center,
//                    sortedAscending = true,
//                    sortingArrowAlignment = TextAlignment.Left,
//                    width = 100,
//                    minWidth = 80,
//                    autoResize = false
//                }
//                ,
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("S/T Build", "打包方式,整体/分开"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 120,
                    minWidth = 100,
                    autoResize = false
                },
//                new MultiColumnHeaderState.Column
//                {
//                    headerContent = new GUIContent("Ignore", "忽略"),
//                    headerTextAlignment = TextAlignment.Center,
//                    sortedAscending = true,
//                    sortingArrowAlignment = TextAlignment.Left,
//                    width = 50,
//                    minWidth = 30,
//                    autoResize =false,
//                },
//                 new MultiColumnHeaderState.Column
//                {
//                    headerContent = new GUIContent("查看引用", "查看引用的资源列表"),
//                    headerTextAlignment = TextAlignment.Left,
//                    sortedAscending = true,
//                    sortingArrowAlignment = TextAlignment.Left,
//                    width = 60,
//                    minWidth = 60,
//                    autoResize =false,
//                },
//                  new MultiColumnHeaderState.Column
//                {
//                    headerContent = new GUIContent("被引用", "被引用的资源列表"),
//                    headerTextAlignment = TextAlignment.Left,
//                    sortedAscending = true,
//                    sortingArrowAlignment = TextAlignment.Left,
//                    width = 70,
//                    minWidth = 70,
//                    autoResize =false,
//                },
//                new MultiColumnHeaderState.Column
//                {
//                    headerContent = new GUIContent("查看被引用", "查看被引用的资源列表"),
//                    headerTextAlignment = TextAlignment.Left,
//                    sortedAscending = true,
//                    sortingArrowAlignment = TextAlignment.Left,
//                    width = 80,
//                    minWidth = 80,
//                    autoResize =false,
//                },

            };

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(AssetTreeHeader)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            return state;
        }

    }
}