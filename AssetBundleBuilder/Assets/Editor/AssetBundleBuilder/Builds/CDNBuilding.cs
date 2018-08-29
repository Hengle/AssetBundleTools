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
            SDKConfig curConfigSDk = Builder.CurrentConfigSDK;
            if (curConfigSDk.upload243 == 1)
                UploadInner();
            else if (curConfigSDk.uploadCDN == 1)
            {
                foreach (var item in curConfigSDk.uploadPathes)
                {
                    UploadCDN(item.path, Builder.GameVersion.ToString(), item.script);
                }
            }
            yield return null;
        }

        /// <summary>
        /// 上传资源到内网
        /// </summary>
        public static void UploadInner()
        {
            string script_path = Path.Combine(Application.dataPath, "../rsync_243_res.py");
            script_path = Path.GetFullPath(script_path);

            SVNUtility.Upload(script_path);
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
                string script_path = Path.Combine(Application.dataPath, "../" + upload_script);
                script_path = Path.GetFullPath(script_path);

                string upload_path = string.Format("update/{0}/{1}", path, version);
                //var lastVersion = GameVersion.CreateVersion(version);
                //lastVersion.VersionDecrease();
                //string last_upload_path = string.Format("update/{0}/{1}/{2}", app_name, channel_name, lastVersion.ToString());
                string local_path = BuilderPreference.ASSET_PATH;
                string args = string.Format(" {0} {1} {2} android", script_path, upload_path, local_path);
                SVNUtility.Upload(args);

                string upload_fail_path = Path.Combine(Application.dataPath, "../uploadfaild_list.txt");
                upload_fail_path = Path.GetFullPath(upload_fail_path);
                if (File.Exists(upload_fail_path))
                    EditorUtility.DisplayDialog("Warning", "资源上传CDN发生错误, 请查看项目根目录下uploadfaild_list.txt文件", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "资源上传CDN错误, 没有设置渠道名或游戏名或版本号!", "OK");
            }
        }
    }
}