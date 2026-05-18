using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LightChasePrototype.EditorTools
{
    // Builds a believable water surface that matches the WaterVolume gameplay trigger.
    // Replaces the previous flat blue cube with a subdivided plane shaded by
    // Shader "LightChase/Water" (Gerstner waves, depth-fade transparency, foam).
    public static class LightChaseWaterSurface
    {
        private const string WaterShaderName = "LightChase/Water";
        private const string SharedMaterialPath = "Assets/Project/LightChasePrototype/Art/Water/LightChaseWater.mat";
        private const string MeshFolder = "Assets/Project/LightChasePrototype/Art/Water/Meshes";
        private const int SubdivisionsPerMeter = 1;
        private const int MinSubdivisions = 12;
        private const int MaxSubdivisions = 96;

        public static GameObject CreateSurface(Transform parent, string objectName, Vector3 localCenter, Vector3 size)
        {
            var surface = new GameObject(objectName);
            surface.transform.SetParent(parent, false);
            surface.transform.localPosition = localCenter;
            surface.isStatic = false;
            surface.layer = parent != null ? parent.gameObject.layer : surface.layer;

            var meshFilter = surface.AddComponent<MeshFilter>();
            var meshRenderer = surface.AddComponent<MeshRenderer>();

            var subdivisionsX = ResolveSubdivisions(size.x);
            var subdivisionsZ = ResolveSubdivisions(size.z);
            meshFilter.sharedMesh = GetOrCreateGridMesh(size.x, size.z, subdivisionsX, subdivisionsZ);

            meshRenderer.sharedMaterial = GetOrCreateSharedMaterial();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            DisableNavMeshContribution(surface);
            return surface;
        }

        private static int ResolveSubdivisions(float worldSize)
        {
            var requested = Mathf.RoundToInt(Mathf.Abs(worldSize) * SubdivisionsPerMeter);
            return Mathf.Clamp(requested, MinSubdivisions, MaxSubdivisions);
        }

        private static Mesh GetOrCreateGridMesh(float width, float depth, int subdivisionsX, int subdivisionsZ)
        {
            EnsureMeshFolder();

            var assetName = $"WaterPlane_{width:F1}x{depth:F1}_{subdivisionsX}x{subdivisionsZ}.asset";
            var assetPath = $"{MeshFolder}/{assetName}";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            var mesh = BuildGridMesh(width, depth, subdivisionsX, subdivisionsZ);
            mesh.name = $"WaterPlane_{width:F1}x{depth:F1}";
            AssetDatabase.CreateAsset(mesh, assetPath);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        private static Mesh BuildGridMesh(float width, float depth, int subdivisionsX, int subdivisionsZ)
        {
            var verticesPerRow = subdivisionsX + 1;
            var verticesPerCol = subdivisionsZ + 1;
            var totalVertices = verticesPerRow * verticesPerCol;

            var vertices = new List<Vector3>(totalVertices);
            var normals = new List<Vector3>(totalVertices);
            var uvs = new List<Vector2>(totalVertices);
            var triangles = new List<int>(subdivisionsX * subdivisionsZ * 6);

            var halfWidth = width * 0.5f;
            var halfDepth = depth * 0.5f;

            for (var z = 0; z < verticesPerCol; z++)
            {
                for (var x = 0; x < verticesPerRow; x++)
                {
                    var tx = subdivisionsX == 0 ? 0f : x / (float)subdivisionsX;
                    var tz = subdivisionsZ == 0 ? 0f : z / (float)subdivisionsZ;
                    vertices.Add(new Vector3(-halfWidth + tx * width, 0f, -halfDepth + tz * depth));
                    normals.Add(Vector3.up);
                    uvs.Add(new Vector2(tx, tz));
                }
            }

            for (var z = 0; z < subdivisionsZ; z++)
            {
                for (var x = 0; x < subdivisionsX; x++)
                {
                    var i0 = z * verticesPerRow + x;
                    var i1 = i0 + 1;
                    var i2 = i0 + verticesPerRow;
                    var i3 = i2 + 1;

                    triangles.Add(i0);
                    triangles.Add(i2);
                    triangles.Add(i1);
                    triangles.Add(i1);
                    triangles.Add(i2);
                    triangles.Add(i3);
                }
            }

            var mesh = new Mesh { indexFormat = totalVertices > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static Material GetOrCreateSharedMaterial()
        {
            EnsureMaterialFolder();
            var material = AssetDatabase.LoadAssetAtPath<Material>(SharedMaterialPath);
            if (material != null)
            {
                EnsureShaderAssigned(material);
                return material;
            }

            var shader = Shader.Find(WaterShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[LightChaseWaterSurface] Shader '{WaterShaderName}' no encontrado. Se usara URP/Lit como fallback.");
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            material = new Material(shader) { name = "LightChaseWater" };
            ApplyNightTuning(material);
            AssetDatabase.CreateAsset(material, SharedMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void EnsureShaderAssigned(Material material)
        {
            var shader = Shader.Find(WaterShaderName);
            if (shader == null || material.shader == shader)
            {
                return;
            }

            material.shader = shader;
            ApplyNightTuning(material);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }

        private static void ApplyNightTuning(Material material)
        {
            if (material.HasProperty("_ShallowColor"))
            {
                material.SetColor("_ShallowColor", new Color(0.22f, 0.55f, 0.65f, 0.55f));
            }

            if (material.HasProperty("_DeepColor"))
            {
                material.SetColor("_DeepColor", new Color(0.02f, 0.06f, 0.14f, 0.95f));
            }

            if (material.HasProperty("_DepthFadeDistance"))
            {
                material.SetFloat("_DepthFadeDistance", 2.4f);
            }

            if (material.HasProperty("_FoamColor"))
            {
                material.SetColor("_FoamColor", new Color(0.85f, 0.92f, 1.0f, 1.0f));
            }

            if (material.HasProperty("_FoamDistance"))
            {
                material.SetFloat("_FoamDistance", 0.45f);
            }

            if (material.HasProperty("_SpecularColor"))
            {
                material.SetColor("_SpecularColor", new Color(1f, 0.92f, 0.72f, 1f));
            }
        }

        private static void EnsureMaterialFolder()
        {
            EnsureFolder("Assets/Project/LightChasePrototype/Art", "Water");
        }

        private static void EnsureMeshFolder()
        {
            EnsureMaterialFolder();
            EnsureFolder("Assets/Project/LightChasePrototype/Art/Water", "Meshes");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var fullPath = $"{parent}/{child}";
            if (AssetDatabase.IsValidFolder(fullPath))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                var split = parent.LastIndexOf('/');
                EnsureFolder(parent.Substring(0, split), parent.Substring(split + 1));
            }

            AssetDatabase.CreateFolder(parent, child);
        }

        private static void DisableNavMeshContribution(GameObject surface)
        {
            // The water surface must never contribute to the NavMesh.
            // We avoid taking a hard dependency on the AI Navigation package here:
            // simply leaving the GameObject as non-static + no collider is enough
            // because LightChase builders bake NavMesh from colliders/renderers of
            // tagged geometry only. Renderer flag still set defensively.
            var staticFlags = GameObjectUtility.GetStaticEditorFlags(surface);
            staticFlags &= ~StaticEditorFlags.NavigationStatic;
            GameObjectUtility.SetStaticEditorFlags(surface, staticFlags);
        }
    }
}
