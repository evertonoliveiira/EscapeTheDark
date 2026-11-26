using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using EscapeTheDark;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Smoke check e capturas reais de Game.unity. Executavel tambem com -executeMethod VisualReview.RunBatch.
[InitializeOnLoad]
public static class VisualReview
{
    private static int stage;
    private static double nextCapture;
    private static bool failed;
    private static Camera camera;
    private static PlayerMotor player;
    private static Keyboard reviewKeyboard;
    private static Mouse reviewMouse;
    private static Vector3 movementStart;
    private static string Output = "docs/visual-review";
    private static bool lightingReview;
    private static float originalExposure;
    private static Key[] reviewKeys = { Key.W, Key.LeftShift };
    private static int visibilityRoom;
    private static float illuminatedBrightness;
    private static readonly List<RoomVisibility> visibilityResults = new List<RoomVisibility>();

    [Serializable]
    private sealed class RoomVisibility
    {
        public int room;
        public float flashlightOnBrightness;
        public float flashlightOffBrightness;
        public float brightnessRatio;
        public float brightnessGain;
        public bool passed;
    }

    [Serializable]
    private sealed class VisibilityReport
    {
        public string measurement = "Mean display brightness in the same world-space view; HUD and flashlight housing excluded.";
        public float maximumUnlitBrightness = 0.025f;
        public float minimumUnlitBrightness = 0.001f;
        public float minimumBrightnessGain = 0.005f;
        public RoomVisibility[] rooms;
    }

    [MenuItem("Escape The Dark/Gerar previas de iluminacao")]
    public static void RunLightingBatch()
    {
        lightingReview = true;
        Output = "docs/lighting-review";
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-lightingReviewOutput") Output = args[i + 1];
        SessionState.SetBool("EscapeTheDark.LightingReview", true);
        SessionState.SetString("EscapeTheDark.ReviewOutput", Output);
        RunBatch();
    }

    static VisualReview()
    {
        if (!SessionState.GetBool("EscapeTheDark.VisualReview", false)) return;
        lightingReview = SessionState.GetBool("EscapeTheDark.LightingReview", false);
        Output = SessionState.GetString("EscapeTheDark.ReviewOutput", "docs/visual-review");
        Application.logMessageReceived += CheckLog;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    [MenuItem("Escape The Dark/Gerar previas visuais")]
    public static void RunBatch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Encerre o Play Mode antes de gerar as previas.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        failed = false;
        stage = 0;
        visibilityResults.Clear();
        SessionState.SetBool("EscapeTheDark.VisualReview", true);
        Directory.CreateDirectory(Output);
        Application.logMessageReceived += CheckLog;
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayMode(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        try
        {
            var director = GameDirector.Instance;
            if (!director) throw new InvalidOperationException("GameDirector ausente.");
            Invoke(director, "Begin");
            player = UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();
            camera = player.GetComponentInChildren<Camera>();
            // As comparacoes do feixe acontecem apos a introducao, com a pilha ja instalada.
            director.CollectBattery(director.FlashlightBattery);
            if (!player.HasFlashlightBattery) throw new InvalidOperationException("A pilha da introducao nao foi instalada na revisao visual.");
            if (!camera || !UnityEngine.Object.FindAnyObjectByType<HospitalVisuals>())
                throw new InvalidOperationException("Apresentacao visual incompleta.");
            if (lightingReview)
            {
                var flashlight = player.GetComponent<FlashlightController>();
                if (!flashlight || !flashlight.Beam.cookie || flashlight.Beam.shadows != LightShadows.Soft)
                    throw new InvalidOperationException("Optica ou sombras da lanterna ausentes.");
                originalExposure = Exposure();
            }
            var controller = player.GetComponent<CharacterController>();
            if (controller.height != 1.8f || controller.radius != 0.35f || controller.stepOffset != 0.3f)
                throw new InvalidOperationException("A configuracao fisica do jogador foi alterada.");
            var visuals = UnityEngine.Object.FindAnyObjectByType<HospitalVisuals>();
            foreach (var collider in visuals.DecorationRoot.GetComponentsInChildren<Collider>())
                if (collider.enabled) throw new InvalidOperationException("Cenografia adicionou colisao: " + collider.name);
            player.enabled = false;
            stage = 1;
            nextCapture = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update += Tick;
        }
        catch (Exception exception) { Fail(exception); }
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < nextCapture) return;
        try
        {
            switch (stage++)
            {
                case 1:
                    Pose(new Vector3(0, 0.04f, -8), 180);
                    break;
                case 2:
                    Capture("01-recepcao.png");
                    Pose(new Vector3(-8, 0.04f, -8), -12);
                    break;
                case 3:
                    Capture("02-triagem.png");
                    Pose(new Vector3(0, 0.04f, 0), 0);
                    break;
                case 4:
                    Capture("03-ala-central.png");
                    Pose(new Vector3(-8, 0.04f, 8), 0);
                    break;
                case 5:
                    Capture("04-isolamento.png");
                    Invoke(GameDirector.Instance, "SpawnEnemy");
                    var enemy = UnityEngine.Object.FindAnyObjectByType<EnemyHunter>();
                    enemy.enabled = false;
                    var agent = enemy.GetComponent<NavMeshAgent>();
                    if (!agent.isOnNavMesh) throw new InvalidOperationException("Criatura fora do NavMesh.");
                    agent.isStopped = true;
                    var presentation = enemy.GetComponent<CreaturePresentation>();
                    if (!presentation || !presentation.HasLocomotion)
                        throw new InvalidOperationException("Animacao da criatura ausente.");
                    Pose(new Vector3(8, 0.04f, 2), 0);
                    break;
                case 6:
                    Capture("05-criatura.png");
                    player.GetComponent<FlashlightController>().SetOn(false, false);
                    break;
                case 7:
                    Capture("06-lanterna-desligada.png");
                    if (lightingReview && (player.IsFlashlightOn || player.GetComponent<FlashlightController>().Beam.enabled ||
                        Mathf.Abs(Exposure() - originalExposure) > 0.0001f))
                        throw new InvalidOperationException("O interruptor alterou a exposicao global ou deixou o feixe aceso.");
                    // Verifica a locomocao real durante alguns segundos, sem interferir na IA.
                    var hunter = UnityEngine.Object.FindAnyObjectByType<EnemyHunter>();
                    hunter.enabled = true;
                    nextCapture = EditorApplication.timeSinceStartup + 5;
                    return;
                case 8:
                    var creature = UnityEngine.Object.FindAnyObjectByType<EnemyHunter>();
                    creature.enabled = false;
                    creature.GetComponent<NavMeshAgent>().isStopped = true;
                    Pose(new Vector3(0, 0.04f, -8), 0);
                    player.GetComponent<FlashlightController>().SetOn(true, false);
                    reviewKeyboard = InputSystem.AddDevice<Keyboard>("VisualReviewKeyboard");
                    reviewMouse = InputSystem.AddDevice<Mouse>("VisualReviewMouse");
                    // O Editor em batch nao tem uma Game View com foco; alimenta apenas os dispositivos de teste.
                    InputSystem.onAfterUpdate += HoldReviewInput;
                    HoldReviewInput();
                    movementStart = player.transform.position;
                    Invoke(GameDirector.Instance, "Begin");
                    player.enabled = true;
                    nextCapture = EditorApplication.timeSinceStartup + 0.65;
                    return;
                case 9:
                    if (Vector3.Distance(movementStart, player.transform.position) < 0.1f || camera.fieldOfView <= 72)
                        throw new InvalidOperationException($"Corrida sem resposta: deslocamento={Vector3.Distance(movementStart, player.transform.position)}, FOV={camera.fieldOfView}, dt={Time.deltaTime}, teclado={Keyboard.current?.name}, W={reviewKeyboard.wKey.isPressed}.");
                    Capture("07-movimento-corrida.png");
                    if (lightingReview)
                    {
                        reviewKeys = new[] { Key.F };
                        nextCapture = EditorApplication.timeSinceStartup + 0.2;
                        return;
                    }
                    player.enabled = false;
                    Finish();
                    return;
                case 10:
                    if (player.IsFlashlightOn) throw new InvalidOperationException("A tecla F nao desligou a lanterna.");
                    player.enabled = false;
                    reviewKeys = Array.Empty<Key>();
                    Pose(new Vector3(0, 0.04f, 3.42f), 0);
                    player.GetComponent<FlashlightController>().SetOn(true, false);
                    break;
                case 11:
                    var lightControl = player.GetComponent<FlashlightController>();
                    if (lightControl.WallRetraction < 0.5f || lightControl.Beam.transform.position.z > 3.70f)
                        throw new InvalidOperationException("A lanterna atravessou o revestimento da parede.");
                    Capture("08-proximo-da-parede.png");
                    Pose(new Vector3(-8, 0.04f, -8), -42);
                    camera.transform.localRotation = Quaternion.Euler(12, 0, 0);
                    break;
                case 12:
                    Capture("09-sombras-e-halo.png");
                    if (lightingReview)
                    {
                        visibilityRoom = 0;
                        PoseVisibilityRoom();
                        break;
                    }
                    Finish();
                    return;
                case 13:
                    illuminatedBrightness = Capture($"room-{visibilityRoom + 1:00}-on.png");
                    player.GetComponent<FlashlightController>().SetOn(false, false);
                    break;
                case 14:
                    float darkBrightness = Capture($"room-{visibilityRoom + 1:00}-off.png");
                    float ratio = illuminatedBrightness / Mathf.Max(0.00001f, darkBrightness);
                    visibilityResults.Add(new RoomVisibility
                    {
                        room = visibilityRoom + 1,
                        flashlightOnBrightness = illuminatedBrightness,
                        flashlightOffBrightness = darkBrightness,
                        brightnessRatio = ratio,
                        brightnessGain = illuminatedBrightness - darkBrightness,
                        // O indicador da saida pode continuar visivel. Avalia o limite de luz de fundo
                        // e o ganho no ambiente, sem exigir uma razao relativa a esse ponto luminoso.
                        passed = darkBrightness >= 0.001f && darkBrightness <= 0.025f && illuminatedBrightness - darkBrightness >= 0.005f
                    });
                    if (++visibilityRoom < 9)
                    {
                        PoseVisibilityRoom();
                        stage = 13;
                        break;
                    }
                    File.WriteAllText(Path.Combine(Output, "visibility.json"), JsonUtility.ToJson(
                        new VisibilityReport { rooms = visibilityResults.ToArray() }, true));
                    foreach (var result in visibilityResults)
                        if (!result.passed) throw new InvalidOperationException($"Contraste insuficiente na sala {result.room}: fundo={result.flashlightOffBrightness}, ganho={result.brightnessGain}.");
                    Finish();
                    return;
                default:
                    Finish();
                    return;
            }
            nextCapture = EditorApplication.timeSinceStartup + 0.5;
        }
        catch (Exception exception) { Fail(exception); }
    }

    private static void Pose(Vector3 position, float yaw)
    {
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        controller.enabled = true;
        camera.transform.localPosition = new Vector3(0, 1.62f, 0);
        camera.transform.localRotation = Quaternion.Euler(2, 0, 0);
    }

    private static void PoseVisibilityRoom()
    {
        Pose(new Vector3((visibilityRoom % 3 - 1) * 8, 0.04f, (visibilityRoom / 3 - 1) * 8), -18);
        player.GetComponent<FlashlightController>().SetOn(true, false);
    }

    private static float Capture(string name)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        bool asyncCompilation = ShaderUtil.allowAsyncCompilation;
        var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            // Uma captura precisa de todos os passes prontos, inclusive os novos passes de sombras.
            ShaderUtil.allowAsyncCompilation = false;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(Output, name), image.EncodeToPNG());
            Debug.Log("VisualReview: " + name);
            // Compara a cena realmente renderizada, em vez de apenas os valores das fontes de luz.
            Color32[] pixels = image.GetPixels32();
            double total = 0;
            int count = 0;
            for (int y = 110; y < 610; y += 4)
            for (int x = 100; x < 1180; x += 4)
            {
                if (x > 810 && y < 300) continue;
                Color32 pixel = pixels[y * 1280 + x];
                total += (pixel.r * 0.2126 + pixel.g * 0.7152 + pixel.b * 0.0722) / 255;
                count++;
            }
            return (float)(total / count);
        }
        finally
        {
            ShaderUtil.allowAsyncCompilation = asyncCompilation;
            camera.targetTexture = previousTarget;
            RenderTexture.active = previous;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    private static void Invoke(GameDirector director, string method)
    {
        typeof(GameDirector).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(director, null);
    }

    private static void HoldReviewInput()
    {
        if (reviewKeyboard == null || !reviewKeyboard.added) return;
        InputState.Change(reviewKeyboard, new KeyboardState(reviewKeys));
        reviewKeyboard.MakeCurrent();
        reviewMouse.MakeCurrent();
    }

    private static void CheckLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true;
    }

    private static float Exposure()
    {
        foreach (var volume in UnityEngine.Object.FindObjectsByType<Volume>())
            if (volume.sharedProfile && volume.sharedProfile.TryGet(out ColorAdjustments adjustments))
                return adjustments.postExposure.value;
        throw new InvalidOperationException("Perfil de exposicao ausente.");
    }

    private static void Fail(Exception exception)
    {
        failed = true;
        Debug.LogException(exception);
        Finish();
    }

    private static void Finish()
    {
        SessionState.SetBool("EscapeTheDark.VisualReview", false);
        SessionState.SetBool("EscapeTheDark.LightingReview", false);
        InputSystem.onAfterUpdate -= HoldReviewInput;
        if (reviewKeyboard != null && reviewKeyboard.added) InputSystem.RemoveDevice(reviewKeyboard);
        if (reviewMouse != null && reviewMouse.added) InputSystem.RemoveDevice(reviewMouse);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= CheckLog;
        Debug.Log(failed ? "VisualReview: FAILED" : "VisualReview: PASS - cena, colisores, navegacao, animacao e lanterna.");
        lightingReview = false;
        Output = "docs/visual-review";
        reviewKeys = new[] { Key.W, Key.LeftShift };
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
        else EditorApplication.ExitPlaymode();
    }
}
