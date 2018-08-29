using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{
    /// <summary>
    /// 打包前，更新项目文件
    /// </summary>
    public class SvnUpdateBuilding : ABuilding
    {

        public SvnUpdateBuilding() : base(10)
        {
        }

        public override IEnumerator OnBuilding()
        {
            string[] path = new[]
            {
                BuilderPreference.ASSET_PATH,// 文件版本文件

                BuilderPreference.TEMP_ASSET_PATH, // 更新打包备份目录的AB资源

            };
            SVNUtility.Update(path);
            yield return null;


            string projectPath = Path.Combine(Application.dataPath, "../");
            projectPath = Path.GetFullPath(projectPath);
            
            SVNUtility.Update(projectPath);

            yield return null;

            checkVersionAsset();

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 检查版本资源
        /// </summary>
        public void checkVersionAsset()
        {
            var destVersionPath = BuilderPreference.VERSION_PATH + "/version.txt";
            if (!File.Exists(destVersionPath))
                destVersionPath = BuilderPreference.ASSET_PATH + "/version.txt";

            var versionPath = BuilderPreference.BUILD_PATH + "/version.txt";
            File.Copy(destVersionPath, versionPath, true);
        }

    }
}