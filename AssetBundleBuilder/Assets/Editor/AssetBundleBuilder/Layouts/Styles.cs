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

        public string[] BuildContents = new[]
        {
            "Build Lua",
//            new GUIContent("Build Table" , "表格"), 
            "Build FullAssets", //"整包资源"
            "Build SubAssets" ,//"分包资源"
            "Build SubPackage" ,// "分包APK", 
        };


        public int[] BuildOpts = new[]
        {
            (int)Buildings.Lua,
            (int)Buildings.Package,
            (int)Buildings.Package,
            (int)(Buildings.Assetbundle|Buildings.Package)
        };

        public int[] BuildPackageOpts = new[]
        {
            0,
            (int)PackageBuildings.FullPackage,
            (int)PackageBuildings.SubPackage,
            (int)(PackageBuildings.SubPackage|PackageBuildings.BuildApp)
        };

        public GUIContent[] OnekeyBuilds = new GUIContent[]
        {
            new GUIContent("All Build"),
            new GUIContent("All Assets Build"),
            new GUIContent("All Package Build"),   
        };


        public int[] AutoBuilds = new int[]
        {
            (int)(PackageBuildings.SubPackage | PackageBuildings.FullPackage| PackageBuildings.BuildApp), 
            (int)(PackageBuildings.FullPackage | PackageBuildings.BuildApp),
            (int)(PackageBuildings.FullPackage | PackageBuildings.ForceUpdate | PackageBuildings.BuildApp), 
        };


        public GUIContent[] BuildOptions = new GUIContent[]
        {
            new GUIContent("Seperate"), 
            new GUIContent("TogetherFolders"),
            new GUIContent("TogetherFiles"),  
        };
    }
}