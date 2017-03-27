using UnityEngine;
using System.Collections;
using System.IO;
using UnityEditor;

public class ABBuilder  {

    [MenuItem("AB冗余检测/Build")]
    public static void BuildAssetBundle()
    {
        string testPath = Path.Combine(Application.streamingAssetsPath, "bundles");
        if (!Directory.Exists(testPath)) Directory.CreateDirectory(testPath);

        BuildAssetBundleOptions option = BuildAssetBundleOptions.DeterministicAssetBundle |
                                         BuildAssetBundleOptions.UncompressedAssetBundle;
        BuildPipeline.BuildAssetBundles(testPath, option, EditorUserBuildSettings.activeBuildTarget);

        AssetDatabase.Refresh();
    }
}
