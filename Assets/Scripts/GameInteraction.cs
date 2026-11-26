using UnityEngine;

namespace EscapeTheDark
{
    public enum HospitalCircuit { Reception, Ward, Exit }

    public abstract class GameInteraction : MonoBehaviour
    {
        public abstract string Prompt { get; }
        public virtual float HoldSeconds => 0;
        public abstract void Interact(PlayerMotor player);
    }
}
