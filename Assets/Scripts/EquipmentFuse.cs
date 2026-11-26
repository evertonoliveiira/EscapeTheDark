namespace EscapeTheDark
{
    public sealed class EquipmentFuse : GameInteraction
    {
        public FusePickup Fuse { get; private set; }
        public bool WasRemoved { get; private set; }
        private bool Powered => GameDirector.Instance.IsCircuitInstalled(HospitalCircuit.Ward);
        public override string Prompt => WasRemoved ? "Equipamento isolado - fusivel retirado" :
            Powered ? "Segure [E] para isolar e retirar o fusivel" : "[E] Examinar trava do equipamento";
        public override float HoldSeconds => Powered && !WasRemoved ? 3f : 0;
        public void Configure(FusePickup fuse) { Fuse = fuse; fuse.SetAvailable(false); }

        public override void Interact(PlayerMotor player)
        {
            if (WasRemoved || !GameDirector.Instance.IsPlaying) return;
            if (!Powered)
            {
                GameDirector.Instance.ShowMessage("A trava de seguranca depende do circuito da enfermaria. Consulte a ficha de manutencao.", 5);
                return;
            }
            WasRemoved = true;
            Fuse.SetAvailable(true);
            GameDirector.Instance.CollectFuse(Fuse);
        }
    }
}
