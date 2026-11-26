using System;
using EscapeTheDark;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigureHospitalAssets
{
    private const string Hospital = "Assets/Dnk_Dev/HospitalHorrorPack";
    private const string Footsteps = "Assets/Footsteps - Essentials/Footsteps_Tile";
    private const string Gore = "Assets/Arya of Terror - FREE Gore Gloom Hits";

    [MenuItem("Escape The Dark/Aplicar assets do hospital")]
    public static void Apply()
    {
        const string scenePath = "Assets/Scenes/Game.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var director = UnityEngine.Object.FindAnyObjectByType<GameDirector>();
        if (!director) throw new InvalidOperationException("GameDirector nao encontrado em Game.unity.");

        var serialized = new SerializedObject(director);
        Assign(serialized, "hospitalFloor", TiledMaterial(
            Hospital + "/Models/Materials/Mat_Tile01.mat", "Assets/Materials/HospitalFloor.mat", new Vector2(4, 4)));
        Assign(serialized, "hospitalWall", TiledMaterial(
            Hospital + "/Models/Materials/Mat_Walllime01_C.mat", "Assets/Materials/HospitalWall.mat", new Vector2(3, 1)));
        Assign(serialized, "floorModule", Required<GameObject>(Hospital + "/Prefab/P_Floor_01.prefab"));
        Assign(serialized, "damagedFloorModule", Required<GameObject>(Hospital + "/Prefab/P_Floor_02.prefab"));
        Assign(serialized, "ceilingModule", Required<GameObject>(Hospital + "/Prefab/P_Ceiling_01.prefab"));
        Assign(serialized, "wallModule", Required<GameObject>(Hospital + "/Prefab/P_Wall_01.prefab"));
        Assign(serialized, "damagedWallModule", Required<GameObject>(Hospital + "/Prefab/P_Wall_02.prefab"));
        Assign(serialized, "hospitalBed", Required<GameObject>(Hospital + "/Prefab/P_Bed_01.prefab"));
        Assign(serialized, "medicalBox", Required<GameObject>(Hospital + "/Prefab/P_Med_box_01.prefab"));
        Assign(serialized, "hospitalLamp", Required<GameObject>(Hospital + "/Prefab/P_Lamp.prefab"));
        Assign(serialized, "hospitalColumn", Required<GameObject>(Hospital + "/Prefab/P_Column_01.prefab"));
        Assign(serialized, "hospitalLampOff", Required<Material>(Hospital + "/Models/Materials/Mat_Lamp_Off.mat"));
        Assign(serialized, "hospitalDoor", Required<GameObject>(Hospital + "/Prefab/P_Door_01_.prefab"));
        Assign(serialized, "creaturePrefab", Required<GameObject>("Assets/Creep Horror Creature/Prefabs/Creep1.prefab"));
        Assign(serialized, "smallGoreHit", Required<AudioClip>(Gore + "/2021_GGH_GloomHit_Small_1.wav"));
        Assign(serialized, "mediumGoreHit", Required<AudioClip>(Gore + "/2021_GGH_GloomHit_Medium_1.wav"));
        Assign(serialized, "largeGoreHit", Required<AudioClip>(Gore + "/2021_GGH_GloomHit_Large_1.wav"));
        AssignSteps(serialized.FindProperty("walkSteps"), Footsteps + "/Footsteps_Tile_Walk/Footsteps_Tile_Walk_");
        AssignSteps(serialized.FindProperty("runSteps"), Footsteps + "/Footsteps_Tile_Run/Footsteps_Tile_Run_");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Escape The Dark: quatro pacotes vinculados a Game.unity.");
    }

    private static T Required<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (!asset) throw new InvalidOperationException("Asset ausente: " + path);
        return asset;
    }

    private static Material TiledMaterial(string sourcePath, string targetPath, Vector2 tiling)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
        if (material) return material;
        var source = Required<Material>(sourcePath);
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        material = new Material(source) { name = System.IO.Path.GetFileNameWithoutExtension(targetPath) };
        material.SetTextureScale("_BaseMap", tiling);
        AssetDatabase.CreateAsset(material, targetPath);
        return material;
    }

    private static void Assign(SerializedObject target, string property, UnityEngine.Object value)
    {
        target.FindProperty(property).objectReferenceValue = value;
    }

    private static void AssignSteps(SerializedProperty array, string prefix)
    {
        array.arraySize = 4;
        for (int i = 0; i < 4; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue =
                Required<AudioClip>(prefix + (i + 1).ToString("00") + ".wav");
    }
}
