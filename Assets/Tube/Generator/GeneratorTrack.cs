using UnityEngine.Timeline;

namespace InvertedWorldAssets.Scripts.Tube
{
    [TrackColor(0.2f, 0.8f, 0.4f)] // Greenish
    [TrackBindingType(typeof(TrailGenerator))]
    [TrackClipType(typeof(GeneratorSignal))]
    public class GeneratorTrack : TrackAsset 
    { 
    }
}