using System;
using System.Collections.Generic;
using System.IO;
using PickupCent.Digging;
using PickupCent.Economy;
using PickupCent.Events;
using PickupCent.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PickupCent.EditorTools
{
    /// <summary>
    /// 순수 에디터 유틸리티. Assets/_PickupCent/Art/Stage{N} 아래의 아트 파일을
    /// 기존 컴포넌트와 ScriptableObject의 아트 슬롯에 자동으로 연결한다.
    ///
    /// 파일명 규칙:
    ///   item_<ItemDefinition 에셋 파일명>       예: item_BronzeCoin.png
    ///   tool_<ToolType 이름>                    예: tool_Hand.png, tool_Shovel.png
    ///   effect_Sparkle1 / effect_Sparkle2       예: effect_Sparkle1.png
    ///   structure_<프리팹 파일명>               예: structure_Slide.png
    ///   event_ChildrenSwarm                     예: event_ChildrenSwarm.png
    ///   terrain_..._<dry|wet|dug>               한글 마른/젖은/파낸도 인식
    /// </summary>
    public static class ArtAssetLinker
    {
        private const string ArtRootFolder = "Assets/_PickupCent/Art";
        private const string ItemDataFolder = "Assets/_PickupCent/Data/Items";
        private const string PrefabFolder = "Assets/_PickupCent/Prefabs";

        /// <summary>item/tool/sprite 폴더의 모든 스프라이트에 강제 적용하는 PPU.</summary>
        public const float TargetSpritePixelsPerUnit = 100f;

        private static readonly string[] TextureExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd" };

        [MenuItem("PickupCent/에셋 연결 도구 (Stage1)")]
        public static void LinkStage1Assets()
        {
            LinkAssetsForStage(1);
        }

        private static void LinkAssetsForStage(int stageNumber)
        {
            AssetDatabase.Refresh();

            string stageFolder = $"{ArtRootFolder}/Stage{stageNumber}";
            string itemFolder = $"{stageFolder}/item";
            string toolFolder = $"{stageFolder}/tool";
            string effectFolder = $"{stageFolder}/Effect";
            string spriteFolder = $"{stageFolder}/sprite";
            string textureFolder = $"{stageFolder}/texture";

            EnsureFolderPath(itemFolder);
            EnsureFolderPath(toolFolder);
            EnsureFolderPath(effectFolder);
            EnsureFolderPath(spriteFolder);
            EnsureFolderPath(textureFolder);

            int linked = 0;
            int skipped = 0;

            ScanFolder(itemFolder, "item_", LinkItem, ref linked, ref skipped);
            ScanFolder(toolFolder, "tool_", LinkTool, ref linked, ref skipped);
            ScanFolder(effectFolder, "effect_", LinkEffect, ref linked, ref skipped);
            ScanFolder(spriteFolder, "structure_", LinkStructure, ref linked, ref skipped);
            ScanFolder(spriteFolder, "event_", LinkEvent, ref linked, ref skipped);
            ScanFolder(textureFolder, "terrain_", LinkTerrain, ref linked, ref skipped);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"[ArtAssetLinker] Stage{stageNumber} 스캔 완료 - 연결 {linked}개, 건너뜀 {skipped}개 " +
                      $"(스캔 폴더: {stageFolder}/{{item,tool,Effect,sprite,texture}})");
        }

        private static void ScanFolder(string folder, string prefix, Func<string, string, bool> handler,
            ref int linked, ref int skipped)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string ext = Path.GetExtension(path);
                if (Array.IndexOf(TextureExtensions, ext.ToLowerInvariant()) < 0) continue;

                string fileName = Path.GetFileNameWithoutExtension(path);
                if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

                string suffix = fileName.Substring(prefix.Length);
                bool ok = handler(suffix, path);
                if (ok) linked++;
                else skipped++;
            }
        }

        private static bool LinkItem(string suffix, string path)
        {
            var defs = FindAllAssets<ItemDefinition>(ItemDataFolder);
            ItemDefinition match = null;
            foreach (var def in defs)
            {
                if (string.Equals(def.name, suffix, StringComparison.OrdinalIgnoreCase))
                {
                    match = def;
                    break;
                }
            }

            if (match == null)
            {
                match = ScriptableObject.CreateInstance<ItemDefinition>();
                match.name = suffix;
                match.itemName = suffix;
                string assetPath = $"{ItemDataFolder}/{suffix}.asset";
                AssetDatabase.CreateAsset(match, assetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[ArtAssetLinker] item_{suffix} -> ItemDefinition 생성 ({assetPath})");
            }

            var sprite = SetupSpriteImport(path);
            if (sprite == null) return false;

            match.artSprite = sprite;
            EditorUtility.SetDirty(match);
            Debug.Log($"[ArtAssetLinker] item_{suffix} -> ItemDefinition '{match.name}'.artSprite 연결 ({path})");
            return true;
        }

        private static bool LinkTool(string suffix, string path)
        {
            var sprite = SetupSpriteImport(path);
            if (sprite == null) return false;

            bool found = false;
            if (Enum.TryParse(suffix, true, out ToolManager.ToolType tool))
            {
                var toolManager = UnityEngine.Object.FindFirstObjectByType<ToolManager>();
                if (toolManager != null)
                {
                    var toolManagerSo = new SerializedObject(toolManager);
                    string fieldName = tool switch
                    {
                        ToolManager.ToolType.Hand => "handIcon",
                        ToolManager.ToolType.Shovel => "shovelIcon",
                        ToolManager.ToolType.Rake => "rakeIcon",
                        ToolManager.ToolType.Detector => "detectorIcon",
                        _ => null
                    };

                    if (!string.IsNullOrEmpty(fieldName))
                    {
                        toolManagerSo.FindProperty(fieldName).objectReferenceValue = sprite;
                        toolManagerSo.ApplyModifiedPropertiesWithoutUndo();
                        found = true;
                    }
                }
            }

            var toolBarGO = GameObject.Find("ToolBar");
            var toolBar = toolBarGO != null ? toolBarGO.GetComponent<ToolBarController>() : null;
            if (toolBar == null)
            {
                if (!found)
                    Debug.LogWarning($"[ArtAssetLinker] tool_{suffix} -> ToolBar/ToolManager를 찾지 못했습니다 ({path})");
                return found;
            }

            var so = new SerializedObject(toolBar);
            var entriesProp = so.FindProperty("entries");
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                var entryProp = entriesProp.GetArrayElementAtIndex(i);
                var toolProp = entryProp.FindPropertyRelative("tool");
                string toolName = ((ToolManager.ToolType)toolProp.enumValueIndex).ToString();
                if (!string.Equals(toolName, suffix, StringComparison.OrdinalIgnoreCase)) continue;

                entryProp.FindPropertyRelative("icon").objectReferenceValue = sprite;
                found = true;
                break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!found)
            {
                Debug.LogWarning($"[ArtAssetLinker] tool_{suffix} -> 일치하는 도구 버튼을 찾지 못했습니다 " +
                                  $"(hand / shovel / detector 중 하나여야 함) ({path})");
                return false;
            }

            Debug.Log($"[ArtAssetLinker] tool_{suffix} -> 도구 아이콘 연결 ({path})");
            return true;
        }

        private static bool LinkEffect(string suffix, string path)
        {
            string fieldName = suffix.ToLowerInvariant() switch
            {
                "sparkle1" => "sparkleSprite1",
                "sparkle2" => "sparkleSprite2",
                _ => null
            };

            if (string.IsNullOrEmpty(fieldName))
            {
                Debug.LogWarning($"[ArtAssetLinker] effect_{suffix} -> 알 수 없는 이펙트 이름입니다 ({path})");
                return false;
            }

            var itemSpawner = UnityEngine.Object.FindFirstObjectByType<ItemSpawner>();
            if (itemSpawner == null)
            {
                Debug.LogWarning($"[ArtAssetLinker] effect_{suffix} -> ItemSpawner를 찾지 못했습니다 ({path})");
                return false;
            }

            var sprite = SetupSpriteImport(path);
            if (sprite == null) return false;

            var so = new SerializedObject(itemSpawner);
            so.FindProperty(fieldName).objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[ArtAssetLinker] effect_{suffix} -> ItemSpawner.{fieldName} 연결 ({path})");
            return true;
        }

        private static bool LinkStructure(string suffix, string path)
        {
            string prefabPath = FindAssetPathByName(PrefabFolder, "t:Prefab", suffix);
            if (prefabPath == null)
            {
                Debug.LogWarning($"[ArtAssetLinker] structure_{suffix} -> 일치하는 지형지물 프리팹을 찾지 못했습니다 " +
                                  $"({PrefabFolder} 안에 '{suffix}.prefab'이 있어야 함) ({path})");
                return false;
            }

            var sprite = SetupSpriteImport(path);
            if (sprite == null) return false;

            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var sr = prefabRoot.GetComponentInChildren<SpriteRenderer>(true);
                if (sr == null)
                {
                    Debug.LogWarning($"[ArtAssetLinker] structure_{suffix} -> 프리팹에 SpriteRenderer가 없습니다 ({prefabPath})");
                    return false;
                }

                sr.sprite = sprite;
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            Debug.Log($"[ArtAssetLinker] structure_{suffix} -> 프리팹 '{prefabPath}' SpriteRenderer 연결 ({path})");
            return true;
        }

        private static bool LinkEvent(string suffix, string path)
        {
            const string expectedName = "ChildrenSwarm";
            if (!string.Equals(suffix, expectedName, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[ArtAssetLinker] event_{suffix} -> 알 수 없는 이벤트 이름입니다 " +
                                  $"('{expectedName}'만 지원) ({path})");
                return false;
            }

            var swarmGO = GameObject.Find("ChildrenSwarmEvent");
            var swarmEvent = swarmGO != null ? swarmGO.GetComponent<ChildrenSwarmEvent>() : null;
            if (swarmEvent == null)
            {
                Debug.LogWarning($"[ArtAssetLinker] event_{suffix} -> 씬에서 ChildrenSwarmEvent를 찾지 못했습니다 " +
                                  $"(Test6 미실행?) ({path})");
                return false;
            }

            var sprite = SetupSpriteImport(path);
            if (sprite == null) return false;

            var so = new SerializedObject(swarmEvent);
            so.FindProperty("artSprite").objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[ArtAssetLinker] event_{suffix} -> ChildrenSwarmEvent.artSprite 연결 ({path})");
            return true;
        }

        private static bool LinkTerrain(string suffix, string path)
        {
            string normalized = suffix.ToLowerInvariant();
            string state = normalized;
            int lastUnderscore = normalized.LastIndexOf('_');
            if (lastUnderscore >= 0) state = normalized.Substring(lastUnderscore + 1);

            string fieldName;
            if (state.Contains("마른") || state.Contains("dry")) fieldName = "sandTexture";
            else if (state.Contains("젖은") || state.Contains("wet")) fieldName = "wetTexture";
            else if (state.Contains("파낸") || state.Contains("dug")) fieldName = "dugFloorTexture";
            else
            {
                Debug.LogWarning($"[ArtAssetLinker] terrain_{suffix} -> 상태를 인식하지 못했습니다 " +
                                  "(마른/dry, 젖은/wet, 파낸/dug 중 하나가 파일명 끝부분에 포함돼야 함) " +
                                  $"({path})");
                return false;
            }

            var sandGO = GameObject.Find("SandLayer");
            var mask = sandGO != null ? sandGO.GetComponent<SandMaskController>() : null;
            if (mask == null)
            {
                Debug.LogWarning($"[ArtAssetLinker] terrain_{suffix} -> 씬에서 SandMaskController를 찾지 못했습니다 " +
                                  $"(Test1 미실행?) ({path})");
                return false;
            }

            var texture = SetupTextureImport(path);
            if (texture == null) return false;

            var so = new SerializedObject(mask);
            so.FindProperty(fieldName).objectReferenceValue = texture;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[ArtAssetLinker] terrain_{suffix} -> SandMaskController.{fieldName} 연결 " +
                      "(지역별 구분은 아직 없어 이름의 지역 부분은 무시됨, 셰이더에서는 아직 미사용) " +
                      $"({path})");
            return true;
        }

        private static Sprite SetupSpriteImport(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                Debug.LogWarning($"[ArtAssetLinker] {path} -> TextureImporter를 가져오지 못했습니다");
                return null;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, TargetSpritePixelsPerUnit))
            {
                importer.spritePixelsPerUnit = TargetSpritePixelsPerUnit;
                changed = true;
            }
            changed |= ApplyCommonImportSettings(importer);

            if (changed) importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"[ArtAssetLinker] {path} -> Sprite로 로드하지 못했습니다");
            return sprite;
        }

        private static Texture2D SetupTextureImport(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                Debug.LogWarning($"[ArtAssetLinker] {path} -> TextureImporter를 가져오지 못했습니다");
                return null;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Default;
                changed = true;
            }
            changed |= ApplyCommonImportSettings(importer);

            if (changed) importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                Debug.LogWarning($"[ArtAssetLinker] {path} -> Texture2D로 로드하지 못했습니다");
            return texture;
        }

        private static bool ApplyCommonImportSettings(TextureImporter importer)
        {
            bool changed = false;
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            return changed;
        }

        private static List<T> FindAllAssets<T>(string folder) where T : UnityEngine.Object
        {
            var results = new List<T>();
            if (!AssetDatabase.IsValidFolder(folder)) return results;

            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            foreach (var guid in guids)
            {
                var obj = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (obj != null) results.Add(obj);
            }
            return results;
        }

        private static string FindAssetPathByName(string folder, string typeFilter, string name)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return null;

            var guids = AssetDatabase.FindAssets(typeFilter, new[] { folder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (string.Equals(fileName, name, StringComparison.OrdinalIgnoreCase)) return path;
            }
            return null;
        }

        private static void EnsureFolderPath(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
