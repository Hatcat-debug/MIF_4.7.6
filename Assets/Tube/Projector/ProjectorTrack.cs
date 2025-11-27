using UnityEngine.Timeline;

namespace InvertedWorldAssets.Scripts.Tube.Projector
{
    [TrackColor(0.3f, 0.6f, 0.95f)] // Blueish
    [TrackBindingType(typeof(TubeSurfaceProjector))]
    [TrackClipType(typeof(ProjectorSignal))]
    public class ProjectorTrack : TrackAsset 
    { 
    }
}