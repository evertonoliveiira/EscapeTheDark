using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EscapeTheDark
{
    public sealed partial class GameDirector : MonoBehaviour
    {
        public static GameDirector Instance { get; private set; }
        public bool IsPlaying => state == GameState.Playing;
        public int FuseCount { get; private set; }

        [Header("Hospital Horror Pack")]
        [SerializeField] private Material hospitalFloor;
        [SerializeField] private Material hospitalWall;
        [SerializeField] private GameObject floorModule;
        [SerializeField] private GameObject damagedFloorModule;
        [SerializeField] private GameObject ceilingModule;
        [SerializeField] private GameObject wallModule;
        [SerializeField] private GameObject damagedWallModule;
        [SerializeField] private GameObject hospitalBed;
        [SerializeField] private GameObject medicalBox;
        [SerializeField] private GameObject hospitalLamp;
        [SerializeField] private GameObject hospitalColumn;
        [SerializeField] private Material hospitalLampOff;
        [SerializeField] private GameObject hospitalDoor;
        [Header("Creature and sound")]
        [SerializeField] private GameObject creaturePrefab;
        [SerializeField] private AudioClip[] walkSteps;
        [SerializeField] private AudioClip[] runSteps;
        [SerializeField] private AudioClip smallGoreHit;
        [SerializeField] private AudioClip mediumGoreHit;
        [SerializeField] private AudioClip largeGoreHit;

        private enum GameState { Menu, Playing, Paused, Reading, Keypad, Won, Lost }
        private GameState state = GameState.Menu;
        private static bool startImmediately;
        private readonly Color wallColor = new Color(0.16f, 0.20f, 0.22f);
        private readonly Color metalColor = new Color(0.27f, 0.32f, 0.33f);
        private readonly Color floorColor = new Color(0.12f, 0.15f, 0.16f);
        private readonly Color accentColor = new Color(0.12f, 0.65f, 0.68f);
        private readonly Color dangerColor = new Color(0.85f, 0.12f, 0.13f);
        public float ThreatLevel => enemy && player
            ? Mathf.Clamp01(1f - Vector3.Distance(enemy.transform.position, player.transform.position) / 8f)
            : 0f;
        private Transform level;
        private Material frameMaterial;
        private PlayerMotor player;
        private EnemyHunter enemy;
        private ExitDoor exitDoor;
        private Light[] roomLights;
        private float[] roomLightBase;
        private bool[] roomLightEnabled;
        private readonly Renderer[][] poweredLampRenderers = new Renderer[9][];
        private readonly Material[][][] poweredLampMaterials = new Material[9][][];
        private AudioSource ambience;
        private AudioSource effects;
        private AudioSource distantSteps;
        private float nextDistantStep;
        private int distantStepCount;
        private AudioClip pickupClip;
        private AudioClip scareClip;
        private AudioClip winClip;
        private string message;
        private float messageUntil;
        private GUIStyle titleStyle;
        private GUIStyle textStyle;
        private GUIStyle smallStyle;
        private GUIStyle buttonStyle;
        private GUIStyle counterStyle;
        private VolumeProfile visualProfile;
        private float controlsUntil;

        private void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            BuildLevel();
            BuildScenario();
            BuildNavigation();
            BuildPlayer();
            BuildAudio();
            SetPlaying(false);
            if (startImmediately)
            {
                startImmediately = false;
                Begin();
            }
        }

        private void Update()
        {
            AnimateLights();
            PlayDistantSteps();
            if (UnityEngine.InputSystem.Keyboard.current == null) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (state == GameState.Reading || state == GameState.Keypad)
            {
                UpdateDocumentInput(keyboard);
                return;
            }
            if (state == GameState.Playing && keyboard.escapeKey.wasPressedThisFrame)
            {
                state = GameState.Paused;
                SetPlaying(false);
            }
            else if (state == GameState.Paused && keyboard.escapeKey.wasPressedThisFrame)
            {
                state = GameState.Playing;
                SetPlaying(true);
            }
            else if ((state == GameState.Won || state == GameState.Lost) && keyboard.rKey.wasPressedThisFrame)
            {
                Restart();
            }
        }

        private void OnDestroy()
        {
            if (visualProfile) Destroy(visualProfile);
        }

        private void BuildLevel()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Custom;
            // Um pouco de luz indireta fria revela apenas silhuetas fora do feixe.
            Color darkness = new Color(0.025f, 0.032f, 0.045f);
            RenderSettings.ambientLight = darkness;
            RenderSettings.ambientIntensity = 1;
            RenderSettings.reflectionIntensity = 0;
            // Custom mantem os SH definidos aqui; o modo Flat pode substitui-los durante a atualizacao de GI.
            var ambientProbe = new SphericalHarmonicsL2();
            ambientProbe.AddAmbientLight(darkness);
            RenderSettings.ambientProbe = ambientProbe;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.002f, 0.004f, 0.007f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.022f;

            level = new GameObject("Hospital abandonado").transform;
            var wall = hospitalWall ? hospitalWall : MaterialFor(wallColor);
            var floor = hospitalFloor ? hospitalFloor : MaterialFor(floorColor);
            var metal = MaterialFor(metalColor);
            frameMaterial = MaterialFor(new Color(0.10f, 0.13f, 0.14f));
            var stripe = MaterialFor(new Color(0.24f, 0.23f, 0.18f));
            roomLights = new Light[9];
            roomLightBase = new float[9];
            roomLightEnabled = new bool[9];

            for (int z = 0; z < 3; z++)
            for (int x = 0; x < 3; x++)
            {
                Vector3 c = Cell(x, z);
                var floorCollision = Cube("Piso - colisao", c + new Vector3(0, -0.16f, 0), new Vector3(8, 0.3f, 8), floor, level);
                var ceilingCollision = Cube("Teto - colisao", c + new Vector3(0, 3.25f, 0), new Vector3(8, 0.25f, 8), wall, level);
                if (floorModule && ceilingModule)
                {
                    floorCollision.GetComponent<Renderer>().enabled = false;
                    ceilingCollision.GetComponent<Renderer>().enabled = false;
                    BuildRoomSurfaces(c, z * 3 + x);
                }
                else Cube("Faixa de piso", c + new Vector3(0, 0.006f, -3.5f), new Vector3(7.8f, 0.012f, 0.08f), stripe, level);

                // Cada parede aparece uma vez. A abertura central tem 2,8 m.
                if (z == 0) HorizontalWall(c + new Vector3(0, 0, -4), false, wall);
                if (x == 0) VerticalWall(c + new Vector3(-4, 0, 0), false, wall);
                HorizontalWall(c + new Vector3(0, 0, 4), z == 2 ? x == 1 : VerticalConnection(x, z), wall);
                VerticalWall(c + new Vector3(4, 0, 0), x < 2 && HorizontalConnection(x, z), wall);

                // Salas vazias, enfermarias e deposito criam uma distribuicao legivel.
                int n = z * 3 + x;
                if (n == 0 || n == 2 || n == 5 || n == 6 || n == 8)
                {
                    Vector3 bedPosition = c + new Vector3(n % 2 == 0 ? -2.2f : 2.2f, 0, n % 3 == 0 ? 2f : -2f);
                    if (hospitalBed) PlaceHospitalProp(hospitalBed, bedPosition, n % 2 == 0 ? 0 : 90, 2.5f);
                    else Cube("Maca", bedPosition + Vector3.up * 0.45f, new Vector3(1.5f, 0.9f, 2.3f), metal, level);
                }
                if (medicalBox && (n == 0 || n == 3 || n == 5 || n == 7))
                    PlaceHospitalProp(medicalBox, c + new Vector3(3.1f, 0, n % 2 == 0 ? -2.5f : 2.5f), n == 3 ? 90 : 0, 0.85f);
                if (medicalBox && (n == 3 || n == 7))
                    PlaceHospitalProp(medicalBox, c + new Vector3(-2.8f, 0, -2.5f), 90, 0.85f);
                if (n == 3 || n == 7)
                    Cube("Armario de suprimentos", c + new Vector3(-3.35f, 0.95f, 2.45f), new Vector3(0.7f, 1.9f, 1.25f), metal, level);
                if (hospitalColumn && (n == 1 || n == 4 || n == 7))
                {
                    PlaceHospitalProp(hospitalColumn, c + new Vector3(-3.72f, 0, -3.72f), 0, 0.55f);
                    PlaceHospitalProp(hospitalColumn, c + new Vector3(3.72f, 0, 3.72f), 0, 0.55f);
                }
                var lamp = new GameObject("Lampada " + n);
                lamp.transform.SetParent(level);
                lamp.transform.position = c + new Vector3(0, 2.9f, 0);
                if (hospitalLamp)
                {
                    GameObject lampVisual = PlaceHospitalProp(hospitalLamp, lamp.transform.position, 0, 1.1f, false);
                    poweredLampRenderers[n] = lampVisual.GetComponentsInChildren<Renderer>();
                    poweredLampMaterials[n] = new Material[poweredLampRenderers[n].Length][];
                    for (int renderer = 0; renderer < poweredLampRenderers[n].Length; renderer++)
                        poweredLampMaterials[n][renderer] = poweredLampRenderers[n][renderer].sharedMaterials;
                    if (hospitalLampOff && n != 1 && n != 4 && n != 6)
                        foreach (var renderer in lampVisual.GetComponentsInChildren<Renderer>())
                            renderer.sharedMaterial = hospitalLampOff;
                }
                else Cube("Luminaria", lamp.transform.position, new Vector3(1.1f, 0.08f, 0.35f), MaterialFor(accentColor, true), level);
                Light light = lamp.AddComponent<Light>();
                // Fonte abaixo do difusor: o proprio modelo da luminaria nao bloqueia a luz.
                lamp.transform.position -= Vector3.up * 0.17f;
                lamp.transform.rotation = Quaternion.Euler(90, 0, 0);
                light.type = LightType.Spot;
                light.spotAngle = 78f;
                light.innerSpotAngle = 52f;
                light.range = 3.6f;
                // Pequenas ilhas de luz deixam o restante de cada sala dependente da lanterna.
                roomLightBase[n] = n == 1 ? 0.4f : n == 4 ? 0.16f : n == 6 ? 0.12f : 0f;
                roomLightEnabled[n] = roomLightBase[n] > 0;
                light.enabled = roomLightEnabled[n];
                light.intensity = roomLightBase[n];
                light.color = n == 6 ? new Color(1f, 0.19f, 0.12f) :
                    n == 1 ? new Color(1f, 0.88f, 0.70f) : new Color(0.78f, 0.89f, 0.94f);
                light.shadows = LightShadows.Soft;
                light.shadowBias = 0.045f;
                light.shadowNormalBias = 0.12f;
                var lightData = light.GetUniversalAdditionalLightData();
                lightData.usePipelineSettings = false;
                lightData.additionalLightsShadowResolutionTier = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow;
                roomLights[n] = light;
            }

            // Marcas proximas da saida orientam o retorno sem um minimapa.
            Cube("Sinal da saida", new Vector3(0, 2.55f, 11.78f), new Vector3(1.6f, 0.25f, 0.08f), MaterialFor(dangerColor, true), level);
            var doorObject = Cube("Porta de saida", new Vector3(0, 1.5f, 12), new Vector3(2.7f, 3, 0.3f), metal, level);
            exitDoor = doorObject.AddComponent<ExitDoor>();
            exitDoor.SetIndicator(Cube("Indicador", new Vector3(0, 2.1f, 11.81f), new Vector3(0.8f, 0.12f, 0.06f),
                MaterialFor(dangerColor, true), doorObject.transform).GetComponent<Renderer>());
            level.gameObject.AddComponent<HospitalVisuals>().Build(level);
        }

        private void AnimateLights()
        {
            if (roomLights == null || roomLightBase == null) return;
            for (int i = 0; i < roomLights.Length; i++)
            {
                if (!roomLights[i] || !roomLightEnabled[i]) continue;
                float noise = Mathf.PerlinNoise(Time.time * 1.6f + i * 7.3f, 0.37f);
                float phase = Mathf.Repeat(Time.time + i * 1.37f, 13f);
                bool faulty = (i == 4 || i == 8) && phase > 11.8f && phase < 12.3f;
                roomLights[i].intensity = roomLightBase[i] * (faulty
                    ? Mathf.Lerp(0.12f, 0.7f, Mathf.PerlinNoise(Time.time * 35f, i))
                    : Mathf.Lerp(0.97f, 1f, noise));
            }
        }

        private void BuildNavigation()
        {
            var surface = level.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
        }

        private void BuildPlayer()
        {
            var body = new GameObject("Jogador");
            body.transform.position = Cell(1, 0) + Vector3.up * 0.1f;
            body.transform.rotation = Quaternion.Euler(0, 90, 0);
            var controller = body.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0, 0.9f, 0);
            controller.stepOffset = 0.3f;
            var cameraObject = new GameObject("Camera e lanterna");
            cameraObject.transform.SetParent(body.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 1.62f, 0);
            var cam = cameraObject.AddComponent<Camera>();
            cam.fieldOfView = 72;
            cam.nearClipPlane = 0.04f;
            cam.farClipPlane = 45;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.006f, 0.012f, 0.018f);
            cameraObject.AddComponent<AudioListener>();
            BuildVisualProfile(cam);
            var flashlightBody = new GameObject("Lanterna na mao");
            flashlightBody.transform.SetParent(cameraObject.transform, false);
            flashlightBody.transform.localPosition = new Vector3(0.28f, -0.29f, 0.27f);
            flashlightBody.transform.localScale = Vector3.one * 0.8f;
            var casing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            casing.name = "Corpo da lanterna";
            casing.transform.SetParent(flashlightBody.transform, false);
            casing.transform.localPosition = new Vector3(0, 0, 0.18f);
            casing.transform.localRotation = Quaternion.Euler(90, 0, 0);
            casing.transform.localScale = new Vector3(0.105f, 0.18f, 0.105f);
            casing.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(0.08f, 0.10f, 0.11f));
            casing.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(casing.GetComponent<Collider>());
            // Aros, empunhadura e interruptor dao escala ao objeto em primeira pessoa.
            var rubber = MaterialFor(new Color(0.035f, 0.045f, 0.047f));
            var rim = MaterialFor(new Color(0.26f, 0.30f, 0.29f));
            for (int i = 0; i < 7; i++)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Aro da empunhadura";
                ring.transform.SetParent(flashlightBody.transform, false);
                ring.transform.localPosition = new Vector3(0, 0, 0.045f + i * 0.035f);
                ring.transform.localRotation = Quaternion.Euler(90, 0, 0);
                ring.transform.localScale = new Vector3(0.113f, 0.004f, 0.113f);
                ring.GetComponent<Renderer>().sharedMaterial = rubber;
                ring.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                ring.GetComponent<Collider>().enabled = false;
                Destroy(ring.GetComponent<Collider>());
            }
            var bezel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bezel.name = "Aro metalico da lente";
            bezel.transform.SetParent(flashlightBody.transform, false);
            bezel.transform.localPosition = new Vector3(0, 0, 0.36f);
            bezel.transform.localRotation = Quaternion.Euler(90, 0, 0);
            bezel.transform.localScale = new Vector3(0.132f, 0.025f, 0.132f);
            bezel.GetComponent<Renderer>().sharedMaterial = rim;
            bezel.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            bezel.GetComponent<Collider>().enabled = false;
            Destroy(bezel.GetComponent<Collider>());
            var button = Cube("Interruptor da lanterna", Vector3.zero, new Vector3(0.035f, 0.013f, 0.065f), rubber, null);
            button.transform.SetParent(flashlightBody.transform, false);
            button.transform.localPosition = new Vector3(0, 0.054f, 0.23f);
            button.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            button.GetComponent<Collider>().enabled = false;
            Destroy(button.GetComponent<Collider>());
            var lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lens.name = "Lente da lanterna";
            lens.transform.SetParent(flashlightBody.transform, false);
            lens.transform.localPosition = new Vector3(0, 0, 0.38f);
            lens.transform.localRotation = Quaternion.Euler(90, 0, 0);
            lens.transform.localScale = new Vector3(0.12f, 0.018f, 0.12f);
            lens.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(0.25f, 0.36f, 0.39f), true);
            lens.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Destroy(lens.GetComponent<Collider>());
            var beamObject = new GameObject("Feixe da lanterna");
            beamObject.transform.SetParent(flashlightBody.transform, false);
            beamObject.transform.localPosition = new Vector3(0, 0, 0.42f);
            beamObject.transform.localRotation = Quaternion.Euler(-1.5f, -3f, 0);
            var beam = beamObject.AddComponent<Light>();
            beam.type = LightType.Spot;
            beam.range = 14;
            beam.spotAngle = 48;
            beam.innerSpotAngle = 27;
            beam.intensity = 5f;
            beam.shadows = LightShadows.Soft;
            beam.shadowStrength = 0.85f;
            beam.color = new Color(0.75f, 0.86f, 0.95f);
            player = body.AddComponent<PlayerMotor>();
            player.Configure(cam, beam);
            player.SetFlashlightVisual(lens.GetComponent<Renderer>());
            player.SetFlashlightRig(flashlightBody.transform);
            var flashlightControl = body.AddComponent<FlashlightController>();
            flashlightControl.Configure(cam, flashlightBody.transform, beam, lens.GetComponent<Renderer>());
            player.SetFlashlightController(flashlightControl);
            player.SetFootsteps(walkSteps, runSteps);
        }

        private void BuildVisualProfile(Camera cam)
        {
            var cameraData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (!cameraData) cameraData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            var volumeObject = new GameObject("Tratamento da imagem");
            volumeObject.transform.SetParent(transform, false);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100;
            visualProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = visualProfile;
            var colorAdjustments = visualProfile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.Override(-0.5f);
            colorAdjustments.contrast.Override(8f);
            colorAdjustments.saturation.Override(-12f);
            var vignetteEffect = visualProfile.Add<Vignette>(true);
            vignetteEffect.color.Override(Color.black);
            vignetteEffect.intensity.Override(0.24f);
            vignetteEffect.smoothness.Override(0.45f);
            var bloom = visualProfile.Add<Bloom>(true);
            bloom.threshold.Override(1.2f);
            bloom.intensity.Override(0.10f);
            bloom.scatter.Override(0.55f);
            var tonemapping = visualProfile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
        }

        private FusePickup AddFuse(Vector3 position, HospitalCircuit circuit)
        {
            var body = Cube("Fusivel", position, new Vector3(0.22f, 0.62f, 0.22f), MaterialFor(accentColor, true), null);
            body.GetComponent<Renderer>().enabled = false;
            var pickup = body.AddComponent<FusePickup>();
            pickup.Configure(circuit);
            var caps = MaterialFor(new Color(0.36f, 0.39f, 0.36f));
            caps.SetFloat("_Metallic", 0.65f);
            caps.SetFloat("_Smoothness", 0.6f);
            foreach (int side in new[] { -1, 1 })
            {
                Vector3 capPosition = position + Vector3.up * side * 0.37f;
                // Mantem a geometria de interacao original; apenas a representacao muda.
                var cap = Cube("Contato - interacao", capPosition, new Vector3(0.3f, 0.12f, 0.3f), caps, body.transform);
                cap.GetComponent<Renderer>().enabled = false;
                VisualCylinder("Contato metalico", capPosition, new Vector3(0.23f, 0.06f, 0.23f), caps, body.transform);
            }
            var ceramic = MaterialFor(new Color(0.21f, 0.34f, 0.31f));
            ceramic.SetFloat("_Smoothness", 0.55f);
            VisualCylinder("Corpo de ceramica", position, new Vector3(0.16f, 0.31f, 0.16f), ceramic, body.transform);
            var band = MaterialFor(new Color(0.11f, 0.34f, 0.28f));
            for (int i = -1; i <= 1; i++)
                VisualCylinder("Faixa do fusivel", position + Vector3.up * i * 0.17f,
                    new Vector3(0.168f, 0.015f, 0.168f), band, body.transform);
            return pickup;
        }

        private static void VisualCylinder(string name, Vector3 position, Vector3 size, Material material, Transform parent)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = size;
            obj.transform.SetParent(parent, true);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            obj.GetComponent<Collider>().enabled = false;
            Destroy(obj.GetComponent<Collider>());
        }

        private void BuildAudio()
        {
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.clip = Tone("Ambiente", 4, 0.045f, 54, true);
            ambience.loop = true;
            ambience.Play();
            effects = gameObject.AddComponent<AudioSource>();
            pickupClip = Tone("Coleta", 0.35f, 0.16f, 560, false);
            scareClip = mediumGoreHit ? mediumGoreHit : Tone("Susto", 0.9f, 0.27f, 85, true);
            winClip = Tone("Saida", 0.8f, 0.18f, 440, false);
            if (walkSteps != null && walkSteps.Length > 0)
            {
                var source = new GameObject("Passos ao longe");
                distantSteps = source.AddComponent<AudioSource>();
                distantSteps.playOnAwake = false;
                distantSteps.spatialBlend = 1f;
                distantSteps.minDistance = 1.5f;
                distantSteps.maxDistance = 18f;
                distantSteps.volume = 0.18f;
                nextDistantStep = Time.time + Random.Range(14f, 23f);
            }
        }

        private void PlayDistantSteps()
        {
            if (!IsPlaying || !distantSteps || Time.time < nextDistantStep) return;
            if (distantStepCount == 0)
            {
                Vector3 position = Cell(Random.Range(0, 3), Random.Range(0, 3));
                for (int attempt = 0; attempt < 5 && Vector3.Distance(position, player.transform.position) < 10f; attempt++)
                    position = Cell(Random.Range(0, 3), Random.Range(0, 3));
                distantSteps.transform.position = position + Vector3.up * 0.2f;
                distantStepCount = Random.Range(2, 4);
            }
            AudioClip clip = walkSteps[Random.Range(0, walkSteps.Length)];
            if (clip) distantSteps.PlayOneShot(clip);
            distantStepCount--;
            nextDistantStep = Time.time + (distantStepCount > 0 ? 0.55f : Random.Range(16f, 29f));
        }

        public void CollectFuse(FusePickup fuse)
        {
            if (!IsPlaying || !fuse || !fuse.Available || fuse.Collected || collectedFuses[(int)fuse.Circuit]) return;
            if (fuse.Circuit == HospitalCircuit.Ward && !IsCircuitInstalled(HospitalCircuit.Reception)) return;
            if (fuse.Circuit == HospitalCircuit.Exit && !IsCircuitInstalled(HospitalCircuit.Ward)) return;
            fuse.MarkCollected();
            collectedFuses[(int)fuse.Circuit] = true;
            Destroy(fuse.gameObject);
            FuseCount++;
            effects.PlayOneShot(pickupClip);
            if (FuseCount == 1)
            {
                ShowMessage("Reserva encontrada. Instale o fusivel no quadro da recepcao.", 5);
                StartCoroutine(Flicker());
                StartCoroutine(FirstWarning());
            }
            else if (FuseCount == 2)
            {
                ShowMessage("Voce nao esta sozinho. Restaure a enfermaria no quadro da recepcao.", 6);
                SpawnEnemy();
                effects.PlayOneShot(scareClip);
            }
            else
            {
                ShowMessage("Ultima peca retirada. Volte ao quadro e restaure o circuito da saida.", 6);
            }
        }

        private IEnumerator Flicker()
        {
            effects.PlayOneShot(smallGoreHit ? smallGoreHit : scareClip, 0.7f);
            for (int i = 0; i < 6; i++)
            {
                for (int n = 0; n < roomLights.Length; n++)
                    if (roomLights[n]) roomLights[n].enabled = roomLightEnabled[n] && i % 2 == 0;
                yield return WaitWhilePlaying(i % 2 == 0 ? 0.11f : 0.25f);
            }
            for (int n = 0; n < roomLights.Length; n++)
                if (roomLights[n]) roomLights[n].enabled = roomLightEnabled[n];
        }

        private void SpawnEnemy()
        {
            if (enemy) return;
            var root = new GameObject("Criatura");
            Vector3 spawn = Cell(2, 2) + new Vector3(0, 0, -2f);
            if (player && Vector3.Distance(spawn, player.transform.position) < 10f)
                spawn = Vector3.Distance(Cell(0, 2), player.transform.position) > Vector3.Distance(Cell(0, 0), player.transform.position)
                    ? Cell(0, 2) : Cell(0, 0);
            root.transform.position = spawn;
            root.transform.rotation = Quaternion.Euler(0, 180, 0);
            var agent = root.AddComponent<NavMeshAgent>();
            agent.speed = 1.35f;
            agent.angularSpeed = 400;
            agent.acceleration = 8;
            agent.stoppingDistance = 0.55f;
            agent.radius = 0.4f;
            agent.height = 2.1f;
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(root.transform.position, out NavMeshHit nearest, 2f, NavMesh.AllAreas))
                agent.Warp(nearest.position);
            if (creaturePrefab)
            {
                var visual = Instantiate(creaturePrefab, root.transform);
                visual.name = "Criatura - Creep Horror Creature";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                foreach (var collider in visual.GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                    Destroy(collider);
                }
                ConvertCreatureMaterials(visual);
                root.AddComponent<CreaturePresentation>().Configure(agent, visual);
            }
            else
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Silhueta";
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0, 1.1f, 0);
                body.transform.localScale = new Vector3(0.85f, 1.1f, 0.85f);
                body.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(0.035f, 0.04f, 0.045f));
                Destroy(body.GetComponent<Collider>());
            }
            var glow = root.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.range = 0.9f;
            glow.intensity = 0.08f;
            glow.color = dangerColor;
            enemy = root.AddComponent<EnemyHunter>();
            enemy.Configure(agent, player, new[] { Cell(2, 2), Cell(2, 1), Cell(1, 1), Cell(1, 2) }, 2.3f);
            enemy.SetFootsteps(walkSteps, runSteps);
        }

        public void ReachExit()
        {
            if (!IsPlaying || InstalledCircuitCount < 3 || !exitDoor.IsUnlocked) return;
            state = GameState.Won;
            SetPlaying(false);
            effects.PlayOneShot(winClip);
        }

        public void Caught()
        {
            if (!IsPlaying) return;
            state = GameState.Lost;
            SetPlaying(false);
            effects.PlayOneShot(largeGoreHit ? largeGoreHit : scareClip);
        }

        public void ShowMessage(string value, float seconds = 3)
        {
            message = value;
            messageUntil = Time.time + seconds;
        }

        private void Begin()
        {
            state = GameState.Playing;
            SetPlaying(true);
            controlsUntil = Time.time + 12f;
            distantStepCount = 0;
            nextDistantStep = Time.time + Random.Range(11f, 18f);
            ShowMessage(HasFlashlightBattery ? "A saida esta sem energia. Examine o quadro de emergencia na recepcao." :
                "Esta escuro. Tente ligar a lanterna com F e examine o balcao da recepcao.", 6);
        }

        private void SetPlaying(bool enabled)
        {
            Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !enabled;
            if (player) player.enabled = enabled;
            if (enemy)
            {
                enemy.SetSuspended(!enabled);
                enemy.enabled = enabled;
            }
        }

        private void Restart()
        {
            startImmediately = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnGUI()
        {
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            EnsureStyles();
            if (state == GameState.Reading || state == GameState.Keypad)
            {
                DrawDocument();
                return;
            }
            if (state == GameState.Playing || state == GameState.Paused)
            {
                DrawRect(new Rect(28, 25, 280, 84), new Color(0.01f, 0.025f, 0.03f, 0.83f));
                GUI.Label(new Rect(42, 30, 254, 40), $"FUSIVEIS   {FuseCount} / 3", counterStyle);
                GUI.Label(new Rect(44, 72, 250, 28), $"CIRCUITOS ATIVOS   {InstalledCircuitCount} / 3", smallStyle);
                DrawObjective();
                if (Time.time < controlsUntil)
                    GUI.Label(new Rect(31, 680, 750, 25), "WASD mover   |   Shift correr   |   F lanterna   |   E interagir   |   Esc pausar", smallStyle);
                DrawRect(new Rect(637, 353, 6, 14), Color.white);
                DrawRect(new Rect(633, 357, 14, 6), Color.white);
                if (Time.time < messageUntil)
                {
                    DrawTextPanel(new Rect(315, 95, 650, 50), message, textStyle, new Vector2(14, 12));
                }
                if (state == GameState.Playing && !string.IsNullOrEmpty(player.Prompt))
                {
                    DrawTextPanel(new Rect(420, 564, 500, 74), player.Prompt, textStyle, new Vector2(14, 10), true);
                    if (player.InteractionProgress > 0)
                    {
                        DrawRect(new Rect(450, 645, 440, 5), new Color(0.10f, 0.16f, 0.17f));
                        DrawRect(new Rect(450, 645, 440 * player.InteractionProgress, 5), accentColor);
                    }
                }
            }

            if (state == GameState.Playing) return;
            DrawRect(new Rect(0, 0, 1280, 720), new Color(0, 0.015f, 0.02f, 0.82f));
            string heading = state == GameState.Menu ? "ESCAPE THE DARK" :
                             state == GameState.Paused ? "PAUSADO" :
                             state == GameState.Won ? "VOCE ESCAPOU" : "VOCE FOI ENCONTRADO";
            GUI.Label(new Rect(150, 145, 980, 100), heading, titleStyle);
            string subtitle = state == GameState.Menu ? "Investigue o hospital. Restaure tres circuitos. Encontre a saida." :
                              state == GameState.Won ? "A porta abriu antes que a criatura chegasse." :
                              state == GameState.Lost ? "Tente outra rota e use a corrida para fugir." : "A criatura espera no escuro.";
            GUI.Label(new Rect(240, 265, 800, 55), subtitle, textStyle);
            if (state == GameState.Menu)
            {
                if (GUI.Button(new Rect(490, 390, 300, 62), "JOGAR", buttonStyle)) Begin();
                if (GUI.Button(new Rect(490, 468, 300, 62), "SAIR", buttonStyle)) Application.Quit();
            }
            else if (state == GameState.Paused)
            {
                if (GUI.Button(new Rect(490, 390, 300, 62), "CONTINUAR", buttonStyle)) { state = GameState.Playing; SetPlaying(true); }
                if (GUI.Button(new Rect(490, 468, 300, 62), "REINICIAR", buttonStyle)) Restart();
            }
            else
            {
                if (GUI.Button(new Rect(490, 390, 300, 62), "JOGAR NOVAMENTE", buttonStyle)) Restart();
                if (GUI.Button(new Rect(490, 468, 300, 62), "MENU", buttonStyle)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 62, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(0.84f, 0.94f, 0.94f);
            textStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 23, wordWrap = true };
            textStyle.normal.textColor = new Color(0.78f, 0.87f, 0.87f);
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            smallStyle.normal.textColor = new Color(0.67f, 0.77f, 0.78f);
            counterStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            counterStyle.normal.textColor = new Color(0.42f, 0.9f, 0.88f);
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 27, fontStyle = FontStyle.Bold };
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = new Color(0.42f, 0.9f, 0.88f);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color before = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = before;
        }

        private static void DrawTextPanel(Rect area, string value, GUIStyle style, Vector2 padding, bool anchorBottom = false)
        {
            // Mede com a mesma fonte e largura usadas no desenho, incluindo quebras de linha.
            float textWidth = area.width - padding.x * 2;
            float textHeight = Mathf.Ceil(style.CalcHeight(new GUIContent(value), textWidth)) + 2;
            float height = Mathf.Max(area.height, textHeight + padding.y * 2);
            var panel = new Rect(area.x, anchorBottom ? area.yMax - height : area.y, area.width, height);
            DrawRect(panel, new Color(0.01f, 0.025f, 0.03f, 0.85f));
            GUI.Label(new Rect(panel.x + padding.x, panel.y + padding.y, textWidth, panel.height - padding.y * 2), value, style);
        }

        private static Vector3 Cell(int x, int z) => new Vector3((x - 1) * 8, 0, (z - 1) * 8);
        private static bool HorizontalConnection(int x, int z) => !(z == 0 && x == 1) && !(z == 2 && x == 0);
        private static bool VerticalConnection(int x, int z) => true;

        private void BuildRoomSurfaces(Vector3 center, int room)
        {
            const float tileSize = 8f / 3f;
            for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
            {
                Vector3 offset = new Vector3((column - 1) * tileSize, 0, (row - 1) * tileSize);
                bool damaged = damagedFloorModule && (room * 7 + row * 3 + column) % 11 == 0;
                PlaceSurfaceTile(damaged ? damagedFloorModule : floorModule,
                    center + offset + Vector3.up * 0.006f, tileSize, "Piso modular");
                PlaceSurfaceTile(ceilingModule, center + offset + Vector3.up * 3.11f,
                    tileSize, "Teto modular");
            }
        }

        private void PlaceSurfaceTile(GameObject prefab, Vector3 center, float size, string name)
        {
            var tile = Instantiate(prefab, level);
            tile.name = name;
            tile.transform.localScale = new Vector3(size / 3f, 1, size / 3f);
            DisableDecorationColliders(tile);
            CenterVisual(tile, center);
        }

        private void WallSection(Vector3 center, float width, bool vertical, Material material)
        {
            Vector3 size = vertical ? new Vector3(0.3f, 3.1f, width) : new Vector3(width, 3.1f, 0.3f);
            var collision = Cube("Parede - colisao", center + Vector3.up * 1.55f, size, material, level);
            if (!wallModule) return;
            collision.GetComponent<Renderer>().enabled = false;
            int panels = width > 4f ? 3 : 1;
            for (int i = 0; i < panels; i++)
            {
                float along = ((i + 0.5f) / panels - 0.5f) * width;
                Vector3 panelCenter = center + (vertical ? Vector3.forward : Vector3.right) * along + Vector3.up * 1.55f;
                GameObject prefab = damagedWallModule && (Mathf.RoundToInt(center.x * 3 + center.z * 5) + i) % 7 == 0
                    ? damagedWallModule : wallModule;
                for (int side = -1; side <= 1; side += 2)
                {
                    var panel = Instantiate(prefab, level);
                    panel.name = "Parede modular";
                    panel.transform.rotation = Quaternion.Euler(0, (vertical ? 90 : 0) + (side == 1 ? 180 : 0), 0);
                    panel.transform.localScale = new Vector3(width / panels / 3f, 3.1f / 3f, 1);
                    DisableDecorationColliders(panel);
                    Vector3 facing = vertical ? Vector3.right : Vector3.forward;
                    CenterVisual(panel, panelCenter + facing * (side * 0.17f));
                }
            }
        }

        private static void DisableDecorationColliders(GameObject decoration)
        {
            foreach (var collider in decoration.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
                Destroy(collider);
            }
        }

        private static void CenterVisual(GameObject visual, Vector3 target)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { visual.transform.position = target; return; }
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            visual.transform.position += target - bounds.center;
        }

        private void HorizontalWall(Vector3 center, bool opening, Material material)
        {
            if (!opening) { WallSection(center, 8, false, material); return; }
            WallSection(center + Vector3.left * 2.7f, 2.6f, false, material);
            WallSection(center + Vector3.right * 2.7f, 2.6f, false, material);
            Cube("Viga", center + new Vector3(0, 2.85f, 0), new Vector3(2.8f, 0.5f, 0.3f), material, level);
            Cube("Batente esquerdo", center + new Vector3(-1.35f, 1.37f, 0), new Vector3(0.13f, 2.74f, 0.38f), frameMaterial, level);
            Cube("Batente direito", center + new Vector3(1.35f, 1.37f, 0), new Vector3(0.13f, 2.74f, 0.38f), frameMaterial, level);
            Cube("Batente superior", center + new Vector3(0, 2.75f, 0), new Vector3(2.8f, 0.13f, 0.38f), frameMaterial, level);
            if (hospitalDoor && ((Mathf.Abs(center.x - 8f) < 0.1f && Mathf.Abs(center.z - 4f) < 0.1f)
                || (Mathf.Abs(center.x + 8f) < 0.1f && Mathf.Abs(center.z + 4f) < 0.1f)))
                PlaceHospitalDoor(center);
        }

        private void VerticalWall(Vector3 center, bool opening, Material material)
        {
            if (!opening) { WallSection(center, 8, true, material); return; }
            WallSection(center + Vector3.back * 2.7f, 2.6f, true, material);
            WallSection(center + Vector3.forward * 2.7f, 2.6f, true, material);
            Cube("Viga", center + new Vector3(0, 2.85f, 0), new Vector3(0.3f, 0.5f, 2.8f), material, level);
            Cube("Batente baixo", center + new Vector3(0, 1.37f, -1.35f), new Vector3(0.38f, 2.74f, 0.13f), frameMaterial, level);
            Cube("Batente alto", center + new Vector3(0, 1.37f, 1.35f), new Vector3(0.38f, 2.74f, 0.13f), frameMaterial, level);
            Cube("Batente superior", center + new Vector3(0, 2.75f, 0), new Vector3(0.38f, 0.13f, 2.8f), frameMaterial, level);
        }

        private void PlaceHospitalDoor(Vector3 doorway)
        {
            var door = Instantiate(hospitalDoor, level);
            door.name = "Porta entreaberta";
            door.transform.rotation = Quaternion.Euler(0, 63f, 0);
            foreach (var collider in door.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
                Destroy(collider);
            }
            var renderers = door.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float scale = Mathf.Min(1.5f, 2.65f / Mathf.Max(0.01f, bounds.size.y),
                1.9f / Mathf.Max(0.01f, Mathf.Max(bounds.size.x, bounds.size.z)));
            door.transform.localScale *= scale;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Vector3 target = doorway + new Vector3(-0.92f, 0, 0.25f);
            door.transform.position += new Vector3(target.x - bounds.center.x, -bounds.min.y, target.z - bounds.center.z);
        }

        private static GameObject Cube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent) go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private GameObject PlaceHospitalProp(GameObject prefab, Vector3 position, float angle, float maxFootprint, bool onFloor = true)
        {
            var prop = Instantiate(prefab, level);
            prop.transform.position = position;
            prop.transform.rotation = Quaternion.Euler(0, angle, 0);
            var renderers = prop.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return prop;
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint > 0.01f)
                prop.transform.localScale *= Mathf.Min(1.5f, maxFootprint / footprint);
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (onFloor) prop.transform.position += Vector3.up * (position.y - bounds.min.y);
            else prop.transform.position += Vector3.up * (position.y - bounds.center.y);
            if (onFloor && prop.GetComponentInChildren<Collider>() == null)
            {
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                var blocker = prop.AddComponent<BoxCollider>();
                blocker.center = prop.transform.InverseTransformPoint(bounds.center);
                Vector3 size = bounds.size;
                if (Mathf.Abs(Mathf.DeltaAngle(angle, 90)) < 1 || Mathf.Abs(Mathf.DeltaAngle(angle, 270)) < 1)
                    size = new Vector3(size.z, size.y, size.x);
                blocker.size = size / prop.transform.localScale.x;
            }
            return prop;
        }

        private static void ConvertCreatureMaterials(GameObject visual)
        {
            Shader urp = Shader.Find("Universal Render Pipeline/Lit");
            if (!urp) return;
            var converted = new Dictionary<Material, Material>();
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (!source || source.shader == urp) continue;
                    if (!converted.TryGetValue(source, out Material copy))
                    {
                        copy = new Material(urp) { name = source.name + " (URP)" };
                        if (source.HasProperty("_Color")) copy.SetColor("_BaseColor", source.GetColor("_Color"));
                        if (source.HasProperty("_MainTex")) copy.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
                        if (source.HasProperty("_BumpMap") && source.GetTexture("_BumpMap"))
                        {
                            copy.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
                            copy.EnableKeyword("_NORMALMAP");
                        }
                        converted.Add(source, copy);
                    }
                    materials[i] = copy;
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static Material MaterialFor(Color color, bool glowing = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) shader = Shader.Find("Standard");
            var material = new Material(shader);
            material.color = color;
            if (glowing)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.5f);
            }
            return material;
        }

        private static AudioClip Tone(string name, float duration, float volume, float frequency, bool ominous)
        {
            const int rate = 22050;
            int length = Mathf.RoundToInt(rate * duration);
            float[] samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)rate;
                float envelope = ominous ? Mathf.Min(1, t * 9) * (1 - t / duration) : Mathf.Exp(-t * 7);
                float wave = Mathf.Sin(2 * Mathf.PI * frequency * t) * 0.65f + Mathf.Sin(2 * Mathf.PI * frequency * 1.47f * t) * 0.35f;
                samples[i] = wave * volume * envelope;
            }
            var clip = AudioClip.Create(name, length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
