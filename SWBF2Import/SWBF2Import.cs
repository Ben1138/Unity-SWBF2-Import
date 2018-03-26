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
    public static readonly string MESH_FOLDER = "Meshes";
    public static readonly string SHADER = "Standard (Roughness setup)";
    public static readonly string NORMAL_MAP_SUFFIX = "_normal";
    public static readonly bool CREATE_ASSETS = false;
    public static string AssetPath = Application.dataPath;


    public static Material DEFAULT_MATERIAL = null;
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

        for (int i = 0; i < world.Layers.Count; i++) {
            if (!layersToImport[i]) {
                Debug.Log("Skip Layer[" + i + "]: " + world.Layers[i].Name);
                continue;
            }

            Debug.Log("Layer " + world.Layers[i].Name + " has " + world.Layers[i].WorldObjects.Count + " objects in it");
            for (int j = 0; j < world.Layers[i].WorldObjects.Count; j++) {
                WorldObject obj = world.Layers[i].WorldObjects[j];

                bool found = false;
                foreach (string dir in mshDirs) {
                    if (!Directory.Exists(dir)) {
                        continue;
                    }

                    string mshPath = dir + "/" + obj.meshName + ".msh";

                    if (File.Exists(mshPath)) {
                        GameObject msh = ImportMSH(mshPath);
                        msh.transform.position = Vector2Unity(obj.position);
                        msh.transform.rotation = Quaternion2Unity(obj.rotation);
                        found = true;
                    }
                }

                if (!found) {
                    Debug.LogWarning("Could not find mesh: " + obj.meshName);
                }
            }
        }

        //Import Terrain
        if (importTerrain) {
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

            for (int x = 0; x < heights.GetLength(0); x++) {
                for (int y = 0; y < heights.GetLength(1); y++) {
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

            Debug.Log("Terrain Min: " + min);
            Debug.Log("Terrain Max: " + max);
            Debug.Log("Terrain Range: " + range);

            //Normalize given Terrain heights
            for (int x = 0; x < heights.GetLength(0); x++) {
                for (int y = 0; y < heights.GetLength(1); y++) {
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
    }

	public static GameObject ImportMSH(string path) {
        FileInfo mshFile = new FileInfo(path);

        if (mshFile.Exists) {
            MSH msh;

            try {
                msh = MSH.LoadFromFile(mshFile.FullName);
            } catch {
                return null;
            }

            string fileName = mshFile.Name.Replace(".msh", "");

            if (!Directory.Exists(AssetPath + "/" + MESH_FOLDER) && CREATE_ASSETS)
                AssetDatabase.CreateFolder("Assets", MESH_FOLDER);

            GameObject rootObj = new GameObject(fileName);


            foreach (MODL mdl in msh.Models) {
                if (mdl.Type != MTYP.Static)
                    continue;

                if (mdl.Tag == ModelTag.Lowrez)
                    continue;

                GameObject modelObj = new GameObject(mdl.Name);
                modelObj.transform.SetParent(rootObj.transform);
                modelObj.transform.position = Vector2Unity(mdl.Translation);
                modelObj.transform.rotation = Quaternion.Euler(Vector2Unity(mdl.Rotation));
                modelObj.transform.localScale = Vector2Unity(mdl.Scale);

                if (mdl.Geometry != null) {
                    for (int si = 0; si < mdl.Geometry.Segments.Length; si++) {
                        SEGM segm = mdl.Geometry.Segments[si];

                        List<Vector3> vertices = new List<Vector3>();
                        List<Vector3> normals = new List<Vector3>();
                        List<Vector2> uvs = new List<Vector2>();
                        List<int> triangles = new List<int>();

                        foreach (Vertex vert in segm.Vertices) {
                            vertices.Add(new Vector3(-vert.position.X, vert.position.Y, vert.position.Z));
                            normals.Add(new Vector3(vert.normal.X, vert.normal.Y, vert.normal.Z));
                            uvs.Add(new Vector2(vert.uvCoordinate.X, vert.uvCoordinate.Y));
                        }

                        for (int pi = 0; pi < segm.polygons.Count; pi++) {
                            Polygon poly = segm.polygons[pi];

                            int triCount = 0;
                            List<int> tris = new List<int>();

                            //in MSH, polygons are defined as triangle strips.
                            //since unity expects just triangles, we have to strip them ourselfs
                            for (int vi = 0; vi < poly.VertexIndices.Count; vi++) {
                                if (triCount == 3) {
                                    vi -= 2;
                                    triCount = 0;
                                }

                                tris.Add(poly.VertexIndices[vi]);
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

                            triangles.AddRange(tris);
                        }

                        GameObject SegmObj = new GameObject("SEGM" + si);
                        SegmObj.transform.SetParent(modelObj.transform);

                        Mesh mesh = new Mesh();
                        mesh.name = mdl.Name + "_segm" + si;
                        mesh.vertices = vertices.ToArray();
                        mesh.normals = normals.ToArray();
                        mesh.uv = uvs.ToArray();

                        mesh.triangles = triangles.ToArray();


                        //we're just interested in common and collision geometry
                        //discard the rest
                        if (mdl.Tag == ModelTag.Collision) {
                            MeshCollider collider = SegmObj.AddComponent<MeshCollider>();
                            collider.sharedMesh = mesh;
                        }
                        else if (mdl.Tag == ModelTag.Common) {
                            MeshFilter filter = SegmObj.AddComponent<MeshFilter>();
                            filter.mesh = mesh;

                            MeshRenderer renderer = SegmObj.AddComponent<MeshRenderer>();

                    }
                }
            }

            if (CREATE_ASSETS)
                PrefabUtility.CreatePrefab("Assets/Meshes/" + fileName + "/" + fileName + ".prefab", rootObj, ReplacePrefabOptions.ConnectToPrefab);

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

    public static Material Material2Unity(MATD from) {
        Material material = new Material(Shader.Find(SHADER));
        material.EnableKeyword("_METALLICGLOSSMAP");
        material.EnableKeyword("_SPECGLOSSMAP");
        material.name = from.Name;
        material.SetColor("_Color", Color2Unity(from.Diffuse));
        material.SetFloat("_Glossiness", 1.0f);
        material.SetFloat("_Metallic", 0.0f);
        

        if (!string.IsNullOrEmpty(from.Texture)) {

            string texPath = "Textures/" + from.Texture.Replace(".tga", "");
            string normalPath = "Textures/" + (from.Texture + "_normal").Replace(".tga", "");
            Texture2D texture = Resources.Load(texPath) as Texture2D;
            Texture2D normal = Resources.Load(normalPath) as Texture2D;

            if (texture != null) {
                material.SetTexture("_MainTex", texture);
            } else {
                Debug.LogWarning("Could not find " + texPath);
            }

            if (normal != null) {
                material.EnableKeyword("_NORMALMAP");
                material.SetTexture("_BumpMap", normal);
                //material.SetFloat("_BumpScale", NormalScale);
            } 
            else {
                Debug.LogWarning("Could not find " + normalPath);
            }
        }

        return material;
    }

    public static Material ChangeMaterial(MATD from, Material baseMat) {
        baseMat.name = from.Name;

        if (!string.IsNullOrEmpty(from.Texture)) {

            string texPath = "Textures/" + from.Texture.Replace(".tga", "");
            string normalPath = "Textures/" + from.Texture.Replace(".tga", "") + NORMAL_MAP_SUFFIX;
            Texture2D texture = Resources.Load(texPath) as Texture2D;
            Texture2D normal = Resources.Load(normalPath) as Texture2D;

            if (texture != null) {
                baseMat.SetTexture("_MainTex", texture);
            } 
            else {
                Debug.LogWarning("Could not find " + texPath);
            }

            if (normal != null) {
                baseMat.SetTexture("_BumpMap", normal);
            } else {
                Debug.LogWarning("Could not find " + normalPath);
            }
        }

        return baseMat;
    }
}
