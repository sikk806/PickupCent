using System;
using System.Collections.Generic;
using System.IO;
using PickupCent.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PickupCent.EditorTools
{
    /// <summary>
    /// [순수 에디터 유틸리티 - Test1~Test7 체계와도, ArtAssetLinker와도 별개의 메뉴. Test 번호를 붙이지 않는다]
    ///
    /// Assets/_PickupCent/Audio 밑을 스캔해서 "audio_&lt;id&gt;.*" 파일을 찾아 SoundManager의 클립 목록에
    /// id 기준으로 연결한다. SoundManager가 없으면 씬에 새로 만든다. ArtAssetLinker와 달리 이벤트별로
    /// 다른 컴포넌트를 찾아다닐 필요가 없다 — 모든 사운드가 SoundManager 하나의 클립 목록(id→AudioClip)을
    /// 거쳐 재생되므로, 연결 로직은 "파일명에서 id를 뽑아 목록에 넣는다"는 단순한 작업으로 충분하다.
    /// 반복 실행해도 안전하다 — 매번 폴더 내용으로 클립 목록을 통째로 다시 채운다.
    /// </summary>
    public static class AudioAssetLinker
    {
        private const string AudioFolder = "Assets/_PickupCent/Audio";
        private const string Prefix = "audio_";
        private static readonly string[] AudioExtensions = { ".wav", ".mp3", ".ogg", ".aiff", ".aif" };

        [MenuItem("PickupCent/사운드 연결 도구")]
        public static void LinkSounds()
        {
            AssetDatabase.Refresh();
            EnsureFolderPath(AudioFolder);

            var soundManager = EnsureSoundManager();

            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder });
            var found = new List<(string id, AudioClip clip, string path)>();
            int skipped = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (Array.IndexOf(AudioExtensions, ext) < 0) continue;

                string fileName = Path.GetFileNameWithoutExtension(path);
                if (!fileName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"[AudioAssetLinker] {path} — 'audio_' 접두사가 없어 건너뜁니다.");
                    skipped++;
                    continue;
                }

                string id = fileName.Substring(Prefix.Length);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                {
                    Debug.LogWarning($"[AudioAssetLinker] {path} — AudioClip으로 로드하지 못했습니다.");
                    skipped++;
                    continue;
                }

                found.Add((id, clip, path));
            }

            var so = new SerializedObject(soundManager);
            var clipsProp = so.FindProperty("clips");
            clipsProp.arraySize = found.Count;
            for (int i = 0; i < found.Count; i++)
            {
                var element = clipsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = found[i].id;
                element.FindPropertyRelative("clip").objectReferenceValue = found[i].clip;
                Debug.Log($"[AudioAssetLinker] audio_{found[i].id} → SoundManager 클립 목록에 연결됨 ({found[i].path})");
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log($"[AudioAssetLinker] 완료 — 연결 {found.Count}개, 건너뜀 {skipped}개 " +
                      $"(스캔 폴더: {AudioFolder}). 목록에 없는 id는 SoundManager가 재생 시점에 조용히 무시합니다.");
        }

        private static SoundManager EnsureSoundManager()
        {
            var go = GameObject.Find("SoundManager");
            if (go == null) go = new GameObject("SoundManager");

            var soundManager = go.GetComponent<SoundManager>();
            if (soundManager == null) soundManager = go.AddComponent<SoundManager>();
            return soundManager;
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
