﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{
    /// <summary>
    /// AssetBundle资源处理，主要功能包括整理AB名
    /// </summary>
    public class AssetBundleBinding : ABuilding
    {
        private static string[] includeExtensions = new []{
            ".prefab", ".unity", ".mat", ".asset",
            ".ogg", ".wav", ".jpg", ".png", ".bytes"
        };

        public AssetBundleBinding() : base(30)
        {
        }

        public override IEnumerator OnBuilding()
        {
            //1.清除Assetbundle标记
            BuildUtil.ClearAssetBundleName();
            AssetDatabase.SaveAssets();
            yield return null;

            //重新设置AB分配
            this.SetAbName();
            yield return null;

            //开始启动Unity打包
            bool result = this.buildAssetBundle(true);

            if(!result) yield break;
        }

        /// <summary>
        /// 设置资源的AB名称
        /// </summary>
        private void SetAbName()
        {
            string savePath = BuilderPreference.BUILD_PATH + "/tempsizefile.txt";
            if (File.Exists(savePath)) File.Delete(savePath);
            AssetDatabase.Refresh();

            // 设置ab名
            AssetBuildRule[] buildRules = AssetBuildRuleManager.Instance.Rules;

            Builder.AddBuildLog("Set AssetBundleName...");

            List<string> files = new List<string>();
            //获取根目录下的所有文件
            for (int i = 0; i < buildRules.Length; i++)
            {
                string[] newFiles = BuildUtil.SearchFiles(buildRules[i]);
                files.AddRange(newFiles);
            }

            Builder.AssetMaps = new Dictionary<string, AssetMap>();
            Dictionary<string, AssetMap> assetMaps = Builder.AssetMaps;

            //构建映射关系
            for (int i = 0; i < files.Count; i++)
            {
                AssetMap fileAssetMap = null;
                if (!assetMaps.TryGetValue(files[i], out fileAssetMap))
                {
                    AssetBuildRule rule = findRuleByPath(files[i], buildRules);
                    fileAssetMap = new AssetMap(files[i] , rule);
                    assetMaps[files[i]] = fileAssetMap;
                }

                string[] dependency = AssetDatabase.GetDependencies(files[i]);

                for (int j = 0; j < dependency.Length; j++)
                {
                    string extension = Path.GetExtension(dependency[j]);

                    if(BuilderPreference.ExcludeFiles.Contains(extension))   continue;  

                    AssetMap assetMap = null;
                    if (!assetMaps.TryGetValue(dependency[j], out assetMap))
                    {
                        AssetBuildRule rule = findRuleByPath(files[i], buildRules);
                        assetMap = new AssetMap(dependency[j] , rule);
                        assetMaps[dependency[j]] = assetMap;
                    }

                    assetMap.AddReference(fileAssetMap);

                    fileAssetMap.AddDependency(assetMap);
                }
            }

            //根据明确的子目录设置AB名,即定义了指定的打包规则的目录
            foreach (AssetMap asset in assetMaps.Values)
            {
                BuildUtil.SetAssetbundleName(asset.AssetPath, asset.Rule.AssetBundleName);
            }

            Builder.AddBuildLog("set assetbundle name ... end");


            //设置依赖文件的Assetbundle分配
            this.checkDependency(assetMaps);

            Builder.AddBuildLog("Check Dependency... end");
        }


        private List<AssetBuildRule> sortBuildRules(AssetBuildRule[] builds )
        {
            List<AssetBuildRule> rules = new List<AssetBuildRule>();

            for (int i = 0; i < builds.Length; i++)
            {
                builds[i].TreeToList(builds[i] , rules);
            }

            rules.Sort((x, y) =>
            {
                int result = x.Order.CompareTo(y.Order);
                if (result == 0) //equal
                    result = x.Path.CompareTo(y.Path);
                return result;
            });

            return rules;
        }


        /// <summary>
        /// 查找父目录的打包规则
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="rules"></param>
        /// <returns></returns>
        private AssetBuildRule findRuleByPath(string filePath, AssetBuildRule[] rules)
        {
            int length = 0;
            AssetBuildRule rule = null;
            FileType fileType = BuildUtil.GetFileType(new FileInfo(filePath));

            for (int i = 0; i < rules.Length; i++)
            {
                if(rules[i].FileFilterType != fileType) continue;

                int pathDepth = rules[i].Path.Length;

                if (pathDepth > length && filePath.StartsWith(rules[i].Path))
                {
                    length = pathDepth;
                    rule = rules[i];
                }
            }
            return rule;
        }

        /// <summary>
        /// 查找最小打包顺序的打包规则
        /// </summary>
        /// <returns></returns>
        private AssetBuildRule findRuleByOrder(List<AssetMap> assets)
        {
            int minOrder = int.MaxValue;
            AssetBuildRule rule = null;

            for (int i = 0; i < assets.Count; i++)
            {
                AssetBuildRule assetRule = assets[i].Rule;
                if (assetRule.Order < minOrder)
                {
                    minOrder = assetRule.Order;
                    rule = assetRule;
                }

            }
            return rule;
        }

        //设置依赖文件的Assetbundle分配
        // 1.生成引用与依赖的映射关系
        // 2.查找打包规则中的最小Order进行设置
        private void checkDependency(Dictionary<string , AssetMap> files)
        {
            //设置依赖文件的Assetbundle名称
            foreach (AssetMap asset in files.Values)
            {
                List<AssetMap> dependencys = asset.Dependencys;
                if(dependencys == null) continue;

                for (int j = 0; j < dependencys.Count; j++)
                {
                    AssetImporter importer = AssetImporter.GetAtPath(dependencys[j].AssetPath);
                    if(!string.IsNullOrEmpty(importer.assetBundleName)) continue;  //已经被设置

                    AssetMap depAsset = dependencys[j];
                    //查询引用文件中最小的Order,使用最小Order的Assetbundle名称
                    AssetBuildRule depAssetRule = findRuleByOrder(depAsset.References);  
                    BuildUtil.SetAssetbundleName(importer , depAssetRule.AssetBundleName);
                }
            }

        }
        
        /// <summary>
        /// 启动Unity打包Assetbundle
        /// </summary>
        /// <param name="copyAssets"></param>
        /// <returns></returns>
        private bool buildAssetBundle(bool copyAssets = true)
        {
            try
            {
                if (copyAssets) CopyToAssetBundle();

                RemoveNotExsitBundles();
                // 更新资源版本号
                string bundlePath = BuilderPreference.BUILD_PATH;
                //                gameVersion.VersionIncrease();
                //PlayerSettings.bundleVersion = gameVersion.ToString();
                //                File.WriteAllText(bundlePath + "/version.txt", gameVersion.ToString());
                //                ABPackHelper.SaveVersion(gameVersion.ToString());

                //AssetDatabase.Refresh();
                AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(bundlePath, BuilderPreference.BuildBundleOptions, EditorUserBuildSettings.activeBuildTarget);
                if (manifest == null)
                    throw new Exception("Build assetbundle error");

                string manifestPath = string.Concat(bundlePath , "/" ,BuilderPreference.PlatformTargetFolder.ToLower());
                if (File.Exists(manifestPath))
                {
                    byte[] bytes = File.ReadAllBytes(manifestPath);
                    File.Delete(manifestPath);
                    File.WriteAllBytes(manifestPath+".ab", bytes);
                }
                else
                    Debug.LogError("<<BuildAssetBundle>> Cant find root manifest. ps:" + manifestPath);
                
                return true;
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("error", e.Message.ToString(), "ok");
                return false;
            }
        }


        public void CopyToAssetBundle()
        {
            string fromPath = BuilderPreference.TEMP_ASSET_PATH;
            string toPath = BuilderPreference.BUILD_PATH;

            if (Directory.Exists(toPath)) Directory.Delete(toPath, true);
            Directory.CreateDirectory(toPath);

            var dirInfo = new DirectoryInfo(fromPath);

            int index = 0;
            FileInfo[] files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                index++;
                if (file.Name.Contains("config.txt"))   continue;

                //ShowProgress("Copying to AssetBundle folder...", (float)index / (float)files.Length);
                string relativePath = file.FullName.Replace("\\", "/");
                string to = relativePath.Replace(fromPath, toPath);
                if (!Directory.Exists(to)) Directory.CreateDirectory(to);
                File.Copy(relativePath, to, true);
            }
//            ShowProgress("", 1);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 删除被清除的Bundle
        /// </summary>
        private void RemoveNotExsitBundles()
        {
            string[] files = Directory.GetFiles(BuilderPreference.BUILD_PATH, "*.ab", SearchOption.AllDirectories);
//            ABPackHelper.ShowProgress("", 0);

            string[] assetBundles = AssetDatabase.GetAllAssetBundleNames();
            HashSet<string> allBundleSet = new HashSet<string>(assetBundles);

            for (int i = 0; i < files.Length; ++i)
            {
                var file = files[i].Replace("\\", "/");
                //ABPackHelper.ShowProgress("Removing bundles...", (float)i / (float)files.Length);

                string fileName = Path.GetFileName(file);
                if (!allBundleSet.Contains(fileName))
                {
                    File.Delete(file);
                    File.Delete(file + ".meta");
                    File.Delete(file + ".manifest");
                    File.Delete(file + ".manifest.meta");
                }
            }
            //ABPackHelper.ShowProgress("", 1);
            AssetDatabase.Refresh();
        }
    }
}