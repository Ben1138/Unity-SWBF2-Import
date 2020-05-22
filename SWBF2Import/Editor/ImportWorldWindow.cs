using System;
using System.Threading;
using System.IO;
using UnityEngine;
using UnityEditor;
using LibSWBF2.WLD;

public class ImportWorldWindow : EditorWindow {
    private Vector2 scrollPos = Vector2.zero;

    private string[] altMshDirs = new string[4];
    private FileInfo worldFile = null;
    private bool importTerrain = false;
    private bool[] layerSelected = new bool[0];
    private WLD world = null;


    [MenuItem("SWBF2/Import World/Import *.wld")]
    private static void Init() {
        ImportWorldWindow window = GetWindow<ImportWorldWindow>();

        for (int i = 0; i < window.altMshDirs.Length; i++)
            window.altMshDirs[i] = PlayerPrefs.GetString("SWBF2Import_AltDirs_" + i);

        if (string.IsNullOrEmpty(window.altMshDirs[0]))
            window.altMshDirs[0] = @"C:\BF2_ModTools\data\Common\mshs";

        if (string.IsNullOrEmpty(window.altMshDirs[1]))
            window.altMshDirs[1] = @"C:\BF2_ModTools\data\Common\mshs\PC";

        window.Show();
        MshImportOptionsWindow.Init();
    }

    private void OnGUI() {
        EditorGUIUtility.labelWidth = 250;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("Open *.wld File")) {
            string worldPath = EditorUtility.OpenFilePanelWithFilters("Open Battlefront 2 World", "", new string[] { "SWBF2 World (*.wld)", "wld" });

            if (worldPath == null || worldPath.Length == 0)
                return;

            worldFile = new FileInfo(worldPath);
            if (worldFile.Exists) {
                try {
                    world = WLD.LoadFromFile(worldFile.FullName);
                } catch (Exception ex) {
                    EditorUtility.DisplayDialog("Error", "Error: " + ex.Message, "ok");
                    world = null;
                    return;
                }

                layerSelected = new bool[world.Layers.Count];
            } 
            else {
                EditorUtility.DisplayDialog("Not Found", worldFile.FullName + " could not be found!", "ok");
            }
        }

        EditorGUILayout.Space();

        string path = "";

        if (worldFile != null && worldFile.Exists)
            path = worldFile.FullName;

        EditorGUILayout.LabelField("Specify alternate MSH Directorys (Optional)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("The importer will look there for meshes it can't find in the worlds own msh folder.");
        EditorGUILayout.LabelField("This e.g. applies for health droids, command posts, etc.");
        EditorGUILayout.LabelField("Adapt the default paths below if necessary, or leave them empty:");
        for (int i = 0; i < altMshDirs.Length; i++) {
            string previous = altMshDirs[i];
            altMshDirs[i] = EditorGUILayout.TextField("Alternate MSH Directory", altMshDirs[i]);
            if (altMshDirs[i] != previous)
                PlayerPrefs.SetString("SWBF2Import_AltDirs_" + i, altMshDirs[i]);
        }

        EditorGUILayout.Space();

        if (world != null) {
            EditorGUILayout.LabelField("WLD File", path);
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            //Display Terrain Info
            EditorGUILayout.LabelField("Terrain Information", EditorStyles.boldLabel);

            if (world.Terrain != null) {
                EditorGUILayout.LabelField("Terrain Version", world.Terrain.Version.ToString());
                EditorGUILayout.LabelField("Terrain Size", world.Terrain.GridSize + "x" + world.Terrain.GridSize);
                EditorGUILayout.LabelField("Terrain Scale", world.Terrain.GridScale.ToString());
                EditorGUILayout.LabelField("Terrain Height Multiplier", world.Terrain.HeightMultiplier.ToString());
                importTerrain = EditorGUILayout.Toggle("Import Terrain", importTerrain);
                EditorGUILayout.Space();
            }
            else {
                EditorGUILayout.LabelField("No Terrain available!");
            }

            // Display Layer Selection
            EditorGUILayout.LabelField("Layers to import", EditorStyles.boldLabel);

            for (int i = 0; i < world.Layers.Count; i++) {
                layerSelected[i] = EditorGUILayout.Toggle(
                    world.Layers[i].Name + "  (" + world.Layers[i].WorldObjects.Count + ")",
                    layerSelected[i]
                );
            }

            EditorGUILayout.Space();

            //Import Button
            if (GUILayout.Button("IMPORT")) {
                if (SWBF2Import.DEFAULT_MATERIAL == null)
                {
                    EditorUtility.DisplayDialog("ERROR", "Please specify a default Material first in the Mesh Import Options!", "ok");
                    return;
                }

                bool goOn = true;

                if (!AtLeastOneLayerSelected() && !importTerrain) {
                    EditorUtility.DisplayDialog("Nothing to import", "Nothing selected to import! Select Terrain or at least one Layer to import!", "OK");
                    goOn = false;
                }

                if (goOn) {
                    string[] mshDirs = new string[4 + altMshDirs.Length];

                    mshDirs[0] = worldFile.DirectoryName;
                    mshDirs[1] = worldFile.DirectoryName + "/PC";
                    mshDirs[2] = Directory.GetParent(worldFile.DirectoryName).FullName + "/msh";
                    mshDirs[3] = Directory.GetParent(worldFile.DirectoryName).FullName + "/msh/PC";

                    for (int i = 0; i < altMshDirs.Length; i++) {
                        mshDirs[i + 4] = altMshDirs[i];
                    }

                    SWBF2Import.ImportWLD(world, mshDirs, layerSelected, importTerrain);

                    //foreach (string s in LibSWBF2.Log.GetAllLines(LibSWBF2.LogType.Info))
                    //Debug.Log(s);
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void Update() {
        Repaint();
    }

    private bool AtLeastOneLayerSelected() {
        foreach (bool b in layerSelected)
            if (b)
                return true;

        return false;
    }
}
