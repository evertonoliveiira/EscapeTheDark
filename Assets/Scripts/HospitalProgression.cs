using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeTheDark
{
    public sealed partial class GameDirector
    {
        public int InstalledCircuitCount { get; private set; }
        public int CarriedFuseCount => FuseCount - InstalledCircuitCount;
        public bool ExitPowered => InstalledCircuitCount == 3;
        public bool HasReadPlan { get; private set; }
        public bool KnowsWardCode { get; private set; }
        public ServiceShortcut Shortcut => serviceShortcut;
        public PowerPanel EmergencyPanel => powerPanel;
        public KeypadCabinet WardCabinet => wardCabinet;
        public EquipmentFuse SurgicalEquipment => surgicalEquipment;
        public bool HasWon => state == GameState.Won;
        public bool HasFlashlightBattery => player && player.HasFlashlightBattery;
        public BatteryPickup FlashlightBattery => receptionBattery;

        private readonly bool[] collectedFuses = new bool[3];
        private readonly bool[] installedCircuits = new bool[3];
        private readonly HashSet<string> readNotes = new HashSet<string>();
        private PowerPanel powerPanel;
        private KeypadCabinet wardCabinet;
        private EquipmentFuse surgicalEquipment;
        private ServiceShortcut serviceShortcut;
        private Transform shiftedCurtain;
        private BatteryPickup receptionBattery;
        private Light batteryGuideLight;
        private Renderer batteryGuideIndicator;
        private string documentTitle;
        private string documentBody;
        private string enteredCode = "";
        private string keypadError = "";
        private GUIStyle documentStyle;
        private GUIStyle objectiveStyle;
        private GUIStyle codeStyle;
        private static readonly Key[] NumberKeys = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        private static readonly Key[] NumpadKeys = { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        public bool IsCircuitInstalled(HospitalCircuit circuit) => installedCircuits[(int)circuit];
        public static string CircuitName(HospitalCircuit circuit) => circuit == HospitalCircuit.Reception ? "recepcao" :
            circuit == HospitalCircuit.Ward ? "enfermaria" : "saida";

        public string Objective
        {
            get
            {
                if (!HasFlashlightBattery) return player && player.HasTriedEmptyFlashlight
                    ? "Encontre uma pilha no balcao da recepcao, junto a luz de emergencia."
                    : "Teste o interruptor da lanterna [F] e examine o balcao da recepcao.";
                if (!collectedFuses[0]) return HasReadPlan ? "Procure o fusivel de reserva na farmacia." : "Examine o quadro de emergencia na recepcao.";
                if (!installedCircuits[0]) return "Instale a reserva no quadro da recepcao.";
                if (!collectedFuses[1]) return KnowsWardCode ? "Abra o armario da enfermaria. Sequencia registrada: 2 - 4 - 1." :
                    "Procure a ficha de transferencia na observacao; ela explica o codigo da enfermaria.";
                if (!installedCircuits[1]) return "Leve o segundo fusivel ao quadro e restaure a enfermaria.";
                if (!collectedFuses[2]) return "Na cirurgia, isole o equipamento e retire a ultima peca. Observe a criatura.";
                if (!installedCircuits[2]) return "Instale a ultima peca no quadro da recepcao.";
                return "A saida tem energia. Use o corredor de servico e segure E na porta da manutencao.";
            }
        }

        public void UsePowerPanel()
        {
            if (!IsPlaying) return;
            if (!HasFlashlightBattery)
            {
                ShowMessage("Esta escuro demais para ler os circuitos. Procure uma pilha no balcao da recepcao.", 5);
                return;
            }
            HasReadPlan = true;
            for (int i = 0; i < 3; i++)
            {
                if (!collectedFuses[i] || installedCircuits[i]) continue;
                installedCircuits[i] = true;
                InstalledCircuitCount++;
                HospitalCircuit circuit = (HospitalCircuit)i;
                powerPanel.Restore(circuit);
                RestoreCircuit(circuit);
                effects.PlayOneShot(pickupClip, 0.65f);
                if (circuit == HospitalCircuit.Reception)
                {
                    serviceShortcut.Unlock();
                    ShowMessage("RECEPCAO ATIVA - o corredor de servico entre a ala central e a manutencao foi liberado.", 7);
                }
                else if (circuit == HospitalCircuit.Ward)
                    ShowMessage("ENFERMARIA ATIVA - a trava do equipamento da cirurgia pode ser aberta.", 6);
                else
                {
                    exitDoor.Unlock();
                    ShowMessage("SAIDA ENERGIZADA - volte pela passagem de servico e acione a porta.", 7);
                    effects.PlayOneShot(winClip, 0.35f);
                }
                return;
            }
            if (ExitPowered) { ShowMessage("Os tres circuitos estao ativos. A porta de saida fica ao norte da manutencao.", 5); return; }
            OpenDocument("PLANO DE CONTINGENCIA", "A fechadura da saida depende de tres circuitos. Instale cada fusivel neste quadro.\n\n" +
                "1. RECEPCAO - reserva no balcao da FARMACIA.\n\n" +
                "2. ENFERMARIA - armario com teclado. A ficha de transferencia ficou na OBSERVACAO.\n\n" +
                "3. SAIDA - fusivel do equipamento da CIRURGIA. A trava depende da enfermaria energizada.\n\n" +
                "O circuito da recepcao tambem libera o corredor de servico para a manutencao.");
        }

        public void CollectBattery(BatteryPickup battery)
        {
            if (!IsPlaying || !battery || battery != receptionBattery || battery.Collected || HasFlashlightBattery) return;
            var flashlight = player.GetComponent<FlashlightController>();
            if (!flashlight || !flashlight.InsertBattery()) return;
            battery.MarkCollected();
            Destroy(battery.gameObject);
            if (batteryGuideLight) batteryGuideLight.enabled = false;
            if (batteryGuideIndicator)
            {
                var properties = new MaterialPropertyBlock();
                properties.SetColor("_EmissionColor", Color.black);
                batteryGuideIndicator.SetPropertyBlock(properties);
            }
            effects.PlayOneShot(pickupClip, 0.45f);
            ShowMessage("Pilha instalada. Use F para ligar ou desligar a lanterna e examine o quadro da recepcao.", 7);
        }

        public void ReadNote(StoryNote note)
        {
            if (!IsPlaying || !note) return;
            readNotes.Add(note.NoteId);
            if (note.NoteId == "transferencias") KnowsWardCode = true;
            OpenDocument(note.Title, note.Body);
        }

        private void OpenDocument(string title, string body)
        {
            documentTitle = title;
            documentBody = body;
            state = GameState.Reading;
            SetPlaying(false);
        }

        public void OpenKeypad(KeypadCabinet cabinet)
        {
            if (!IsPlaying || cabinet != wardCabinet || cabinet.IsOpen) return;
            enteredCode = "";
            keypadError = "";
            state = GameState.Keypad;
            SetPlaying(false);
        }

        public void CloseDocument()
        {
            if (state != GameState.Reading && state != GameState.Keypad) return;
            state = GameState.Playing;
            SetPlaying(true);
            player.SuppressInteractionUntilRelease();
        }

        public void AddCodeDigit(int digit)
        {
            if (state != GameState.Keypad || enteredCode.Length >= 3 || digit < 0 || digit > 9) return;
            enteredCode += digit.ToString();
            keypadError = "";
        }

        public void SubmitCode()
        {
            if (state != GameState.Keypad) return;
            if (wardCabinet.TryCode(enteredCode))
            {
                CloseDocument();
                effects.PlayOneShot(pickupClip, 0.5f);
                ShowMessage("Armario destravado. Recolha a peca de emergencia.", 4);
            }
            else
            {
                keypadError = "Sequencia incorreta. Consulte a ficha de transferencia.";
                enteredCode = "";
            }
        }

        private void UpdateDocumentInput(Keyboard keyboard)
        {
            if (keyboard.escapeKey.wasPressedThisFrame || (state == GameState.Reading && keyboard.eKey.wasPressedThisFrame))
            { CloseDocument(); return; }
            if (state != GameState.Keypad) return;
            for (int i = 0; i < 10; i++)
                if (keyboard[NumberKeys[i]].wasPressedThisFrame || keyboard[NumpadKeys[i]].wasPressedThisFrame) AddCodeDigit(i);
            if (keyboard.backspaceKey.wasPressedThisFrame && enteredCode.Length > 0)
                enteredCode = enteredCode.Substring(0, enteredCode.Length - 1);
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) SubmitCode();
        }

        private void RestoreCircuit(HospitalCircuit circuit)
        {
            if (circuit == HospitalCircuit.Reception) { RestoreLocalLight(1, 0.55f); RestoreLocalLight(3, 0.22f); }
            else if (circuit == HospitalCircuit.Ward) { RestoreLocalLight(2, 0.16f); RestoreLocalLight(5, 0.22f); }
            else { RestoreLocalLight(7, 0.24f); RestoreLocalLight(8, 0.18f); }
        }

        private void RestoreLocalLight(int room, float strength)
        {
            roomLightBase[room] = strength;
            roomLightEnabled[room] = true;
            roomLights[room].enabled = true;
            roomLights[room].intensity = strength;
            if (poweredLampMaterials[room] == null) return;
            for (int i = 0; i < poweredLampRenderers[room].Length; i++)
                poweredLampRenderers[room][i].sharedMaterials = poweredLampMaterials[room][i];
        }

        private IEnumerator WaitWhilePlaying(float seconds)
        {
            while (seconds > 0)
            {
                if (state == GameState.Won || state == GameState.Lost) yield break;
                if (IsPlaying) seconds -= Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator FirstWarning()
        {
            yield return WaitWhilePlaying(0.8f);
            if (!IsPlaying) yield break;
            if (walkSteps != null && walkSteps.Length > 0 && walkSteps[0])
                AudioSource.PlayClipAtPoint(walkSteps[0], Cell(0, 2) + Vector3.up, 0.65f);
            if (!shiftedCurtain) yield break;
            Vector3 start = shiftedCurtain.localPosition;
            float time = 0;
            while (time < 1f)
            {
                if (IsPlaying) time += Time.deltaTime * 0.8f;
                shiftedCurtain.localPosition = start + Vector3.right * Mathf.SmoothStep(0, 0.38f, time);
                yield return null;
            }
        }

        private void DrawObjective()
        {
            if (objectiveStyle == null) objectiveStyle = new GUIStyle(smallStyle) { wordWrap = true, fontSize = 18, alignment = TextAnchor.UpperLeft };
            DrawTextPanel(new Rect(28, 570, 365, 90), Objective, objectiveStyle, new Vector2(13, 10), true);
        }

        private void DrawDocument()
        {
            if (documentStyle == null) documentStyle = new GUIStyle(textStyle) { alignment = TextAnchor.UpperLeft, fontSize = 21 };
            DrawRect(new Rect(0, 0, 1280, 720), new Color(0, 0.01f, 0.015f, 0.9f));
            if (state == GameState.Reading)
            {
                DrawRect(new Rect(270, 65, 740, 590), new Color(0.035f, 0.055f, 0.055f, 1));
                GUI.Label(new Rect(310, 90, 660, 60), documentTitle, textStyle);
                GUI.Label(new Rect(315, 175, 650, 370), documentBody, documentStyle);
                if (GUI.Button(new Rect(485, 572, 310, 52), "VOLTAR  [E / Esc]", buttonStyle)) CloseDocument();
                return;
            }
            GUI.Label(new Rect(350, 75, 580, 55), "ARMARIO DA ENFERMARIA", textStyle);
            if (codeStyle == null) codeStyle = new GUIStyle(titleStyle) { fontSize = 44 };
            GUI.Label(new Rect(380, 135, 520, 64), enteredCode.PadRight(3, '_'), codeStyle);
            for (int digit = 1; digit <= 9; digit++)
            {
                int column = (digit - 1) % 3, row = (digit - 1) / 3;
                if (GUI.Button(new Rect(500 + column * 96, 215 + row * 76, 88, 66), digit.ToString(), buttonStyle)) AddCodeDigit(digit);
            }
            if (GUI.Button(new Rect(596, 443, 88, 66), "0", buttonStyle)) AddCodeDigit(0);
            if (GUI.Button(new Rect(384, 525, 250, 56), "VOLTAR", buttonStyle)) CloseDocument();
            if (GUI.Button(new Rect(646, 525, 250, 56), "CONFIRMAR", buttonStyle)) SubmitCode();
            string hint = KnowsWardCode ? "Ficha registrada: leitos 2 > 4 > 1." : "A ficha com a sequencia esta na observacao.";
            GUI.Label(new Rect(350, 594, 580, 56), string.IsNullOrEmpty(keypadError) ? hint : keypadError, textStyle);
            GUI.Label(new Rect(350, 657, 580, 28), "0-9 digitar  |  Backspace corrigir  |  Enter confirmar  |  Esc voltar", smallStyle);
        }
    }
}
