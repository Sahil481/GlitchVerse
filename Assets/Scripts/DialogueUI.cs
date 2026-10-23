using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom-of-screen dialogue box with portrait, speaker name and a typewriter effect.
/// Click, Space, Enter or E advances; the first press completes the current line.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public struct Line
    {
        public bool npc;
        public string text;
        public Line(bool npc, string text) { this.npc = npc; this.text = text; }
    }

    public Sprite npcPortrait, heroPortrait;
    public string npcName = "VEX";
    public string heroName = "COURIER";

    CanvasGroup group;
    Image portrait;
    Text nameText, bodyText, promptText;
    RectTransform box;
    Image boxImage;
    bool built;

    void Build()
    {
        if (built) return;
        built = true;
        UIKit.EnsureEventSystem();
        Canvas canvas = UIKit.CreateCanvas("DialogueCanvas", 700);
        canvas.transform.SetParent(transform, false);

        boxImage = UIKit.Panel("Box", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f),
                               new Vector2(1280f, 250f), new Color(0.04f, 0.03f, 0.12f, 0.94f), true, UIKit.Magenta);
        box = boxImage.rectTransform;
        group = boxImage.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        portrait = UIKit.Icon("Portrait", box, null, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f),
                              new Vector2(210f, 210f), Color.white);
        nameText = UIKit.Label("Name", box, "", 34, UIKit.Magenta, new Vector2(0f, 1f), new Vector2(0f, 1f),
                               new Vector2(255f, -18f), new Vector2(700f, 44f), TextAnchor.MiddleLeft, true);
        bodyText = UIKit.Label("Body", box, "", 32, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f),
                               new Vector2(255f, -66f), new Vector2(980f, 140f), TextAnchor.UpperLeft, false);
        promptText = UIKit.Label("Prompt", box, "SPACE  >", 20, new Color(1f, 1f, 1f, 0.6f), new Vector2(1f, 0f),
                                 new Vector2(1f, 0f), new Vector2(-24f, 12f), new Vector2(200f, 28f), TextAnchor.MiddleRight, true);
    }

    public IEnumerator Play(Line[] lines)
    {
        Build();
        yield return Fade(1f, 0.25f);

        for (int i = 0; i < lines.Length; i++)
        {
            Line l = lines[i];
            bool npc = l.npc;
            Color accent = npc ? UIKit.Magenta : UIKit.Cyan;
            portrait.sprite = npc ? npcPortrait : heroPortrait;
            portrait.enabled = portrait.sprite != null;
            nameText.text = npc ? npcName : heroName;
            nameText.color = accent;
            Outline o = boxImage.GetComponent<Outline>();
            if (o != null) o.effectColor = new Color(accent.r, accent.g, accent.b, 0.85f);
            promptText.text = i == lines.Length - 1 ? "SPACE  >  DONE" : "SPACE  >";
            promptText.enabled = false;
            bodyText.text = l.text;

            // typewriter
            string full = l.text;
            bodyText.text = "";
            int shown = 0;
            float acc = 0f;
            bool skip = false;
            yield return null;              // swallow the keypress that opened / advanced the box
            while (shown < full.Length)
            {
                if (WantsAdvance()) { skip = true; }
                if (skip) { shown = full.Length; }
                else
                {
                    acc += Time.unscaledDeltaTime * 52f;
                    int add = (int)acc;
                    if (add > 0)
                    {
                        acc -= add;
                        for (int k = 0; k < add && shown < full.Length; k++)
                        {
                            char ch = full[shown];
                            shown++;
                            if (ch != ' ' && shown % 2 == 0) SfxHub.Play(npc ? "blip" : "blip_hi");
                        }
                    }
                }
                bodyText.text = full.Substring(0, shown);
                float bounce = npc ? 1f + 0.02f * Mathf.Sin(Time.unscaledTime * 30f) : 1f;
                portrait.rectTransform.localScale = Vector3.one * bounce;
                yield return null;
            }
            portrait.rectTransform.localScale = Vector3.one;
            promptText.enabled = true;

            while (!WantsAdvance())
            {
                promptText.color = new Color(1f, 1f, 1f, 0.4f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f));
                yield return null;
            }
        }
        yield return Fade(0f, 0.2f);
    }

    static bool WantsAdvance()
    {
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E)
            || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0);
    }

    IEnumerator Fade(float target, float time)
    {
        float start = group.alpha;
        Vector2 basePos = new Vector2(0f, 50f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / time;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            group.alpha = Mathf.Lerp(start, target, k);
            box.anchoredPosition = basePos + new Vector2(0f, (1f - group.alpha) * -40f);
            yield return null;
        }
        group.alpha = target;
    }
}
