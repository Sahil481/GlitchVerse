#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One-click builder for the GlitchVerse game.
/// Menu: GlitchVerse > Build Everything
///  - imports the generated neon sprites with the right settings
///  - gives the Player prefab the courier sprite
///  - builds MainMenu + 5 level scenes from Assets/GlitchVerse/Levels/levelN.json
///  - fills Build Settings
/// Edit a level's JSON, run Build Everything again, and the scene is rebuilt.
/// </summary>
public static class GlitchVerseBuilder
{
    const string Root = "Assets/GlitchVerse/";
    const string ArtDir = Root + "Art/";
    const string MatDir = Root + "Materials/";
    const string LevelFileDir = "GlitchVerse/Levels/";           // relative to Assets/
    const string SceneDir = "Assets/Scenes/GlitchVerse/";
    const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    const string ManipPrefabPath = "Assets/Prefabs/Manipulation.prefab";
    const int LevelCount = 5;

    static int layerGround = 7, layerPhase = 8, layerDeath = 12;
    static Material unlit, lit;
    static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    // ------------------------------------------------------------ theme colours
    static readonly Color[] GroundTints =
    {
        new Color(0.35f, 0.85f, 1.00f), new Color(0.40f, 1.00f, 0.78f), new Color(0.72f, 0.58f, 1.00f),
        new Color(1.00f, 0.72f, 0.42f), new Color(1.00f, 0.52f, 0.85f)
    };
    static readonly Color[] WallTints =
    {
        new Color(0.75f, 0.50f, 1.00f), new Color(0.45f, 0.70f, 1.00f), new Color(1.00f, 0.50f, 0.90f),
        new Color(1.00f, 0.45f, 0.60f), new Color(0.50f, 0.85f, 1.00f)
    };
    static readonly Color[] CameraBg =
    {
        new Color(0.07f, 0.05f, 0.17f), new Color(0.03f, 0.08f, 0.14f), new Color(0.09f, 0.04f, 0.17f),
        new Color(0.14f, 0.05f, 0.10f), new Color(0.12f, 0.06f, 0.16f)
    };

    static Color Tint(string key, int theme)
    {
        int t = Mathf.Clamp(theme, 1, 5) - 1;
        switch (key)
        {
            case "wall": return WallTints[t];
            case "glitch": return new Color(1.00f, 0.28f, 0.75f);
            case "ghost": return new Color(0.78f, 0.62f, 1.00f);
            case "pad": return new Color(1.00f, 0.72f, 0.28f);
            case "crate": return new Color(1.00f, 0.60f, 0.30f);
            case "ice": return new Color(0.55f, 0.85f, 1.00f);
            default: return GroundTints[t];
        }
    }

    // ============================================================ menu items
    [MenuItem("GlitchVerse/Build Everything (menu + 5 levels)")]
    public static void BuildEverything()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        try
        {
            EditorUtility.DisplayProgressBar("GlitchVerse", "Importing art...", 0.05f);
            AssetDatabase.Refresh();
            ResolveLayers();
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Scenes/GlitchVerse");
            EnsureFolder("Assets/GlitchVerse/Materials");
            sprites.Clear();

            ConfigureArt();
            ConfigureHeroSheet();
            unlit = GetUnlitMaterial();
            lit = GetLitMaterial();

            EditorUtility.DisplayProgressBar("GlitchVerse", "Patching prefabs...", 0.2f);
            PatchPlayerPrefab();
            PatchManipulationPrefab();

            List<string> scenePaths = new List<string>();
            EditorUtility.DisplayProgressBar("GlitchVerse", "Building main menu...", 0.3f);
            List<string> failures = new List<string>();
            try { scenePaths.Add(BuildMenuScene()); }
            catch (System.Exception ex) { failures.Add("MainMenu: " + ex.Message); Debug.LogError("GlitchVerse: MainMenu failed\n" + ex); }

            for (int i = 1; i <= LevelCount; i++)
            {
                EditorUtility.DisplayProgressBar("GlitchVerse", "Building level " + i + "...", 0.3f + 0.12f * i);
                try { scenePaths.Add(BuildLevelScene(i)); }
                catch (System.Exception ex) { failures.Add("Level " + i + ": " + ex.Message); Debug.LogError("GlitchVerse: level " + i + " failed\n" + ex); }
            }

            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>();
            foreach (string p in scenePaths) list.Add(new EditorBuildSettingsScene(p, true));
            EditorBuildSettings.scenes = list.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (scenePaths.Count > 0) EditorSceneManager.OpenScene(scenePaths[0]);
            if (failures.Count > 0)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("GlitchVerse - some scenes failed",
                    string.Join("\n", failures.ToArray()) + "\n\nSee the Console for the full error.", "OK");
                return;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorUtility.DisplayDialog("GlitchVerse",
            "Done!\n\nMainMenu + 5 levels were built and added to Build Settings.\n" +
            "Use GlitchVerse > Play From Main Menu, or press Play in the MainMenu scene.", "Nice");
    }

    [MenuItem("GlitchVerse/Play From Main Menu")]
    public static void PlayFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string p = SceneDir + LevelCatalog.MenuScene + ".unity";
        if (!File.Exists(p)) { EditorUtility.DisplayDialog("GlitchVerse", "Run GlitchVerse > Build Everything first.", "OK"); return; }
        EditorSceneManager.OpenScene(p);
        EditorApplication.isPlaying = true;
    }

    [MenuItem("GlitchVerse/Reset Saved Progress")]
    public static void ResetProgress()
    {
        GameProgress.ResetAll();
        Debug.Log("GlitchVerse: progress reset.");
    }

    [MenuItem("GlitchVerse/Unlock All Levels")]
    public static void UnlockAllLevels()
    {
        GameProgress.UnlockAll = true;
        Debug.Log("GlitchVerse: all levels unlocked.");
    }

    // ============================================================ setup helpers
    static void ResolveLayers()
    {
        int g = LayerMask.NameToLayer("Ground");
        int p = LayerMask.NameToLayer("PhaseWall");
        int d = LayerMask.NameToLayer("Death");
        if (g >= 0) layerGround = g; else Debug.LogWarning("GlitchVerse: layer 'Ground' not found, using layer 7.");
        if (p >= 0) layerPhase = p; else Debug.LogWarning("GlitchVerse: layer 'PhaseWall' not found, using layer 8.");
        if (d >= 0) layerDeath = d; else Debug.LogWarning("GlitchVerse: layer 'Death' not found, using layer 12.");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        string name = path.Substring(slash + 1);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    static float Ppu(string n)
    {
        if (n.StartsWith("player_")) return 256f;
        if (n == "npc_vex") return 192f;
        if (n == "ramen") return 128f;
        if (n == "ramen_sign") return 128f;
        if (n.StartsWith("portrait_")) return 100f;
        if (n == "neon_block" || n == "neon_door") return 64f;
        if (n == "glow") return 64f;
        if (n == "ring") return 128f;
        if (n == "plate_base" || n == "plate_button") return 80f;
        if (n == "terminal") return 100f;
        if (n == "shard") return 80f;
        if (n == "checkpoint") return 64f;
        if (n == "spikes") return 60f;
        if (n == "laser") return 64f;
        if (n == "zone_fill") return 32f;
        if (n.StartsWith("bg") && n.EndsWith("_sky")) return 60f;
        if (n.StartsWith("bg")) return 32f;
        if (n == "menu_bg" || n == "title") return 100f;
        return 64f;
    }

    static Vector4 Border(string n)
    {
        if (n == "neon_block" || n == "neon_door") return new Vector4(16f, 16f, 16f, 16f);
        return Vector4.zero;
    }

    static void ConfigureArt()
    {
        string[] files = Directory.GetFiles(ArtDir);
        foreach (string f in files)
        {
            string ext = Path.GetExtension(f).ToLower();
            if (ext != ".png" && ext != ".jpg") continue;
            string path = f.Replace('\\', '/');
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            string name = Path.GetFileNameWithoutExtension(path);

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = Ppu(name);
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 4096;
            ti.spriteBorder = Border(name);

            TextureImporterSettings s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
        }
    }

    // the hero sheet is cut into frames at runtime; it only has to be a plain full-size texture
    static void ConfigureHeroSheet()
    {
        HeroSheetImportFix.Fix();
    }

    static Sprite Spr(string name)
    {
        Sprite s;
        if (sprites.TryGetValue(name, out s) && s != null) return s;
        s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + name + ".png");
        if (s == null) s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + name + ".jpg");
        if (s == null) Debug.LogWarning("GlitchVerse: missing sprite '" + name + "'");
        sprites[name] = s;
        return s;
    }

    static Material GetUnlitMaterial()
    {
        string p = MatDir + "NeonUnlit.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m != null && m.shader != null) return m;
        Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        m = new Material(sh);
        AssetDatabase.CreateAsset(m, p);
        return m;
    }

    static Material GetLitMaterial()
    {
        string p = MatDir + "NeonLit.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m != null && m.shader != null) return m;
        Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (sh == null) return unlit;
        m = new Material(sh);
        AssetDatabase.CreateAsset(m, p);
        return m;
    }

    // ============================================================ prefab patches
    static void PatchPlayerPrefab()
    {
        if (!File.Exists(PlayerPrefabPath)) { Debug.LogWarning("GlitchVerse: " + PlayerPrefabPath + " not found."); return; }

        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            PlayerController pc = root.GetComponentInChildren<PlayerController>(true);
            if (pc == null) { Debug.LogWarning("GlitchVerse: no PlayerController in the Player prefab."); return; }

            SpriteRenderer sr = pc.GetComponent<SpriteRenderer>();
            if (sr == null) sr = pc.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Spr("player_idle");
            sr.color = Color.white;
            sr.sharedMaterial = unlit;
            sr.sortingOrder = 10;

            PlayerVisuals pv = pc.GetComponent<PlayerVisuals>();
            if (pv == null) pv = pc.gameObject.AddComponent<PlayerVisuals>();
            pv.body = sr;
            pv.heroSprite = Spr("player_hero");
            sr.sprite = pv.heroSprite;

            // old aura / visual children from earlier builds are replaced by the runtime visual
            Transform aura = pc.transform.Find("Aura");
            if (aura != null) Object.DestroyImmediate(aura.gameObject);
            Transform vis = pc.transform.Find("Visual");
            if (vis != null) Object.DestroyImmediate(vis.gameObject);

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void PatchManipulationPrefab()
    {
        if (!File.Exists(ManipPrefabPath)) return;
        GameObject root = PrefabUtility.LoadPrefabContents(ManipPrefabPath);
        try
        {
            ManipulationController mc = root.GetComponentInChildren<ManipulationController>(true);
            if (mc != null)
            {
                // Dragged platforms stop at ground only, so the player can ride a lift while moving it.
                mc.moveBlockers = 1 << layerGround;
                PrefabUtility.SaveAsPrefabAsset(root, ManipPrefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ============================================================ shared scene pieces
    static VolumeProfile GetVolumeProfile()
    {
        string p = Root + "NeonVolumeProfile.asset";
        VolumeProfile vp = AssetDatabase.LoadAssetAtPath<VolumeProfile>(p);
        if (vp == null)
        {
            vp = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(vp, p);
        }

        Bloom bloom;
        if (!vp.TryGet(out bloom)) { bloom = vp.Add<Bloom>(true); AssetDatabase.AddObjectToAsset(bloom, vp); }
        bloom.threshold.Override(0.6f);
        bloom.intensity.Override(1.7f);
        bloom.scatter.Override(0.8f);
        bloom.tint.Override(new Color(0.9f, 0.85f, 1f));

        Vignette vig;
        if (!vp.TryGet(out vig)) { vig = vp.Add<Vignette>(true); AssetDatabase.AddObjectToAsset(vig, vp); }
        vig.intensity.Override(0.30f);
        vig.smoothness.Override(0.65f);
        vig.color.Override(new Color(0.05f, 0.0f, 0.12f));

        ChromaticAberration ca;
        if (!vp.TryGet(out ca)) { ca = vp.Add<ChromaticAberration>(true); AssetDatabase.AddObjectToAsset(ca, vp); }
        ca.intensity.Override(0.05f);

        ColorAdjustments adj;
        if (!vp.TryGet(out adj)) { adj = vp.Add<ColorAdjustments>(true); AssetDatabase.AddObjectToAsset(adj, vp); }
        adj.saturation.Override(28f);
        adj.contrast.Override(12f);

        EditorUtility.SetDirty(vp);
        AssetDatabase.SaveAssets();
        return vp;
    }

    static Camera MakeCamera(Color bg, float size)
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = size;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bg;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 100f;
        go.AddComponent<AudioListener>();
        UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;
        return cam;
    }

    static readonly Color[] AmbientColors =
    {
        new Color(0.62f, 0.66f, 1.00f), new Color(0.55f, 0.85f, 0.95f), new Color(0.80f, 0.60f, 1.00f),
        new Color(1.00f, 0.62f, 0.72f), new Color(0.90f, 0.62f, 1.00f)
    };

    static void MakeLightAndVolume(int theme)
    {
        GameObject lg = new GameObject("Global Light 2D");
        Light2D l = lg.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Global;
        l.color = AmbientColors[Mathf.Clamp(theme, 1, 5) - 1];
        l.intensity = 0.5f;

        GameObject vg = new GameObject("Post Process Volume");
        Volume v = vg.AddComponent<Volume>();
        v.isGlobal = true;
        v.sharedProfile = GetVolumeProfile();
        vg.AddComponent<PostFx>();
    }

    static NeonLight AddNeonLight(GameObject go, Color color, float radius, float intensity, Vector2 offset)
    {
        NeonLight nl = go.AddComponent<NeonLight>();
        nl.color = color;
        nl.radius = radius;
        nl.intensity = intensity;
        nl.offset = offset;
        return nl;
    }

    static SpriteRenderer AddSprite(GameObject go, Sprite s, Color c, int order)
    {
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = c;
        sr.sortingOrder = order;
        sr.sharedMaterial = unlit;
        return sr;
    }

    static GameObject Child(Transform parent, string name, Vector3 localPos)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        return g;
    }

    static SpriteRenderer GlowChild(Transform parent, string name, Vector3 localPos, float worldSize, Color c, int order)
    {
        GameObject g = Child(parent, name, localPos);
        g.transform.localScale = Vector3.one * (worldSize / 2f);   // glow.png is 2 units wide
        return AddSprite(g, Spr("glow"), c, order);
    }

    // ============================================================ main menu
    static string BuildMenuScene()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera mcam = MakeCamera(new Color(0.05f, 0.03f, 0.14f), 6f);
        GameObject mvg = new GameObject("Post Process Volume");
        Volume mv = mvg.AddComponent<Volume>();
        mv.isGlobal = true;
        mv.sharedProfile = GetVolumeProfile();
        AmbientFx mamb = mcam.gameObject.AddComponent<AmbientFx>();
        mamb.tint = new Color(0.4f, 0.9f, 1f);

        GameObject go = new GameObject("MainMenu");
        MainMenu mm = go.AddComponent<MainMenu>();
        mm.background = Spr("menu_bg");
        mm.titleSprite = Spr("title");
        mm.starOn = Spr("star_on");
        mm.starOff = Spr("star_off");
        mm.lockIcon = Spr("icon_lock");
        mm.shardIcon = Spr("shard");
        mm.heroSprite = Spr("player_hero");

        GameObject music = new GameObject("Music");
        MusicPlayer mp = music.AddComponent<MusicPlayer>();
        mp.trackName = "theme_calm";

        string path = SceneDir + LevelCatalog.MenuScene + ".unity";
        EditorSceneManager.SaveScene(scene, path);
        return path;
    }

    // ============================================================ level scenes
    class Ctx
    {
        public LevelData data;
        public int theme;
        public Transform world;
        public PlayerController player;
        public Dictionary<string, NeonDoor> doors = new Dictionary<string, NeonDoor>();
        public Dictionary<string, List<PressurePlate>> platesByDoor = new Dictionary<string, List<PressurePlate>>();
        public List<PressurePlate> plates = new List<PressurePlate>();
        public List<LevelObj> plateObjs = new List<LevelObj>();
        public float minX = 1e9f, maxX = -1e9f;
        public int shards;
        public Vector2 start;
    }

    static string BuildLevelScene(int index)
    {
        string jsonPath = Path.Combine(Application.dataPath, LevelFileDir + "level" + index + ".json");
        if (!File.Exists(jsonPath)) throw new FileNotFoundException("Missing level file: " + jsonPath);
        LevelData d = JsonUtility.FromJson<LevelData>(File.ReadAllText(jsonPath));

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Ctx c = new Ctx();
        c.data = d;
        c.theme = d.theme;

        Camera cam = MakeCamera(CameraBg[Mathf.Clamp(d.theme, 1, 5) - 1], d.cameraSize);
        MakeLightAndVolume(d.theme);

        GameObject worldGo = new GameObject("World");
        c.world = worldGo.transform;

        // pass 1: find start + extents
        foreach (LevelObj o in d.objects)
        {
            if (o.type == "start") c.start = new Vector2(o.x, o.y);
            if (o.type == "block" || o.type == "phase")
            {
                c.minX = Mathf.Min(c.minX, o.x);
                c.maxX = Mathf.Max(c.maxX, o.x + o.w);
            }
        }

        // pass 2: objects
        foreach (LevelObj o in d.objects)
        {
            switch (o.type)
            {
                case "block": MakeBlock(c, o); break;
                case "phase": MakePhase(c, o); break;
                case "death": MakeDeath(c, o); break;
                case "zone": MakeZone(c, o); break;
                case "plate": MakePlate(c, o); break;
                case "door": MakeDoor(c, o); break;
                case "shard": MakeShard(c, o); break;
                case "checkpoint": MakeCheckpoint(c, o); break;
                case "tip": MakeTip(c, o); break;
                case "goal": MakeGoal(c, o); break;
            }
        }
        LinkPlatesAndDoors(c);
        MakeLamps(c);
        MakeBoundaries(c);

        // player + manipulation
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject playerGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        PlayerController pc = playerGo.GetComponentInChildren<PlayerController>(true);
        Vector3 spawn = new Vector3(c.start.x, c.start.y + 0.85f, 0f);
        playerGo.transform.position += spawn - pc.transform.position;
        c.player = pc;

        GameObject manipPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ManipPrefabPath);
        if (manipPrefab != null) PrefabUtility.InstantiatePrefab(manipPrefab);

        // camera follow
        CameraFollow2D follow = cam.gameObject.AddComponent<CameraFollow2D>();
        follow.target = pc.transform;
        cam.transform.position = new Vector3(spawn.x + follow.offset.x, spawn.y + follow.offset.y, -10f);

        cam.gameObject.AddComponent<CameraFx>();
        cam.gameObject.AddComponent<DimensionFx>();
        AmbientFx amb = cam.gameObject.AddComponent<AmbientFx>();
        amb.tint = Tint("ground", d.theme);
        MakeBackground(c, cam);

        // death screen (uses the level list)
        GameObject dsGo = new GameObject("DeathScreenUI");
        DeathScreenUI dsu = dsGo.AddComponent<DeathScreenUI>();
        SerializedObject so = new SerializedObject(dsu);
        SerializedProperty list = so.FindProperty("levelSceneNames");
        if (list != null)
        {
            list.arraySize = LevelCatalog.SceneNames.Length;
            for (int k = 0; k < LevelCatalog.SceneNames.Length; k++)
                list.GetArrayElementAtIndex(k).stringValue = LevelCatalog.SceneNames[k];
        }
        SerializedProperty pp = so.FindProperty("playerController");
        if (pp != null) pp.objectReferenceValue = pc;
        so.ApplyModifiedPropertiesWithoutUndo();

        // level manager + hud
        GameObject lmGo = new GameObject("LevelManager");
        LevelManager lm = lmGo.AddComponent<LevelManager>();
        lm.levelIndex = index;
        lm.title = LevelCatalog.Titles[index - 1];
        lm.subtitle = LevelCatalog.Subtitles[index - 1];
        lm.objective = "Deliver the hot ramen to Vex before it goes cold.";
        lm.timeLimit = d.timeLimit;
        lm.parTime = d.parTime;
        lm.totalShards = c.shards;

        GameObject hudGo = new GameObject("LevelHUD");
        LevelHUD hud = hudGo.AddComponent<LevelHUD>();
        hud.shardIcon = Spr("shard");
        hud.starOn = Spr("star_on");
        hud.starOff = Spr("star_off");
        lm.Hud = hud;

        GameObject music = new GameObject("Music");
        MusicPlayer mp = music.AddComponent<MusicPlayer>();
        mp.trackName = "theme_calm";

        string path = SceneDir + LevelCatalog.SceneNames[index - 1] + ".unity";
        EditorSceneManager.SaveScene(scene, path);
        return path;
    }

    // ------------------------------------------------------------ blocks
    static SurfaceType ParseSurface(string s)
    {
        switch ((s ?? "").Trim().ToLower())
        {
            case "ice": return SurfaceType.Ice;
            case "mud": return SurfaceType.Mud;
            case "boost": return SurfaceType.Boost;
            case "bouncy": return SurfaceType.Bouncy;
            default: return SurfaceType.Normal;
        }
    }

    static SurfaceType[] ParseMats(string csv)
    {
        List<SurfaceType> list = new List<SurfaceType>();
        if (!string.IsNullOrEmpty(csv))
        {
            foreach (string part in csv.Split(','))
            {
                SurfaceType t = ParseSurface(part);
                if (!list.Contains(t)) list.Add(t);
            }
        }
        if (list.Count == 0) { list.Add(SurfaceType.Normal); }
        return list.ToArray();
    }

    static GameObject MakeBlock(Ctx c, LevelObj o)
    {
        bool ghost = o.kind == "shift" || o.kind == "shiftinv";
        GameObject go = new GameObject(string.IsNullOrEmpty(o.label) ? (ghost ? "GhostPlatform" : "Block") : o.label);
        go.transform.SetParent(c.world, false);
        go.transform.position = new Vector3(o.x + o.w * 0.5f, o.y + o.h * 0.5f, 0f);
        go.layer = layerGround;

        string colorKey = ghost ? "ghost" : o.color;
        SpriteRenderer sr = AddSprite(go, Spr("neon_block"), Tint(colorKey, c.theme), 0);
        if (lit != null) sr.sharedMaterial = lit;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(o.w, o.h);

        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(o.w, o.h);

        if (ghost)
        {
            ShiftPlatform sp = go.AddComponent<ShiftPlatform>();
            sp.inverted = o.kind == "shiftinv";
            sp.hiddenAlpha = 0.13f;
        }

        bool fixedMaterial = !string.IsNullOrEmpty(o.startMat) && ParseSurface(o.startMat) != SurfaceType.Normal;
        if (o.mat || fixedMaterial)
        {
            SurfaceMaterial sm = go.AddComponent<SurfaceMaterial>();
            sm.type = ParseSurface(o.startMat);
        }

        if (o.move || o.del || o.mat)
        {
            Manipulable m = go.AddComponent<Manipulable>();
            m.displayName = string.IsNullOrEmpty(o.label) ? "Platform" : o.label;
            m.canMove = o.move;
            m.canDelete = o.del;
            m.canChangeMaterial = o.mat;
            m.axis = o.axis == "h" ? Manipulable.MoveAxis.HorizontalOnly
                   : o.axis == "v" ? Manipulable.MoveAxis.VerticalOnly
                   : Manipulable.MoveAxis.Both;
            m.moveRange = o.range > 0f ? o.range : 3f;
            m.allowedMaterials = ParseMats(o.mats);

            // soft pulsing glow tells the player "you can click this"
            SpriteRenderer hint = GlowChild(go.transform, "Hint", Vector3.zero, 2f, new Color(1f, 0.75f, 0.3f, 0.30f), -1);
            hint.transform.localScale = new Vector3((o.w + 1.8f) / 2f, (o.h + 1.8f) / 2f, 1f);
            NeonPulse np = hint.gameObject.AddComponent<NeonPulse>();
            np.scaleAmount = 0.03f;
            np.alphaAmount = 0.12f;
            np.speed = 2.2f;
        }
        return go;
    }

    static void MakePhase(Ctx c, LevelObj o)
    {
        GameObject go = new GameObject("PhaseWall");
        go.transform.SetParent(c.world, false);
        go.transform.position = new Vector3(o.x + o.w * 0.5f, o.y + o.h * 0.5f, 0f);
        go.layer = layerPhase;
        SpriteRenderer sr = AddSprite(go, Spr("neon_door"), new Color(1f, 0.3f, 0.85f, 0.85f), 1);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(o.w, o.h);
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(o.w, o.h);

        SpriteRenderer glow = GlowChild(go.transform, "Glow", Vector3.zero, 2f, new Color(1f, 0.3f, 0.85f, 0.18f), -1);
        glow.transform.localScale = new Vector3((o.w + 2f) / 2f, (o.h + 2f) / 2f, 1f);
        NeonPulse np = go.AddComponent<NeonPulse>();
        np.scaleAmount = 0f;
        np.alphaAmount = 0.1f;
        np.speed = 2.5f;
    }

    static void MakeDeath(Ctx c, LevelObj o)
    {
        bool down = o.kind == "spikesdown";
        GameObject go = new GameObject(down ? "SpikesDown" : "Spikes");
        go.transform.SetParent(c.world, false);
        go.transform.position = new Vector3(o.x + o.w * 0.5f, o.y + o.h * 0.5f, 0f);
        go.layer = layerDeath;
        SpriteRenderer sr = AddSprite(go, Spr("spikes"), Color.white, 3);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(o.w, o.h);
        sr.flipY = down;
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(o.w * 0.92f, o.h * 0.7f);
        bc.offset = new Vector2(0f, down ? o.h * 0.15f : -o.h * 0.15f);
    }

    static void MakeZone(Ctx c, LevelObj o)
    {
        bool invert = o.kind == "invert";
        GameObject go = new GameObject(invert ? "InversionZone" : "GravityZone");
        go.transform.SetParent(c.world, false);
        go.transform.position = new Vector3(o.x + o.w * 0.5f, o.y + o.h * 0.5f, 0f);

        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(o.w, o.h);
        GlitchZone gz = go.AddComponent<GlitchZone>();
        gz.type = invert ? GlitchZone.ZoneType.ControlInversion : GlitchZone.ZoneType.GravityMultiplier;
        gz.value = o.value;
        ZoneFx zfx = go.AddComponent<ZoneFx>();
        zfx.size = new Vector2(o.w, o.h);
        zfx.invert = invert;
        zfx.gravityValue = o.value;

        Color col = invert ? new Color(1f, 0.35f, 0.9f, 0.16f) : new Color(0.55f, 0.65f, 1f, 0.16f);
        GameObject fill = Child(go.transform, "Fill", Vector3.zero);
        SpriteRenderer fsr = AddSprite(fill, Spr("zone_fill"), col, -5);
        fsr.drawMode = SpriteDrawMode.Tiled;
        fsr.tileMode = SpriteTileMode.Continuous;
        fsr.size = new Vector2(o.w, o.h);

        // repeating icon so the player can read the zone
        Sprite icon = Spr(invert ? "icon_swap" : "icon_gravity");
        int count = Mathf.Max(1, Mathf.RoundToInt(o.w / 7f));
        for (int i = 0; i < count; i++)
        {
            float px = -o.w * 0.5f + (i + 0.5f) * (o.w / count);
            GameObject ig = Child(go.transform, "Icon", new Vector3(px, 0f, 0f));
            ig.transform.localScale = Vector3.one * 1.6f;
            SpriteRenderer isr = AddSprite(ig, icon, new Color(col.r, col.g, col.b, 0.45f), -4);
            isr.flipY = !invert && o.value < 0f;
            NeonPulse np = ig.AddComponent<NeonPulse>();
            np.scaleAmount = 0.05f;
            np.alphaAmount = 0.15f;
            np.speed = 1.6f;
            np.phaseOffset = i * 0.9f;
        }
    }

    // ------------------------------------------------------------ plates and doors
    static void MakePlate(Ctx c, LevelObj o)
    {
        GameObject go = new GameObject("Plate_" + o.id);
        go.transform.SetParent(c.world, false);
        go.transform.position = new Vector3(o.x, o.y, 0f);
        float s = o.flipped ? -1f : 1f;

        SpriteRenderer baseSr = AddSprite(Child(go.transform, "Base", new Vector3(0f, 0.2f * s, 0f)), Spr("plate_base"), Color.white, 2);
        baseSr.flipY = o.flipped;
        SpriteRenderer btnSr = AddSprite(Child(go.transform, "Button", new Vector3(0f, 0.5f * s, 0f)), Spr("plate_button"), Color.white, 2);
        btnSr.flipY = o.flipped;
        SpriteRenderer glow = GlowChild(go.transform, "Glow", new Vector3(0f, 0.45f * s, 0f), 2.4f, new Color(1f, 0.7f, 0.28f, 0.4f), 1);

        PressurePlate pp = go.AddComponent<PressurePlate>();
        pp.latch = o.latch;
        pp.flipped = o.flipped;
        pp.baseRenderer = baseSr;
        pp.buttonRenderer = btnSr;
        pp.glowRenderer = glow;

        c.plates.Add(pp);
        AddNeonLight(go, new Color(0.36f, 1f, 0.69f), 5.5f, 1.3f, new Vector2(0f, 0.3f));
        c.plateObjs.Add(o);
    }

    static void MakeDoor(Ctx c, LevelObj o)
    {
        GameObject go = new GameObject("Door_" + o.id);
        go.transform.SetParent(c.world, false);
        go.transform.position = new Vector3(o.x, o.y + o.h * 0.5f, 0f);
        go.layer = layerGround;

        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(o.w, o.h);

        GameObject slabGo = Child(go.transform, "Slab", Vector3.zero);
        SpriteRenderer slab = AddSprite(slabGo, Spr("neon_door"), new Color(1f, 0.24f, 0.78f, 0.9f), 1);
        slab.drawMode = SpriteDrawMode.Sliced;
        slab.size = new Vector2(o.w, o.h);

        NeonDoor nd = go.AddComponent<NeonDoor>();
        nd.slab = slab;
        nd.doorCollider = bc;
        nd.topEmitter = GlowChild(go.transform, "TopEmitter", new Vector3(0f, o.h * 0.5f, 0f), 1.1f, Color.white, 2);
        nd.bottomEmitter = GlowChild(go.transform, "BottomEmitter", new Vector3(0f, -o.h * 0.5f, 0f), 1.1f, Color.white, 2);
        c.doors[o.id] = nd;
        AddNeonLight(go, new Color(1f, 0.3f, 0.85f), 8f, 1.6f, Vector2.zero);
    }

    static void LinkPlatesAndDoors(Ctx c)
    {
        for (int i = 0; i < c.plates.Count; i++)
        {
            PressurePlate p = c.plates[i];
            LevelObj o = c.plateObjs[i];
            List<NeonDoor> targets = new List<NeonDoor>();
            if (!string.IsNullOrEmpty(o.targets))
            {
                foreach (string id in o.targets.Split(','))
                {
                    string key = id.Trim();
                    NeonDoor nd;
                    if (!c.doors.TryGetValue(key, out nd)) { Debug.LogWarning("GlitchVerse: plate " + o.id + " targets unknown door '" + key + "'"); continue; }
                    targets.Add(nd);
                    List<PressurePlate> list;
                    if (!c.platesByDoor.TryGetValue(key, out list)) { list = new List<PressurePlate>(); c.platesByDoor[key] = list; }
                    list.Add(p);
                }
            }
            p.doors = targets.ToArray();
        }

        foreach (KeyValuePair<string, NeonDoor> kv in c.doors)
        {
            List<PressurePlate> list;
            if (!c.platesByDoor.TryGetValue(kv.Key, out list)) list = new List<PressurePlate>();
            NeonDoor nd = kv.Value;
            nd.plates = list.ToArray();

            float h = nd.slab != null ? nd.slab.size.y : 4f;
            nd.leds = new SpriteRenderer[list.Count];
            for (int k = 0; k < list.Count; k++)
            {
                float x = (k - (list.Count - 1) * 0.5f) * 0.45f;
                GameObject led = Child(nd.transform, "Led" + k, new Vector3(x, h * 0.5f + 0.45f, 0f));
                led.transform.localScale = Vector3.one * 0.28f;
                nd.leds[k] = AddSprite(led, Spr("ui_circle"), new Color(1f, 0.24f, 0.78f, 0.55f), 3);
            }
        }
    }

    // ------------------------------------------------------------ collectibles, checkpoints, goal, tips
    static void MakeShard(Ctx c, LevelObj o)
    {
        GameObject go = new GameObject("Shard");
        go.transform.SetParent(c.world, false);
        go.transform.position = new Vector3(o.x, o.y, 0f);
        SpriteRenderer sr = AddSprite(go, Spr("shard"), new Color(1f, 0.82f, 0.32f), 4);
        SpriteRenderer glow = GlowChild(go.transform, "Glow", Vector3.zero, 1.8f, new Color(1f, 0.75f, 0.25f, 0.4f), 3);
        CircleCollider2D cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = 0.5f;
        Shard sh = go.AddComponent<Shard>();
        sh.sprite = sr;
        sh.glow = glow;
        c.shards++;
    }

    static void MakeCheckpoint(Ctx c, LevelObj o)
    {
        GameObject go = new GameObject("Checkpoint_" + (int)o.value);
        go.transform.SetParent(c.world, false);
        go.transform.position = new Vector3(o.x, o.y, 0f);
        SpriteRenderer beacon = AddSprite(Child(go.transform, "Beacon", new Vector3(0f, 1f, 0f)), Spr("checkpoint"), Color.white, 2);
        SpriteRenderer glow = GlowChild(go.transform, "Glow", new Vector3(0f, 1.6f, 0f), 2.4f, new Color(0.36f, 1f, 0.69f, 0f), 1);
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(1.4f, 2.6f);
        bc.offset = new Vector2(0f, 1.3f);
        Checkpoint cp = go.AddComponent<Checkpoint>();
        cp.order = (int)o.value;
        cp.spawnPosition = new Vector2(o.x, o.y + 0.85f);
        cp.beacon = beacon;
        cp.glow = glow;
    }

    static void MakeGoal(Ctx c, LevelObj o)
    {
        GameObject go = new GameObject("Vex_RamenGuy");
        go.transform.SetParent(c.world, false);
        go.transform.position = new Vector3(o.x, o.y, 0f);

        // soft neon pad on the floor
        SpriteRenderer pad = GlowChild(go.transform, "FloorGlow", new Vector3(0f, 0.05f, 0f), 6f, new Color(1f, 0.3f, 0.85f, 0.35f), 1);
        pad.transform.localScale = new Vector3(3.2f, 0.7f, 1f);
        NeonPulse padPulse = pad.gameObject.AddComponent<NeonPulse>();
        padPulse.scaleAmount = 0.05f;
        padPulse.alphaAmount = 0.15f;
        GlowChild(go.transform, "Aura", new Vector3(0f, 1.6f, 0f), 6.5f, new Color(1f, 0.35f, 0.8f, 0.2f), 1);

        GameObject bodyGo = Child(go.transform, "Body", new Vector3(0f, 1.5f, 0f));
        SpriteRenderer body = AddSprite(bodyGo, Spr("npc_vex"), Color.white, 3);

        GameObject signGo = Child(go.transform, "RamenSign", new Vector3(0f, 3.7f, 0f));
        AddSprite(signGo, Spr("ramen_sign"), Color.white, 4);

        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(3.2f, 3.2f);
        bc.offset = new Vector2(0f, 1.6f);

        NeonLight nl = AddNeonLight(go, new Color(1f, 0.45f, 0.85f), 12f, 2.2f, new Vector2(0f, 1.8f));
        nl.pulseAmount = 0.15f;
        nl.flicker = 0.25f;

        DeliveryNpc npc = go.AddComponent<DeliveryNpc>();
        npc.body = body;
        npc.sign = signGo.transform;
        npc.ramenSprite = Spr("ramen");
        npc.npcPortrait = Spr("portrait_npc");
        npc.heroPortrait = Spr("portrait_hero");
    }

    // coloured neon street lamps above the longer platforms: they light the blocks and the hero as he passes
    static void MakeLamps(Ctx c)
    {
        Color[] pal =
        {
            new Color(0.2f, 0.9f, 1f), new Color(1f, 0.3f, 0.85f), new Color(0.65f, 0.45f, 1f), new Color(1f, 0.7f, 0.3f), new Color(0.4f, 1f, 0.7f)
        };
        int n = 0;
        foreach (LevelObj o in c.data.objects)
        {
            if (o.type != "block" || o.w < 7f || n >= 36) continue;
            int count = Mathf.Max(1, Mathf.RoundToInt(o.w / 14f));
            for (int i = 0; i < count; i++)
            {
                float px = o.x + (i + 0.5f) * (o.w / count);
                GameObject g = new GameObject("Lamp");
                g.transform.SetParent(c.world, false);
                g.transform.position = new Vector3(px, o.y + o.h + 3.2f, 0f);
                AddNeonLight(g, pal[(n + c.theme) % pal.Length], 11f, 1.15f, Vector2.zero).pulseAmount = 0.1f;
                GlowChild(g.transform, "Bulb", Vector3.zero, 1.6f, new Color(pal[(n + c.theme) % pal.Length].r, pal[(n + c.theme) % pal.Length].g, pal[(n + c.theme) % pal.Length].b, 0.55f), 6);
                n++;
                if (n >= 36) break;
            }
        }
    }

    static void MakeTip(Ctx c, LevelObj o)
    {
        GameObject go = new GameObject("Tip_" + o.title);
        go.transform.SetParent(c.world, false);
        go.transform.position = new Vector3(o.x + o.w * 0.5f, o.y + o.h * 0.5f, 0f);
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(o.w, o.h);
        TutorialTrigger tt = go.AddComponent<TutorialTrigger>();
        tt.title = o.title;
        tt.body = o.text;
    }

    // ------------------------------------------------------------ boundaries and background
    static void MakeBoundaries(Ctx c)
    {
        // kill floor
        GameObject kill = new GameObject("KillFloor");
        kill.transform.SetParent(c.world, false);
        float width = (c.maxX - c.minX) + 80f;
        kill.transform.position = new Vector3((c.minX + c.maxX) * 0.5f, c.data.bottomY - 2f, 0f);
        kill.layer = layerDeath;
        BoxCollider2D kb = kill.AddComponent<BoxCollider2D>();
        kb.isTrigger = true;
        kb.size = new Vector2(width, 4f);

        // left wall so the player cannot walk out of the level
        GameObject wall = new GameObject("LeftWall");
        wall.transform.SetParent(c.world, false);
        wall.layer = layerGround;
        wall.transform.position = new Vector3(c.minX - 1f, 12f, 0f);
        BoxCollider2D wb = wall.AddComponent<BoxCollider2D>();
        wb.size = new Vector2(2f, 60f);
        SpriteRenderer sr = AddSprite(wall, Spr("neon_block"), Tint("wall", c.theme), 0);
        if (lit != null) sr.sharedMaterial = lit;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(2f, 60f);
    }

    static void MakeBackground(Ctx c, Camera cam)
    {
        int t = Mathf.Clamp(c.theme, 1, 5);
        GameObject root = new GameObject("Background");

        // sky: covers the whole screen and follows the camera
        Sprite sky = Spr("bg" + t + "_sky");
        GameObject skyGo = new GameObject("Sky");
        skyGo.transform.SetParent(root.transform, false);
        skyGo.transform.position = new Vector3(0f, 0f, 20f);
        AddSprite(skyGo, sky, Color.white, -100);
        float camH = cam.orthographicSize * 2f;
        float camW = camH * 2.4f;                                   // ultra-wide safe
        float k = Mathf.Max(camW / 32f, camH / 18f) * 1.1f;
        skyGo.transform.localScale = new Vector3(k, k, 1f);
        ParallaxLayer skyP = skyGo.AddComponent<ParallaxLayer>();
        skyP.followX = 1f;
        skyP.followY = 1f;
        skyP.period = 0f;

        string[] names = { "far", "mid", "near" };
        float[] follow = { 0.92f, 0.80f, 0.64f };
        float[] followY = { 0.97f, 0.94f, 0.90f };
        int[] order = { -90, -80, -70 };
        float[] z = { 15f, 10f, 5f };
        for (int i = 0; i < 3; i++)
        {
            Sprite sp = Spr("bg" + t + "_" + names[i]);
            if (sp == null) continue;
            GameObject layer = new GameObject("Layer_" + names[i]);
            layer.transform.SetParent(root.transform, false);
            layer.transform.position = new Vector3(0f, 0f, z[i]);
            float period = sp.bounds.size.x;
            for (int j = -2; j <= 2; j++)
            {
                GameObject copy = new GameObject("Tile" + j);
                copy.transform.SetParent(layer.transform, false);
                copy.transform.localPosition = new Vector3(j * period, 0f, 0f);
                AddSprite(copy, sp, Color.white, order[i]);
            }
            ParallaxLayer pl = layer.AddComponent<ParallaxLayer>();
            pl.followX = follow[i];
            pl.followY = followY[i];
            pl.period = period;
        }
    }
}
#endif
