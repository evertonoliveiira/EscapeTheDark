using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EscapeTheDark
{
    public sealed partial class GameDirector
    {
        private Transform scenarioRoot;
        private HospitalVisuals scenarioVisuals;
        private Material scenarioMetal;
        private Material scenarioDark;
        private Material scenarioFabric;
        private Material scenarioPaper;

        private void BuildScenario()
        {
            scenarioRoot = new GameObject("Hospital - historia e objetivos").transform;
            scenarioRoot.SetParent(level, false);
            scenarioVisuals = level.GetComponent<HospitalVisuals>();
            scenarioMetal = scenarioVisuals.SceneMaterial(new Color(0.24f, 0.29f, 0.28f));
            scenarioDark = scenarioVisuals.SceneMaterial(new Color(0.055f, 0.07f, 0.07f));
            scenarioFabric = scenarioVisuals.SceneMaterial(new Color(0.21f, 0.32f, 0.28f));
            scenarioPaper = scenarioVisuals.SceneMaterial(new Color(0.65f, 0.61f, 0.49f));
            BuildReception();
            BuildPharmacy();
            BuildObservationAndCabinet();
            BuildSurgery();
            BuildServiceShortcut();
            BuildEnvironmentalStory();
        }

        private void BuildReception()
        {
            var panel = ScenarioGroup("Quadro de emergencia", new Vector3(-2.45f, 1.35f, -11.53f), 180);
            powerPanel = panel.gameObject.AddComponent<PowerPanel>();
            ScenarioBox(panel, "Caixa do quadro", Vector3.zero, new Vector3(2f, 1.1f, 0.20f), scenarioMetal, true);
            ScenarioBox(panel, "Face do quadro", new Vector3(0, 0, -0.12f), new Vector3(1.88f, 0.99f, 0.025f), scenarioDark);
            scenarioVisuals.Label(panel, "QUADRO DE EMERGENCIA", new Vector3(0, 0.78f, -0.13f), 0.055f, new Color(0.68f, 0.72f, 0.64f));
            for (int i = 0; i < 3; i++)
            {
                float y = 0.32f - i * 0.31f;
                var indicator = ScenarioBox(panel, "Indicador do circuito " + i, new Vector3(-0.82f, y, -0.15f),
                    new Vector3(0.065f, 0.065f, 0.02f), ScenarioIndicator(new Color(0.24f, 0.065f, 0.025f)));
                scenarioVisuals.Label(panel, CircuitName((HospitalCircuit)i).ToUpperInvariant(), new Vector3(-0.15f, y, -0.145f), 0.042f, new Color(0.65f, 0.68f, 0.61f));
                var status = scenarioVisuals.Label(panel, "FALTA", new Vector3(0.69f, y, -0.145f), 0.033f, new Color(0.48f, 0.56f, 0.46f));
                powerPanel.SetSlot(i, indicator.GetComponent<Renderer>(), status);
            }
            scenarioVisuals.Label(panel, "RESERVAS: FARMACIA > ENFERMARIA > CIRURGIA", new Vector3(0, -0.72f, -0.13f), 0.028f, new Color(0.57f, 0.61f, 0.54f));
            var directions = ScenarioGroup("Rotas da recepcao", new Vector3(2.55f, 2.05f, -4.38f), 0);
            ScenarioBox(directions, "Placa de orientacao", Vector3.zero, new Vector3(2.1f, 0.72f, 0.025f), scenarioDark);
            scenarioVisuals.Label(directions, "FARMACIA  <     ALA CENTRAL  ^", new Vector3(0, 0.16f, -0.02f), 0.036f, new Color(0.62f, 0.67f, 0.57f));
            scenarioVisuals.Label(directions, "OBSERVACAO / VIA ALA CENTRAL", new Vector3(0, -0.15f, -0.02f), 0.032f, new Color(0.56f, 0.61f, 0.52f));

            var desk = ScenarioGroup("Balcao de recepcao", new Vector3(2.7f, 0, -8), 90);
            ScenarioBox(desk, "Corpo do balcao", new Vector3(0, 0.47f, 0), new Vector3(2.7f, 0.94f, 0.85f), scenarioFabric, true);
            ScenarioBox(desk, "Tampo", new Vector3(0, 0.99f, 0), new Vector3(2.85f, 0.09f, 1.05f), scenarioMetal);
            ScenarioBox(desk, "Monitor desligado", new Vector3(0.50f, 1.30f, 0.13f), new Vector3(0.59f, 0.40f, 0.065f), scenarioDark);
            ScenarioBox(desk, "Base do monitor", new Vector3(0.5f, 1.08f, 0.13f), new Vector3(0.3f, 0.08f, 0.24f), scenarioMetal);
            BuildReceptionBattery(desk);
            BuildChair(new Vector3(-2.8f, 0, -5.55f), 170);
            BuildChair(new Vector3(-1.92f, 0, -5.6f), 175);
            BuildChair(new Vector3(2.65f, 0, -10.2f), 75);
            AddScenarioNote("evacuacao", "Aviso de evacuacao",
                "Deixei uma pilha de reserva para a lanterna na bandeja do balcao, sob a luz de emergencia.\n\n" +
                "A evacuacao foi interrompida antes do ultimo turno. A fechadura da saida continua ligada ao quadro da recepcao.\n\n" +
                "A reserva do primeiro circuito ficou no balcao da farmacia. Leve as pecas ao quadro; encontra-las nao restaura a energia sozinho.\n\n" +
                "Evite correr. O barulho ecoa entre os setores.",
                new Vector3(2.14f, 1.10f, -8.6f), 90, 62);
        }

        private void BuildReceptionBattery(Transform desk)
        {
            ScenarioBox(desk, "Bandeja da pilha", new Vector3(-0.55f, 1.05f, -0.25f), new Vector3(0.40f, 0.018f, 0.34f), scenarioMetal);
            scenarioVisuals.Label(desk, "PILHA / LANTERNA", new Vector3(-0.55f, 0.77f, -0.435f), 0.025f, new Color(0.69f, 0.65f, 0.48f));
            var battery = ScenarioGroup("Pilha de reserva da lanterna", new Vector3(2.37f, 1.11f, -7.45f), 90);
            receptionBattery = battery.gameObject.AddComponent<BatteryPickup>();
            var hitbox = battery.gameObject.AddComponent<BoxCollider>();
            hitbox.size = new Vector3(0.32f, 0.16f, 0.18f);
            var casing = scenarioVisuals.SceneMaterial(new Color(0.52f, 0.38f, 0.13f));
            BatteryCylinder(battery, "Corpo da pilha", Vector3.zero, new Vector3(0.105f, 0.115f, 0.105f), casing);
            BatteryCylinder(battery, "Polo negativo", new Vector3(-0.12f, 0, 0), new Vector3(0.11f, 0.008f, 0.11f), scenarioMetal);
            BatteryCylinder(battery, "Polo positivo", new Vector3(0.12f, 0, 0), new Vector3(0.11f, 0.008f, 0.11f), scenarioMetal);
            BatteryCylinder(battery, "Contato positivo", new Vector3(0.135f, 0, 0), new Vector3(0.044f, 0.008f, 0.044f), scenarioMetal);
            scenarioVisuals.Label(battery, "1.5V", new Vector3(0, 0, -0.055f), 0.022f, new Color(0.07f, 0.075f, 0.045f));

            var lamp = ScenarioGroup("Luz de emergencia do balcao", new Vector3(2.10f, 1.76f, -7.45f), 0);
            ScenarioBox(lamp, "Base da luz de emergencia", new Vector3(0.55f, -0.69f, 0.30f), new Vector3(0.24f, 0.06f, 0.22f), scenarioDark);
            ScenarioBox(lamp, "Haste da luminaria", new Vector3(0.55f, -0.34f, 0.30f), new Vector3(0.035f, 0.65f, 0.035f), scenarioMetal);
            ScenarioBox(lamp, "Braco da luminaria", new Vector3(0.28f, 0, 0.30f), new Vector3(0.58f, 0.035f, 0.035f), scenarioMetal);
            ScenarioBox(lamp, "Suporte do refletor", new Vector3(0, 0, 0.15f), new Vector3(0.035f, 0.035f, 0.30f), scenarioMetal);
            ScenarioBox(lamp, "Refletor de emergencia", Vector3.zero, new Vector3(0.23f, 0.10f, 0.20f), scenarioDark);
            var diffuser = ScenarioBox(lamp, "Difusor ambar", new Vector3(0, -0.058f, 0), new Vector3(0.19f, 0.025f, 0.16f), ScenarioIndicator(new Color(0.70f, 0.32f, 0.08f)));
            batteryGuideIndicator = diffuser.GetComponent<Renderer>();
            var lightObject = new GameObject("Luz sobre a reserva");
            lightObject.transform.SetParent(lamp, false);
            lightObject.transform.position = new Vector3(2.10f, 1.66f, -7.45f);
            lightObject.transform.rotation = Quaternion.LookRotation(battery.position - lightObject.transform.position);
            batteryGuideLight = lightObject.AddComponent<Light>();
            batteryGuideLight.type = LightType.Spot;
            batteryGuideLight.color = new Color(1f, 0.67f, 0.32f);
            batteryGuideLight.range = 1.25f;
            batteryGuideLight.spotAngle = 58;
            batteryGuideLight.innerSpotAngle = 36;
            batteryGuideLight.intensity = 4f;
            batteryGuideLight.shadows = LightShadows.Soft;
            batteryGuideLight.shadowNearPlane = 0.035f;
            batteryGuideLight.shadowBias = 0.035f;
            batteryGuideLight.shadowNormalBias = 0.08f;
            var lightData = batteryGuideLight.GetUniversalAdditionalLightData();
            lightData.usePipelineSettings = false;
            lightData.additionalLightsShadowResolutionTier = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow;
        }

        private static void BatteryCylinder(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = Quaternion.Euler(0, 0, 90);
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            obj.GetComponent<Collider>().enabled = false;
            Destroy(obj.GetComponent<Collider>());
        }

        private void BuildPharmacy()
        {
            var worktop = ScenarioGroup("Balcao da farmacia", new Vector3(-10, 0, -1.6f), 0);
            ScenarioBox(worktop, "Base de suprimentos", new Vector3(0, 0.47f, 0), new Vector3(1.75f, 0.94f, 0.7f), scenarioFabric, true);
            ScenarioBox(worktop, "Tampo de trabalho", new Vector3(0, 0.97f, 0), new Vector3(1.87f, 0.07f, 0.79f), scenarioMetal);
            var fuse = AddFuse(new Vector3(-10, 1.45f, -1.6f), HospitalCircuit.Reception);
            fuse.transform.SetParent(scenarioRoot, true);
            scenarioVisuals.Label(worktop, "RESERVA / RECEPCAO", new Vector3(0, 0.74f, -0.36f), 0.035f, new Color(0.6f, 0.66f, 0.53f));
            AddScenarioNote("reserva", "Inventario da farmacia",
                "Reserva do circuito da RECEPCAO separada no balcao.\n\n" +
                "Ao instalar a peca no quadro, o contato do corredor de servico tambem deve abrir. A passagem liga a ala central a manutencao.\n\n" +
                "A ficha de transferencia foi levada para a observacao.",
                new Vector3(-5.65f, 1.63f, -3.60f), 180);
            var trolley = ScenarioGroup("Carrinho de medicamentos", new Vector3(-5.5f, 0, 1.3f), 10);
            ScenarioBox(trolley, "Carrinho", new Vector3(0, 0.43f, 0), new Vector3(0.65f, 0.86f, 0.58f), scenarioMetal, true);
            for (int i = 0; i < 3; i++) ScenarioBox(trolley, "Gaveta", new Vector3(0, 0.23f + i * 0.23f, -0.30f), new Vector3(0.52f, 0.025f, 0.025f), scenarioDark);
        }

        private void BuildObservationAndCabinet()
        {
            if (hospitalBed) PlaceHospitalProp(hospitalBed, Cell(2, 0) + new Vector3(2.1f, 0, -2.25f), 0, 2.3f);
            var tag = ScenarioGroup("Transferencias de leitos", new Vector3(10.45f, 1.95f, -4.38f), 0);
            scenarioVisuals.Label(tag, "TRANSFERENCIAS:  2 > 4 > 1", new Vector3(0, 0.32f, -0.03f), 0.050f, new Color(0.65f, 0.7f, 0.62f));
            AddScenarioNote("transferencias", "Ficha de transferencia",
                "ULTIMAS TRANSFERENCIAS DO PLANTAO\n\nLeito 2  >  Leito 4  >  Leito 1\n\n" +
                "Enfermagem: mantive essa mesma ordem no teclado do armario de emergencia da ENFERMARIA.\n\n" +
                "O teclado so responde depois de restaurar o circuito da recepcao. A sequencia fica registrada ao ler esta ficha.",
                new Vector3(10.45f, 1.65f, -4.38f), 0);

            var cabinet = ScenarioGroup("Armario de emergencia da enfermaria", new Vector3(5.4f, 0, -3.25f), 180);
            wardCabinet = cabinet.gameObject.AddComponent<KeypadCabinet>();
            ScenarioBox(cabinet, "Fundo", new Vector3(0, 1.08f, 0.24f), new Vector3(1.5f, 2.16f, 0.08f), scenarioMetal, true);
            ScenarioBox(cabinet, "Lateral esquerda", new Vector3(-0.72f, 1.08f, 0), new Vector3(0.08f, 2.16f, 0.54f), scenarioMetal, true);
            ScenarioBox(cabinet, "Lateral direita", new Vector3(0.72f, 1.08f, 0), new Vector3(0.08f, 2.16f, 0.54f), scenarioMetal, true);
            for (int i = 0; i < 3; i++) ScenarioBox(cabinet, "Prateleira interna", new Vector3(0, 0.1f + i * 0.72f, 0), new Vector3(1.45f, 0.06f, 0.5f), scenarioMetal, true);
            var hinge = new GameObject("Dobradica do armario").transform;
            hinge.SetParent(cabinet, false);
            hinge.localPosition = new Vector3(-0.72f, 1.08f, -0.30f);
            ScenarioBox(hinge, "Porta do armario", new Vector3(0.72f, 0, 0), new Vector3(1.44f, 2.12f, 0.08f), scenarioFabric, true);
            ScenarioBox(hinge, "Teclado", new Vector3(1.12f, 0.18f, -0.065f), new Vector3(0.28f, 0.36f, 0.045f), scenarioDark);
            scenarioVisuals.Label(hinge, "1 2 3\n4 5 6\n7 8 9", new Vector3(1.12f, 0.18f, -0.095f), 0.023f, new Color(0.7f, 0.73f, 0.65f));
            scenarioVisuals.Label(hinge, "EMERGENCIA", new Vector3(0.65f, 0.65f, -0.07f), 0.048f, new Color(0.69f, 0.72f, 0.63f));
            var fuse = AddFuse(cabinet.TransformPoint(new Vector3(0, 1.32f, 0)), HospitalCircuit.Ward);
            fuse.transform.SetParent(cabinet, true);
            wardCabinet.Configure(hinge, fuse);
            BuildCurtain(new Vector3(11.45f, 0, -2.25f), 90, 1.7f);
        }

        private void BuildSurgery()
        {
            var table = ScenarioGroup("Mesa cirurgica", new Vector3(8.5f, 0, 9.9f), 0);
            ScenarioBox(table, "Pedestal", new Vector3(0, 0.43f, 0), new Vector3(0.55f, 0.86f, 0.85f), scenarioMetal, true);
            ScenarioBox(table, "Mesa de atendimento", new Vector3(0, 0.92f, 0), new Vector3(0.92f, 0.15f, 2.05f), scenarioDark, true);
            ScenarioBox(table, "Lençol abandonado", new Vector3(-0.07f, 1.01f, 0.2f), new Vector3(1.04f, 0.018f, 1.3f), scenarioFabric);
            ScenarioBox(table, "Braco da luminaria", new Vector3(0.85f, 2.63f, 0), new Vector3(1.9f, 0.07f, 0.08f), scenarioMetal);
            ScenarioBox(table, "Foco cirurgico desligado", new Vector3(0, 2.51f, 0), new Vector3(0.65f, 0.16f, 0.65f), scenarioMetal);

            var machine = ScenarioGroup("Equipamento com fusivel de saida", new Vector3(11.18f, 0, 8.7f), 90);
            surgicalEquipment = machine.gameObject.AddComponent<EquipmentFuse>();
            ScenarioBox(machine, "Base do equipamento", new Vector3(0, 0.48f, 0), new Vector3(1.05f, 0.96f, 0.68f), scenarioMetal, true);
            ScenarioBox(machine, "Painel de isolamento", new Vector3(0, 1.4f, 0), new Vector3(0.95f, 1.05f, 0.32f), scenarioFabric, true);
            scenarioVisuals.Label(machine, "FUSIVEL / SAIDA", new Vector3(0, 1.87f, -0.18f), 0.038f, new Color(0.67f, 0.72f, 0.63f));
            scenarioVisuals.Label(machine, "ISOLAR ANTES DE RETIRAR", new Vector3(0, 0.65f, -0.355f), 0.025f, new Color(0.65f, 0.48f, 0.26f));
            var fuse = AddFuse(machine.TransformPoint(new Vector3(0, 1.36f, -0.23f)), HospitalCircuit.Exit);
            fuse.transform.SetParent(machine, true);
            foreach (var collider in fuse.GetComponentsInChildren<Collider>()) collider.enabled = false;
            surgicalEquipment.Configure(fuse);
            AddScenarioNote("manutencao", "Ficha de manutencao",
                "A ultima peca compativel com a SAIDA esta no equipamento da cirurgia.\n\n" +
                "Restaure a ENFERMARIA antes de abrir a trava de seguranca. Depois, mantenha E pressionado para isolar o equipamento e retirar a peca.\n\n" +
                "Leve-a ao quadro da recepcao. A saida fica ao norte da manutencao; a passagem de servico encurta o retorno.",
                new Vector3(2.55f, 1.62f, 4.38f), 180);
        }

        private void BuildServiceShortcut()
        {
            var doorway = ScenarioGroup("Acesso de servico", new Vector3(0, 0, 4), 0);
            serviceShortcut = doorway.gameObject.AddComponent<ServiceShortcut>();
            // O portao e dinamico: o bake preserva o piso e o carving fecha a passagem ate ser liberada.
            var modifier = doorway.gameObject.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;
            var shutter = new GameObject("Folha deslizante").transform;
            shutter.SetParent(doorway, false);
            var leaf = ScenarioBox(shutter, "Portao de servico", new Vector3(0, 1.3f, 0), new Vector3(2.48f, 2.60f, 0.20f), scenarioMetal, true);
            for (int i = -4; i <= 4; i++)
                ScenarioBox(shutter, "Friso do portao", new Vector3(i * 0.25f, 1.3f, -0.12f), new Vector3(0.028f, 2.4f, 0.025f), scenarioDark);
            var obstacle = doorway.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = new Vector3(0, 1.3f, 0);
            obstacle.size = new Vector3(2.5f, 2.6f, 0.28f);
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            var indicator = ScenarioBox(doorway, "Contato do corredor", new Vector3(1.15f, 2.84f, -0.25f), new Vector3(0.08f, 0.08f, 0.025f), ScenarioIndicator(new Color(0.26f, 0.065f, 0.025f)));
            ScenarioBox(doorway, "Placa do servico", new Vector3(0, 2.81f, -0.24f), new Vector3(2.06f, 0.43f, 0.03f), scenarioDark);
            scenarioVisuals.Label(doorway, "SERVICO > MANUTENCAO", new Vector3(0, 2.94f, -0.27f), 0.040f, new Color(0.58f, 0.62f, 0.55f));
            scenarioVisuals.Label(doorway, "LIBERACAO PELO QUADRO DA RECEPCAO", new Vector3(0, 2.69f, -0.27f), 0.025f, new Color(0.51f, 0.55f, 0.45f));
            serviceShortcut.Configure(shutter, leaf.GetComponent<Collider>(), obstacle, indicator.GetComponent<Renderer>());
        }

        private void BuildEnvironmentalStory()
        {
            shiftedCurtain = BuildCurtain(new Vector3(-8.4f, 0, 10.95f), 0, 2.1f);
            AddScenarioNote("isolamento", "Ultimo plantao",
                "A transferencia do isolamento foi cancelada. As camas foram deixadas como estavam.\n\n" +
                "Quando as luzes voltaram, ouvimos passos do outro lado da cortina. Ninguem tinha entrado naquela sala.\n\n" +
                "Nao tente descobrir quem ficou para tras.",
                new Vector3(-5.65f, 1.65f, 11.62f), 0);
            var scratches = ScenarioGroup("Marcas no isolamento", new Vector3(-4.37f, 0, 10.35f), 90);
            var scratchMaterial = scenarioVisuals.SceneMaterial(new Color(0.32f, 0.22f, 0.15f));
            for (int i = 0; i < 4; i++)
            {
                var mark = ScenarioBox(scratches, "Risco na parede", new Vector3(i * 0.1f, 1.3f, 0), new Vector3(0.014f, 0.48f - i * 0.035f, 0.006f), scratchMaterial);
                mark.localRotation = Quaternion.Euler(0, 0, -18 + i * 4);
            }
            BuildChair(new Vector3(-5.65f, 0, -10.6f), 35);
            var evidence = ScenarioGroup("Pertences esquecidos", new Vector3(-9.55f, 0, -6.6f), -12);
            ScenarioBox(evidence, "Mala de paciente", new Vector3(0, 0.19f, 0), new Vector3(0.55f, 0.38f, 0.32f), scenarioFabric, true);
            ScenarioBox(evidence, "Alca", new Vector3(0, 0.40f, 0), new Vector3(0.18f, 0.04f, 0.08f), scenarioDark);
            var tools = ScenarioGroup("Bancada de manutencao", new Vector3(2.7f, 0, 9.55f), 0);
            ScenarioBox(tools, "Bancada", new Vector3(0, 0.47f, 0), new Vector3(1.65f, 0.94f, 0.7f), scenarioMetal, true);
            ScenarioBox(tools, "Ferramenta esquecida", new Vector3(0.12f, 0.96f, -0.15f), new Vector3(0.35f, 0.025f, 0.07f), scenarioDark);
        }

        private void BuildChair(Vector3 position, float yaw)
        {
            var chair = ScenarioGroup("Cadeira de espera", position, yaw);
            ScenarioBox(chair, "Assento", new Vector3(0, 0.46f, 0), new Vector3(0.53f, 0.08f, 0.48f), scenarioFabric);
            ScenarioBox(chair, "Encosto", new Vector3(0, 0.78f, 0.21f), new Vector3(0.53f, 0.5f, 0.065f), scenarioFabric);
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
                ScenarioBox(chair, "Pe da cadeira", new Vector3(x * 0.22f, 0.22f, z * 0.19f), new Vector3(0.035f, 0.44f, 0.035f), scenarioMetal);
            var collider = chair.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0, 0.51f, 0);
            collider.size = new Vector3(0.55f, 1.02f, 0.53f);
        }

        private Transform BuildCurtain(Vector3 position, float yaw, float width)
        {
            var frame = ScenarioGroup("Cortina de leito", position, yaw);
            ScenarioBox(frame, "Trilho", new Vector3(0, 2.45f, 0), new Vector3(width + 0.3f, 0.055f, 0.06f), scenarioMetal);
            var cloth = new GameObject("Tecido entreaberto").transform;
            cloth.SetParent(frame, false);
            int folds = 14;
            for (int i = 0; i < folds; i++)
            {
                float x = (i / (float)(folds - 1) - 0.5f) * width;
                float wave = Mathf.Sin(i * 2.2f) * 0.035f;
                ScenarioBox(cloth, "Dobra da cortina", new Vector3(x, 1.48f, wave), new Vector3(width / folds + 0.018f, 1.82f, 0.025f), scenarioFabric);
            }
            return cloth;
        }

        private StoryNote AddScenarioNote(string id, string title, string body, Vector3 position, float yaw, float pitch = 0)
        {
            var note = ScenarioGroup(title, position, yaw);
            note.rotation = Quaternion.Euler(pitch, yaw, 0);
            ScenarioBox(note, "Ficha de papel", Vector3.zero, new Vector3(0.55f, 0.69f, 0.025f), scenarioPaper, true);
            scenarioVisuals.Label(note, title.ToUpperInvariant(), new Vector3(0, 0.22f, -0.018f), 0.014f, new Color(0.075f, 0.09f, 0.07f));
            for (int i = 0; i < 5; i++) ScenarioBox(note, "Linha impressa", new Vector3(0, 0.10f - i * 0.075f, -0.019f), new Vector3(i % 2 == 0 ? 0.39f : 0.31f, 0.008f, 0.002f), scenarioDark);
            var interaction = note.gameObject.AddComponent<StoryNote>();
            interaction.Configure(id, title, body);
            return interaction;
        }

        private Transform ScenarioGroup(string name, Vector3 position, float yaw)
        {
            var group = new GameObject(name).transform;
            group.SetParent(scenarioRoot, false);
            group.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            return group;
        }

        private Transform ScenarioBox(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool solid = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            obj.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            if (!solid)
            {
                obj.layer = 2;
                obj.GetComponent<Collider>().enabled = false;
                Destroy(obj.GetComponent<Collider>());
            }
            return obj.transform;
        }

        private static Material ScenarioIndicator(Color color)
        {
            Material material = MaterialFor(color, true);
            material.SetColor("_EmissionColor", color * 0.18f);
            return material;
        }
    }
}
