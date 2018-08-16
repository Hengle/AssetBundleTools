using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
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
            return base.OnBuilding();
        }



        public static void CommitPackAssets()
        {
            try
            {
                string path = ABPackHelper.ASSET_PATH + LuaConst.osDir;
                SVNUtility.Commit(path , "提交打包资源");
                
                path = ABPackHelper.TEMP_ASSET_PATH + LuaConst.osDir;
                SVNUtility.Commit(path , "提交临时打包资源");

                SVNUtility.Commit(Application.dataPath , "资源整理");
                SVNUtility.Commit(ABPackHelper.VERSION_PATH , "版本号更新");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "资源上传svn错误: " + e.Message, "OK");
            }
            finally
            {
                AssetDatabase.Refresh();
            }
        }


    }
}