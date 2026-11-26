using UnityEngine;

namespace EscapeTheDark
{
    public sealed class PowerPanel : GameInteraction
    {
        private readonly Renderer[] indicators = new Renderer[3];
        private readonly TextMesh[] statuses = new TextMesh[3];
        private MaterialPropertyBlock properties;
        private void Awake() => properties = new MaterialPropertyBlock();
        public override string Prompt => GameDirector.Instance.CarriedFuseCount > 0
            ? "[E] Instalar fusivel no quadro" : "[E] Consultar circuitos de emergencia";

        public void SetSlot(int index, Renderer indicator, TextMesh status)
        {
            indicators[index] = indicator;
            statuses[index] = status;
        }

        public void Restore(HospitalCircuit circuit)
        {
            int index = (int)circuit;
            Color green = new Color(0.12f, 0.55f, 0.30f);
            properties.SetColor("_BaseColor", green);
            properties.SetColor("_EmissionColor", green * 0.6f);
            indicators[index].SetPropertyBlock(properties);
            statuses[index].text = "ATIVO";
        }

        public override void Interact(PlayerMotor player) => GameDirector.Instance.UsePowerPanel();
    }
}
