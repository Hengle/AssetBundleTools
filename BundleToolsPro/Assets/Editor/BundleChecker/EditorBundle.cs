using System.Collections.Generic;
using System.IO;

namespace BundleChecker
{
    

    public class EditorBundleBean
    {

        private string bundlePath;
        //包含资源
        private List<ResoucresBean> containeRes = new List<ResoucresBean>(); 
        //外部依赖
        private List<EditorBundleBean> dependencies = new List<EditorBundleBean>();
        /// <summary>
        /// 被其它Bundle依赖
        /// </summary>
        private List<EditorBundleBean> beDependcies = new List<EditorBundleBean>(); 
        public EditorBundleBean(string path)
        {
            this.bundlePath = path;
        }
        

        public string BundlePath { get { return bundlePath; } }

        public string BundleName
        {
            get { return Path.GetFileNameWithoutExtension(bundlePath); }
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
        public static string ShaderType = "Shader";
        public static string MatrialType = "Matrial";
        public static string AnimationClipType = "AnimationClip";
        public static string MeshType = "Mesh";
        public static string TextureType = "Texture";
        public static string AudioType = "Audio";
        public static string Prefab = "Prefab";
        public static string UnKnow = "UnKnow";

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
        protected string path;

        protected string mType;
        protected string fileName;

        protected EditorBundleBean mainBundle;
        public ResoucresBean(string path , EditorBundleBean bundle)
        {
            this.path = path;
            this.fileName = Path.GetFileName(path);
            this.mType = EResoucresTypes.GetResourceType(Path.GetExtension(path));

            mainBundle = bundle;

            ABMainChecker mainCheck = ABMainChecker.MainChecker;
            List<EditorBundleBean> bundles = null;
            
            if (!mainCheck.RedundancyDic.TryGetValue(fileName, out bundles))
            {
                bundles = new List<EditorBundleBean>();
                mainCheck.RedundancyDic[fileName] = bundles;
            }
            
            if(!bundles.Contains(bundle))   bundles.Add(bundle);
        }

        /// <summary>
        /// 资源名
        /// </summary>
        public string Name
        {
            get { return fileName; }
        }
        /// <summary>
        /// 资源类型
        /// </summary>
        public string ResourceType
        {
            get { return mType;}
        }
    }
}