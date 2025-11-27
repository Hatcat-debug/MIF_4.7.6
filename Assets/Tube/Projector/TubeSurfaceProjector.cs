using DancingLineFanmade.Level;
using UnityEngine;

namespace InvertedWorldAssets.Scripts.Tube
{
    // 负责处理核心的圆柱体映射逻辑
    public class TubeSurfaceProjector : MonoBehaviour
    {
        [Header("Linkage")]
        [SerializeField] private Player _targetAvatar;
        [SerializeField] private Transform _avatarProxy; // 映射后的虚拟变换
        [SerializeField] private Transform _tubePivot;   // 圆柱轴心参考

        [Header("Configuration")]
        [SerializeField] private Vector3 _axialVelocity; // 沿轴速度
        [SerializeField] private Vector3 _originOffset;  // 原始参考点
        [SerializeField] private float _tubeRadius = 5f;
        
        [SerializeField, Range(0f, 360f)] 
        private float _startAngle = 0f;

        [HideInInspector] public float OrbitOffset;

        private bool _isActive = false;
        private bool _landingArmed = false; // 是否预备着陆
        private Vector3 _lastProxyUp;
        
        private static readonly Vector3 _axisUp = Vector3.up;
        private const float _radCalc = Mathf.PI / 180f;

        private void Awake()
        {
            ResetState();
        }

        private void ResetState()
        {
            OrbitOffset = 0f;
            _isActive = false;
            _landingArmed = false;
        }

        #region Timeline Interface

        public void Activate()
        {
            _isActive = true;
            _landingArmed = false;
        }

        public void Deactivate()
        {
            _isActive = false;
            _landingArmed = false;
        }

        public void ArmLanding()
        {
            _landingArmed = true;
            if (_avatarProxy != null)
            {
                _lastProxyUp = _avatarProxy.up;
            }
        }

        public void ShiftOrbit(float deltaAngle)
        {
            OrbitOffset += deltaAngle;
            // 立即强制刷新一次位置，防止帧延迟导致的错位
            SyncCoordinates();
        }

        #endregion

        private void SyncCoordinates()
        {
            if (_targetAvatar == null || _tubePivot == null) return;

            Vector3 rawDelta = _targetAvatar.transform.position - _originOffset;
            rawDelta = Vector3.ProjectOnPlane(rawDelta, _axisUp);

            Vector3 axialComp = Vector3.Project(rawDelta, _axialVelocity);
            Vector3 radialComp = rawDelta - axialComp;

            // 计算高度位移
            float heightStep = axialComp.magnitude * Mathf.Sign(Vector3.Dot(_axialVelocity, axialComp));

            // 计算弧度位移
            float arcLen = radialComp.magnitude;
            float rawRad = arcLen / _tubeRadius;

            // 侧向判断
            float sideFactor = Mathf.Sign(Vector3.Cross(_axialVelocity, radialComp).y);
            if (radialComp.sqrMagnitude < 1e-6f) sideFactor = 1f;

            rawRad *= sideFactor;

            // 最终角度合成
            float finalRad = (_startAngle + OrbitOffset) * _radCalc + rawRad;

            Vector3 pivotUp = _tubePivot.up;
            Vector3 pivotFwd = _tubePivot.forward;

            Quaternion radialRot = Quaternion.AngleAxis(finalRad * Mathf.Rad2Deg, pivotUp);
            Vector3 normalDir = (radialRot * pivotFwd).normalized;

            // 应用变换
            _avatarProxy.position = _tubePivot.position + (pivotUp * heightStep) + (normalDir * _tubeRadius);
            _avatarProxy.rotation = Quaternion.LookRotation(pivotUp, normalDir);
        }

        private void Update()
        {
            if (!_isActive) return;

            SyncCoordinates();

            if (_landingArmed)
            {
                CheckLandingCondition();
            }

            _lastProxyUp = _avatarProxy.up;
        }

        private void CheckLandingCondition()
        {
            // 通过检测法线是否跨越世界上方来判断是否到达圆柱体顶部
            Vector3 xPrev = Vector3.Cross(_lastProxyUp, _axisUp);
            Vector3 xCurr = Vector3.Cross(_avatarProxy.up, _axisUp);

            if (Vector3.Dot(xPrev, xCurr) < 0f)
            {
                FinalizeLanding();
            }
        }

        private void FinalizeLanding()
        {
            // 注意：这里不要再调用 SyncCoordinates()，因为帧延迟会导致位置略微“滑过”顶点
            // 直接根据几何原理计算“完美的顶点位置”并吸附

            if (_targetAvatar != null && _avatarProxy != null && _tubePivot != null)
            {
                _targetAvatar.BreakLine();

                // 1. 计算当前在管子轴向上的进度（Height Step）
                // 我们假设玩家在这一帧的轴向位置是准确的，因为轴向运动通常是线性的
                Vector3 rawDelta = _targetAvatar.transform.position - _originOffset;
                Vector3 axialComp = Vector3.Project(rawDelta, _axialVelocity); 
                float heightStep = axialComp.magnitude * Mathf.Sign(Vector3.Dot(_axialVelocity, axialComp));

                // 2. 找到该高度下，圆柱体轴心的位置
                Vector3 axisCenterPos = _tubePivot.position + (_tubePivot.up * heightStep);

                // 3. 计算几何顶点 (Apex)
                // 既然我们在检测是否跨越 World Up，那么着陆点必然是圆柱体正上方 (Vector3.up)
                // 位置 = 轴心 + (世界向上 * 半径)
                Vector3 apexPos = axisCenterPos + (Vector3.down * _tubeRadius);

                // 4. 强制修正玩家位置
                _targetAvatar.transform.position = apexPos;
                
                // 5. 开启新线段，此时起点是完美的水平切点
                _targetAvatar.StartNewTail();
            }

            _isActive = false;
            _landingArmed = false;
        }
    }
}