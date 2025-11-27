using UnityEngine.Timeline;

namespace InvertedWorldAssets.Scripts.Tube
{
    [TrackColor(0.85f, 0.15f, 0.15f)] // Reddish
    [TrackBindingType(typeof(TubeSurfaceProjector))]
    [TrackClipType(typeof(EventSignal))]
    public class EventTrack : TrackAsset 
    { 
    }
}