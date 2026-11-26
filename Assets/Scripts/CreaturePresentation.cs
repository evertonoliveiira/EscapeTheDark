using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace EscapeTheDark
{
    // Usa os clips originais sem o ciclo de demonstracao (ataques, morte e saltos).
    // O NavMeshAgent continua sendo o unico responsavel pelo deslocamento.
    public sealed class CreaturePresentation : MonoBehaviour
    {
        private sealed class Rig
        {
            public PlayableGraph graph;
            public AnimationMixerPlayable mixer;
            public AnimationClipPlayable idle;
            public AnimationClipPlayable walk;
            public AnimationClipPlayable hurry;
        }

        private readonly List<Rig> rigs = new List<Rig>();
        private readonly List<AnimationClip> ownedClips = new List<AnimationClip>();
        private NavMeshAgent agent;
        private float locomotion;
        private float urgency;
        public bool HasLocomotion => rigs.Count > 0;

        public void Configure(NavMeshAgent source, GameObject visual)
        {
            agent = source;
            foreach (var animator in visual.GetComponentsInChildren<Animator>(true))
            {
                if (!animator.runtimeAnimatorController) continue;
                AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
                AnimationClip idle = Find(clips, "Idle1_Action");
                AnimationClip walk = Find(clips, "Walk1_Action");
                AnimationClip hurry = Find(clips, "Walk2_Action");
                if (!idle || !walk) continue;
                if (!hurry) hurry = walk;

                animator.applyRootMotion = false;
                animator.runtimeAnimatorController = null;
                var rig = new Rig { graph = PlayableGraph.Create("Locomocao da criatura") };
                rig.graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                rig.mixer = AnimationMixerPlayable.Create(rig.graph, 3);
                rig.idle = Loop(rig.graph, idle);
                rig.walk = Loop(rig.graph, walk);
                rig.hurry = Loop(rig.graph, hurry);
                rig.graph.Connect(rig.idle, 0, rig.mixer, 0);
                rig.graph.Connect(rig.walk, 0, rig.mixer, 1);
                rig.graph.Connect(rig.hurry, 0, rig.mixer, 2);
                rig.mixer.SetInputWeight(0, 1);
                var output = AnimationPlayableOutput.Create(rig.graph, "Corpo", animator);
                output.SetSourcePlayable(rig.mixer);
                rig.graph.Play();
                rigs.Add(rig);
            }
        }

        private AnimationClipPlayable Loop(PlayableGraph graph, AnimationClip source)
        {
            // Clona somente em memoria para manter os FBX e os prefabs importados intactos.
            var clip = Instantiate(source);
            clip.wrapMode = WrapMode.Loop;
            ownedClips.Add(clip);
            var playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(false);
            return playable;
        }

        private static AnimationClip Find(AnimationClip[] clips, string suffix)
        {
            foreach (var clip in clips)
                if (clip && clip.name.EndsWith(suffix, System.StringComparison.Ordinal)) return clip;
            return null;
        }

        private void Update()
        {
            bool playing = GameDirector.Instance && GameDirector.Instance.IsPlaying;
            float speed = playing && agent && agent.isOnNavMesh && !agent.isStopped ? agent.velocity.magnitude : 0;
            float blend = 1f - Mathf.Exp(-Time.deltaTime * 7f);
            locomotion = Mathf.Lerp(locomotion, Mathf.Clamp01(speed / 0.65f), blend);
            urgency = Mathf.Lerp(urgency, Mathf.InverseLerp(2.0f, 3.25f, speed), blend);
            foreach (var rig in rigs)
            {
                rig.mixer.SetSpeed(playing ? 1 : 0);
                rig.mixer.SetInputWeight(0, 1f - locomotion);
                rig.mixer.SetInputWeight(1, locomotion * (1f - urgency));
                rig.mixer.SetInputWeight(2, locomotion * urgency);
                rig.walk.SetSpeed(Mathf.Clamp(speed / 1.55f, 0.65f, 1.6f));
                rig.hurry.SetSpeed(Mathf.Clamp(speed / 2.4f, 0.8f, 1.5f));
                Repeat(rig.idle);
                Repeat(rig.walk);
                Repeat(rig.hurry);
            }
        }

        private static void Repeat(AnimationClipPlayable playable)
        {
            // Alguns clips do pacote nao trazem Loop Time; a repeticao e local ao playable.
            double length = playable.GetAnimationClip().length;
            if (length > 0 && playable.GetTime() >= length)
                playable.SetTime(playable.GetTime() % length);
        }

        private void OnDestroy()
        {
            foreach (var rig in rigs) if (rig.graph.IsValid()) rig.graph.Destroy();
            foreach (var clip in ownedClips) if (clip) Destroy(clip);
        }
    }
}
