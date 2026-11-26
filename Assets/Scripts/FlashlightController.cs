using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EscapeTheDark
{
    // Iluminacao e apresentacao da lanterna, independentes do motor e da IA.
    public sealed class FlashlightController : MonoBehaviour
    {
        public bool IsOn { get; private set; }
        public bool HasBattery { get; private set; }
        public bool HasTriedEmptySwitch { get; private set; }
        public Light Beam => beam;
        public float WallRetraction => retraction;

        private Camera view;
        private Transform rig;
        private Transform housing;
        private Light beam;
        private Renderer lens;
        private MaterialPropertyBlock lensProperties;
        private Texture2D cookie;
        private AudioSource switchSource;
        private AudioClip switchClip;
        private Quaternion aim;
        private float output;
        private float retraction;
        private readonly RaycastHit[] contacts = new RaycastHit[16];
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        private const float Power = 22f;

        public void Configure(Camera camera, Transform presentationRig, Light source, Renderer glass)
        {
            view = camera;
            rig = presentationRig;
            beam = source;
            lens = glass;
            lensProperties = new MaterialPropertyBlock();
            // O motor anima o rig; este segundo pivô recolhe somente a lanterna junto das paredes.
            housing = new GameObject("Recuo junto a obstaculos").transform;
            housing.SetParent(rig, false);
            var children = new Transform[rig.childCount - 1];
            int index = 0;
            foreach (Transform child in rig)
                if (child != housing) children[index++] = child;
            foreach (var child in children) child.SetParent(housing, true);

            beam.type = LightType.Spot;
            beam.range = 18f;
            beam.spotAngle = 62f;
            beam.innerSpotAngle = 54f;
            beam.color = new Color(1f, 0.94f, 0.84f);
            beam.intensity = 0;
            beam.enabled = false;
            beam.shadows = LightShadows.Soft;
            beam.shadowStrength = 1f;
            beam.shadowBias = 0.035f;
            beam.shadowNormalBias = 0.08f;
            beam.shadowNearPlane = 0.03f;
            var additional = beam.GetUniversalAdditionalLightData();
            additional.usePipelineSettings = false;
            additional.additionalLightsShadowResolutionTier = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh;
            cookie = MakeCookie();
            beam.cookie = cookie;
            aim = view.transform.rotation;

            switchClip = MakeSwitchClick();
            switchSource = gameObject.AddComponent<AudioSource>();
            switchSource.playOnAwake = false;
            switchSource.spatialBlend = 0;
            switchSource.volume = 0.28f;
            UpdateLens();
        }

        public void SetOn(bool enabled, bool playSound = true)
        {
            if (enabled && !HasBattery)
            {
                HasTriedEmptySwitch = true;
                if (playSound)
                {
                    PlaySwitch(true);
                    if (GameDirector.Instance && GameDirector.Instance.IsPlaying)
                        GameDirector.Instance.ShowMessage("A lanterna esta sem pilha. Procure uma reserva no balcao da recepcao, junto a luz de emergencia.", 6);
                }
                return;
            }
            if (IsOn == enabled) return;
            IsOn = enabled;
            if (playSound) PlaySwitch(enabled);
        }

        public bool InsertBattery()
        {
            if (HasBattery || !beam) return false;
            HasBattery = true;
            SetOn(true);
            return true;
        }

        private void PlaySwitch(bool enabled)
        {
            if (switchSource && switchClip)
            {
                switchSource.pitch = enabled ? 1.08f : 0.94f;
                switchSource.PlayOneShot(switchClip);
            }
        }

        public void Toggle() => SetOn(!IsOn);

        private void LateUpdate()
        {
            if (!view || !beam || !housing) return;
            float dt = Time.deltaTime;
            float blend = 1f - Mathf.Exp(-dt * 16f);
            // A apresentacao fica parada quando o jogo pausa, mas F continua sendo controlado pelo jogador.
            bool playing = GameDirector.Instance && GameDirector.Instance.IsPlaying;
            if (playing) aim = Quaternion.Slerp(aim, view.transform.rotation, blend);

            float distance = FindObstacle(view.transform.position, view.transform.forward, 2.5f, 0.10f);
            float targetRetraction = 1f - Mathf.InverseLerp(0.38f, 1.10f, distance);
            retraction = Mathf.Lerp(retraction, targetRetraction, 1f - Mathf.Exp(-dt * 20f));
            housing.localPosition = new Vector3(-0.025f, -0.06f, -0.46f) * retraction;
            housing.localRotation = Quaternion.Euler(12f * retraction, 0, 0);

            // A origem nunca fica do outro lado de uma parede, mesmo durante uma virada rapida.
            Vector3 desiredOrigin = housing.TransformPoint(new Vector3(0, 0, 0.42f));
            Vector3 toOrigin = desiredOrigin - view.transform.position;
            float length = toOrigin.magnitude;
            if (length > 0.001f)
            {
                // Inclui a espessura dos revestimentos visuais, externos ao collider simplificado.
                float clearance = FindObstacle(view.transform.position, toOrigin / length, length + 0.22f, 0.04f);
                if (clearance < length + 0.22f)
                    desiredOrigin = view.transform.position + toOrigin.normalized * Mathf.Clamp(clearance - 0.22f, 0, length);
            }
            beam.transform.position = desiredOrigin;
            Vector3 aimPoint = view.transform.position + aim * Vector3.forward * 7f;
            beam.transform.rotation = Quaternion.LookRotation(aimPoint - desiredOrigin, view.transform.up);

            // Acionamento curto, sem piscar por proximidade do inimigo ou mudar a exposicao da sala.
            output = Mathf.MoveTowards(output, IsOn && HasBattery ? 1f : 0f, dt / (IsOn ? 0.075f : 0.045f));
            beam.enabled = output > 0.001f;
            float nearSurface = Mathf.Lerp(0.55f, 1f, Mathf.InverseLerp(0.3f, 1.8f, distance));
            beam.intensity = Power * output * nearSurface;
            UpdateLens();
        }

        private float FindObstacle(Vector3 origin, Vector3 direction, float distance, float radius)
        {
            int count = Physics.SphereCastNonAlloc(origin, radius, direction, contacts, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float nearest = distance;
            for (int i = 0; i < count; i++)
            {
                var collider = contacts[i].collider;
                if (!collider || collider.transform.IsChildOf(view.transform.root)) continue;
                nearest = Mathf.Min(nearest, contacts[i].distance);
            }
            return nearest;
        }

        private void UpdateLens()
        {
            if (!lens) return;
            // O vidro permanece presente com a luz apagada; somente a emissao desaparece.
            lens.GetPropertyBlock(lensProperties);
            lensProperties.SetColor(Emission, new Color(1f, 0.87f, 0.63f) * output * 1.6f);
            lens.SetPropertyBlock(lensProperties);
        }

        private static Texture2D MakeCookie()
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "Optica da lanterna - nucleo e halo",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                float radius = p.magnitude;
                float hotspot = Mathf.Exp(-radius * radius / 0.085f) * 0.78f;
                float spill = Mathf.Exp(-radius * radius / 0.55f) * 0.22f;
                float edge = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.76f, 1f, radius));
                float optics = 1f + Mathf.Sin(radius * 43f) * 0.018f +
                    (Mathf.PerlinNoise(p.x * 8f + 17f, p.y * 8f + 13f) - 0.5f) * 0.025f;
                float value = Mathf.Clamp01((hotspot + spill) * edge * optics);
                pixels[y * size + x] = new Color(value, value, value, value);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static AudioClip MakeSwitchClick()
        {
            const int rate = 22050;
            var samples = new float[1544];
            var noise = new System.Random(704);
            float previous = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                float raw = (float)noise.NextDouble() * 2f - 1f;
                float transient = Mathf.Exp(-t * 180f) + (t > 0.022f ? Mathf.Exp(-(t - 0.022f) * 230f) * 0.45f : 0);
                samples[i] = (raw - previous) * transient * 0.28f;
                previous = raw;
            }
            var clip = AudioClip.Create("Interruptor mecanico", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (cookie) Destroy(cookie);
            if (switchClip) Destroy(switchClip);
        }
    }
}
