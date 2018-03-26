﻿using System;
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
    }

    private void OnGUI() {
        EditorGUIUtility.labelWidth = 250;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Import Mesh Types", EditorStyles.boldLabel);

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

        //Build up Toggle List of availablke Tags
        List<ModelTag> legalModelsFinal = new List<ModelTag>();

        for (int i = 0; i < legalModels.Length; i++) {
            legalModelsSelected[i] = EditorGUILayout.Toggle(legalModels[i].ToString(), legalModelsSelected[i]);

            if (legalModelsSelected[i])
                legalModelsFinal.Add(legalModels[i]);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Base Material", EditorStyles.boldLabel);
        SWBF2Import.DEFAULT_MATERIAL = EditorGUILayout.ObjectField("Material", SWBF2Import.DEFAULT_MATERIAL, typeof(Material), true) as Material;

        SWBF2Import.LEGAL_TYPES = legalTypesFinal.ToArray();
        SWBF2Import.LEGAL_MODELS = legalModelsFinal.ToArray();

        EditorGUILayout.Space();
        EditorGUILayout.EndScrollView();
    }
}
