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


        public int[] BuildingOpts = new[]
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


        public static GUIContent[] FolderBundleBuildOptions = new GUIContent[]
        {
            new GUIContent("Seperate"), 
            new GUIContent("Together Folders"),
            new GUIContent("Together Files"),  
            new GUIContent("Together All"), 
            new GUIContent("Ignore"), 
        };


        public static GUIContent[] FileBundleBuildOptions = new GUIContent[]
        {
            new GUIContent("Together Files"), 
            new GUIContent("Ignore"), 
        };

        public static int[] FolderBundleBuildEnums = new[]
        {
            (int)BundleBuildType.Separate, 
            (int)BundleBuildType.TogetherFolders, 
            (int)BundleBuildType.TogetherFiles, 
            (int)(BundleBuildType.TogetherFolders | BundleBuildType.TogetherFiles), 
            (int)BundleBuildType.Ignore,
        };

        public static int[] FileBundleBuildEnums = new[]
        {
            (int)BundleBuildType.TogetherFiles,
            (int)BundleBuildType.Ignore,
        };
    }
}