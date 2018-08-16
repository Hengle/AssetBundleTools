using System.Collections;

namespace AssetBundleBuilder
{
    public abstract class ABuilding
    {

        public int Weight { get; private set; }

        public AssetBundleBuilder Builder;

        protected float progress;

        public float Progress { get { return progress;} }

        public ABuilding() { }

        public ABuilding(int weight)
        {
            this.Weight = weight;

        }

        public virtual IEnumerator OnBuilding()
        {
            return null;
        }

    }
}