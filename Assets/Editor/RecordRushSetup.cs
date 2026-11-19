#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Editor helper: builds the whole scene in one click (menu: Record Rush > Build Scene).
// It only creates GameObjects/prefabs/UI and wires the references of the other scripts.
public static class RecordRushSetup
{
    [MenuItem("Record Rush/Build Scene")]
    public static void Build()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Animations");

        // ---------- Materials ----------
        Material groundMat = MakeMat("Ground", new Color(0.12f, 0.13f, 0.2f));
        Material playerMat = MakeMat("Player", new Color(0.1f, 0.9f, 0.9f));
        Material coinMat = MakeMat("Record", new Color(1f, 0.85f, 0.1f));
        Material bombMat = MakeMat("Bomb", new Color(0.9f, 0.1f, 0.1f));
        Material doorMat = MakeMat("Door", new Color(0.8f, 0.1f, 0.9f));
        Material wallMat = MakeMat("Wall", new Color(0.25f, 0.25f, 0.35f));

        // ---------- Ground + walls ----------
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(2f, 1f, 2f); // 20 x 20 units
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        MakeWall("Wall_E", new Vector3(10.25f, 1f, 0f), new Vector3(0.5f, 2f, 21f), wallMat);
        MakeWall("Wall_W", new Vector3(-10.25f, 1f, 0f), new Vector3(0.5f, 2f, 21f), wallMat);
        MakeWall("Wall_S", new Vector3(0f, 1f, -10.25f), new Vector3(21f, 2f, 0.5f), wallMat);
        // north wall has a gap for the door (door is at x = 0)
        MakeWall("Wall_N_Left", new Vector3(-6.25f, 1f, 10.25f), new Vector3(7.5f, 2f, 0.5f), wallMat);
        MakeWall("Wall_N_Right", new Vector3(6.25f, 1f, 10.25f), new Vector3(7.5f, 2f, 0.5f), wallMat);

        // ---------- Player ----------
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        player.transform.position = new Vector3(0f, 1.1f, -7f);
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        PlayerController pc = player.AddComponent<PlayerController>();

        // A small "nose" so you can see where the player is facing
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        Object.DestroyImmediate(nose.GetComponent<BoxCollider>());
        nose.transform.SetParent(player.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.5f, 0.45f);
        nose.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        nose.GetComponent<Renderer>().sharedMaterial = wallMat;

        // ---------- Coin prefab (empty parent with trigger + visual child) ----------
        GameObject coin = new GameObject("Coin");
        SphereCollider coinCol = coin.AddComponent<SphereCollider>();
        coinCol.isTrigger = true;
        coinCol.radius = 0.7f;
        Rigidbody coinRb = coin.AddComponent<Rigidbody>();
        coinRb.isKinematic = true;
        coin.AddComponent<Coin>();
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Disc";
        Object.DestroyImmediate(disc.GetComponent<CapsuleCollider>());
        disc.transform.SetParent(coin.transform, false);
        disc.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // stand on its edge
        disc.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
        disc.GetComponent<Renderer>().sharedMaterial = coinMat;
        GameObject coinPrefab = PrefabUtility.SaveAsPrefabAsset(coin, "Assets/Prefabs/Coin.prefab");
        Object.DestroyImmediate(coin);

        // ---------- Bomb prefab ----------
        GameObject bomb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bomb.name = "Bomb";
        bomb.transform.localScale = Vector3.one * 0.9f;
        bomb.GetComponent<Renderer>().sharedMaterial = bombMat;
        Rigidbody bombRb = bomb.AddComponent<Rigidbody>();
        bombRb.useGravity = true;
        bomb.AddComponent<Bomb>();
        GameObject bombPrefab = PrefabUtility.SaveAsPrefabAsset(bomb, "Assets/Prefabs/Bomb.prefab");
        Object.DestroyImmediate(bomb);

        // ---------- Door (trigger zone + animated panel) ----------
        GameObject doorRoot = new GameObject("Door");
        doorRoot.transform.position = new Vector3(0f, 0f, 10.25f);
        BoxCollider zone = doorRoot.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.center = new Vector3(0f, 1.5f, -1.5f);
        zone.size = new Vector3(4f, 3f, 3f);

        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "Panel";
        panel.transform.SetParent(doorRoot.transform, false);
        panel.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        panel.transform.localScale = new Vector3(4f, 3f, 0.5f);
        panel.GetComponent<Renderer>().sharedMaterial = doorMat;
        Animator panelAnim = panel.AddComponent<Animator>();
        panelAnim.runtimeAnimatorController = BuildDoorController();

        Door door = doorRoot.AddComponent<Door>();
        door.animator = panelAnim;

        // ---------- Camera ----------
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        CameraFollow follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = player.transform;
        cam.transform.position = player.transform.position + follow.offset;
        cam.transform.LookAt(player.transform);

        // ---------- Canvas (HUD) ----------
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        TMP_Text coinsText = MakeText(canvasGo, "CoinsText", "Records: 0/10", new Vector2(0f, 1f), new Vector2(30f, -30f), 48, TextAlignmentOptions.TopLeft);
        TMP_Text livesText = MakeText(canvasGo, "LivesText", "Lives: 3", new Vector2(0f, 1f), new Vector2(30f, -90f), 48, TextAlignmentOptions.TopLeft);
        TMP_Text timerText = MakeText(canvasGo, "TimerText", "Time: 90", new Vector2(1f, 1f), new Vector2(-30f, -30f), 48, TextAlignmentOptions.TopRight);
        TMP_Text dashText = MakeText(canvasGo, "DashText", "Dash: READY (click)", new Vector2(0f, 0f), new Vector2(30f, 30f), 36, TextAlignmentOptions.BottomLeft);
        TMP_Text messageText = MakeText(canvasGo, "MessageText", "", new Vector2(0.5f, 0.5f), Vector2.zero, 80, TextAlignmentOptions.Center);
        messageText.rectTransform.sizeDelta = new Vector2(1400f, 400f);

        // ---------- Game Manager + spawner ----------
        GameObject gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();
        gm.player = pc;
        gm.door = door;
        gm.coinPrefab = coinPrefab;
        gm.coinsText = coinsText;
        gm.livesText = livesText;
        gm.timerText = timerText;
        gm.dashText = dashText;
        gm.messageText = messageText;

        BombSpawner spawner = gmGo.AddComponent<BombSpawner>();
        spawner.bombPrefab = bombPrefab;
        spawner.player = player.transform;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Record Rush scene built. Press Play!");
    }


    // ---------- Light visual polish (run after Build Scene) ----------
    [MenuItem("Record Rush/Polish Look")]
    public static void Polish()
    {
        // Materials: neon emission + grid floor
        Glow("Player", new Color(0.1f, 0.9f, 0.9f), 0.6f);
        Glow("Record", new Color(1f, 0.85f, 0.1f), 1.5f);
        Glow("Bomb", new Color(0.9f, 0.1f, 0.1f), 1.2f);
        Glow("Door", new Color(0.8f, 0.1f, 0.9f), 1.5f);

        Material ground = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ground.mat");
        ground.color = Color.white;
        ground.mainTexture = MakeGridTexture();
        ground.mainTextureScale = new Vector2(10f, 10f);
        EditorUtility.SetDirty(ground);

        // Atmosphere: dark background, fog, cool light
        Camera cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.02f, 0.08f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.03f, 0.02f, 0.08f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 18f;
        RenderSettings.fogEndDistance = 45f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.25f, 0.25f, 0.4f);
        Light sun = Object.FindFirstObjectByType<Light>();
        if (sun != null) sun.color = new Color(0.8f, 0.85f, 1f);

        // Magenta light over the door
        GameObject door = GameObject.Find("Door");
        if (door != null && door.transform.Find("DoorLight") == null)
        {
            GameObject l = new GameObject("DoorLight");
            l.transform.SetParent(door.transform, false);
            l.transform.localPosition = new Vector3(0f, 2.5f, -2f);
            Light light = l.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.8f, 0.1f, 0.9f);
            light.intensity = 40f;
            light.range = 9f;
        }

        // HUD: panels behind texts, colors, bold
        GameObject canvas = GameObject.Find("Canvas");
        StyleText("CoinsText", new Color(1f, 0.85f, 0.1f), 48);
        StyleText("LivesText", new Color(1f, 0.35f, 0.35f), 48);
        StyleText("TimerText", Color.white, 48);
        StyleText("DashText", new Color(0.3f, 1f, 1f), 36);
        StyleText("MessageText", new Color(1f, 0.9f, 0.3f), 80);
        MakePanel(canvas, "Panel_TopLeft", new Vector2(0f, 1f), new Vector2(15f, -15f), new Vector2(480f, 150f));
        MakePanel(canvas, "Panel_TopRight", new Vector2(1f, 1f), new Vector2(-15f, -15f), new Vector2(330f, 85f));
        MakePanel(canvas, "Panel_BottomLeft", new Vector2(0f, 0f), new Vector2(15f, 15f), new Vector2(520f, 70f));

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("Record Rush look polished.");
    }

    static void Glow(string matName, Color c, float intensity)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + matName + ".mat");
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * intensity);
        EditorUtility.SetDirty(m);
    }

    static Texture2D MakeGridTexture()
    {
        const int size = 128;
        Texture2D t = new Texture2D(size, size);
        Color bg = new Color(0.08f, 0.09f, 0.16f);
        Color line = new Color(0.25f, 0.3f, 0.7f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                t.SetPixel(x, y, (x < 3 || y < 3) ? line : bg);
        t.Apply();
        string path = "Assets/Materials/Grid.png";
        System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.wrapMode = TextureWrapMode.Repeat;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void StyleText(string name, Color color, float size)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) return;
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.color = color;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        EditorUtility.SetDirty(t);
    }

    static void MakePanel(GameObject canvas, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        if (canvas.transform.Find(name) != null) return;
        GameObject p = new GameObject(name);
        p.transform.SetParent(canvas.transform, false);
        p.transform.SetAsFirstSibling(); // behind the texts
        Image img = p.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.45f);
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void MakeWall(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = name;
        w.transform.position = pos;
        w.transform.localScale = scale;
        w.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static TMP_Text MakeText(GameObject parent, string name, string text, Vector2 anchor, Vector2 pos, float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        RectTransform rt = t.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(700f, 70f);
        return t;
    }

    static AnimatorController BuildDoorController()
    {
        // Clip: the panel slides up 3 units in 1 second
        AnimationClip clip = new AnimationClip();
        clip.name = "DoorOpen";
        EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Transform), "localPosition.y");
        AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.EaseInOut(0f, 1.5f, 1f, 4.5f));
        AssetDatabase.CreateAsset(clip, "Assets/Animations/DoorOpen.anim");

        // Controller: Closed --(trigger Open)--> Open
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/Door.controller");
        controller.AddParameter("Open", AnimatorControllerParameterType.Trigger);
        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState closed = sm.AddState("Closed");
        AnimatorState open = sm.AddState("Open");
        open.motion = clip;
        sm.defaultState = closed;
        AnimatorStateTransition t = closed.AddTransition(open);
        t.hasExitTime = false;
        t.duration = 0f;
        t.AddCondition(AnimatorConditionMode.If, 0f, "Open");
        return controller;
    }

    static Material MakeMat(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = color;
        EditorUtility.SetDirty(m);
        return m;
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
