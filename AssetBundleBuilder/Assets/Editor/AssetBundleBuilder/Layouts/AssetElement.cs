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
            BuildRule.BuildType = (int)(FileType != FileType.Folder ? BundleBuildType.TogetherFiles : BundleBuildType.Separate);

            depth = BuildRule.Path.Split('/').Length - 1;
            GUID = AssetDatabase.AssetPathToGUID(BuildRule.Path);
        }

        public AssetElement(FileSystemInfo info, AssetBuildRule rule)
        {
            this.name = info.Name;
            fileName = this.name.Split('.')[0].ToLower();
            FileType = BuildUtil.GetFileType(info);

            BuildRule = rule;

            depth = BuildRule.Path.Split('/').Length - 1;
            GUID = AssetDatabase.AssetPathToGUID(BuildRule.Path);
        }

        public AssetElement(string name, int depth, int id)
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

            if (parentAssetItem.BuildRule.BuildType == (int)BundleBuildType.Ignore) //ignore
            {
                BuildRule.Order = -1;
                BuildRule.BuildType = (int)BundleBuildType.Ignore;
                return;
            }

            if (this.FileType != FileType.Folder)
            {
                if ((parentAssetItem.BuildRule.BuildType & (int)BundleBuildType.TogetherFiles) != 0)
                {
                    BuildRule.AssetBundleName = parentAssetItem.BuildRule.AssetBundleName;
                    BuildRule.Order = parentAssetItem.BuildRule.Order;
                    BuildRule.DownloadOrder = parentAssetItem.BuildRule.DownloadOrder;
                    BuildRule.BuildType = (int)BundleBuildType.TogetherFiles;
                    return;
                }
            }
            else if (this.FileType == FileType.Folder)
            {
                if ((parentAssetItem.BuildRule.BuildType & (int)BundleBuildType.TogetherFolders) != 0)
                {
                    BuildRule.AssetBundleName = parentAssetItem.BuildRule.AssetBundleName;
                    BuildRule.Order = parentAssetItem.BuildRule.Order;
                    BuildRule.DownloadOrder = parentAssetItem.BuildRule.DownloadOrder;
                    BuildRule.BuildType = (int)(BundleBuildType.TogetherFolders | BundleBuildType.TogetherFiles);
                    return;
                }
            }

            //bundle name
            string curBundleName = Path.GetFileNameWithoutExtension(BuildRule.AssetBundleName);
            string parentBundleName = parentAssetItem.BuildRule.AssetBundleName;
            if (!string.IsNullOrEmpty(parentBundleName))
            {
                if(!fileName.Equals(curBundleName) && !parentAssetItem.name.Equals(curBundleName))
                    BuildRule.AssetBundleName = string.Concat(parentBundleName, "/", curBundleName);
                else
                    BuildRule.AssetBundleName = string.Concat(parentBundleName, "/", fileName);
            }
                
            //打包顺序
            int offsetOrder = this.BuildRule.Order % 1000;
            if (BuildRule.FileFilterType == FileType.Folder)
            {
                //未设置的情况
                BuildRule.FileFilterType = parentAssetItem.BuildRule.FileFilterType;
            }
            BuildRule.Order = BuildUtil.GetFileOrder(BuildRule.FileFilterType) + offsetOrder;

            if (BuildRule.BuildType == (int)BundleBuildType.Ignore)
                BuildRule.Order = -1;

            //            BuildRule.BuildType = (int)BundleBuildType.Separate;

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