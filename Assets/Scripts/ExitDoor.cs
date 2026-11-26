using UnityEngine;

namespace EscapeTheDark
{
    public sealed class ExitDoor : GameInteraction
    {
        public bool IsUnlocked { get; private set; }
        public override string Prompt => IsUnlocked ? "Segure [E] para acionar a saida" : "[E] Examinar saida sem energia";
        public override float HoldSeconds => IsUnlocked ? 2f : 0;
        private Renderer indicator;
        public override void Interact(PlayerMotor player)
        {
            if (IsUnlocked) GameDirector.Instance.ReachExit();
            else GameDirector.Instance.ShowMessage("A fechadura precisa dos tres circuitos. Consulte o quadro na recepcao.", 5);
        }
        public void SetIndicator(Renderer lightRenderer) => indicator = lightRenderer;
        public void Unlock()
        {
            IsUnlocked = true;
            if (indicator)
            {
                indicator.material.color = new Color(0.1f, 0.9f, 0.55f);
                indicator.material.SetColor("_EmissionColor", new Color(0.1f, 2.5f, 1.1f));
            }
        }
    }
}
