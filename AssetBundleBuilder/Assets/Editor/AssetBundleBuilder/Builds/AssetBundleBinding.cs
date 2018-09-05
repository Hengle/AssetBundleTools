using System;
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
        private static string[] includeExtensions = new[]{
            ".prefab", ".unity", ".mat", ".asset",
            ".ogg", ".wav", ".jpg", ".png", ".bytes"
        };

        public AssetBundleBinding() : base(30)
        {
        }

        public override IEnumerator OnBuilding()
        {
            yield return null;

            //1.清除Assetbundle标记
            BuildUtil.ClearAssetBundleName();
            AssetDatabase.Refresh();
            Builder.AddBuildLog("<Assetbundle Building> clear all assetbundle names ");

            yield return null;

            //重新设置AB分配
            this.SetAbName();
            yield return null;

            //开始启动Unity打包
            bool result = this.buildAssetBundle(!Builder.IsDebug);

            if (!result)
            {
                //打包失败,停止继续处理
                this.Builder.CanleBuild();
            }

            yield return null;

            PackPlayerModelTexture();

            AssetDatabase.Refresh();
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
            AssetBuildRule[] rules = AssetBuildRuleManager.Instance.Rules;

            Builder.AddBuildLog("<Assetbundle Building> Start set AssetBundleName...");

            Dictionary<string, List<AssetBuildRule>> path2ruleMap = new Dictionary<string, List<AssetBuildRule>>();
            for (int i = 0; i < rules.Length; i++)
            {
                List<AssetBuildRule> ruleList = rules[i].TreeToList();
                for (int j = 0; j < ruleList.Count; j++)
                {
                    AssetBuildRule rule = ruleList[j];
                    List<AssetBuildRule> pathRules = null;
                    if (!path2ruleMap.TryGetValue(rule.Path, out pathRules))
                    {
                        pathRules = new List<AssetBuildRule>();
                        path2ruleMap[rule.Path] = pathRules;
                    }

                    pathRules.Add(rule);
                }
            }

            //获取根目录下的所有文件
            List<string> files = new List<string>();
            for (int i = 0; i < rules.Length; i++)
            {
                List<string> rootFiles = BuildUtil.SearchFiles(rules[i], path2ruleMap);
                if (rootFiles != null)
                    files.AddRange(rootFiles);
            }

            Builder.AssetMaps = new Dictionary<string, AssetMap>();
            Dictionary<string, AssetMap> assetMaps = Builder.AssetMaps;

            //构建映射关系
            for (int i = 0; i < files.Count; i++)
            {
                AssetMap fileAssetMap = null;
                FileType fileType = BuildUtil.GetFileType(new FileInfo(files[i]));
                if (!assetMaps.TryGetValue(files[i], out fileAssetMap))
                {
                    AssetBuildRule rule = findRuleByPath(files[i], path2ruleMap, fileType);
                    if (rule == null)
                        Debug.LogError("Cant find bundle rule!" + files[i]);
                    fileAssetMap = new AssetMap(files[i], rule);
                    assetMaps[files[i]] = fileAssetMap;
                }

                fileAssetMap.IsBinding = true;  //显示设置bundle规则的文件

                //被忽略的规则不查找依赖
                if (fileAssetMap.Rule.BuildType == (int)BundleBuildType.Ignore) continue;

                string[] dependency = AssetDatabase.GetDependencies(files[i]);
                
                for (int j = 0; j < dependency.Length; j++)
                {
                    string relativePath = BuildUtil.Replace(dependency[j]);
                    string extension = Path.GetExtension(dependency[j]);

                    if (BuilderPreference.ExcludeFiles.Contains(extension) || relativePath.Equals(files[i])) continue;

                    AssetMap assetMap = null;
                    FileType depFileType = BuildUtil.GetFileType(new FileInfo(relativePath));
                    if (!assetMaps.TryGetValue(relativePath, out assetMap))
                    {
                        AssetBuildRule rule = findRuleByPath(relativePath, path2ruleMap, depFileType);
                        rule = rule == null ? fileAssetMap.Rule : rule;
                        assetMap = new AssetMap(relativePath, rule);
                        assetMaps[relativePath] = assetMap;
                    }

                    assetMap.AddReference(fileAssetMap);

                    fileAssetMap.AddDependency(assetMap);
                }
            }

            //根据明确的子目录设置AB名,即定义了指定的打包规则的目录
            foreach (AssetMap asset in assetMaps.Values)
            {
                if (asset.Rule.BuildType == (int)BundleBuildType.Ignore || !asset.IsBinding) continue;

//                Builder.AddBuildLog(string.Format("set assetbundle name , path {0} : {1}", asset.AssetPath, asset.Rule.AssetBundleName));

                BuildUtil.SetAssetbundleName(asset.AssetPath, asset.Rule);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Builder.AddBuildLog("<Assetbundle Building> set assetbundle name ... end");


            //设置依赖文件的Assetbundle分配
            this.checkDependency(assetMaps);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Builder.AddBuildLog("<Assetbundle Building> Check Dependency... end");
        }

        /// <summary>
        /// 向上递规查找打包规则
        /// </summary>
        /// <returns></returns>
        private AssetBuildRule findRuleByPath(string filePath, Dictionary<string, List<AssetBuildRule>> ruleMap , FileType fileType)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            List<AssetBuildRule> rules = null;
            if (ruleMap.TryGetValue(filePath, out rules))
            {
                for (int i = 0; i < rules.Count; i++)
                {
                    if (rules[i].FileFilterType == fileType)
                        return rules[i];
                }
            }

            string parentPath = Path.GetDirectoryName(filePath);

            return findRuleByPath(parentPath , ruleMap , fileType);
        }

        /// <summary>
        /// 查找最小打包顺序的打包规则
        /// </summary>
        /// <returns></returns>
        private AssetBuildRule findRuleByOrder(List<AssetMap> assets , AssetBuildRule srcRule)
        {
            int minOrder = srcRule.Order;
            AssetBuildRule rule = srcRule;

            int sceneUnityRef = 0;  //是否是场景引用的资源

            for (int i = 0; i < assets.Count; i++)
            {
                AssetBuildRule assetRule = assets[i].Rule;
                if (assetRule.Path.EndsWith(".unity"))
                    sceneUnityRef++;

                if (assetRule.Order < minOrder)
                {
                    minOrder = assetRule.Order;
                    rule = assetRule;
                }
            }

            if (sceneUnityRef > 0)
            {
                int ruleOrder = rule.Order;
                int sceneOrder = BuildUtil.GetFileOrder(FileType.Scene);

                rule = null;
                if (sceneUnityRef > 1 && ruleOrder >= sceneOrder)
                {
                    //存在多个场景引用，就打到公共场景资源包内
                    rule = new AssetBuildRule();
                    rule.Order = sceneOrder - 100;
                    rule.AssetBundleName = "scene_publics";
                }
            }

            return rule;
        }

        //设置依赖文件的Assetbundle分配
        // 1.生成引用与依赖的映射关系
        // 2.查找打包规则中的最小Order进行设置
        private void checkDependency(Dictionary<string, AssetMap> files)
        {
            //设置依赖文件的Assetbundle名称
            foreach (AssetMap asset in files.Values)
            {
                if(!asset.IsBinding)    continue;  //过滤非显示bundle rule下的资源

                List<AssetMap> dependencys = asset.Dependencys;
                if (dependencys == null) continue;

                for (int j = 0; j < dependencys.Count; j++)
                {
                    AssetImporter importer = AssetImporter.GetAtPath(dependencys[j].AssetPath);
                    if (!string.IsNullOrEmpty(importer.assetBundleName)) continue;  //已经被设置

                    AssetMap depAsset = dependencys[j];
                    //查询引用文件中最小的Order,使用最小Order的Assetbundle名称
                    AssetBuildRule depAssetRule = findRuleByOrder(depAsset.References , depAsset.Rule);

                    if (depAssetRule == null || depAssetRule.BuildType == (int)BundleBuildType.Ignore) continue;

//                    Builder.AddBuildLog(string.Format("set dep assetbundle name , path {0} : {1}", depAsset.AssetPath, depAssetRule.AssetBundleName));

                    BuildUtil.SetAssetbundleName(importer, depAssetRule);
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
                Builder.GameVersion.VersionIncrease();
                PlayerSettings.bundleVersion = Builder.GameVersion.ToString();
                File.WriteAllText(bundlePath + "/version.txt", Builder.GameVersion.ToString());
                Builder.SaveVersion();

                AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(bundlePath, BuilderPreference.BuildBundleOptions, EditorUserBuildSettings.activeBuildTarget);
                if (manifest == null)
                    throw new Exception("Build assetbundle error");

                string manifestPath = string.Concat(bundlePath, "/", BuilderPreference.PlatformTargetFolder.ToLower());
                if (File.Exists(manifestPath))
                {
                    byte[] bytes = File.ReadAllBytes(manifestPath);
                    File.Delete(manifestPath);
                    File.WriteAllBytes(manifestPath + ".ab", bytes);
                }
                else
                    Debug.LogError("<<BuildAssetBundle>> Cant find root manifest. ps:" + manifestPath);

                Builder.AddBuildLog("<Assetbundle Building> build assetbundles finish !");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }


        public void CopyToAssetBundle()
        {
            Builder.AddBuildLog("<Assetbundle Building> Copying to AssetBundle folder...");

            string fromPath = BuilderPreference.TEMP_ASSET_PATH;
            string toPath = BuilderPreference.BUILD_PATH;

            if (Directory.Exists(toPath)) Directory.Delete(toPath, true);
            Directory.CreateDirectory(toPath);

            var dirInfo = new DirectoryInfo(fromPath);
            if (dirInfo.Exists)
            {
                int index = 0;
                FileInfo[] files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    index++;
                    if (file.Name.Contains("config.txt")) continue;

                    string relativePath = file.FullName.Replace("\\", "/");
                    string to = relativePath.Replace(fromPath, toPath);

                    BuildUtil.SwapPathDirectory(to);
                    File.Copy(relativePath, to, true);
                }
            }

            Builder.AddBuildLog("<Assetbundle Building> Copying to AssetBundle folder end ...");
        }

        /// <summary>
        /// 删除被清除的Bundle
        /// </summary>
        private void RemoveNotExsitBundles()
        {
            string[] files = Directory.GetFiles(BuilderPreference.BUILD_PATH, "*.ab", SearchOption.AllDirectories);
            Builder.AddBuildLog("<Assetbundle Building> Removing not exsit bundles...");

            string[] assetBundles = AssetDatabase.GetAllAssetBundleNames();
            HashSet<string> allBundleSet = new HashSet<string>(assetBundles);

            for (int i = 0; i < files.Length; ++i)
            {
                var file = files[i].Replace("\\", "/");

                string fileName = Path.GetFileName(file);
                if (!allBundleSet.Contains(fileName))
                {
                    File.Delete(file);
                    File.Delete(file + ".meta");
                    File.Delete(file + ".manifest");
                    File.Delete(file + ".manifest.meta");
                }
            }
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 特殊处理主角相关的贴图
        /// </summary>
        /// <summary>
        /// 特殊处理主角相关的贴图
        /// </summary>
        private void PackPlayerModelTexture()
        {
            // 删除与主角合并Texture相关的AB
            string bundlePath = BuilderPreference.BUILD_PATH;
            string[] tempFiles = Directory.GetFiles(bundlePath, "*.ab", SearchOption.AllDirectories)
                                 .Where(f => f.Contains("_tmp")).ToArray();

            for (int i = 0; i < tempFiles.Length; i++)
            {
                string relativePath = BuildUtil.RelativePaths(tempFiles[i]);
                AssetDatabase.DeleteAsset(relativePath);
                AssetDatabase.DeleteAsset(relativePath + ".manifest");
            }

            //合并贴图
            string root = "Assets/Models/RoleModels/";
            string[] subFolder = new string[3] { "Players", "Weapons", "Wings" };

            Dictionary<string, List<string>> textureDict = new Dictionary<string, List<string>>();
            Builder.AddBuildLog("<Assetbundle Building> Get model texture map...");

            for (int i = 0; i < subFolder.Length; ++i)
            {
                string path = root + subFolder[i];

                List<string> files = new List<string>();
                string[] jpgs = Directory.GetFiles(path, "*.jpg", SearchOption.AllDirectories);
                files.AddRange(jpgs);

                string[] pngs = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
                files.AddRange(pngs);

                for (int j = 0; j < files.Count; ++j)
                {
                    var file = files[j].Replace("\\", "/").ToLower();
                    string id = Path.GetFileNameWithoutExtension(file).Replace("_light", "");
                    List<string> lists;
                    if (!textureDict.TryGetValue(id, out lists))
                    {
                        lists = new List<string>();
                        lists.Add(file);
                        textureDict.Add(id, lists);
                    }
                    else
                    {
                        if (file.EndsWith(".png"))
                            lists.Insert(0, file);
                        else
                            lists.Add(file);
                    }
                }
            }
            string save_path = Path.Combine(BuilderPreference.BUILD_PATH, "/combinedtextures");
            BuildUtil.SwapDirectory(save_path);

            int index = 0;
            Builder.AddBuildLog("<Assetbundle Building> Pack Model Texture...");
            foreach (string fileName in textureDict.Keys)
            {
                List<string> files = textureDict[fileName];
                if (fileName.Contains("_normal", StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = 0; i < files.Count; ++i)
                    {
                        if (files[i].Contains("/Players/"))
                        {
                            if (File.Exists(files[i]))
                            {
                                File.Delete(fileName);
                                break;
                            }
                        }
                    }
                }

                if (files.Count < 2) continue;

                var file_path = string.Concat(save_path, "/", fileName, ".bytes").ToLower();
                var pngBytes = File.ReadAllBytes(files[0]);
                var jpgBytes = File.ReadAllBytes(files[1]);
                using (var fs = new FileStream(file_path, FileMode.OpenOrCreate))
                {
                    byte[] intPngBuff = BitConverter.GetBytes(pngBytes.Length);
                    fs.Write(intPngBuff, 0, 4);
                    fs.Write(pngBytes, 0, pngBytes.Length);

                    byte[] intJpgBuff = BitConverter.GetBytes(jpgBytes.Length);
                    fs.Write(intJpgBuff, 0, 4);
                    fs.Write(jpgBytes, 0, jpgBytes.Length);
                    fs.Flush();
                }
                index++;
            } //end foreach
        }

    }
}