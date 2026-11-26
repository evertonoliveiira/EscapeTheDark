using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EscapeTheDark;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Integracao em Play Mode: usa os mesmos raycasts, teclas e tempos de interacao do jogador.
[InitializeOnLoad]
public static class ScenarioReview
{
    private sealed class Step
    {
        public string name;
        public Action action;
        public Func<bool> condition;
        public double delay;
    }
    private static readonly Queue<Step> steps = new Queue<Step>();
    private static readonly List<string> passed = new List<string>();
    private static GameDirector director;
    private static PlayerMotor player;
    private static Camera camera;
    private static Keyboard keyboard;
    private static Mouse mouse;
    private static Key[] keys = Array.Empty<Key>();
    private static double next;
    private static double deadline;
    private static bool failed;
    private static float closedPathLength;
    private static Vector3 pausedEnemyPosition;
    private static EnemyHunter hunter;
    private static GameDirector previousDirector;
    private static BatteryPickup tutorialBattery;
    private static string output = "docs/scenario-review";

    static ScenarioReview()
    {
        if (!SessionState.GetBool("EscapeTheDark.ScenarioReview", false)) return;
        Application.logMessageReceived += CheckLog;
        EditorApplication.playModeStateChanged += Entered;
    }

    [MenuItem("Escape The Dark/Validar percurso e objetivos")]
    public static void RunBatch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Encerre o Play Mode antes da revisao.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-scenarioReviewOutput") output = args[i + 1];
        SessionState.SetString("EscapeTheDark.ScenarioReviewOutput", output);
        SessionState.SetBool("EscapeTheDark.ScenarioReview", true);
        Directory.CreateDirectory(output);
        steps.Clear();
        passed.Clear();
        failed = false;
        Application.logMessageReceived += CheckLog;
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        EditorApplication.playModeStateChanged += Entered;
        EditorApplication.EnterPlaymode();
    }

    private static void Entered(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        EditorApplication.playModeStateChanged -= Entered;
        try
        {
            output = SessionState.GetString("EscapeTheDark.ScenarioReviewOutput", output);
            RefreshReferences();
            BeginGame();
            keyboard = InputSystem.AddDevice<Keyboard>("ScenarioReviewKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("ScenarioReviewMouse");
            InputSystem.onAfterUpdate += FeedInput;
            BuildSteps();
            next = EditorApplication.timeSinceStartup + 2;
            deadline = next + 12;
            EditorApplication.update += Tick;
        }
        catch (Exception exception) { Fail(exception); }
    }

    private static void BuildSteps()
    {
        Add("Estado inicial e bloqueios", () =>
        {
            Require(director.FuseCount == 0 && director.InstalledCircuitCount == 0 && !director.ExitPowered, "Estado inicial incorreto.");
            Require(!director.WardCabinet.TryCode(KeypadCabinet.AccessCode), "Armario abriu sem energia.");
            director.CollectFuse(director.WardCabinet.Fuse);
            director.SurgicalEquipment.Interact(player);
            director.ReachExit();
            Require(director.FuseCount == 0 && !director.HasWon && !director.SurgicalEquipment.WasRemoved, "Objetivos puderam ser ignorados.");
            var flashlight = player.GetComponent<FlashlightController>();
            Require(!flashlight.HasBattery && !flashlight.IsOn && !flashlight.Beam.enabled && flashlight.Beam.intensity == 0,
                "A partida nao comecou com a lanterna descarregada.");
            director.UsePowerPanel();
            Require(!director.HasReadPlan && director.IsPlaying, "O quadro foi lido antes de encontrar a pilha.");
            closedPathLength = PathLength(new Vector3(0, 0, 2), new Vector3(0, 0, 6));
            Require(closedPathLength > 12, "A passagem de servico ja estava aberta.");
            Capture("00-recepcao-sem-pilha.png");
        });
        Press(Key.F);
        Add("Interruptor sem pilha", () =>
        {
            var flashlight = player.GetComponent<FlashlightController>();
            Require(flashlight.HasTriedEmptySwitch && !flashlight.HasBattery && !flashlight.IsOn && !flashlight.Beam.enabled,
                "O interruptor ligou uma lanterna sem pilha.");
            Require(director.Objective.Contains("pilha"), "O objetivo nao orientou a busca pela pilha.");
            tutorialBattery = director.FlashlightBattery;
            Pose(new Vector3(0.65f, 0.04f, -7.45f), tutorialBattery.transform.position);
        });
        Wait("Jogador focaliza a pilha", () => player.FocusedInteraction is BatteryPickup);
        Add("Pilha visivel sob luz de emergencia", () => Capture("00-pilha-no-balcao.png"));
        Press(Key.E);
        Add("Pilha instalada acende a lanterna", () =>
        {
            var flashlight = player.GetComponent<FlashlightController>();
            Require(flashlight.HasBattery && flashlight.IsOn && flashlight.Beam.enabled && flashlight.Beam.intensity > 0,
                "Coletar a pilha nao acendeu a lanterna.");
            Require(!UnityEngine.Object.FindAnyObjectByType<BatteryPickup>() && director.FuseCount == 0,
                "A coleta duplicou a pilha ou alterou a contagem de fusiveis.");
            director.CollectBattery(tutorialBattery);
            Require(!flashlight.InsertBattery(), "A mesma pilha foi instalada duas vezes.");
            Capture("00-lanterna-com-pilha.png");
        });
        Press(Key.F);
        Add("Desliga apos instalar a pilha", () => Require(player.HasFlashlightBattery && !player.IsFlashlightOn, "A lanterna nao desligou apos receber a pilha."));
        Press(Key.F);
        Add("Religa sem perder a pilha", () =>
        {
            Require(player.HasFlashlightBattery && player.IsFlashlightOn, "A lanterna perdeu a pilha ao alternar F.");
            Pose(new Vector3(-2.45f, 0.04f, -9.5f), director.EmergencyPanel.transform.position);
        });
        Wait("Jogador focaliza o quadro", () => player.FocusedInteraction is PowerPanel);
        Add("Quadro visivel", () => { Capture("01-quadro.png"); RequireFocus<PowerPanel>(); });
        Press(Key.E);
        Add("Plano de contingencia", () =>
        {
            Require(director.HasReadPlan && !director.IsPlaying, "O quadro nao abriu o plano.");
        }, 0.5);
        Press(Key.Escape);
        Add("Reserva da farmacia", () => Pose(new Vector3(-10, 0.04f, -0.1f), FindFuse(HospitalCircuit.Reception).transform.position));
        Wait("Jogador focaliza a reserva", () => player.FocusedInteraction is FusePickup);
        Add("Reserva alcançavel", () => { RequireFocus<FusePickup>(); Capture("03-reserva-farmacia.png"); });
        Press(Key.E);
        Add("Primeira coleta sem energia automatica", () =>
        {
            Require(director.FuseCount == 1 && director.CarriedFuseCount == 1 && director.InstalledCircuitCount == 0, "Coleta nao separada da instalacao.");
            Require(!director.Shortcut.IsUnlocked, "Atalho abriu antes da instalacao.");
            Pose(new Vector3(-2.45f, 0.04f, -9.5f), director.EmergencyPanel.transform.position);
        });
        Press(Key.E);
        Add("Circuito da recepcao", () => Require(director.IsCircuitInstalled(HospitalCircuit.Reception) && director.CarriedFuseCount == 0 && director.Shortcut.IsUnlocked, "Instalacao nao restaurou o primeiro circuito."));
        Wait("Abertura fisica do atalho", () => director.Shortcut.IsPassable);
        Add("NavMesh do atalho", () =>
        {
            float openedLength = PathLength(new Vector3(0, 0, 2), new Vector3(0, 0, 6));
            Require(openedLength < 6 && openedLength < closedPathLength * 0.5f, "O NavMesh nao usa o atalho liberado.");
            Pose(new Vector3(10.45f, 0.04f, -6.2f), FindNote("transferencias").transform.position);
        }, 0.5);
        Add("Pista da observacao", () => { RequireFocus<StoryNote>(); Capture("04-ficha-observacao.png"); });
        Press(Key.E);
        Add("Codigo registrado", () => Require(director.KnowsWardCode && !director.IsPlaying, "A leitura nao registrou a pista."));
        Press(Key.Escape);
        Add("Teclado da enfermaria", () => Pose(new Vector3(5.4f, 0.04f, -1.4f), director.WardCabinet.transform.position + Vector3.up * 1.35f));
        Add("Armario alcançavel", () => { RequireFocus<KeypadCabinet>(); Capture("05-armario-fechado.png"); });
        Press(Key.E);
        Press(Key.Digit1); Press(Key.Digit1); Press(Key.Digit1); Press(Key.Enter);
        Add("Codigo incorreto recusado", () => Require(!director.WardCabinet.IsOpen && !director.IsPlaying, "Codigo incorreto abriu o armario."));
        Press(Key.Digit2); Press(Key.Digit4); Press(Key.Digit1); Press(Key.Enter);
        Wait("Abertura do armario", () => director.WardCabinet.IsOpen && director.IsPlaying);
        Add("Peca na prateleira", () => Pose(new Vector3(5.4f, 0.04f, -1.4f), director.WardCabinet.Fuse.transform.position), 1.3);
        Add("Segunda peca visivel", () => { RequireFocus<FusePickup>(); Capture("06-armario-aberto.png"); });
        Press(Key.E);
        Add("Ameaca apos a segunda peca", () =>
        {
            Require(director.FuseCount == 2 && director.InstalledCircuitCount == 1, "Segunda coleta incorreta.");
            hunter = UnityEngine.Object.FindAnyObjectByType<EnemyHunter>();
            Require(hunter && Vector3.Distance(hunter.transform.position, player.transform.position) >= 9, "Criatura apareceu perto demais.");
            hunter.GetComponent<NavMeshAgent>().Warp(Sample(new Vector3(-8, 0, -8)));
            Pose(new Vector3(2.55f, 0.04f, 6.1f), FindNote("manutencao").transform.position);
        });
        Press(Key.E);
        Add("Leitura pausa a criatura", () =>
        {
            Require(!director.IsPlaying && hunter.GetComponent<NavMeshAgent>().isStopped, "O agente continua movendo durante a leitura.");
            pausedEnemyPosition = hunter.transform.position;
        }, 0.65);
        Add("Criatura permanece parada", () => Require(Vector3.Distance(pausedEnemyPosition, hunter.transform.position) < 0.02f, "A criatura se moveu no modal."));
        Press(Key.Escape);
        Add("Travessia real do agente pelo atalho", () =>
        {
            hunter.enabled = false; // Isola a navegacao da captura durante o roteiro automatizado.
            hunter.SetSuspended(false);
            var agent = hunter.GetComponent<NavMeshAgent>();
            Require(agent.Warp(Sample(new Vector3(0, 0, 2))), "Warp para o teste de navegacao falhou.");
            agent.SetDestination(Sample(new Vector3(0, 0, 6)));
        });
        Wait("Agente atravessou o corredor", () => Vector3.Distance(hunter.transform.position, new Vector3(0, 0, 6)) < 0.8f);
        Add("Instalacao do segundo circuito", () =>
        {
            hunter.SetSuspended(true);
            Pose(new Vector3(-2.45f, 0.04f, -9.5f), director.EmergencyPanel.transform.position);
        });
        Press(Key.E);
        Add("Equipamento energizado", () =>
        {
            Require(director.IsCircuitInstalled(HospitalCircuit.Ward) && !director.ExitPowered, "O segundo circuito nao foi instalado.");
            Pose(new Vector3(9.2f, 0.04f, 8.7f), director.SurgicalEquipment.transform.position + Vector3.up * 1.4f);
        });
        Add("Acao de isolar equipamento", () => { RequireFocus<EquipmentFuse>(); Capture("07-equipamento-cirurgia.png"); });
        Add("Inicia retirada", () => keys = new[] { Key.E }, 0.75);
        Add("Interrompe retirada", () => keys = Array.Empty<Key>());
        Add("Soltar E cancela a retirada", () => Require(!director.SurgicalEquipment.WasRemoved && director.FuseCount == 2 && player.InteractionProgress == 0, "A retirada parcial nao foi cancelada."));
        Add("Retirada completa", () => keys = new[] { Key.E });
        Wait("Ultimo fusivel retirado", () => director.SurgicalEquipment.WasRemoved);
        Add("Ultima peca nao libera saida sozinha", () =>
        {
            keys = Array.Empty<Key>();
            director.ReachExit();
            Require(director.FuseCount == 3 && director.InstalledCircuitCount == 2 && !director.HasWon, "A coleta final liberou a saida antes da instalacao.");
            Pose(new Vector3(-2.45f, 0.04f, -9.5f), director.EmergencyPanel.transform.position);
        });
        Press(Key.E);
        Add("Tres circuitos instalados", () =>
        {
            Require(director.ExitPowered && director.CarriedFuseCount == 0 && !director.HasWon, "A energia final nao foi restaurada corretamente.");
            Require(player.HasFlashlightBattery, "A pilha acabou durante o percurso.");
            Capture("08-quadro-restaurado.png");
            Pose(new Vector3(0, 0.04f, 10), UnityEngine.Object.FindAnyObjectByType<ExitDoor>().transform.position);
        });
        Add("Saida exige acionamento", () => { RequireFocus<ExitDoor>(); Require(!director.HasWon, "Vitoria automatica antes de acionar a porta."); Capture("09-saida.png"); });
        Add("Aciona a saida", () => keys = new[] { Key.E });
        Wait("Vitoria pelo comando da saida", () => director.HasWon);
        Add("Solta interacao", () => { keys = Array.Empty<Key>(); previousDirector = director; });
        Press(Key.R);
        Wait("Reinicio carrega nova partida", () => GameDirector.Instance && GameDirector.Instance != previousDirector);
        Add("Estado reiniciado", () =>
        {
            RefreshReferences();
            Require(director.IsPlaying && director.FuseCount == 0 && director.InstalledCircuitCount == 0 && !director.KnowsWardCode && !director.Shortcut.IsUnlocked && !director.WardCabinet.IsOpen, "O reinicio preservou progresso antigo.");
            Require(!UnityEngine.Object.FindAnyObjectByType<EnemyHunter>(), "A criatura persistiu no reinicio.");
            var flashlight = player.GetComponent<FlashlightController>();
            Require(!flashlight.HasBattery && !flashlight.IsOn && !flashlight.Beam.enabled && !flashlight.HasTriedEmptySwitch && director.FlashlightBattery,
                "O reinicio nao restaurou a busca pela primeira pilha.");
        }, 1.5);
        Add("Atalho fecha novamente", () => Require(PathLength(new Vector3(0, 0, 2), new Vector3(0, 0, 6)) > 12, "O NavMesh do reinicio deixou a passagem aberta."));
    }

    private static void Add(string name, Action action, double delay = 0.25) => steps.Enqueue(new Step { name = name, action = action, delay = delay });
    private static void Wait(string name, Func<bool> condition) => steps.Enqueue(new Step { name = name, condition = condition, delay = 0.1 });
    private static void Press(Key key)
    {
        Add("Tecla " + key, () => keys = new[] { key }, 0.12);
        Add("Soltar " + key, () => keys = Array.Empty<Key>(), 0.12);
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (steps.Count == 0) { Finish(); return; }
            Step step = steps.Peek();
            if (step.condition != null && !step.condition())
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException(step.name);
                return;
            }
            steps.Dequeue();
            step.action?.Invoke();
            passed.Add(step.name);
            Debug.Log("ScenarioReview: " + step.name);
            next = EditorApplication.timeSinceStartup + step.delay;
            deadline = next + 12;
        }
        catch (Exception exception) { Fail(exception); }
    }

    private static void RefreshReferences()
    {
        director = GameDirector.Instance;
        player = UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();
        camera = player.GetComponentInChildren<Camera>();
    }

    private static void BeginGame() => typeof(GameDirector).GetMethod("Begin", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(director, null);
    private static FusePickup FindFuse(HospitalCircuit circuit)
    {
        foreach (var fuse in UnityEngine.Object.FindObjectsByType<FusePickup>()) if (fuse.Circuit == circuit) return fuse;
        throw new InvalidOperationException("Fusivel nao encontrado: " + circuit);
    }
    private static StoryNote FindNote(string id)
    {
        foreach (var note in UnityEngine.Object.FindObjectsByType<StoryNote>()) if (note.NoteId == id) return note;
        throw new InvalidOperationException("Nota nao encontrada: " + id);
    }

    private static void Pose(Vector3 position, Vector3 target)
    {
        var controller = player.GetComponent<CharacterController>();
        controller.enabled = false;
        player.transform.position = position;
        camera.transform.localPosition = new Vector3(0, 1.62f, 0);
        Quaternion look = Quaternion.LookRotation(target - camera.transform.position);
        float pitch = look.eulerAngles.x > 180 ? look.eulerAngles.x - 360 : look.eulerAngles.x;
        player.transform.rotation = Quaternion.Euler(0, look.eulerAngles.y, 0);
        typeof(PlayerMotor).GetField("pitch", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, pitch);
        camera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    private static void RequireFocus<T>() where T : GameInteraction => Require(player.FocusedInteraction is T,
        "Raycast esperava " + typeof(T).Name + ", encontrou " + (player.FocusedInteraction ? player.FocusedInteraction.GetType().Name : "nada") + ".");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static Vector3 Sample(Vector3 point)
    {
        if (!NavMesh.SamplePosition(point, out NavMeshHit hit, 1.5f, NavMesh.AllAreas)) throw new InvalidOperationException("Ponto fora do NavMesh: " + point);
        return hit.position;
    }
    private static float PathLength(Vector3 from, Vector3 to)
    {
        var path = new NavMeshPath();
        Require(NavMesh.CalculatePath(Sample(from), Sample(to), NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "Nao existe rota completa entre os setores.");
        float length = 0;
        for (int i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        return length;
    }
    private static void FeedInput()
    {
        if (keyboard == null || !keyboard.added) return;
        InputState.Change(keyboard, new KeyboardState(keys));
        keyboard.MakeCurrent();
        mouse.MakeCurrent();
    }

    private static void Capture(string name)
    {
        var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var active = RenderTexture.active;
        var original = camera.targetTexture;
        bool async = ShaderUtil.allowAsyncCompilation;
        try
        {
            ShaderUtil.allowAsyncCompilation = false;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(output, name), texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = original;
            RenderTexture.active = active;
            ShaderUtil.allowAsyncCompilation = async;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void CheckLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true;
    }
    private static void Fail(Exception exception)
    {
        failed = true;
        Debug.LogException(exception);
        if (camera && player)
        {
            Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit, 3.2f, ~0, QueryTriggerInteraction.Ignore);
            Debug.Log($"ScenarioReview contexto: camera={camera.transform.position}, direcao={camera.transform.forward}, colisao={(hit.collider ? hit.collider.name : "nenhuma")}, jogadorAtivo={player.enabled}, jogando={director.IsPlaying}, teclado={Keyboard.current?.name}, mouse={Mouse.current?.name}.");
            Capture("failure.png");
        }
        Finish();
    }
    private static void Finish()
    {
        SessionState.SetBool("EscapeTheDark.ScenarioReview", false);
        EditorApplication.update -= Tick;
        InputSystem.onAfterUpdate -= FeedInput;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        Application.logMessageReceived -= CheckLog;
        File.WriteAllLines(Path.Combine(output, "checks.txt"), passed);
        Debug.Log(failed ? "ScenarioReview: FAILED" : "ScenarioReview: PASS - percurso completo, bloqueios, atalho, pausa, vitoria e reinicio.");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
        else EditorApplication.ExitPlaymode();
    }
}
