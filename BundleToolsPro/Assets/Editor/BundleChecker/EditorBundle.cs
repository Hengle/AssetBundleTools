using System.Collections.Generic;
using System.IO;
using BundleChecker.ResoucreAttribute;
using UnityEditor;
using UnityEngine;

namespace BundleChecker
{
    

    public class EditorBundleBean
    {
        //包含资源
        private List<ResoucresBean> containeRes = new List<ResoucresBean>(); 
        //外部依赖
        private List<EditorBundleBean> dependencies = new List<EditorBundleBean>();
        /// <summary>
        /// 被其它Bundle依赖
        /// </summary>
        private List<EditorBundleBean> beDependcies = new List<EditorBundleBean>();

        #region ---------------Public Attribute-------------------------
        public string BundleName { get; private set; }
        public string BundlePath { get; private set; }
        #endregion

        public EditorBundleBean(string path)
        {
            this.BundlePath = path;
            this.BundleName = Path.GetFileName(path);
        }
        
        /// <summary>
        /// 依赖的外部资源
        /// </summary>
        /// <returns></returns>
        public List<EditorBundleBean> GetAllDependencies()
        {
            return dependencies;
        }
        /// <summary>
        /// 其它Bundle的依赖（被依赖）
        /// </summary>
        /// <returns></returns>
        public List<EditorBundleBean> GetBedependencies()
        {
            return beDependcies;
        } 

        public List<ResoucresBean> GetAllAssets()
        {
            return containeRes;
        } 
    }


    public sealed class EResoucresTypes
    {
        public const string ShaderType = "Shader";
        public const string MatrialType = "Matrial";
        public const string AnimationClipType = "AnimationClip";
        public const string MeshType = "Mesh";
        public const string TextureType = "Texture";
        public const string AudioType = "Audio";
        public const string Prefab = "Prefab";
        public const string UnKnow = "UnKnow";

        private const string shaderSfx = ".shader";
        private const string materialSfx = ".mat";
        private const string texturePngSfx = ".png";
        private const string textureJpgSfx = ".jpg";
        private const string animSfx = ".anim";
        private const string prefabSfx = ".prefab";

        public static string GetResourceType(string suffix)
        {
            switch (suffix)
            {
                case shaderSfx: return ShaderType;
                case materialSfx:   return MatrialType;
                case textureJpgSfx:
                case texturePngSfx:
                    return TextureType;
                case animSfx:   return AnimationClipType;
                case prefabSfx: return Prefab;
            }
            
            return UnKnow;
        }
    }

    public class ResoucresBean
    {

        #region -------------------Public Attribute-----------------------------
        /// <summary>
        /// 资源名
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// 资源路径
        /// </summary>
        public string AssetPath { get; private set; }
        /// <summary>
        /// 资源类型
        /// </summary>
        public string ResourceType { get; private set; }
        /// <summary>
        /// 是否丢失
        /// </summary>
        public bool IsMissing { get; set; }
        /// <summary>
        /// 资源的原始数据
        /// </summary>
        public ABaseResource RawRes { get; set; }
        #endregion
        /// <summary>
        /// 所属Bundle
        /// </summary>
        public List<EditorBundleBean>  IncludeBundles = new List<EditorBundleBean>();
        /// <summary>
        /// 依赖的资源
        /// </summary>
        public Dictionary<string , ResoucresBean> Dependencies = new Dictionary<string, ResoucresBean>(); 
        public ResoucresBean(string path)
        {
            this.AssetPath = path;
            this.Name = Path.GetFileName(path);
            this.ResourceType = EResoucresTypes.GetResourceType(Path.GetExtension(path));

            this.loadRawAsset();
        }


        private void loadRawAsset()
        {
            switch (this.ResourceType)
            {
                case EResoucresTypes.TextureType:
                    RawRes = new TextureAttribute(this);
                    break;
            }
        }
        /// <summary>
        /// 检测依赖资源
        /// </summary>
        public void CheckDependencies()
        {
            Object[] assetObjs = AssetDatabase.LoadAllAssetsAtPath(this.AssetPath);
            Object[] depArr = EditorUtility.CollectDependencies(assetObjs);

            Dictionary<string, ResoucresBean> resDic = ABMainChecker.MainChecker.ResourceDic;
            foreach (Object depAsset in depArr)
            {
                string depAssetPath = AssetDatabase.GetAssetPath(depAsset);
                string depAssetName = Path.GetFileName(depAssetPath);
                if(depAssetName == Name)    continue;

                //排除不打包的文件，比如.cs
                string suffix = Path.GetExtension(depAssetPath);
                if(ABMainChecker.ExcludeFiles.Contains(suffix)) continue;

                ResoucresBean rb = null;
                if (!resDic.TryGetValue(depAssetName, out rb))
                {
                    rb = new ResoucresBean(depAssetPath);
                    rb.IsMissing = true;
                    resDic[depAssetName] = rb;

                    ABMainChecker.MainChecker.MissingRes.Add(rb);
                }

                Dependencies[depAssetPath] = rb;
            }            
        }
    }
}