using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace InvertedWorldAssets.Scripts.Tube
{
    [System.Serializable]
    public class GeneratorSignal : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<GeneratorBehaviour>.Create(graph, new GeneratorBehaviour());
        }
    }
}