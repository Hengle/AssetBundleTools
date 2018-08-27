using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using UnityEditor;
using UnityEditor.CustomTreeView;

namespace AssetBundleBuilder
{
    public class AssetElement : TreeElement
    {

        public AssetBuildRule BuildRule;

        public FileType FileType { get; private set; }

        private string fileName;
         
        public AssetElement(FileSystemInfo info)
        {
            this.name = info.Name;
            fileName = this.name.Split('.')[0].ToLower();
            FileType = BuildUtil.GetFileType(info);
            
            BuildRule = new AssetBuildRule();
            BuildRule.AssetBundleName = info.Name.ToLower();
            BuildRule.Path = BuildUtil.RelativePaths(info.FullName);
            BuildRule.FileFilterType = FileType;
            BuildRule.Order = BuildUtil.GetFileOrder(FileType);

            depth = BuildRule.Path.Split('/').Length - 1;
            GUID = AssetDatabase.AssetPathToGUID(BuildRule.Path);
        }

        public AssetElement(FileSystemInfo info , AssetBuildRule rule)
        {
            this.name = info.Name;
            fileName = this.name.Split('.')[0].ToLower();
            FileType = BuildUtil.GetFileType(info);

            BuildRule = rule;

            depth = BuildRule.Path.Split('/').Length - 1;
            GUID = AssetDatabase.AssetPathToGUID(BuildRule.Path);
        }

        public AssetElement(string name , int depth, int id)
        {
            this.name = name;
            this.depth = depth;
            this.id = id;

            FileType = FileType.Folder;
            BuildRule = new AssetBuildRule();
            BuildRule.FileFilterType = FileType;
        }


        public void Reflush()
        {
            AssetElement parentAssetItem = parent as AssetElement;

            if (this.FileType != FileType.Folder)
            {
                if ((parentAssetItem.BuildRule.BuildType & BuildType.TogetherFiles) != 0)
                {
                    BuildRule.AssetBundleName = parentAssetItem.BuildRule.AssetBundleName;
                    BuildRule.Order = parentAssetItem.BuildRule.Order;
                    BuildRule.DownloadOrder = parentAssetItem.BuildRule.DownloadOrder;
                    return;
                }
            }
            else if (this.FileType == FileType.Folder)
            {
                if ((parentAssetItem.BuildRule.BuildType & BuildType.TogetherFolders) != 0)
                {
                    BuildRule.AssetBundleName = parentAssetItem.BuildRule.AssetBundleName;
                    BuildRule.Order = parentAssetItem.BuildRule.Order;
                    BuildRule.DownloadOrder = parentAssetItem.BuildRule.DownloadOrder;
                    BuildRule.BuildType = (BuildType)(-1); //everything
                    return;
                }                
            }

            string parentBundleName = parentAssetItem.BuildRule.AssetBundleName;
            if(!string.IsNullOrEmpty(parentBundleName))
                BuildRule.AssetBundleName = string.Concat(parentBundleName, "/", fileName);
            
            //打包顺序
            int offsetOrder = this.BuildRule.Order - BuildUtil.GetFileOrder(this.FileType);
            BuildRule.Order = parentAssetItem.BuildRule.Order + offsetOrder;

            //下载顺序 todo
        }

        //        /// <summary>
        //        /// 更新子目录过滤不匹配的文件
        //        /// </summary>
        //        /// <param name="fileType"></param>
        //        public void UpdateFileFilter(FileType fileType)
        //        {
        //            if (children == null) return;
        //
        //            for (int i = children.Count - 1; i >= 0; i--)
        //            {
        //                AssetElement child = children[i] as AssetElement;
        //                if(child.FileType == FileType.Folder || child.FileType == fileType)
        //                    continue;
        //                children.RemoveAt(i);
        //            }
        //        }
    }


}