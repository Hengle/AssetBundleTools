using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{
    /// <summary>
    /// 将打包生成的资源，提交SVN备份
    /// </summary>
    public class SvnCommitBuilding : ABuilding
    {
        public SvnCommitBuilding() : base(10)
        {
        }

        public override IEnumerator OnBuilding()
        {
            CommitPackAssets();

            yield return null;

            SVNUtility.Commit(Application.dataPath, "资源整理");
        }



        public void CommitPackAssets()
        {
            try
            {
                string[] localPaths = new string[]
                {
                  BuilderPreference.ASSET_PATH,
                  BuilderPreference.TEMP_ASSET_PATH
                } ;
                SVNUtility.Commit(localPaths, "提交打包资源");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "资源上传svn错误: " + e.Message, "OK");
            }
        }


    }
}