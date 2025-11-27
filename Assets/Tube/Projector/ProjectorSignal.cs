using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace InvertedWorldAssets.Scripts.Tube.Projector
{
    [System.Serializable]
    public class ProjectorSignal : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<ProjectorBehaviour>.Create(graph, new ProjectorBehaviour());
        }
    }
}