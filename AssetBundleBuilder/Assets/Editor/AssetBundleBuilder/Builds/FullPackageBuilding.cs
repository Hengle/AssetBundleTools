using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Riverlake.Crypto;
using UnityEditor;
using UnityEngine;
using ZstdNet;

namespace AssetBundleBuilder
{
    /// <summary>
    /// 整包资源阶段，打包资源完成后，将不同资源拷贝到对应包体对应的目录
    /// </summary>
    public class FullPackageBuilding : PackageBuilding
    {
        private bool isForceUpdate;

        public FullPackageBuilding(bool buildApp, bool forceUpdate) : base(buildApp)
        {
            this.isForceUpdate = forceUpdate;
        }

        public override IEnumerator OnBuilding()
        {
            CopyAllBundles();

            yield return null;

            CompressWithZSTD(1024 * 1024 * 5);

            while (compressIndex < compressCount)
            {
                yield return null;
            }

            AssetDatabase.Refresh();

            genPacklistFile();
            Builder.AddBuildLog("<FullPackage Building> Compress zstd finish !");

            yield return null;

            if (isBuildApp)
            {
                string appPath = BuildApp(true, isForceUpdate);

                Builder.AddBuildLog("<FullPackage Building>Build App  Finished...");

                EditorUtility.RevealInFinder(appPath);

                ResetConfig();
            }
        }

        private void CopyAllBundles()
        {
            string targetPath = BuilderPreference.StreamingAssetsPlatormPath;
            if (Directory.Exists(targetPath)) Directory.Delete(targetPath, true);
            Directory.CreateDirectory(targetPath);

            string buildPath = BuilderPreference.BUILD_PATH;
            HashSet<string> withExtensions = new HashSet<string>() { ".ab", ".unity3d", ".txt", ".conf", ".pb", ".bytes" };
            List<string> files = BuildUtil.SearchIncludeFiles(buildPath, SearchOption.AllDirectories, withExtensions);

            Builder.AddBuildLog("<FullPackage Building> Copy all bundle ...");
            //            int buildPathLength = buildPath.Length + 1; 
            for (int i = 0; i < files.Count; ++i)
            {
                string fileName = Path.GetFileName(files[i]);
                if (fileName == "tempsizefile.txt" || fileName == "luamd5.txt") continue;
                //ABPackHelper.ShowProgress("Copying files...", (float)i / (float)files.Length);

                string streamBundlePath = files[i].Replace(buildPath, targetPath);

                BuildUtil.SwapPathDirectory(streamBundlePath);

                File.Copy(files[i], streamBundlePath);
            }
            AssetDatabase.Refresh();
            //            ABPackHelper.ShowProgress("", 1);
        }


        void ResetConfig()
        {
            string resources_path = "Assets/Resources/";
            if (File.Exists(resources_path + "config1.tmp"))
            {
                File.WriteAllText(resources_path + "config.txt", File.ReadAllText(resources_path + "config1.tmp"));
                File.Delete(resources_path + "config1.tmp");
                AssetDatabase.Refresh();
            }

            Builder.AddBuildLog("<FullPackage Building> reset config !");
        }
    }
}