using System.Collections.Generic;

namespace AssetBundleBuilder
{
    public class AssetMap
    {
        private List<AssetMap> references;

        private string assetPath;

        private AssetBuildRule rule;

        private List<AssetMap> dependencys;

        public List<AssetMap> References
        {
            get { return references; }
        }

        public string AssetPath
        {
            get { return assetPath; }
        }

        public AssetBuildRule Rule
        {
            get { return rule; }
        }

        public List<AssetMap> Dependencys
        {
            get { return dependencys; }
        }

        public AssetMap(string path, AssetBuildRule rule)
        {
            this.assetPath = path;
            this.rule = rule;
        }


        public void AddReference(AssetMap asset)
        {
            if (references == null)
                references = new List<AssetMap>();

            references.Add(asset);
        }


        public void AddDependency(AssetMap asset)
        {
            if (dependencys == null)
                dependencys = new List<AssetMap>();
            dependencys.Add(asset);
        }
    }
}