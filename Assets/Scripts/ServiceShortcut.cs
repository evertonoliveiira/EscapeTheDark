using UnityEngine;
using UnityEngine.AI;

namespace EscapeTheDark
{
    public sealed class ServiceShortcut : GameInteraction
    {
        public bool IsUnlocked { get; private set; }
        public bool IsPassable { get; private set; }
        private Transform shutter;
        private Collider blocker;
        private NavMeshObstacle obstacle;
        private Renderer indicator;
        private float opening;
        public override string Prompt => IsUnlocked ? "Corredor de servico liberado" : "[E] Examinar acesso de servico";

        public void Configure(Transform leaf, Collider physicalBlocker, NavMeshObstacle navigationBlocker, Renderer light)
        {
            shutter = leaf;
            blocker = physicalBlocker;
            obstacle = navigationBlocker;
            indicator = light;
        }

        public void Unlock()
        {
            if (IsUnlocked) return;
            IsUnlocked = true;
            var properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", new Color(0.12f, 0.45f, 0.27f));
            properties.SetColor("_EmissionColor", new Color(0.035f, 0.15f, 0.08f));
            indicator.SetPropertyBlock(properties);
        }

        private void Update()
        {
            if (!IsUnlocked || IsPassable || !GameDirector.Instance || !GameDirector.Instance.IsPlaying) return;
            opening = Mathf.MoveTowards(opening, 1f, Time.deltaTime / 1.2f);
            shutter.localPosition = Vector3.left * Mathf.SmoothStep(0, 2.65f, opening);
            if (opening < 1) return;
            blocker.enabled = false;
            obstacle.enabled = false;
            IsPassable = true;
        }

        public override void Interact(PlayerMotor player)
        {
            if (!IsUnlocked) GameDirector.Instance.ShowMessage("O acesso de servico abre ao restaurar o circuito da recepcao.", 5);
        }
    }
}
