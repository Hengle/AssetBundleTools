using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using ZstdNet;

namespace AssetBundleBuilder
{
    /// <summary>
    /// 压缩资源，用于处理首包资源的压缩
    /// </summary>
    public class CompressBuilding : ABuilding
    {
        public CompressBuilding() : base(20)
        {
        }

        public override IEnumerator OnBuilding()
        {
            bool rebuildAll = false;
            // 打包lua
            Packager.BuildAssetResource(EditorUserBuildSettings.activeBuildTarget, rebuildAll);

            yield return null;

            //压缩资源
            bool copyAssets = true;
            if (copyAssets)
            {
                CopyAssets(BuilderPreference.BUILD_PATH);
                CopyToTempAssets();
            }
        }

        public static void CopyAssets(string fromPath)
        {
            string toPath = BuilderPreference.ASSET_PATH ;
            if (Directory.Exists(toPath)) Directory.Delete(toPath, true);
            Directory.CreateDirectory(toPath);

            var dirInfo = new DirectoryInfo(fromPath);
//            ShowProgress("", 0);
            int index = 0;
            FileInfo[] files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                index++;
//                ShowProgress("Copying assets...", (float)index / (float)files.Length);
                if (file.Name.EndsWith(".meta") || file.Name.EndsWith(".manifest") || file.Name.Contains("config.txt"))
                    continue;

                string relativePath = file.FullName.Replace("\\", "/");
                string to = relativePath.Replace(fromPath, toPath);
                if (!Directory.Exists(to)) Directory.CreateDirectory(to);

                if (relativePath.EndsWith(".ab"))
                {
                    using (var compressor = new Compressor(new CompressionOptions(CompressionOptions.MaxCompressionLevel)))
                    {
                        var buffer = compressor.Wrap(File.ReadAllBytes(relativePath));
                        File.WriteAllBytes(to, buffer);
                    }
                }
                else File.Copy(relativePath, to, true);
            }
            ResetFlist();
//            ShowProgress("", 1);
        }


        static void ResetFlist()
        {
            string root = BuilderPreference.ASSET_PATH;
            string flistPath = root + "/files.txt";
            string[] fs = File.ReadAllLines(flistPath);
            List<string> list = new List<string>();
            for (int i = 0; i < fs.Length; ++i)
            {
                string[] elements = fs[i].Split('|');
                Debug.Assert(elements.Length >= 3);
                StringBuilder builder = new StringBuilder();
                for (int j = 0; j < elements.Length; ++j)
                {
                    if (j == 2)
                        builder.Append(File.ReadAllBytes(root + "/" + elements[0]).Length);
                    else
                        builder.Append(elements[j]);
                    if (j != elements.Length - 1) builder.Append('|');
                }
                list.Add(builder.ToString());
            }
            File.WriteAllLines(flistPath, list.ToArray());
        }

        public static void CopyToTempAssets()
        {
            string fromPath = BuilderPreference.BUILD_PATH;
            string toPath = BuilderPreference.TEMP_ASSET_PATH;
            if (Directory.Exists(toPath)) Directory.Delete(toPath, true);
            Directory.CreateDirectory(toPath);

            var dirInfo = new DirectoryInfo(fromPath);
//            ShowProgress("", 0);
            int index = 0;
            FileInfo[] files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                index++;
//                ShowProgress("Copying to temp assets...", (float)index / (float)files.Length);
                if (file.Name.EndsWith(".meta") || file.Name.Contains("config.txt"))
                    continue;

                string relativePath = file.FullName.Replace("\\", "/");
                string to = relativePath.Replace(fromPath, toPath);

                if (!Directory.Exists(to)) Directory.CreateDirectory(to);

                File.Copy(relativePath, to, true);
            }
//            ShowProgress("", 1);
        }
    }
}