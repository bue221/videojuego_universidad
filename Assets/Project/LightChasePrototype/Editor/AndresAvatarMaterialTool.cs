using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LightChasePrototype.EditorTools
{
    public static class AndresAvatarMaterialTool
    {
        private const string AvatarFolder = "Assets/Project/LightChasePrototype/Resources/PlayerAvatars/Avatar andres";
        private const string ExtractedTexturesFolder = AvatarFolder + "/ExtractedTextures";
        private const string GeneratedFolder = AvatarFolder + "/Generated";
        private const string GeneratedMaterialsFolder = GeneratedFolder + "/Materials";
        private const string GeneratedTexturesFolder = GeneratedFolder + "/Textures";
        private const string FbxPath = AvatarFolder + "/avatar.fbx";

        // Material name -> (baseColor, normal, metallicRough)
        // Indices come from parsing model.glb (kept stable in repo).
        private static readonly Dictionary<string, (int baseColor, int normal, int metalRough)> MaterialToTex =
            new()
            {
                { "avaturn_body_material", (1, 2, 0) },
                { "avaturn_glasses_0_material", (4, -1, 3) },
                { "avaturn_hair_0_material", (5, 6, -1) },
                { "avaturn_hair_1_material", (5, 6, -1) },
                { "avaturn_shoes_0_material", (8, 9, 7) },
                { "avaturn_look_0_material", (12, 13, 11) },
            };

        [MenuItem("Tools/Prototype/Avatar/Apply Andres GLB Textures To FBX")]
        public static void Apply()
        {
            EnsureFolder(GeneratedFolder);
            EnsureFolder(GeneratedMaterialsFolder);
            EnsureFolder(GeneratedTexturesFolder);

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbx == null)
            {
                Debug.LogError($"No pude cargar FBX en '{FbxPath}'. ¿Existe y está dentro de Assets?");
                return;
            }

            // Convert glTF metallicRoughness to Unity metallicGloss (metallic in R, smoothness in A).
            var metallicGlossByIndex = new Dictionary<int, Texture2D>();
            foreach (var entry in MaterialToTex.Values)
            {
                if (entry.metalRough < 0 || metallicGlossByIndex.ContainsKey(entry.metalRough))
                {
                    continue;
                }

                var source = LoadExtractedTexture(entry.metalRough);
                if (source == null)
                {
                    continue;
                }

                var converted = ConvertMetalRoughToMetallicGloss(source, entry.metalRough);
                metallicGlossByIndex[entry.metalRough] = converted;
            }

            // Create / update materials.
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("No encontré shader URP/Lit. ¿El proyecto está en URP y el paquete está instalado?");
                return;
            }

            foreach (var kvp in MaterialToTex)
            {
                var matName = kvp.Key;
                var texIds = kvp.Value;

                var matPath = $"{GeneratedMaterialsFolder}/{matName}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(shader) { name = matName };
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                else
                {
                    mat.shader = shader;
                }

                var baseColor = LoadExtractedTexture(texIds.baseColor);
                if (baseColor != null)
                {
                    mat.SetTexture("_BaseMap", baseColor);
                    mat.SetColor("_BaseColor", Color.white);
                }

                var normal = texIds.normal >= 0 ? LoadExtractedTexture(texIds.normal) : null;
                if (normal != null)
                {
                    EnsureTextureIsNormalMap(normal);
                    mat.EnableKeyword("_NORMALMAP");
                    mat.SetTexture("_BumpMap", normal);
                }
                else
                {
                    mat.DisableKeyword("_NORMALMAP");
                    mat.SetTexture("_BumpMap", null);
                }

                if (texIds.metalRough >= 0 && metallicGlossByIndex.TryGetValue(texIds.metalRough, out var metallicGloss) && metallicGloss != null)
                {
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                    mat.SetTexture("_MetallicGlossMap", metallicGloss);
                    mat.SetFloat("_Metallic", 1f);
                    mat.SetFloat("_Smoothness", 1f);
                }
                else
                {
                    mat.DisableKeyword("_METALLICSPECGLOSSMAP");
                    mat.SetTexture("_MetallicGlossMap", null);
                }

                EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Remap FBX materials by name to our generated ones.
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("No pude obtener ModelImporter del FBX.");
                return;
            }

            importer.materialImportMode = ModelImporterMaterialImportMode.None; // We'll remap manually.

            foreach (var kvp in MaterialToTex.Keys)
            {
                var matPath = $"{GeneratedMaterialsFolder}/{kvp}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    continue;
                }

                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kvp), mat);
            }

            importer.SaveAndReimport();

            Debug.Log("AndresAvatarMaterialTool: materiales generados y remapeo aplicado al FBX. Revisa el prefab/escena para confirmar.");
        }

        private static Texture2D LoadExtractedTexture(int index)
        {
            if (index < 0)
            {
                return null;
            }

            var jpgPath = $"{ExtractedTexturesFolder}/tex_{index:00}.jpg";
            var pngPath = $"{ExtractedTexturesFolder}/tex_{index:00}.png";
            var path = File.Exists(jpgPath) ? jpgPath : File.Exists(pngPath) ? pngPath : null;
            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.LogWarning($"No encontré textura extraída para índice {index:00}.");
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var name = Path.GetFileName(assetPath);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static void EnsureTextureIsNormalMap(Texture2D texture)
        {
            var path = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            if (importer.textureType == TextureImporterType.NormalMap)
            {
                return;
            }

            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }

        private static Texture2D ConvertMetalRoughToMetallicGloss(Texture2D source, int sourceIndex)
        {
            // glTF metallicRoughness is typically:
            // - Roughness in G
            // - Metallic in B
            // Unity metallic gloss map expects:
            // - Metallic in R
            // - Smoothness in A (we use 1 - roughness)
            var srcPath = AssetDatabase.GetAssetPath(source);
            var srcImporter = AssetImporter.GetAtPath(srcPath) as TextureImporter;
            if (srcImporter != null && !srcImporter.isReadable)
            {
                srcImporter.isReadable = true;
                srcImporter.SaveAndReimport();
            }

            var pixels = source.GetPixels32();
            for (var i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                var rough = p.g / 255f;
                var metal = p.b / 255f;
                var smooth = Mathf.Clamp01(1f - rough);
                pixels[i] = new Color32((byte)Mathf.RoundToInt(metal * 255f), 0, 0, (byte)Mathf.RoundToInt(smooth * 255f));
            }

            var tex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            tex.name = $"metallicGloss_{sourceIndex:00}";

            var outPath = $"{GeneratedTexturesFolder}/{tex.name}.png";
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

            var outTex = AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
            if (outTex != null)
            {
                var importer = AssetImporter.GetAtPath(outPath) as TextureImporter;
                if (importer != null)
                {
                    importer.sRGBTexture = false;
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                }
            }

            // Restore readability to avoid leaving the project in an unexpected state.
            if (srcImporter != null)
            {
                srcImporter.isReadable = false;
                srcImporter.SaveAndReimport();
            }

            return outTex;
        }
    }
}

