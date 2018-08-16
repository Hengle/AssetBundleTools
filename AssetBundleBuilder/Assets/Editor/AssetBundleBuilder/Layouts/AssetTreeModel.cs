using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.CustomTreeView;
using UnityEngine;

namespace AssetBundleBuilder
{
    public class AssetTreeModel : TreeModel<AssetElement>
    {
        
        static int index = 0;

        private AssetBuildRuleManager rulManger;

        public AssetTreeModel(IList<AssetElement> data) : base(data)
        {
            rulManger = AssetBuildRuleManager.Instance;

            this.Init();
        }


        private void Init()
        {
            rulManger.LoadConifg();

            AssetBuildRule[] rules = rulManger.Rules;
            if (rules == null) return;
            
            for (int i = 0; i < rules.Length; i++)
            {
                this.InitTreeElement(rules[i], root);
            }
        }

        public AssetElement FindGuid(string guid)
        {
            return m_Data.FirstOrDefault(n => n.GUID == guid);
        }

        /// <summary>
        /// 添加根目录
        /// </summary>
        /// <param name="folderPath"></param>
        public void AddRoot(string folderPath)
        {
            string guid = AssetDatabase.AssetPathToGUID(folderPath);
            AssetElement childItem = FindGuid(guid);

            if (childItem == null)
            {
                childItem = CreateTreeViewItemForGameObject(new DirectoryInfo(folderPath));
                childItem.parent = root;
            }
            
            if (root.children == null)
                root.children = new List<TreeElement>();

            root.children.Add(childItem);
            m_Data.Add(childItem);
            index++;

            this.AddChildrenRecursive(folderPath , childItem, m_Data);
        }


        public void InitTreeElement(AssetBuildRule rule , AssetElement parent)
        {
            index++;

            string suffix = Path.GetExtension(rule.Path);
            FileSystemInfo fsi = null;
            if(string.IsNullOrEmpty(suffix))
                fsi = new DirectoryInfo(rule.Path);
            else
                fsi = new FileInfo(rule.Path);

            AssetElement newEle = new AssetElement(fsi , rule);
            newEle.id = index;
            newEle.parent = parent;

            m_Data.Add(newEle);

            if (parent.children == null)
                parent.children = new List<TreeElement>();

            parent.children.Add(newEle);

            if (rule.Childrens != null)
            {
                for (int i = 0; i < rule.Childrens.Length; i++)
                {
                    InitTreeElement(rule.Childrens[i], newEle);
                }
            }  
        }

        /// <summary>
        /// 添加选中目录的子资源
        /// </summary>
        /// <param name="selections"></param>
        public void AddChildrens(IList<int> selections)
        {
            for (int i = 0; i < selections.Count; i++)
            {
                AssetElement ele = Find(selections[i]);

                AddChildrenRecursive(ele.BuildRule.Path , ele , m_Data , true);
            }
        }


        public void AddChildrenRecursive(string path, AssetElement item, IList<AssetElement> rows, bool checkHave = false)
        {
            DirectoryInfo folder = new DirectoryInfo(path);
            if (!folder.Exists) return;

            FileSystemInfo[] files = folder.GetFileSystemInfos();
            int length = files.Length;
            for (int i = 0; i < length; ++i)
            {
                //隐藏文件
                if ((files[i].Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)//&& (files[i].Attributes & FileAttributes.System) != FileAttributes.System)
                {
                    continue;
                }
                if (files[i] is DirectoryInfo)
                {
                    if (checkHave)
                    {
                        string guid = AssetDatabase.AssetPathToGUID(BuildUtil.RelativePaths(files[i].FullName));
                        AssetElement e = FindGuid(guid);

                        if (e == null)
                        {
                            var childItem = CreateTreeViewItemForGameObject(files[i]);
                            addChildrenElement(childItem, item, i);

                            if (rows.Count > index)
                                rows.Insert(index, childItem);
                            else
                                rows.Add(childItem);
                            
                            //AddChildrenRecursive(files[i].FullName, item, rows, checkHave);
                        }
                        else
                        {
                            index++;
                            //AddChildrenRecursive(files[i].FullName, e, rows, checkHave);

                        }
                    }
                    else
                    {
                        var childItem = CreateTreeViewItemForGameObject(files[i]);
                        addChildrenElement(childItem, item, i);

                        rows.Add(childItem);
                        //AddChildrenRecursive(files[i].FullName, childItem, rows, checkHave);
                    }
                }
                else
                {
                    if (!files[i].Name.EndsWith(".meta"))
                    {
                        if (checkHave)
                        {
                            string guid = AssetDatabase.AssetPathToGUID(BuildUtil.RelativePaths(files[i].FullName));
                            AssetElement e = FindGuid(guid);

                            if (e == null)
                            {
                                var childItem = CreateTreeViewItemForGameObject(files[i]);
                                addChildrenElement(childItem, item, i);

                                if (rows.Count > index)
                                    rows.Insert(index, childItem);
                                else
                                    rows.Add(childItem);
                            }
                            else
                            {
                                index++;
                            }
                        }
                        else
                        {
                            var childItem = CreateTreeViewItemForGameObject(files[i]);
                            addChildrenElement(childItem , item , i);
                            rows.Add(childItem);
                        }
                    }
                }
                //Debug.LogError(rows.Count + "====" + index + "===" + files[i].Name);
            }
        }

        static AssetElement CreateTreeViewItemForGameObject(FileSystemInfo file)
        {
            // We can use the GameObject instanceID for TreeViewItem id, as it ensured to be unique among other items in the tree.
            // To optimize reload time we could delay fetching the transform.name until it used for rendering (prevents allocating strings 
            // for items not rendered in large trees)
            // We just set depth to -1 here and then call SetupDepthsFromParentsAndChildren at the end of BuildRootAndRows to set the depths.
            index++;
            
            AssetElement newEle = new AssetElement(file);
            newEle.id = index;
            
            return newEle;
        }


        private void addChildrenElement(AssetElement element, AssetElement parent, int insertIndex)
        {
            element.parent = parent;

            if (parent.children == null)
                parent.children = new List<TreeElement>();

            if (parent.children.Count > insertIndex)
                parent.children.Insert(insertIndex, element);
            else
                parent.children.Add(element);

            parent.BuildRule.AddChild(element.BuildRule);
        }

        /// <summary>
        /// 删除选择的单元项
        /// </summary>
        /// <returns></returns>
        public void RemoveSelectElement()
        {
            List<int> removeIds = new List<int>();
            for (int i = 0; i < m_Data.Count; i++)
            {
                if (m_Data[i].Toggle)
                {
                    removeIds.Add(m_Data[i].id);

                    AssetElement parentEle = (AssetElement) m_Data[i].parent;
                    if(parentEle.BuildRule != null)
                        parentEle.BuildRule.RemoveChild(m_Data[i].BuildRule);
                }
            }
            
            this.RemoveElements(removeIds);
        }


        public void Save()
        {
            if (root.children == null || root.children.Count <= 0) return;

            AssetBuildRule[] rules = new AssetBuildRule[root.children.Count];
            for (int i = 0; i < root.children.Count; i++)
            {
                rules[i] = ((AssetElement) root.children[i]).BuildRule;
            }
            rulManger.SaveConfig(rules);
        }
    }
}