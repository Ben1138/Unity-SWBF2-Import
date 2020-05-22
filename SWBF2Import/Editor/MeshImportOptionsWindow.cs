using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LibSWBF2.MSH.Types;

public class MshImportOptionsWindow : EditorWindow {
    public static MTYP[] legalTypesDefault = new MTYP[] {
        MTYP.Static
    };

    public static ModelTag[] legalModelsDefault = new ModelTag[] {
        ModelTag.Collision,
        ModelTag.Common
    };

    private static MTYP[] legalTypes = Enum.GetValues(typeof(MTYP)) as MTYP[];
    private static bool[] legalTypesSelected = null;

    private static ModelTag[] legalModels = Enum.GetValues(typeof(ModelTag)) as ModelTag[];
    private static bool[] legalModelsSelected = null;

    private Vector2 scrollPos;


    [MenuItem("SWBF2/Import Mesh/Options", false, 10)]
    public static void Init() {
        if (legalTypesSelected == null) {
            legalTypesSelected = new bool[legalTypes.Length];

            foreach (MTYP def in legalTypesDefault) {
                int index = Array.IndexOf(legalTypes, def);

                if (index >= 0)
                    legalTypesSelected[index] = true;
            }
        }

        if (legalModelsSelected == null) {
            legalModelsSelected = new bool[legalModels.Length];

            foreach (ModelTag def in legalModelsDefault) {
                int index = Array.IndexOf(legalModels, def);

                if (index >= 0)
                    legalModelsSelected[index] = true;
            }
        }

        MshImportOptionsWindow window = GetWindow<MshImportOptionsWindow>();
        window.Show();

        // try load our default material we ship
        SWBF2Import.DEFAULT_MATERIAL = AssetDatabase.LoadAssetAtPath<Material>("Assets/SWBF2Import/DefaultImportMaterial.mat");
    }

    private void OnGUI() {
        EditorGUIUtility.labelWidth = 250;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Import Mesh Types", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("NOTE: For unchecked models empty GameObjects will be created anyway to not break parentship.");

        //Build up Toggle List of availablke Types
        List<MTYP> legalTypesFinal = new List<MTYP>();

        for (int i = 0; i < legalTypes.Length; i++) {
            legalTypesSelected[i] = EditorGUILayout.Toggle(legalTypes[i].ToString(), legalTypesSelected[i]);

            if (legalTypesSelected[i])
                legalTypesFinal.Add(legalTypes[i]);
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Import Models", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("NOTE: For unchecked models empty GameObjects will be created anyway to not break parentship.");

        //Build up Toggle List of available Tags
        List<ModelTag> legalModelsFinal = new List<ModelTag>();

        for (int i = 0; i < legalModels.Length; i++) {
            legalModelsSelected[i] = EditorGUILayout.Toggle(legalModels[i].ToString(), legalModelsSelected[i]);

            if (legalModelsSelected[i])
                legalModelsFinal.Add(legalModels[i]);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Base Material", EditorStyles.boldLabel);
        SWBF2Import.DEFAULT_MATERIAL = EditorGUILayout.ObjectField("Material", SWBF2Import.DEFAULT_MATERIAL, typeof(Material), true) as Material;
        SWBF2Import.DEFAULT_MATERIAL_ALBEDO = EditorGUILayout.TextField("Albedo Shader Parameter Name", SWBF2Import.DEFAULT_MATERIAL_ALBEDO);
        //SWBF2Import.DEFAULT_MATERIAL_NORMAL = EditorGUILayout.TextField("Normal Map Shader Parameter Name", SWBF2Import.DEFAULT_MATERIAL_NORMAL);
        //SWBF2Import.NORMAL_MAP_SUFFIX = EditorGUILayout.TextField("Normal Map suffix", SWBF2Import.NORMAL_MAP_SUFFIX);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Asset Creation", EditorStyles.boldLabel);
        SWBF2Import.CREATE_ASSETS = EditorGUILayout.Toggle("Create Assets", SWBF2Import.CREATE_ASSETS);
        //if (!SWBF2Import.CREATE_ASSETS)
        //{
        //    SWBF2Import.CREATE_MESH_ASSETS = false;
        //}
        GUI.enabled = SWBF2Import.CREATE_ASSETS;
        //SWBF2Import.CREATE_MESH_ASSETS = EditorGUILayout.Toggle("Create individual Mesh Assets", SWBF2Import.CREATE_MESH_ASSETS);
        SWBF2Import.MODELS_FOLDER = EditorGUILayout.TextField("Models Folder", SWBF2Import.MODELS_FOLDER);
        if (SWBF2Import.MODELS_FOLDER[0] != '/')
            SWBF2Import.MODELS_FOLDER = '/' + SWBF2Import.MODELS_FOLDER;

        GUI.enabled = true;
        SWBF2Import.IMPORT_TEXTURES = EditorGUILayout.Toggle("Import Textures", SWBF2Import.IMPORT_TEXTURES);
        GUI.enabled = SWBF2Import.IMPORT_TEXTURES;
        SWBF2Import.TEXTURES_FOLDER = EditorGUILayout.TextField("Textures Folder", SWBF2Import.TEXTURES_FOLDER);
        GUI.enabled = true;
        if (SWBF2Import.TEXTURES_FOLDER[0] != '/')
            SWBF2Import.TEXTURES_FOLDER = '/' + SWBF2Import.TEXTURES_FOLDER;

        SWBF2Import.LEGAL_TYPES = legalTypesFinal.ToArray();
        SWBF2Import.LEGAL_MODELS = legalModelsFinal.ToArray();

        EditorGUILayout.Space();
        EditorGUILayout.EndScrollView();
    }
}
