using UnityEngine;

namespace EscapeTheDark
{
    public sealed class FusePickup : GameInteraction
    {
        public HospitalCircuit Circuit { get; private set; }
        public bool Available { get; private set; } = true;
        public bool Collected { get; private set; }
        public override string Prompt => Available ? "[E] Recolher fusivel - " + GameDirector.CircuitName(Circuit) : "Fusivel protegido";
        private Vector3 origin;
        public void Configure(HospitalCircuit circuit) => Circuit = circuit;
        public void SetAvailable(bool available) => Available = available;
        public void MarkCollected() { Collected = true; Available = false; }
        public override void Interact(PlayerMotor player)
        {
            if (Available && !Collected) GameDirector.Instance.CollectFuse(this);
        }
        private void Start() => origin = transform.position;
        private void Update()
        {
            if (!GameDirector.Instance || !GameDirector.Instance.IsPlaying) return;
            // Uma oscilacao pequena destaca a peca sem faze-la flutuar muito acima do suporte.
            transform.position = origin + Vector3.up * Mathf.Sin(Time.time * 2.6f) * 0.015f;
        }
    }
}
