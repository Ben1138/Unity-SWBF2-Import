using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LibSWBF2.MSH;
using LibSWBF2.MSH.Types;
using LibSWBF2.MSH.Chunks;
using LibSWBF2.WLD;
using LibSWBF2.WLD.Types;


public static class SWBF2Import {
    public static string MODELS_FOLDER = "/Models";
    public static string TEXTURES_FOLDER = "/Textures";
    //public static string NORMAL_MAP_SUFFIX = "_bump"; //It is normally bump map, You have to fix to normal map in unity. Not 100% accurate. You should change it back to "_normal" if you use custom normals.
    public static bool CREATE_ASSETS = false;
    //public static bool CREATE_MESH_ASSETS = false;
    public static bool IMPORT_TEXTURES = true;

    public static string ASSET_PATH = Application.dataPath;
    public static MTYP[] LEGAL_TYPES = new MTYP[] {
        MTYP.Static
    };

    public static ModelTag[] LEGAL_MODELS = new ModelTag[] {
        ModelTag.Collision,
        ModelTag.Common
    };

    public static Material DEFAULT_MATERIAL = null;
    public static string DEFAULT_MATERIAL_ALBEDO = "_MainTex";
    public static string DEFAULT_MATERIAL_NORMAL = "_BumpMap";

    // from msh path (BF2_ModTools) to "Assets/" path
    private static Dictionary<string, GameObject> PrefabMap = new Dictionary<string, GameObject>();


    public static void ImportWLD(WLD world, string[] mshDirs, bool[] layersToImport, bool importTerrain) {

        if (mshDirs == null || mshDirs.Length == 0) {
            Debug.LogError("No msh directorys specified!");
            return;
        }

        if (layersToImport == null || layersToImport.Length == 0) {
            Debug.LogError("No layers to import specified!");
            return;
        }

        if (world.Layers.Count != layersToImport.Length) {
            Debug.LogError(string.Format("Number of Layers ({0}) does not match Muber of bool[] layersToImport ({1})!", world.Layers.Count, layersToImport.Length));
            return;
        }

        PrefabMap.Clear();
        EditorUtility.DisplayProgressBar("Import world", "Import world", 0.0f);
        for (int i = 0; i < world.Layers.Count; i++) {
            if (!layersToImport[i]) {
                //Debug.Log("Skip Layer[" + i + "]: " + world.Layers[i].Name);
                continue;
            }

            //Debug.Log("Layer " + world.Layers[i].Name + " has " + world.Layers[i].WorldObjects.Count + " objects in it");
            for (int j = 0; j < world.Layers[i].WorldObjects.Count; j++) {
                WorldObject obj = world.Layers[i].WorldObjects[j];
                
                bool found = false;
                foreach (string dir in mshDirs) {
                    if (!Directory.Exists(dir)) {
                        continue;
                    }
                    
                    string mshPath = dir + "/" + obj.meshName + ".msh";

                    if (File.Exists(mshPath)) {
                        EditorUtility.DisplayProgressBar("Import Mesh", "'"+ obj.meshName + ".msh' ("+(j+1)+"/"+ world.Layers[i].WorldObjects.Count + ") in Layer '"+ world.Layers[i].Name + "' ("+(i+1)+"/"+ world.Layers.Count + ")", j / (float)world.Layers[i].WorldObjects.Count);
                        GameObject msh = ImportMSH(mshPath, mshDirs, true);
                        if (msh != null)
                        {
                            msh.transform.position = Vector2Unity(obj.position);
                            msh.transform.rotation = Quaternion2Unity(obj.rotation);
                            msh.name = obj.name;
                            found = true;
                            break;
                        }
                        else
                        {
                            Debug.LogWarning("COULD NOT IMPORT: " + mshPath);
                        }
                    }
                }

                if (!found) {
                    Debug.LogWarning("Could not find mesh: " + obj.meshName);
                }
            }
        }

        //Import Terrain
        if (importTerrain)
        {
            EditorUtility.DisplayProgressBar("Import Terrain", "Import Terrain...", 0.9f);
            TER terrain = world.Terrain;
            TerrainData data = new TerrainData();

            //gridSize
            data.heightmapResolution = terrain.GridSize + 1;

            //texture res
            data.baseMapResolution = 1024;
            data.SetDetailResolution(1024, 8);

            float[,] heights = new float[terrain.GridSize, terrain.GridSize];

            //save min and max values from imported terrain
            float min = 0;
            float max = 0;

            int xLen = heights.GetLength(0);
            int yLen = heights.GetLength(1);

            for (int x = 0; x < xLen; x++) {
                for (int y = 0; y < yLen; y++) {
                    float h = terrain.GetHeight(x, y);

                    if (h < min)
                        min = h;
                    if (h > max)
                        max = h;

                    heights[x, y] = h;
                }
            }

            //e.g. min = -10 & max = 120, then
            //range = 120 - (-10) = 130
            //or e.g. min = -10 & max = -3, then
            //range = -3 - (-10) = 7
            float range = max - min;

            //Debug.Log("Terrain Min: " + min);
            //Debug.Log("Terrain Max: " + max);
            //Debug.Log("Terrain Range: " + range);

            //Normalize given Terrain heights
            for (int x = 0; x < xLen; x++) {
                for (int y = 0; y < yLen; y++) {
                    //since unity's terrain range goes from 0 to 1, we have to lift everything up. 
                    //so we're in a range of 0-666 (or whatever)
                    heights[x,y] -= min;

                    //normalize by full range to get heights between 0 and 1
                    heights[x, y] /= range;
                }
            }

            data.SetHeights(0, 0, heights);

            //calc true size
            float size = terrain.GridSize * terrain.GridScale;
            data.size = new Vector3(size, range, size);



            GameObject terrainObj = Terrain.CreateTerrainGameObject(data);

            //since we normalized everything, we have to shift to the former offset,
            //so lift everything down by min again.
            //and of course we have to set the terrain into the center, so shift by half the size
            terrainObj.transform.Translate(-size/2, min, -size/2);

            //we're ignoring the Terrains extend window. just display the whole terrain
        }

        EditorUtility.ClearProgressBar();
    }


	public static GameObject ImportMSH(string path, string[] additionalTextureSearchPaths = null, bool bCheckEditorExistence=false)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Tried to call ImportMSH with an empty path!");
            return null;
        }

        if (PrefabMap.TryGetValue(path, out GameObject prefab))
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            return instance;
        }

        FileInfo mshFile = new FileInfo(path);
        if (mshFile.Exists) {
            MSH msh;

            try {
                msh = MSH.LoadFromFile(mshFile.FullName);
            } catch {
                return null;
            }

            string fileName = mshFile.Name.Replace(".msh", "");

            if (CREATE_ASSETS)
            {
                if (!AssetDatabase.IsValidFolder("Assets" + MODELS_FOLDER))
                    AssetDatabase.CreateFolder("Assets", MODELS_FOLDER.Remove(0, 1));

                if (!AssetDatabase.IsValidFolder("Assets" + MODELS_FOLDER + "/" + fileName))
                    AssetDatabase.CreateFolder("Assets" + MODELS_FOLDER, fileName);

                if (!AssetDatabase.IsValidFolder("Assets" + MODELS_FOLDER + "/" + fileName + "/MeshData"))
                    AssetDatabase.CreateFolder("Assets" + MODELS_FOLDER + "/" + fileName, "MeshData");
            }

            GameObject rootObj = new GameObject(fileName);

            Dictionary<MODL, GameObject> alreadyLoaded = new Dictionary<MODL, GameObject>();
            GameObject LoadMODL(MODL mdl, GameObject currentParent)
            {
                if (mdl == null)
                {
                    Debug.LogError("MODL was NULL!");
                    return null;
                }
                if (currentParent == null)
                {
                    Debug.LogError("currentParent was NULL!");
                    return null;
                }

                if (alreadyLoaded.TryGetValue(mdl, out GameObject obj))
                    return obj;

                GameObject modelObj = new GameObject(mdl.Name);
                if (mdl.Parent != null)
                {
                    modelObj.transform.SetParent(LoadMODL(mdl.Parent, modelObj).transform);
                }
                else
                {
                    modelObj.transform.SetParent(currentParent.transform);
                }

                modelObj.transform.position = Vector2Unity(mdl.Translation);
                modelObj.transform.rotation = Quaternion.Euler(Vector2Unity(mdl.Rotation));
                modelObj.transform.localScale = Vector2Unity(mdl.Scale);

                if (!Array.Exists(LEGAL_TYPES, t => t == mdl.Type))
                    return modelObj;

                if (!Array.Exists(LEGAL_MODELS, t => t == mdl.Tag))
                    return modelObj;

                if (mdl.Geometry == null || mdl.Geometry.Segments == null)
                    return modelObj;

                // TODO: use arrays instead of lists
                List<Vector3> vertices = new List<Vector3>();
                List<Vector3> normals = new List<Vector3>();
                List<Vector2> uvs = new List<Vector2>();
                List<Material> materials = new List<Material>();

                //each segment contains its own list of triangles
                List<int>[] triangles = new List<int>[mdl.Geometry.Segments.Count];

                //since each Segment acts as its own object (vertices, normals, triangles...) and we want to
                //merge all vertices into one array (as Unity expects it) but preserve our Segment Materials,
                //we have to set a sub mesh for each Segment, containing a list of vertex indices (of the now global vertex list).
                //so we have to store an offset
                int vertexOffset = 0;

                Mesh mesh = new Mesh();
                mesh.name = mdl.Name;
                mesh.subMeshCount = mdl.Geometry.Segments.Count;

                //Debug.Log("Sub Mesh Count set to: " + mesh.subMeshCount);

                for (int si = 0; si < mdl.Geometry.Segments.Count; si++) {
                    //Debug.Log("Segment No: " + si);
                    SEGM segm = mdl.Geometry.Segments[si];

                    triangles[si] = new List<int>();

                    foreach (Vertex vert in segm.Vertices) {
                        vertices.Add(new Vector3(-vert.position.X, vert.position.Y, vert.position.Z));
                        normals.Add(new Vector3(vert.normal.X, vert.normal.Y, vert.normal.Z));
                        uvs.Add(new Vector2(vert.uvCoordinate.X, vert.uvCoordinate.Y));
                    }

                    for (int pi = 0; pi < segm.Polygons.Length; pi++) {
                        Polygon poly = segm.Polygons[pi];

                        int triCount = 0;
                        List<int> tris = new List<int>();

                        //in MSH, polygons are defined as triangle strips.
                        //since unity expects just triangles, we have to strip them ourselfs
                        for (int vi = 0; vi < poly.VertexIndices.Count; vi++) {
                            if (triCount == 3) {
                                vi -= 2;
                                triCount = 0;
                            }

                            tris.Add(poly.VertexIndices[vi] + vertexOffset);
                            triCount++;
                        }

                        //triangles are listed CW CCW CW CCW...
                        bool flip = true;
                        for (int j = 0; j < tris.Count; j += 3) {
                            if (flip) {
                                int tmp = tris[j];
                                tris[j] = tris[j + 2];
                                tris[j + 2] = tmp;
                            }

                            flip = !flip;
                        }

                        triangles[si].AddRange(tris);
                    }

                    string[] textureDirs = new string[1] { mshFile.Directory.FullName };
                    if (additionalTextureSearchPaths != null)
                    {
                        textureDirs = new string[additionalTextureSearchPaths.Length + 1];
                        additionalTextureSearchPaths[0] = mshFile.Directory.FullName;
                        Array.Copy(additionalTextureSearchPaths, 0, textureDirs, 1, additionalTextureSearchPaths.Length);
                    }

                    //Add Segment Material to overall Material List
                    materials.Add(Material2Unity(textureDirs, fileName, segm.Material, DEFAULT_MATERIAL));

                    //don't forget to increase our vertex offset
                    vertexOffset += segm.Vertices.Length;
                }

                mesh.vertices = vertices.ToArray();
                mesh.normals = normals.ToArray();
                mesh.uv = uvs.ToArray();

                //set triangles AFTER vertices to avoid out of bounds errors
                for (int si = 0; si < triangles.Length; si++) {
                    mesh.SetTriangles(triangles[si], si);
                }

                //Interpret Common and Lowrez as Geometry
                if (mdl.Tag == ModelTag.Common || mdl.Tag == ModelTag.Lowrez) {
                    MeshFilter filter = modelObj.AddComponent<MeshFilter>();
                    filter.mesh = mesh;

                    MeshRenderer renderer = modelObj.AddComponent<MeshRenderer>();
                    renderer.materials = materials.ToArray();
                    renderer.sharedMaterials = materials.ToArray();
                }

                //and everything else as collision / trigger
                else {
                    MeshCollider collider = modelObj.AddComponent<MeshCollider>();
                    collider.sharedMesh = mesh;

                    if (mdl.Tag != ModelTag.Collision && mdl.Tag != ModelTag.VehicleCollision) {
                        collider.convex = true;
                        collider.isTrigger = true;
                    }
                }

                if (CREATE_ASSETS)
                {
                    AssetDatabase.CreateAsset(mesh, "Assets" + MODELS_FOLDER + "/" + fileName + "/MeshData/" + mesh.name + ".unitymesh");
                }

                alreadyLoaded.Add(mdl, modelObj);
                return modelObj;
            }

            foreach (MODL mdl in msh.Models)
            {
                LoadMODL(mdl, rootObj);
            }

            if (CREATE_ASSETS)
            {
                string prefabPath = "Assets" + MODELS_FOLDER + "/" + fileName + "/" + fileName + ".prefab";
                GameObject newPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(rootObj, prefabPath, InteractionMode.AutomatedAction);
                PrefabUtility.ApplyPrefabInstance(rootObj, InteractionMode.AutomatedAction);
                PrefabMap.Add(path, newPrefab);
            }

            return rootObj;
        }

        return null;
    }

    public static bool ImportTerrain(string path) {
        FileInfo terFile = new FileInfo(path);

        if (terFile.Exists) {
            try {
                TER.LoadFromFile(terFile.FullName);
            } catch {
                return false;
            }
        }

        return false;
    }

    public static Color Color2Unity(LibSWBF2.Types.Color color) {
        return new Color(color.R, color.G, color.B, color.A);
    }

    public static Vector2 Vector2Unity(LibSWBF2.Types.Vector2 vector) {
        return new Vector2(vector.X, vector.Y);
    }

    public static Vector3 Vector2Unity(LibSWBF2.Types.Vector3 vector) {
        return new Vector3(vector.X, vector.Y, vector.Z);
    }

    public static Vector4 Vector2Unity(LibSWBF2.Types.Vector4 vector) {
        return new Vector4(vector.X, vector.Y, vector.Z, vector.W);
    }

    public static Quaternion Quaternion2Unity(LibSWBF2.Types.Vector4 vector) {
        return new Quaternion(vector.W, vector.Z, vector.Y, vector.X);
    }

    public static Material Material2Unity(string[] textureSearchDirs, string mshObjName, MATD from, Material baseMat, string[] additionalTextureSearchPaths = null) {
        Material material = new Material(baseMat);
        //material.EnableKeyword("_METALLICGLOSSMAP");
        //material.EnableKeyword("_SPECGLOSSMAP");
        material.name = from.Name;
        material.SetColor("_Color", Color2Unity(from.Diffuse));
        
        if (!string.IsNullOrEmpty(from.Texture) && IMPORT_TEXTURES)
        {
            Texture2D albedo = ImportTexture(textureSearchDirs, from.Texture);
            //Texture2D normal = ImportTexture(mshSourceDir, from.Texture);

            if (albedo != null)
                material.SetTexture(DEFAULT_MATERIAL_ALBEDO, albedo);

            //if (normal != null)
            //{
            //    material.EnableKeyword("_NORMALMAP");
            //    material.SetTexture(DEFAULT_MATERIAL_NORMAL, normal);
            //}
        }

        if (CREATE_ASSETS)
        {
            if (!AssetDatabase.IsValidFolder("Assets" + MODELS_FOLDER + "/" + mshObjName + "/Materials"))
                AssetDatabase.CreateFolder("Assets" + MODELS_FOLDER + "/" + mshObjName, "Materials");
            AssetDatabase.CreateAsset(material, "Assets" + MODELS_FOLDER + "/" + mshObjName + "/Materials/" + material.name + ".mat");
        }

        return material;
    }

    public static Texture2D ImportTexture(string[] textureSearchDirs, string texName)
    {
        foreach (string sourceDir in textureSearchDirs)
        {
            Texture2D tex = ImportTexture(sourceDir, texName);
            if (tex != null)
                return tex;
        }

        Debug.LogWarning("Could not find Texture: " + texName);
        return null;
    }

    // sourceDir is absolute windows path, name includes extension (*.tga)
    public static Texture2D ImportTexture(string sourceDir, string texName)
    {
        if (IMPORT_TEXTURES && !AssetDatabase.IsValidFolder("Assets" + TEXTURES_FOLDER))
            AssetDatabase.CreateFolder("Assets", TEXTURES_FOLDER.Remove(0, 1));

        string texSource = sourceDir + "/" + texName;
        string texDest = ASSET_PATH + TEXTURES_FOLDER + "/" + texName;
        if (File.Exists(texSource))
        {
            if (!File.Exists(texDest))
            {
                try
                {
                    File.Copy(texSource, texDest, false);
                }
                catch (Exception e)
                {
                    Debug.LogError("ERROR: " + e.Message);
                    return null;
                }
            }

            AssetDatabase.ImportAsset("Assets" + TEXTURES_FOLDER + "/" + texName, ImportAssetOptions.Default);
            return AssetDatabase.LoadAssetAtPath<Texture2D>("Assets" + TEXTURES_FOLDER + "/" + texName);
        }
        else
        {
            //Debug.LogWarning("Could not find Texture: " + texSource);
            return null;
        }
    }
}
