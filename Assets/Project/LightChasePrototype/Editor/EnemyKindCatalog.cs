using System;

public enum EnemyKind
{
    Director = 0,
    Deshilachador = 1,
    BromaFinal = 2
}

public readonly struct EnemyKindAssets
{
    public EnemyKindAssets(
        EnemyKind kind,
        string displayName,
        string modelPath,
        string albedoPath,
        string normalPath,
        string metallicPath,
        string roughnessPath,
        string materialPath,
        string controllerPath,
        string walkClipPath,
        string modelObjectName)
    {
        Kind = kind;
        DisplayName = displayName;
        ModelPath = modelPath;
        AlbedoPath = albedoPath;
        NormalPath = normalPath;
        MetallicPath = metallicPath;
        RoughnessPath = roughnessPath;
        MaterialPath = materialPath;
        ControllerPath = controllerPath;
        WalkClipPath = walkClipPath;
        ModelObjectName = modelObjectName;
    }

    public EnemyKind Kind { get; }
    public string DisplayName { get; }
    public string ModelPath { get; }
    public string AlbedoPath { get; }
    public string NormalPath { get; }
    public string MetallicPath { get; }
    public string RoughnessPath { get; }
    public string MaterialPath { get; }
    public string ControllerPath { get; }
    public string WalkClipPath { get; }
    public string ModelObjectName { get; }
}

public static class EnemyKindCatalog
{
    private const string AnimationFolder = "Assets/Project/LightChasePrototype/Animation";

    private static readonly EnemyKindAssets DirectorAssets = new(
        EnemyKind.Director,
        "El Director",
        "Assets/MeshyImports/Enemigo_01/Meshy_AI_El_Director_biped_Animation_Walking_withSkin.fbx",
        "Assets/MeshyImports/Enemigo_01/Meshy_AI_El_Director_biped_texture_0.png",
        "Assets/MeshyImports/Enemigo_01/Meshy_AI_El_Director_biped_texture_0_normal.png",
        "Assets/MeshyImports/Enemigo_01/Meshy_AI_El_Director_biped_texture_0_metallic.png",
        "Assets/MeshyImports/Enemigo_01/Meshy_AI_El_Director_biped_texture_0_roughness.png",
        AnimationFolder + "/Enemigo01_Material.mat",
        AnimationFolder + "/Enemigo01.controller",
        AnimationFolder + "/Enemigo01_Walk.anim",
        "Enemigo_01_Model");

    private static readonly EnemyKindAssets DeshilachadorAssets = new(
        EnemyKind.Deshilachador,
        "El Deshilachador",
        "Assets/MeshyImports/Enemigo_02/Meshy_AI_El_Deshilachador_biped_Animation_Walking_withSkin.fbx",
        "Assets/MeshyImports/Enemigo_02/Meshy_AI_El_Deshilachador_biped_texture_0.png",
        "Assets/MeshyImports/Enemigo_02/Meshy_AI_El_Deshilachador_biped_texture_0_normal.png",
        "Assets/MeshyImports/Enemigo_02/Meshy_AI_El_Deshilachador_biped_texture_0_metallic.png",
        "Assets/MeshyImports/Enemigo_02/Meshy_AI_El_Deshilachador_biped_texture_0_roughness.png",
        AnimationFolder + "/Enemigo02_Material.mat",
        AnimationFolder + "/Enemigo02.controller",
        AnimationFolder + "/Enemigo02_Walk.anim",
        "Enemigo_02_Model");

    private static readonly EnemyKindAssets BromaFinalAssets = new(
        EnemyKind.BromaFinal,
        "La Broma Final",
        "Assets/MeshyImports/Enemigo_03/Meshy_AI_La_Broma_Final_biped_Animation_Walking_withSkin.fbx",
        "Assets/MeshyImports/Enemigo_03/Meshy_AI_La_Broma_Final_biped_texture_0.png",
        "Assets/MeshyImports/Enemigo_03/Meshy_AI_La_Broma_Final_biped_texture_0_normal.png",
        "Assets/MeshyImports/Enemigo_03/Meshy_AI_La_Broma_Final_biped_texture_0_metallic.png",
        "Assets/MeshyImports/Enemigo_03/Meshy_AI_La_Broma_Final_biped_texture_0_roughness.png",
        AnimationFolder + "/Enemigo03_Material.mat",
        AnimationFolder + "/Enemigo03.controller",
        AnimationFolder + "/Enemigo03_Walk.anim",
        "Enemigo_03_Model");

    public static EnemyKindAssets GetAssets(EnemyKind kind)
    {
        switch (kind)
        {
            case EnemyKind.Director: return DirectorAssets;
            case EnemyKind.Deshilachador: return DeshilachadorAssets;
            case EnemyKind.BromaFinal: return BromaFinalAssets;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown enemy kind");
        }
    }
}
