using UnityEngine;

namespace EscapeTheDark
{
    public sealed class BatteryPickup : GameInteraction
    {
        public bool Collected { get; private set; }
        public override string Prompt => "[E] Pegar pilha e colocar na lanterna";

        public void MarkCollected()
        {
            Collected = true;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        }

        public override void Interact(PlayerMotor player)
        {
            if (!Collected) GameDirector.Instance.CollectBattery(this);
        }
    }
}
