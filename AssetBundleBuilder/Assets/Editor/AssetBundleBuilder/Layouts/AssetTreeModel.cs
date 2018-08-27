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
                childItem = CreateTreeViewItemForGameObject(new DirectoryInfo(folderPath) , root ,
                                                             root.children == null ? 0 : root.children.Count);
            }
            
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

            this.Changed();
        }

        /// <summary>
        /// 刷新选择项中的数据
        /// </summary>
        /// <param name="selections"></param>
        public void ReflushChildrens(IList<int> selections)
        {
            for (int i = 0; i < selections.Count; i++)
            {
                AssetElement ele = Find(selections[i]);
                ReflushChildrenRecursive(ele);
            }

            this.Changed();
        }


        public void AddChildrenRecursive(string path, AssetElement item, IList<AssetElement> rows, bool checkHave = false)
        {
            DirectoryInfo folder = new DirectoryInfo(path);
            if (!folder.Exists) return;

            FileSystemInfo[] files = folder.GetFileSystemInfos();
            int length = files.Length;

            string[] includeExtensions = BuildUtil.GetFileExtension(item.BuildRule.FileFilterType);
            HashSet<string> includeSet = new HashSet<string>();
            if (includeExtensions != null)
            {
                for (int i = 0; i < includeExtensions.Length; i++)
                    includeSet.Add(includeExtensions[i]);
            }

            HashSet<string> includes = new HashSet<string>();
            for (int i = 0; i < length; ++i)
            {
                //隐藏文件
                if ((files[i].Attributes & FileAttributes.Hidden) == FileAttributes.Hidden || files[i].Name.EndsWith(".meta"))//&& (files[i].Attributes & FileAttributes.System) != FileAttributes.System)
                {
                    continue;
                }

                string extension = Path.GetExtension(files[i].Name);
                if(!string.IsNullOrEmpty(extension) && !includeSet.Contains(extension)) continue;

                AssetElement childItem = null;
                if (checkHave)
                {
                    string guid = AssetDatabase.AssetPathToGUID(BuildUtil.RelativePaths(files[i].FullName));
                    childItem = FindGuid(guid);

                    if (childItem == null)
                    {
                        childItem = CreateTreeViewItemForGameObject(files[i] , item, i);

                        if (rows.Count > index)
                            rows.Insert(index, childItem);
                        else
                            rows.Add(childItem);
                    }
                }
                else
                {
                    childItem = CreateTreeViewItemForGameObject(files[i] , item, i);

                    rows.Add(childItem);
                    //AddChildrenRecursive(files[i].FullName, childItem, rows, checkHave);
                }
                
                childItem.Reflush();
                
                includes.Add(childItem.GUID);
                //Debug.LogError(rows.Count + "====" + index + "===" + files[i].Name);
            }

            //删除旧数据
            if (item.children != null && item.children.Count != includes.Count)
            {
                for (int i = item.children.Count - 1; i >= 0; i--)
                {
                    TreeElement element = item.children[i];
                    if (!includes.Contains(element.GUID))
                    {
                        item.children.RemoveAt(i);
                        m_Data.Remove(element as AssetElement);
                    }
                        
                }
            }
        }

        static AssetElement CreateTreeViewItemForGameObject(FileSystemInfo file , AssetElement parent , int insertIndex)
        {
            // We can use the GameObject instanceID for TreeViewItem id, as it ensured to be unique among other items in the tree.
            // To optimize reload time we could delay fetching the transform.name until it used for rendering (prevents allocating strings 
            // for items not rendered in large trees)
            // We just set depth to -1 here and then call SetupDepthsFromParentsAndChildren at the end of BuildRootAndRows to set the depths.
            index++;
            
            AssetElement newEle = new AssetElement(file);
            newEle.id = index;
            newEle.parent = parent;

            if (parent.BuildRule != null)
            {
                if(!string.IsNullOrEmpty(parent.BuildRule.AssetBundleName))
                    newEle.BuildRule.AssetBundleName = string.Concat(parent.BuildRule.AssetBundleName, "/", newEle.BuildRule.AssetBundleName);

                parent.BuildRule.AddChild(newEle.BuildRule);
            }
            
            if (parent.children == null)
                parent.children = new List<TreeElement>();

            if (parent.children.Count > insertIndex)
                parent.children.Insert(insertIndex, newEle);
            else
                parent.children.Add(newEle);

            return newEle;
        }


        public void ReflushChildrenRecursive(AssetElement item)
        {
            if (item.children == null) return;

            for (int i = 0; i < item.children.Count; i++)
            {
                AssetElement childItem = item.children[i] as AssetElement;
                childItem.Reflush();

                ReflushChildrenRecursive(childItem);
            }
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
            if (root.children == null || root.children.Count <= 0)
            {
                string defaultConfigPath = BuilderPreference.DEFAULT_CONFIG_NAME;
                if (File.Exists(defaultConfigPath))
                {
                    File.Delete(defaultConfigPath);
                    AssetDatabase.Refresh();
                }

                return;
            }

            AssetBuildRule[] rules = new AssetBuildRule[root.children.Count];
            for (int i = 0; i < root.children.Count; i++)
            {
                rules[i] = ((AssetElement) root.children[i]).BuildRule;
            }
            rulManger.SaveConfig(rules);
        }
    }
}