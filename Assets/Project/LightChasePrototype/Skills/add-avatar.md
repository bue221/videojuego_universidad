# /add-avatar — Agregar personaje jugador desde FBX

Agrega un nuevo personaje jugador al juego LightChasePrototype a partir de un FBX exportado desde Avaturn u otra herramienta. Todo el sistema de gameplay (pasos, detección, respawn, transición de niveles) funciona automáticamente gracias a la arquitectura de `PlayerAvatarEntry` + `PlayerAvatarBootstrap`.

## Uso

```
/add-avatar <NombrePersonaje> "<ruta_al_fbx>"
```

**Ejemplos:**
```
/add-avatar Carlos "Assets/Project/LightChasePrototype/Resources/PlayerAvatars/Avatar carlos/avatar.fbx"
/add-avatar Sofia "Assets/Project/LightChasePrototype/Resources/PlayerAvatars/Avatar sofia/avatar.fbx"
```

## Requisitos previos del FBX

1. El FBX debe estar en `Assets/` (dentro del proyecto Unity)
2. La **carpeta padre** del FBX debe contener una subcarpeta `ExtractedTextures/` con texturas `.jpg`/`.png` extraídas del GLB original
3. El FBX debe tener rig **Humanoid** configurado en Unity
4. Estructura esperada:
   ```
   Assets/.../Avatar <nombre>/
   ├── avatar.fbx
   ├── ExtractedTextures/
   │   ├── tex_00.jpg   (metallic-roughness)
   │   ├── tex_01.jpg   (body base color)
   │   ├── tex_02.jpg   (body normal)
   │   └── ...
   └── model.glb        (opcional, referencia)
   ```

## Instrucciones para Claude

### Paso 1 — Parsear argumentos
- `NOMBRE` = primer arg con primera letra mayúscula (ej: "Carlos")
- `FBX_PATH` = segundo arg (ruta dentro de Assets)
- `AVATAR_ID` = NOMBRE en minúsculas sin espacios ni tildes (ej: "carlos")
- `PREFAB_NAME` = "Player" + NOMBRE (ej: "PlayerCarlos")
- `PREFAB_PATH` = `"Assets/Project/LightChasePrototype/Resources/PlayerAvatars/"` + PREFAB_NAME + `".prefab"`
- `ENTRY_PATH` = `"Assets/Project/LightChasePrototype/Resources/PlayerAvatars/"` + PREFAB_NAME + `"_Entry.asset"`

### Paso 2 — Abrir la herramienta editor y ejecutar el pipeline

Usa `mcp__UnityMCP__execute_menu_item` o `mcp__UnityMCP__execute_code` para abrir:
```
Tools/LightChase/Add Avatar from FBX
```

O ejecuta el pipeline completo directamente via `execute_code` en el editor:

```csharp
// Pipeline completo sin UI
LightChasePrototype.Editor.AddAvatarFromFbxTool.BuildFromCode(
    fbxPath: "<FBX_PATH>",
    avatarName: "<NOMBRE>",
    avatarId: "<AVATAR_ID>"
);
```

Si `BuildFromCode` no existe, usar el MenuItem:
```csharp
UnityEditor.EditorApplication.ExecuteMenuItem("Tools/LightChase/Add Avatar from FBX");
```

### Paso 3 — Verificar resultado con execute_code

```csharp
var sb = new System.Text.StringBuilder();

// Verificar prefab creado
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("<PREFAB_PATH>");
sb.AppendLine($"Prefab: {(prefab != null ? "OK" : "FALTA")}");

// Verificar entry en catalog
var catalog = LightChasePrototype.PlayerAvatarCatalog.Load();
var entry = catalog?.FindById("<AVATAR_ID>");
sb.AppendLine($"Entry en catalog: {(entry != null ? "OK" : "FALTA")}");

// Verificar footstep clips
sb.AppendLine($"FootstepClips: {entry?.footstepClips?.Length ?? 0}");

// Verificar que aparece en las opciones del juego
var options = LightChasePrototype.PlayerAvatarSelection.GetAllOptions();
var found = System.Array.Exists(options, o => o.Id == "<AVATAR_ID>");
sb.AppendLine($"En menú del juego: {found}");

// Verificar bootstrap en prefab
if (prefab != null)
{
    var boot = prefab.GetComponent<LightChasePrototype.PlayerAvatarBootstrap>();
    sb.AppendLine($"Bootstrap: {(boot != null ? "OK" : "FALTA")}");
}

return sb.ToString();
```

### Paso 4 — Reporte al usuario

Reporta:
- ✅/❌ Prefab creado en Resources
- ✅/❌ Entry registrado en PlayerAvatarCatalog
- ✅/❌ Aparece en el menú de selección del juego
- `avatarId` para referencia (se usa en PlayerPrefs)
- Instrucción: para probarlo en el juego, iniciar Play y seleccionar `<NOMBRE>` en el menú de avatares

## Garantías automáticas del sistema

Al agregar un personaje correctamente, estos behaviors son automáticos:

| Feature | Mecanismo |
|---------|-----------|
| **Sonido de pasos** | `PlayerAvatarBootstrap.Awake()` cablea `FootstepAudioClips` al TPC; `ThirdPersonController.OnFootstep` usa `PlayClipAtPoint` como fallback |
| **El enemigo lo detecta** | `EnemyLightSeeker.Update()` re-adquiere `PlayerLightState` cuando la referencia se pierde (ej: swap de avatar) |
| **Respawn correcto** | `PrototypeLevelManager.ResetRuntimeRunState()` preserva la posición de spawn original antes de re-cachear referencias |
| **Transición entre niveles** | `PrototypeLevelManager.IsSelectedAvatar()` compara el player en escena vs `SelectedAvatarId`; si no coincide, llama a `EnsureSelectedAvatarInScene()` |
| **Input funciona** | `MainMenuController.HideMenu()` deselecciona EventSystem + fuerza scheme KeyboardMouse si no hay gamepad |
| **Texturas URP** | `AddAvatarFromFbxTool` convierte metallic-roughness glTF → Unity MetallicGloss y asigna materiales URP/Lit |
| **Escala correcta** | AvatarVisual se escala a `95f` (modelos Avaturn en cm → Unity en m) |

## Archivos involucrados en el sistema

```
Scripts/Gameplay/
├── PlayerAvatarEntry.cs         ← ScriptableObject por personaje
├── PlayerAvatarCatalog.cs       ← registro global, Resources/PlayerAvatarCatalog.asset
├── PlayerAvatarBootstrap.cs     ← MonoBehaviour en cada prefab, auto-cablea audio
├── PlayerAvatarSelection.cs     ← GetAllOptions() combina hardcoded + catalog
├── PlayerAvatarSetup.cs         ← EnsureSelectedAvatarInScene(), EnsureGameplayPresentation()
├── PrototypeLevelManager.cs     ← IsSelectedAvatar() + fix respawn position
├── EnemyLightSeeker.cs          ← re-adquiere player ref cuando es null

Editor/
└── AddAvatarFromFbxTool.cs      ← EditorWindow: Tools/LightChase/Add Avatar from FBX

Resources/
├── PlayerAvatarCatalog.asset
└── PlayerAvatars/
    ├── PlayerArmature.prefab + PlayerArmature_Entry.asset
    ├── PlayerAndres.prefab  + PlayerAndres_Entry.asset
    └── PlayerXxx.prefab     + PlayerXxx_Entry.asset  (nuevos)
```
