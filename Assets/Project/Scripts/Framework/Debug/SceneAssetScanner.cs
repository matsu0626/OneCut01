#if _DEBUG
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneAssetScanner
{
    /// <summary>
    /// 現在のアクティブシーン内で、コンポーネントから辿れるアセットの一覧を作る。
    /// ・UI Image / RawImage
    /// ・SpriteRenderer
    /// ・AudioSource
    /// を対象。
    /// </summary>
    public static string BuildSceneAssetsDebugText()
    {
        var scene = SceneManager.GetActiveScene();
        var sceneName = scene.name;

        var sb = new StringBuilder();
        sb.AppendLine("=== Scene-Referenced Assets ===");
        sb.AppendLine($"[Scene: {sceneName}]");

        // 重複を避けるためのセット
        var sprites = new HashSet<Sprite>();
        var textures = new HashSet<Texture>();
        var audioClips = new HashSet<AudioClip>();

        // 1) UI Image
        {
            var images = Object.FindObjectsByType<Image>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (var img in images)
            {
                if (img == null) continue;
                if (img.gameObject.scene != scene) continue;

                if (img.sprite != null)
                {
                    sprites.Add(img.sprite);

                    if (img.sprite.texture != null)
                    {
                        textures.Add(img.sprite.texture);
                    }
                }
            }
        }

        // 2) RawImage
        {
            var raws = Object.FindObjectsByType<RawImage>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (var raw in raws)
            {
                if (raw == null) continue;
                if (raw.gameObject.scene != scene) continue;

                if (raw.texture != null)
                {
                    textures.Add(raw.texture);
                }
            }
        }

        // 3) SpriteRenderer
        {
            var srs = Object.FindObjectsByType<SpriteRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (var sr in srs)
            {
                if (sr == null) continue;
                if (sr.gameObject.scene != scene) continue;

                if (sr.sprite != null)
                {
                    sprites.Add(sr.sprite);

                    if (sr.sprite.texture != null)
                    {
                        textures.Add(sr.sprite.texture);
                    }
                }
            }
        }

        // 4) AudioSource
        {
            var audios = Object.FindObjectsByType<AudioSource>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (var audio in audios)
            {
                if (audio == null) continue;
                if (audio.gameObject.scene != scene) continue;

                if (audio.clip != null)
                {
                    audioClips.Add(audio.clip);
                }
            }
        }

        // --- 出力 ---
        if (sprites.Count == 0 && textures.Count == 0 && audioClips.Count == 0)
        {
            sb.AppendLine("  (no scene-referenced assets collected)");
            return sb.ToString();
        }

        if (sprites.Count > 0)
        {
            sb.AppendLine("  [Sprites]");
            foreach (var s in sprites.OrderBy(x => x.name))
            {
                sb.AppendLine($"    - {s.name}");
            }
        }

        if (textures.Count > 0)
        {
            sb.AppendLine("  [Textures]");
            foreach (var t in textures.OrderBy(x => x.name))
            {
                sb.AppendLine($"    - {t.name}");
            }
        }

        if (audioClips.Count > 0)
        {
            sb.AppendLine("  [AudioClips]");
            foreach (var a in audioClips.OrderBy(x => x.name))
            {
                sb.AppendLine($"    - {a.name}");
            }
        }

        return sb.ToString();
    }
}
#endif
