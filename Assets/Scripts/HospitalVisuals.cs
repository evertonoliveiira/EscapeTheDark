using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EscapeTheDark
{
    // Cenografia sem colisores: a malha de navegacao e as rotas continuam sendo as do mapa original.
    public sealed class HospitalVisuals : MonoBehaviour
    {
        private static readonly string[] Names =
        {
            "TRIAGEM", "RECEPCAO", "OBSERVACAO", "FARMACIA", "ALA CENTRAL",
            "ENFERMARIA", "ISOLAMENTO", "MANUTENCAO", "CIRURGIA"
        };
        private static readonly string[] Details =
        {
            "01 / ATENDIMENTO", "HOSPITAL SANTA LUZIA", "03 / ACESSO RESTRITO",
            "04 / SUPRIMENTOS", "05 / INTERNACAO", "06 / LEITOS",
            "07 / RISCO BIOLOGICO", "08 / SERVICO", "09 / BLOCO CIRURGICO"
        };
        private static readonly Color[] Colors =
        {
            new Color(0.32f, 0.59f, 0.55f), new Color(0.64f, 0.51f, 0.28f),
            new Color(0.37f, 0.53f, 0.65f), new Color(0.42f, 0.59f, 0.43f),
            new Color(0.31f, 0.57f, 0.56f), new Color(0.35f, 0.51f, 0.62f),
            new Color(0.63f, 0.28f, 0.24f), new Color(0.65f, 0.49f, 0.26f),
            new Color(0.36f, 0.55f, 0.47f)
        };
        private readonly List<Object> owned = new List<Object>();
        private readonly Dictionary<Color, Material> painted = new Dictionary<Color, Material>();
        private Transform root;
        private Font font;
        private Material metal;
        private Material dark;
        private Material paper;
        private Material dirt;
        private Material dust;
        private Material lettering;
        private Material emergencyLettering;
        public Transform DecorationRoot => root;
        public Material SceneMaterial(Color color) => Paint(color);
        public TextMesh Label(Transform parent, string content, Vector3 position, float size, Color color) => Text(parent, content, position, size, color);

        public void Build(Transform hospital)
        {
            root = new GameObject("Cenografia - hospital Santa Luzia").transform;
            root.SetParent(hospital, false);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lettering = new Material(Shader.Find("EscapeTheDark/Hospital World Text"));
            lettering.mainTexture = font.material.mainTexture;
            owned.Add(lettering);
            emergencyLettering = new Material(lettering);
            emergencyLettering.SetFloat("_Emission", 0.06f);
            owned.Add(emergencyLettering);
            Font.textureRebuilt += RefreshFont;
            metal = Paint(new Color(0.19f, 0.23f, 0.23f));
            dark = Paint(new Color(0.055f, 0.075f, 0.075f));
            paper = Paint(new Color(0.56f, 0.53f, 0.43f));
            dirt = TransparentMaterial("Umidade", MakeTexture(false));
            dust = TransparentMaterial("Poeira", MakeTexture(true));

            // Os acabamentos seguem somente os trechos solidos das paredes, sem cobrir portas.
            foreach (var collider in hospital.GetComponentsInChildren<BoxCollider>())
                if (collider.name == "Parede - colisao") DressWall(collider.transform);

            for (int room = 0; room < 9; room++) DressRoom(room);
            ExitSign(new Vector3(0, 2.57f, 11.70f));
        }

        private void DressWall(Transform wall)
        {
            bool vertical = wall.localScale.x < wall.localScale.z;
            float width = vertical ? wall.localScale.z : wall.localScale.x;
            Vector3 normal = vertical ? Vector3.right : Vector3.forward;
            Vector3 along = vertical ? Vector3.forward : Vector3.right;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 face = wall.position + normal * (side * 0.215f);
                int x = Mathf.Clamp(Mathf.RoundToInt((face.x + 8f) / 8f), 0, 2);
                int z = Mathf.Clamp(Mathf.RoundToInt((face.z + 8f) / 8f), 0, 2);
                Color accent = Colors[z * 3 + x] * 0.55f;
                Box("Rodape", new Vector3(face.x, 0.11f, face.z),
                    vertical ? new Vector3(0.045f, 0.20f, width) : new Vector3(width, 0.20f, 0.045f), metal);
                Box("Faixa do setor", new Vector3(face.x, 1.12f, face.z),
                    vertical ? new Vector3(0.025f, 0.085f, width) : new Vector3(width, 0.085f, 0.025f), Paint(accent));
                Box("Protetor de parede", new Vector3(face.x, 0.78f, face.z),
                    vertical ? new Vector3(0.055f, 0.09f, width) : new Vector3(width, 0.09f, 0.055f), dark);
                if (width < 4f) continue;
                var pipe = Box("Tubulacao superior", new Vector3(face.x, 2.87f, face.z) + normal * side * 0.055f,
                    vertical ? new Vector3(0.065f, 0.065f, width) : new Vector3(width, 0.065f, 0.065f), metal);
                for (int i = -1; i <= 1; i++)
                    Box("Suporte de tubulacao", pipe.position + along * i * 2.3f,
                        vertical ? new Vector3(0.095f, 0.12f, 0.045f) : new Vector3(0.045f, 0.12f, 0.095f), dark);
            }
        }

        private void DressRoom(int room)
        {
            Vector3 center = new Vector3((room % 3 - 1) * 8, 0, (room / 3 - 1) * 8);
            Color accent = Colors[room];
            var sign = Group("Identificacao " + Names[room], center + new Vector3(-2.55f, 2.12f, 3.72f), 0);
            LocalBox(sign, "Placa", Vector3.zero, new Vector3(2.12f, 0.73f, 0.055f), dark);
            LocalBox(sign, "Cor do setor", new Vector3(-0.99f, 0, -0.037f), new Vector3(0.08f, 0.73f, 0.02f), Paint(accent));
            Text(sign, Names[room], new Vector3(0.03f, 0.10f, -0.04f), 0.085f, new Color(0.72f, 0.78f, 0.73f));
            Text(sign, Details[room], new Vector3(0.03f, -0.19f, -0.04f), 0.043f, accent);

            // Paineis rasos e detalhes junto as paredes preservam o espaco de circulacao.
            var board = Group("Quadro " + room, center + new Vector3(2.6f, 1.85f, -3.70f), 180);
            LocalBox(board, "Moldura", Vector3.zero, new Vector3(1.32f, 1.02f, 0.055f), metal);
            LocalBox(board, "Fundo", new Vector3(0, 0, -0.036f), new Vector3(1.23f, 0.93f, 0.015f), Paint(new Color(0.18f, 0.22f, 0.19f)));
            for (int sheet = 0; sheet < 3; sheet++)
            {
                Vector3 p = new Vector3((sheet - 1) * 0.35f, sheet == 1 ? -0.08f : 0.08f, -0.05f);
                var note = LocalBox(board, "Aviso", p, new Vector3(0.27f, 0.39f, 0.005f), paper);
                note.localRotation = Quaternion.Euler(0, 0, sheet * 4f - 3f);
                for (int line = 0; line < 4; line++)
                    LocalBox(note, "Linha impressa", new Vector3(0, 0.23f - line * 0.15f, -0.7f),
                        new Vector3(line == 0 ? 0.62f : 0.74f, 0.028f, 0.1f), dark);
            }

            Vent(center + new Vector3(-2.7f, 2.74f, -3.69f));
            WallStain(center + new Vector3(3.68f, 0.85f, 2.6f), -90, room);
            WallStain(center + new Vector3(-2.65f, 0.56f, -3.68f), 180, room + 9);
            FloorPapers(center, room);
            Dust(center, room);

            if (room == 1)
            {
                var welcome = Group("Identidade da recepcao", center + new Vector3(0, 2.15f, -3.69f), 180);
                Text(welcome, "+", new Vector3(0, 0.50f, 0), 0.28f, accent);
                Text(welcome, "SANTA LUZIA", new Vector3(0, -0.07f, 0), 0.13f, new Color(0.64f, 0.69f, 0.62f));
                Text(welcome, "CUIDAR E A NOSSA HISTORIA", new Vector3(0, -0.35f, 0), 0.043f, accent);
            }
            if (room == 3 || room == 7) SupplyShelves(center, room);
            if (room == 2 || room == 5 || room == 8) ClinicalPanel(center, accent);
            if (room == 6) IsolationWarning(center);

            // Luzes locais discretas dao profundidade sem iluminar todo o hospital.
            Vector3 fixture = center + new Vector3(3.65f, 2.38f, 2.65f);
            Box("Arandela - base", fixture, new Vector3(0.10f, 0.35f, 0.24f), metal);
            Color glow = room == 6 ? new Color(0.8f, 0.17f, 0.10f) :
                room == 1 || room == 7 ? new Color(0.8f, 0.52f, 0.24f) : new Color(0.30f, 0.58f, 0.59f);
            bool emergencyPowered = room == 1 || room == 4 || room == 6;
            Box("Arandela - difusor", fixture + Vector3.left * 0.065f,
                new Vector3(0.04f, 0.23f, 0.16f), emergencyPowered ? Glow(glow, 0.5f) : dark);
            var lightObject = new GameObject("Luz de referencia " + room);
            lightObject.transform.SetParent(root, false);
            lightObject.transform.position = fixture + Vector3.left * 0.2f;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            lightObject.transform.rotation = Quaternion.LookRotation(new Vector3(-1f, -0.35f, -0.7f));
            light.spotAngle = 72;
            light.innerSpotAngle = 45;
            light.color = glow;
            light.intensity = 0.16f;
            light.range = 2.1f;
            light.enabled = emergencyPowered;
            ConfigureFixtureShadows(light);

            // A identificacao do setor e tinta sobre a placa; precisa receber luz para ser lida.
        }

        private static void ConfigureFixtureShadows(Light light)
        {
            light.shadows = LightShadows.Hard;
            light.shadowBias = 0.045f;
            light.shadowNormalBias = 0.10f;
            light.shadowNearPlane = 0.035f;
            var data = light.GetUniversalAdditionalLightData();
            data.usePipelineSettings = false;
            data.additionalLightsShadowResolutionTier = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow;
        }

        private void Vent(Vector3 position)
        {
            var vent = Group("Grelha de ventilacao", position, 180);
            LocalBox(vent, "Moldura", Vector3.zero, new Vector3(0.95f, 0.35f, 0.06f), metal);
            LocalBox(vent, "Interior", new Vector3(0, 0, -0.036f), new Vector3(0.85f, 0.26f, 0.015f), dark);
            for (int i = 0; i < 5; i++)
                LocalBox(vent, "Aleta", new Vector3(0, -0.10f + i * 0.05f, -0.055f), new Vector3(0.84f, 0.02f, 0.04f), metal);
        }

        private void SupplyShelves(Vector3 center, int room)
        {
            var shelf = Group("Suprimentos de parede", center + new Vector3(-3.63f, 2.36f, 1.7f), -90);
            LocalBox(shelf, "Fundo", Vector3.zero, new Vector3(1.5f, 0.84f, 0.07f), metal);
            for (int row = 0; row < 2; row++)
            {
                LocalBox(shelf, "Prateleira", new Vector3(0, -0.39f + row * 0.43f, -0.11f), new Vector3(1.5f, 0.045f, 0.29f), metal);
                for (int item = 0; item < 5; item++)
                {
                    Vector3 p = new Vector3(-0.59f + item * 0.29f, -0.24f + row * 0.43f, -0.10f);
                    LocalBox(shelf, "Caixa de suprimentos", p, new Vector3(0.21f, 0.25f, 0.20f), paper);
                    LocalBox(shelf, "Rotulo", p + new Vector3(0, 0, -0.107f), new Vector3(0.15f, 0.045f, 0.008f), Paint(Colors[room]));
                }
            }
        }

        private void ClinicalPanel(Vector3 center, Color accent)
        {
            var panel = Group("Painel clinico", center + new Vector3(3.68f, 1.85f, 1.95f), 90);
            LocalBox(panel, "Base", Vector3.zero, new Vector3(1.35f, 0.20f, 0.06f), metal);
            for (int i = 0; i < 4; i++)
                LocalBox(panel, "Tomada", new Vector3(-0.45f + i * 0.30f, 0, -0.045f), new Vector3(0.10f, 0.11f, 0.025f), Paint(accent * 0.6f));
            Text(panel, "OXIGENIO  /  VACUO", new Vector3(0, 0.23f, -0.04f), 0.048f, accent);
        }

        private void IsolationWarning(Vector3 center)
        {
            var warning = Group("Aviso de isolamento", center + new Vector3(2.6f, 1.45f, 3.7f), 0);
            LocalBox(warning, "Placa amarela", Vector3.zero, new Vector3(1.28f, 0.60f, 0.03f), Paint(new Color(0.55f, 0.39f, 0.14f)));
            Text(warning, "!  ISOLAMENTO", new Vector3(0, 0.08f, -0.025f), 0.075f, new Color(0.09f, 0.075f, 0.04f));
            Text(warning, "MANTENHA DISTANCIA", new Vector3(0, -0.15f, -0.025f), 0.043f, new Color(0.10f, 0.08f, 0.04f));
        }

        private void ExitSign(Vector3 position)
        {
            var sign = Group("Sinalizacao da saida", position, 0);
            LocalBox(sign, "Fundo", Vector3.zero, new Vector3(1.85f, 0.40f, 0.06f), dark);
            Text(sign, "SAIDA  ^", new Vector3(0, 0, -0.045f), 0.12f, new Color(0.51f, 0.82f, 0.64f), true);
        }

        private void FloorPapers(Vector3 center, int room)
        {
            for (int i = 0; i < 5; i++)
            {
                float seed = room * 13.7f + i * 7.1f;
                Vector3 p = center + new Vector3(2.1f + Mathf.Sin(seed) * 0.65f, 0.025f + i * 0.0007f, -2.4f + Mathf.Cos(seed) * 0.75f);
                var sheet = Box("Prontuario abandonado", p, new Vector3(0.22f, 0.002f, 0.31f), paper);
                sheet.localRotation = Quaternion.Euler(0, seed * 27f, 0);
            }
        }

        private void WallStain(Vector3 position, float yaw, int seed)
        {
            var stain = GameObject.CreatePrimitive(PrimitiveType.Quad);
            RemoveCollider(stain);
            stain.name = "Infiltracao";
            stain.transform.SetParent(root, false);
            stain.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            stain.transform.localScale = new Vector3(1.4f + Mathf.Sin(seed) * 0.3f, 1.5f, 1);
            stain.GetComponent<Renderer>().sharedMaterial = dirt;
            stain.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private void Dust(Vector3 center, int room)
        {
            var obj = new GameObject("Poeira suspensa " + room);
            obj.transform.SetParent(root, false);
            obj.transform.position = center + Vector3.up * 1.5f;
            var particles = obj.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.useAutoRandomSeed = false;
            particles.randomSeed = (uint)(room + 17);
            var main = particles.main;
            main.startLifetime = 12;
            main.startSpeed = 0.015f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.022f);
            main.startColor = new Color(0.53f, 0.59f, 0.55f, 0.16f);
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(6.6f, 2.6f, 6.6f);
            var emission = particles.emission;
            emission.rateOverTime = 1.8f;
            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = dust;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            particles.Simulate(12f, true, true);
            particles.Play();
        }

        private Transform Group(string name, Vector3 position, float yaw)
        {
            var group = new GameObject(name).transform;
            group.SetParent(root, false);
            group.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            return group;
        }

        private Transform Box(string name, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            RemoveCollider(obj);
            obj.name = name;
            obj.transform.SetParent(root, false);
            obj.transform.position = position;
            obj.transform.localScale = size;
            var renderer = obj.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = name == "Placa" || name == "Moldura" || name == "Caixa de suprimentos" ||
                name == "Arandela - base" || name == "Prateleira" ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return obj.transform;
        }

        private Transform LocalBox(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            Transform box = Box(name, Vector3.zero, size, material);
            box.SetParent(parent, false);
            box.localPosition = position;
            return box;
        }

        private TextMesh Text(Transform parent, string content, Vector3 position, float size, Color color, bool emergency = false)
        {
            var obj = new GameObject(content);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            var text = obj.AddComponent<TextMesh>();
            text.font = font;
            text.text = content;
            text.fontSize = 64;
            text.characterSize = size * 0.45f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = emergency ? emergencyLettering : lettering;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return text;
        }

        private static void RemoveCollider(GameObject obj)
        {
            obj.layer = 2; // Ignore Raycast; o collider ja fica inativo antes do bake.
            var collider = obj.GetComponent<Collider>();
            if (!collider) return;
            collider.enabled = false;
            Destroy(collider);
        }

        private Material Paint(Color color)
        {
            if (painted.TryGetValue(color, out Material material)) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.17f);
            owned.Add(material);
            painted.Add(color, material);
            return material;
        }

        private Material Glow(Color color, float intensity)
        {
            var material = new Material(Paint(color));
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * intensity);
            owned.Add(material);
            return material;
        }

        private Material TransparentMaterial(string name, Texture2D texture)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_Smoothness", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            owned.Add(material);
            return material;
        }

        private Texture2D MakeTexture(bool particle)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = particle ? "Poeira suave" : "Umidade irregular", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (size - 1f), v = y / (size - 1f);
                float radius = new Vector2((u - 0.5f) * 2f, (v - 0.5f) * 2f).magnitude;
                float edge = Mathf.Clamp01((1f - radius) * 2.4f);
                float noise = Mathf.PerlinNoise(u * 8f + 17f, v * 7f + 9f);
                pixels[y * size + x] = particle ? new Color(1, 1, 1, edge * edge) :
                    new Color(0.13f, 0.16f, 0.11f, edge * Mathf.Clamp01((noise - 0.28f) * 2f) * 0.7f);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            owned.Add(texture);
            return texture;
        }

        private void OnDestroy()
        {
            Font.textureRebuilt -= RefreshFont;
            foreach (var asset in owned) if (asset) Destroy(asset);
        }

        private void RefreshFont(Font rebuilt)
        {
            if (rebuilt == font && lettering) lettering.mainTexture = font.material.mainTexture;
            if (rebuilt == font && emergencyLettering) emergencyLettering.mainTexture = font.material.mainTexture;
        }
    }
}
