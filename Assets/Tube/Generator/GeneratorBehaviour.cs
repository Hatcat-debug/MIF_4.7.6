using UnityEngine.Playables;

namespace InvertedWorldAssets.Scripts.Tube
{
    public class GeneratorBehaviour : PlayableBehaviour
    {
        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            var gen = info.output.GetUserData() as TrailGenerator;
            if (gen != null) gen.StartGen();
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            var gen = info.output.GetUserData() as TrailGenerator;
            if (gen != null) gen.StopGen();
        }
    }
}