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

        public AssetElement(FileSystemInfo info)
        {
            this.name = info.Name;

            FileType = BuildUtil.GetFileType(info);

            BuildRule = new AssetBuildRule();
            BuildRule.Path = BuildUtil.RelativePaths(info.FullName);
            
            depth = BuildRule.Path.Split('/').Length - 1;
            GUID = AssetDatabase.AssetPathToGUID(BuildRule.Path);
        }

        public AssetElement(FileSystemInfo info , AssetBuildRule rule)
        {
            this.name = info.Name;

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
        }
    }


}