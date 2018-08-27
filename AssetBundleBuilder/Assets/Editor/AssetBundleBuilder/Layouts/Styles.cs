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
            new GUIContent("All Assets Build"),
            new GUIContent("All Package Build"),   
        };


        public AutoBuildType[] AutoBuilds = new AutoBuildType[]
        {
            AutoBuildType.ALLBuild, 
            AutoBuildType.AllAssetBuild, 
            AutoBuildType.AllPackageBuild, 
        };


        public GUIContent[] BuildOptions = new GUIContent[]
        {
            new GUIContent("Seperate"), 
            new GUIContent("TogetherFolders"),
            new GUIContent("TogetherFiles"),  
        };
    }
}