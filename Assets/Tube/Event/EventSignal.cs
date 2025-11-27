using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace InvertedWorldAssets.Scripts.Tube
{
    public enum TubeCommand
    {
        ShiftOrbit,
        Land
    }

    [System.Serializable]
    public class EventSignal : PlayableAsset, ITimelineClipAsset
    {
        [Header("Command Config")]
        public TubeCommand Command = TubeCommand.ShiftOrbit;

        [Header("Orbit Params")]
        public float DeltaAngle = 180f;
        public bool SwapMaterial = false;
        
        public ExposedReference<Material> NextMaterial;
        public ExposedReference<TrailGenerator> TargetGenerator;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var p = ScriptPlayable<EventBehaviour>.Create(graph, new EventBehaviour());
            var worker = p.GetBehaviour();

            worker.CommandType = Command;
            worker.OrbitDelta = DeltaAngle;
            worker.DoMaterialSwitch = SwapMaterial;
            worker.TargetMat = NextMaterial.Resolve(graph.GetResolver());
            worker.GeneratorRef = TargetGenerator.Resolve(graph.GetResolver());

            return p;
        }
    }
}