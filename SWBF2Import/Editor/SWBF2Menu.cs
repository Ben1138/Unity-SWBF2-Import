using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SWBF2Menu : ScriptableObject {

    [MenuItem("SWBF2/Import Mesh/Import *.msh", false, 1)]
    public static void ImportMSH() {
        string fileName = EditorUtility.OpenFilePanelWithFilters("Open Mesh File", "", new string[] { "SWBF2 Mesh File", "msh" });

        if (fileName == null || fileName.Length == 0)
            return;

        FileInfo file = new FileInfo(fileName);

        if (file.Exists) {
            GameObject msh = SWBF2Import.ImportMSH(file.FullName);

            if (msh != null) {
                
            }
            else {
                EditorUtility.DisplayDialog("Error", "Error while opening " + file.FullName, "ok");
            }
        }
        else {
            EditorUtility.DisplayDialog("Not found!", fileName + " could not be found!", "ok");
        }
    }
}
