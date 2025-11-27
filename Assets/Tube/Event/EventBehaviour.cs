using UnityEngine;
using UnityEngine.Playables;

namespace InvertedWorldAssets.Scripts.Tube
{
    public class EventBehaviour : PlayableBehaviour
    {
        public TubeCommand CommandType;
        public float OrbitDelta;
        public bool DoMaterialSwitch;
        public Material TargetMat;
        public TrailGenerator GeneratorRef;

        private bool _fired = false;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (_fired) return;
            
            // 获取轨道绑定的对象
            var surfaceMapper = info.output.GetUserData() as TubeSurfaceProjector;
            if (surfaceMapper == null) return;

            if (CommandType == TubeCommand.ShiftOrbit)
            {
                if (DoMaterialSwitch && GeneratorRef != null && TargetMat != null)
                {
                    GeneratorRef.SwitchTrailMaterial(TargetMat);
                }

                surfaceMapper.ShiftOrbit(OrbitDelta);
            }
            else if (CommandType == TubeCommand.Land)
            {
                surfaceMapper.ArmLanding();
            }

            _fired = true;
        }

        public override void OnGraphStart(Playable playable)
        {
            _fired = false;
        }
    }
}