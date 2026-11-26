using UnityEngine;

namespace EscapeTheDark
{
    public sealed class KeypadCabinet : GameInteraction
    {
        public const string AccessCode = "241";
        public bool IsOpen { get; private set; }
        public FusePickup Fuse { get; private set; }
        private Transform hinge;
        private float angle;
        public override string Prompt => IsOpen ? "Armario aberto - recolha o fusivel" : "[E] Usar teclado do armario";

        public void Configure(Transform doorHinge, FusePickup fuse)
        {
            hinge = doorHinge;
            Fuse = fuse;
            Fuse.SetAvailable(false);
        }

        public bool TryCode(string code)
        {
            if (IsOpen || !GameDirector.Instance.IsCircuitInstalled(HospitalCircuit.Reception) || code != AccessCode) return false;
            IsOpen = true;
            Fuse.SetAvailable(true);
            return true;
        }

        public override void Interact(PlayerMotor player)
        {
            if (IsOpen) return;
            if (!GameDirector.Instance.IsCircuitInstalled(HospitalCircuit.Reception))
                GameDirector.Instance.ShowMessage("O teclado esta apagado. Restaure o circuito da recepcao no quadro.", 5);
            else GameDirector.Instance.OpenKeypad(this);
        }

        private void Update()
        {
            if (!hinge || !GameDirector.Instance || !GameDirector.Instance.IsPlaying) return;
            angle = Mathf.MoveTowards(angle, IsOpen ? 110f : 0, Time.deltaTime * 100f);
            hinge.localRotation = Quaternion.Euler(0, angle, 0);
        }
    }
}
