using UnityEngine;
using UnityEngine.InputSystem;

namespace EscapeTheDark
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public string Prompt { get; private set; }
        public float InteractionProgress { get; private set; }
        public GameInteraction FocusedInteraction { get; private set; }
        public bool IsFlashlightOn => flashlightControl ? flashlightControl.IsOn : flashlight && flashlight.enabled;
        public bool HasFlashlightBattery => flashlightControl && flashlightControl.HasBattery;
        public bool HasTriedEmptyFlashlight => flashlightControl && flashlightControl.HasTriedEmptySwitch;
        public bool IsSprinting { get; private set; }
        public bool IsMoving { get; private set; }
        private CharacterController controller;
        private Camera view;
        private Light flashlight;
        private FlashlightController flashlightControl;
        private Renderer flashlightLens;
        private AudioSource stepSource;
        private AudioClip[] walkSteps;
        private AudioClip[] runSteps;
        private float stepTimer;
        private float bobPhase;
        private float pitch;
        private float verticalSpeed;
        private Transform flashlightRig;
        private Vector3 flashlightRestPosition;
        private float movementBlend;
        private float sprintBlend;
        private float baseFieldOfView;
        private float holdElapsed;
        private bool holdCompleted;
        private bool suppressInteraction;
        private readonly RaycastHit[] interactionHits = new RaycastHit[16];

        [Header("Apresentacao visual")]
        [Range(0f, 1f)] public float cameraMotion = 0.65f;
        [Range(0f, 1f)] public float flashlightMotion = 0.8f;

        public void Configure(Camera playerCamera, Light beam)
        {
            controller = GetComponent<CharacterController>();
            view = playerCamera;
            flashlight = beam;
            baseFieldOfView = view.fieldOfView;
        }

        public void SetFootsteps(AudioClip[] walking, AudioClip[] running)
        {
            walkSteps = walking;
            runSteps = running;
            stepSource = gameObject.AddComponent<AudioSource>();
            stepSource.playOnAwake = false;
            stepSource.volume = 0.24f;
        }

        public void SetFlashlightVisual(Renderer lens)
        {
            flashlightLens = lens;
        }

        public void SetFlashlightRig(Transform rig)
        {
            flashlightRig = rig;
            flashlightRestPosition = rig.localPosition;
        }

        public void SetFlashlightController(FlashlightController control) => flashlightControl = control;

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null || view == null) return;

            Vector2 delta = mouse.delta.ReadValue() * 0.11f;
            transform.Rotate(0, delta.x, 0);
            pitch = Mathf.Clamp(pitch - delta.y, -85, 85);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1;
            if (keyboard.sKey.isPressed) input.y -= 1;
            if (keyboard.dKey.isPressed) input.x += 1;
            if (keyboard.aKey.isPressed) input.x -= 1;
            input = Vector2.ClampMagnitude(input, 1);
            bool sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            Vector3 motion = (transform.forward * input.y + transform.right * input.x) * (sprint ? 5.8f : 3.6f);
            verticalSpeed = controller.isGrounded ? -1 : verticalSpeed - 18 * Time.deltaTime;
            motion.y = verticalSpeed;
            controller.Move(motion * Time.deltaTime);

            bool moving = input.sqrMagnitude > 0.01f && controller.isGrounded;
            IsMoving = moving;
            IsSprinting = moving && sprint;
            if (moving)
            {
                bobPhase += Time.deltaTime * (sprint ? 15f : 10f);
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0)
                {
                    AudioClip[] choices = sprint ? runSteps : walkSteps;
                    if (stepSource && choices != null && choices.Length > 0)
                        stepSource.PlayOneShot(choices[Random.Range(0, choices.Length)]);
                    stepTimer = sprint ? 0.33f : 0.52f;
                }
            }
            else stepTimer = 0;
            AnimatePresentation(delta);

            if (keyboard.fKey.wasPressedThisFrame)
            {
                if (flashlightControl) flashlightControl.Toggle();
                else flashlight.enabled = !flashlight.enabled;
            }
            Prompt = null;
            GameInteraction target = null;
            int hitCount = Physics.RaycastNonAlloc(view.transform.position, view.transform.forward, interactionHits,
                3.2f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                var hit = interactionHits[i];
                if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
                nearest = hit.distance;
                // A parede ou a porta mais proxima continua bloqueando a interacao.
                target = hit.collider.GetComponentInParent<GameInteraction>();
            }
            UpdateInteraction(target, keyboard);
        }

        private void UpdateInteraction(GameInteraction target, Keyboard keyboard)
        {
            if (suppressInteraction)
            {
                if (!keyboard.eKey.isPressed) suppressInteraction = false;
                return;
            }
            if (target != FocusedInteraction || !keyboard.eKey.isPressed)
            {
                holdElapsed = 0;
                holdCompleted = false;
                InteractionProgress = 0;
            }
            FocusedInteraction = target;
            if (!target) return;
            Prompt = target.Prompt;
            if (target.HoldSeconds <= 0)
            {
                if (keyboard.eKey.wasPressedThisFrame) target.Interact(this);
                return;
            }
            if (!keyboard.eKey.isPressed || holdCompleted) return;
            holdElapsed += Time.deltaTime;
            InteractionProgress = Mathf.Clamp01(holdElapsed / target.HoldSeconds);
            if (InteractionProgress < 1) return;
            holdCompleted = true;
            target.Interact(this);
        }

        public void SuppressInteractionUntilRelease() => suppressInteraction = true;

        private void OnDisable()
        {
            FocusedInteraction = null;
            InteractionProgress = 0;
            holdElapsed = 0;
            holdCompleted = false;
        }

        private void AnimatePresentation(Vector2 lookDelta)
        {
            // Apenas a apresentacao usa a velocidade real. Deteccao, passos e motor permanecem iguais.
            Vector3 velocity = controller.velocity;
            velocity.y = 0;
            bool walking = controller.isGrounded && velocity.sqrMagnitude > 0.05f;
            float blend = 1f - Mathf.Exp(-Time.deltaTime * 9f);
            movementBlend = Mathf.Lerp(movementBlend, walking ? 1f : 0f, blend);
            sprintBlend = Mathf.Lerp(sprintBlend, walking && IsSprinting ? 1f : 0f, blend);
            float breath = Mathf.Sin(Time.time * 1.8f) * 0.003f * (1f - movementBlend);
            float stride = Mathf.Sin(bobPhase);
            float lateral = Mathf.Cos(bobPhase * 0.5f);
            float bob = stride * Mathf.Lerp(0.019f, 0.034f, sprintBlend) * movementBlend;
            Vector3 target = new Vector3(lateral * 0.011f * movementBlend,
                1.62f + (bob + breath) * cameraMotion, 0);
            target.x *= cameraMotion;
            view.transform.localPosition = Vector3.Lerp(view.transform.localPosition, target, blend);
            view.fieldOfView = Mathf.Lerp(view.fieldOfView,
                baseFieldOfView + sprintBlend * 3f * cameraMotion, blend * 0.65f);

            if (!flashlightRig) return;
            Vector3 sway = new Vector3(-Mathf.Clamp(lookDelta.x, -4f, 4f) * 0.004f,
                -Mathf.Clamp(lookDelta.y, -4f, 4f) * 0.003f + breath, 0);
            sway += new Vector3(lateral * 0.012f, -Mathf.Abs(stride) * 0.016f, 0) * movementBlend;
            sway += new Vector3(0.012f, -0.035f, -0.015f) * sprintBlend;
            flashlightRig.localPosition = Vector3.Lerp(flashlightRig.localPosition,
                flashlightRestPosition + sway * flashlightMotion, blend);
            Quaternion rotation = Quaternion.Euler(
                (Mathf.Clamp(lookDelta.y, -5f, 5f) * 0.65f + sprintBlend * 5f) * flashlightMotion,
                -Mathf.Clamp(lookDelta.x, -5f, 5f) * 0.8f * flashlightMotion,
                lateral * movementBlend * 1.8f * flashlightMotion);
            flashlightRig.localRotation = Quaternion.Slerp(flashlightRig.localRotation, rotation, blend);
        }
    }
}
