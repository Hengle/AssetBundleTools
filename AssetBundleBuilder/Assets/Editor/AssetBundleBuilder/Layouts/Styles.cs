using UnityEngine;

namespace AssetBundleBuilder
{
    public class Styles
    {
         public GUIContent[] SDKConfigs = new GUIContent[]
         {
             new GUIContent("DebugIn"),
             new GUIContent("DebugOut"),  
         };


        public GUIContent[] OnekeyBuilds = new GUIContent[]
        {
            new GUIContent("All Build"),
            new GUIContent("All Update Assets Build"),
            new GUIContent("All Update Package Build"),   
        };


        public AutoBuildType[] AutoBuilds = new AutoBuildType[]
        {
            AutoBuildType.ALLBuild, 
            AutoBuildType.UpdateAssetBuild, 
            AutoBuildType.UpdatePackageBuild, 
        };

    }
}