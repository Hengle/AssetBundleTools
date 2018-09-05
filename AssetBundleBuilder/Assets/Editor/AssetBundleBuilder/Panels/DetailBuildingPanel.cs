using UnityEditor;
using UnityEngine;

namespace AssetBundleBuilder
{


    public class DetailBuildingPanel : APanel
    {
        private Vector2 logPosition;

        Rect progressbarRect
        {
            get { return new Rect(110, 35f, Position.width - 260, 20f); }
        }

        public DetailBuildingPanel(AssetBundleBuilderWindow builderWindow) : base(builderWindow)
        {

        }

        public override void OnGUI()
        {
            EditorGUI.ProgressBar(progressbarRect , this.mainBuilder.Progress , string.Empty);

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            GUILayout.Space(30);

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUILayout.Label("Building");
            GUILayout.EndVertical();

            if (mainBuilder.BuildingCount > 0)
            {
                if (GUILayout.Button("Canle", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    this.builderWindow.Builder.CanleBuild();
                }
            }
            else if(GUILayout.Button( "Back", GUILayout.Width(100), GUILayout.Height(30)))
            {
                this.builderWindow.SetPanelState(EToolbar.Home);
            }


            GUILayout.Space(20);

            GUILayout.EndHorizontal();

            if (mainBuilder.BuildingCount <= 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(110);
                GUILayout.Label(UseTimeText());
                GUILayout.EndHorizontal();
            }
            else
                GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Toggle(true, "Console", EditorStyles.toolbarButton, GUILayout.MaxWidth(100));
            GUILayout.Toolbar(0, new[] { "" }, EditorStyles.toolbar, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            logPosition = GUILayout.BeginScrollView(logPosition);
            for (int i = 0; i < mainBuilder.BuildLog.Count; i++)
            {
                EditorGUILayout.TextArea(this.mainBuilder.BuildLog[i], GUILayout.MaxWidth(Position.width - 20));
            }
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
        }


        private string UseTimeText()
        {
            double useTime = this.mainBuilder.UseTime;//(秒)

            int millSeconds = (int)useTime%1000;
            int seconds = (int)((useTime / 1000) % 60);
            int min = ((int)(useTime/1000)/3600)%60;
            
            return string.Format("Total use time :{0:D2}:{1:D2}:{2:D3}", min, seconds , millSeconds);
        }
    }
}