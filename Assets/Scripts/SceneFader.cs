using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Fade-to-black scene transitions. Call SceneFader.Load("SceneName"), and SceneFader.Ready() at scene start.</summary>
public class SceneFader : MonoBehaviour
{
    static SceneFader inst;
    Image img;
    bool loading;

    static void Ensure(bool startBlack)
    {
        if (inst != null) return;
        GameObject go = new GameObject("SceneFader");
        DontDestroyOnLoad(go);
        inst = go.AddComponent<SceneFader>();
        inst.Build(startBlack);
    }

    void Build(bool startBlack)
    {
        Canvas c = UIKit.CreateCanvas("FadeCanvas", 5000);
        c.transform.SetParent(transform, false);
        img = UIKit.Panel("Fade", c.transform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                          new Color(0.02f, 0.01f, 0.06f, startBlack ? 1f : 0f), false, Color.clear);
        UIKit.Stretch(img.rectTransform);
        img.raycastTarget = false;
        SceneManager.sceneLoaded += OnLoaded;
        if (startBlack) StartCoroutine(FadeTo(0f, 0.6f));
    }

    void OnDestroy() { SceneManager.sceneLoaded -= OnLoaded; }

    void OnLoaded(Scene s, LoadSceneMode m)
    {
        StopAllCoroutines();
        loading = false;
        img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);
        StartCoroutine(FadeTo(0f, 0.55f));
    }

    /// <summary>Call from a scene's first Start so the very first scene also fades in.</summary>
    public static void Ready() { Ensure(true); }

    public static void Load(string sceneName)
    {
        Ensure(false);
        if (inst.loading) return;
        inst.StartCoroutine(inst.Do(sceneName));
    }

    IEnumerator Do(string sceneName)
    {
        loading = true;
        img.raycastTarget = true;
        yield return FadeTo(1f, 0.3f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeTo(float target, float time)
    {
        float start = img.color.a;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / time;
            float a = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            img.color = new Color(img.color.r, img.color.g, img.color.b, a);
            yield return null;
        }
        img.raycastTarget = target > 0.5f;
    }
}
