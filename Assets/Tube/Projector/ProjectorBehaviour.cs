using UnityEngine.Playables;

namespace InvertedWorldAssets.Scripts.Tube.Projector
{
    public class ProjectorBehaviour : PlayableBehaviour
    {
        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            var mapLogic = info.output.GetUserData() as TubeSurfaceProjector;
            if (mapLogic != null) mapLogic.Activate();
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            var mapLogic = info.output.GetUserData() as TubeSurfaceProjector;
            if (mapLogic != null) mapLogic.Deactivate();
        }
    }
}