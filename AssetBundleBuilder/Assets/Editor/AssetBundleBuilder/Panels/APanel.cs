using UnityEngine;

namespace AssetBundleBuilder
{
    public abstract class APanel
    {
        protected AssetBundleBuilder mainBuilder;

        protected AssetBundleBuilderWindow builderWindow;

        private bool isInited;

        public AssetBundleBuilder MainBuilder
        {
            get { return mainBuilder; }
        }

        public bool IsInited
        {
            get { return isInited; }
        }

        public Rect Position
        {
            get { return builderWindow.position; }
        }

        protected APanel(AssetBundleBuilderWindow builderWindow)
        {
            this.builderWindow = builderWindow;
            mainBuilder = this.builderWindow.Builder;
        }


        public virtual void OnInit()
        {
            isInited = true;
        }
        

        public abstract void OnGUI();


        public virtual void OnDestroy()
        {
            
        }
    }
}