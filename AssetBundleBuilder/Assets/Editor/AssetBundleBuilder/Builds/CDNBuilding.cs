using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{
    /// <summary>
    /// 打包的最终阶段，上传CDN更新资源
    /// </summary>
    public class CDNBuilding : ABuilding
    {
        public CDNBuilding() : base(30)
        {
        }

        public override IEnumerator OnBuilding()
        {
            return base.OnBuilding();
        }

        /// <summary>
        /// 上传资源到内网
        /// </summary>
        public static void UploadInner()
        {
            string script_path = Application.dataPath.Replace("/Assets", "") + "/rsync_243_res.py";
            string args = string.Format("{0}", script_path);
            SVNUtility.Upload(args);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 上传资源到CDN
        /// </summary>
        /// <param name="path"></param>
        /// <param name="version"></param>
        /// <param name="upload_script"></param>
        public static void UploadCDN(string path, string version, string upload_script)
        {
            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(version))
            {
                string script_path = Application.dataPath.Replace("/Assets", "") + "/" + upload_script;
                string upload_path = string.Format("update/{0}/{1}", path, version);
                //var lastVersion = GameVersion.CreateVersion(version);
                //lastVersion.VersionDecrease();
                //string last_upload_path = string.Format("update/{0}/{1}/{2}", app_name, channel_name, lastVersion.ToString());
                string local_path = BuilderPreference.ASSET_PATH;
                string args = string.Format(" {0} {1} {2} android", script_path, upload_path, local_path);
                SVNUtility.Upload(args);
                string upload_fail_path = Application.dataPath.Replace("/Assets", "") + "uploadfaild_list.txt";
                if (File.Exists(upload_fail_path))
                    EditorUtility.DisplayDialog("Warning", "资源上传CDN发生错误, 请查看项目根目录下uploadfaild_list.txt文件", "OK");
                AssetDatabase.Refresh();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "资源上传CDN错误, 没有设置渠道名或游戏名或版本号!", "OK");
            }
        }
    }
}