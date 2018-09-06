using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            //压缩资源
            CopyAssets(BuilderPreference.BUILD_PATH);
            yield return null;
            CopyToTempAssets();
        }

        public void CopyAssets(string fromPath)
        {
            string toPath = BuilderPreference.ASSET_PATH ;
            if (Directory.Exists(toPath)) Directory.Delete(toPath, true);
            Directory.CreateDirectory(toPath);

            var dirInfo = new DirectoryInfo(fromPath);
            Builder.AddBuildLog("<Compress Building> Copying assets...");
            int index = 0;
            FileInfo[] files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                index++;
//                ShowProgress("Copying assets...", (float)index / (float)files.Length);
                if (file.Name.EndsWith(".meta") || file.Name.EndsWith(".manifest") || file.Name.Contains("config.txt"))
                    continue;

                string relativePath = BuildUtil.RelativePaths(file.FullName);
                string to = relativePath.Replace(fromPath, toPath);

                BuildUtil.SwapPathDirectory(to);

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
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < fs.Length; ++i)
            {
                string[] elements = fs[i].Split('|');
                Debug.Assert(elements.Length >= 3);
                builder.Length = 0;

                for (int j = 0; j < elements.Length; ++j)
                {
                    if (j == 2)
                        builder.Append(new FileInfo(root + "/" + elements[0]).Length);
                    else
                        builder.Append(elements[j]);
                    if (j != elements.Length - 1) builder.Append('|');
                }
                list.Add(builder.ToString());
            }
            File.WriteAllLines(flistPath, list.ToArray());
        }

        public void CopyToTempAssets()
        {
            string fromPath = BuilderPreference.BUILD_PATH;
            string toPath = BuilderPreference.TEMP_ASSET_PATH;
            if (Directory.Exists(toPath)) Directory.Delete(toPath, true);
            Directory.CreateDirectory(toPath);

            var dirInfo = new DirectoryInfo(fromPath);
            Builder.AddBuildLog("<Compress Building> Copying to temp assets...");

            int index = 0;
            FileInfo[] files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                index++;
//                ShowProgress("Copying to temp assets...", (float)index / (float)files.Length);
                if (file.Name.EndsWith(".meta") || file.Name.Contains("config.txt"))
                    continue;

                string relativePath = BuildUtil.RelativePaths(file.FullName);
                string to = relativePath.Replace(fromPath, toPath);

                BuildUtil.SwapPathDirectory(to);

                File.Copy(relativePath, to, true);
            }
//            ShowProgress("", 1);
        }
    }
}