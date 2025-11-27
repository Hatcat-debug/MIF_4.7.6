using System.Collections.Generic;
using DancingLineFanmade.Level;
using UnityEngine;

namespace InvertedWorldAssets.Scripts.Tube
{
    // 负责生成圆柱体表面的轨迹网格
    // [修改版] 实现了 Flat Shading (硬边) 效果，顶点不再共用
    // [修复] 解决了 MeshFilter.mesh 导致的内存泄漏报错
    public class TrailGenerator : MonoBehaviour
    {
        [SerializeField] private Player _sourcePlayer;
        [SerializeField] private MeshFilter _sourceFilter;

        [Header("Mesh Settings")]
        [SerializeField] private int _limitVerts = 60000; 
        [SerializeField] private int _limitBatches = 15;
        [SerializeField] private float _splitDist = 3f;

        [Tooltip("Fallback material")]
        public Material BaseMaterial;

        [HideInInspector] public Material ActiveMaterial;

        // 内部缓存结构
        private LinkedList<(Mesh mesh, Material mat)> _meshChain = new LinkedList<(Mesh, Material)>();
        private List<Vector3> _vCache;
        private List<int> _iCache;
        
        // 原始模型的拓扑数据
        private Vector3[] _templateVerts;
        private int[] _templateIndices;
        private int[] _perimeterOrder;

        private Vector3[] _lastRingPositions; 
        
        private Vector3 _lastStampPos;
        private bool _isRunning = false;
        private float _sqrSplitDist;
        private bool _setupDone = false;

        #region Public API

        public void StartGen()
        {
            if (!_setupDone) Setup();

            _isRunning = true;
            if (ActiveMaterial == null) ActiveMaterial = BaseMaterial;

            // 初始启动
            StartNewBatch(ActiveMaterial, renderInitialQuad: true);
            _lastStampPos = _sourceFilter.transform.position;
        }

        public void StopGen()
        {
            _isRunning = false;
        }

        public void SwitchTrailMaterial(Material newMat)
        {
            ActiveMaterial = newMat;
            if (_isRunning)
            {
                // 切换材质时，新建 Batch，但不渲染起始封口，仅用于承接位置
                StartNewBatch(newMat, renderInitialQuad: false);
            }
        }

        #endregion

        private void Setup()
        {
            if (_setupDone) return;
            if (_vCache != null) _vCache.Clear();

            _sqrSplitDist = _splitDist * _splitDist;
            if (_sourceFilter == null) return;

            // [Fix] 使用 sharedMesh 避免在编辑器模式下实例化副本导致内存泄漏
            Mesh tmpl = _sourceFilter.sharedMesh;
            if (tmpl == null) return; // 增加空检查

            _templateVerts = tmpl.vertices;
            _templateIndices = tmpl.triangles;

            int safeCap = Mathf.Clamp(_limitVerts, 4000, 65000);
            _vCache = new List<Vector3>(safeCap);
            _iCache = new List<int>(safeCap * 3);

            if (_sourcePlayer != null)
                _sourcePlayer.OnTurn.AddListener(OnTurnEvent);

            _perimeterOrder = CalculateRimOrder(_templateIndices, _templateVerts.Length);
            _setupDone = true;
        }

        private void OnDisable()
        {
            if (_sourcePlayer != null)
                _sourcePlayer.OnTurn.RemoveListener(OnTurnEvent);
        }

        private void Start() => Setup();

        /// <summary>
        /// 创建新的网格批次
        /// </summary>
        private void StartNewBatch(Material mat, bool renderInitialQuad)
        {
            Mesh m;
            if (_meshChain.Count >= _limitBatches)
            {
                m = _meshChain.First.Value.mesh;
                _meshChain.RemoveFirst();
                m.Clear();
            }
            else
            {
                m = new Mesh { name = "TubularSegment_HardEdge", hideFlags = HideFlags.DontSave };
                m.MarkDynamic();
            }

            _meshChain.AddLast((m, mat));
            _vCache.Clear();
            _iCache.Clear();

            // 获取当前这一帧的环形顶点位置（世界坐标）
            Vector3[] currentRing = GetCurrentRingWorldPositions();
            
            // 记录为“上一环”，供下一帧挤出使用
            _lastRingPositions = currentRing;

            // 如果需要渲染起始面（封口）
            if (renderInitialQuad && _perimeterOrder != null && _perimeterOrder.Length >= 3)
            {
                if (currentRing.Length == 4)
                {
                    AddQuadToCache(currentRing[0], currentRing[1], currentRing[2], currentRing[3]);
                }
            }
        }

        /// <summary>
        /// 获取当前 SourceFilter 对应周界顶点的世界坐标数组
        /// </summary>
        private Vector3[] GetCurrentRingWorldPositions()
        {
            Transform tr = _sourceFilter.transform;
            Matrix4x4 l2w = tr.localToWorldMatrix;
            int count = (_perimeterOrder != null) ? _perimeterOrder.Length : _templateVerts.Length;
            Vector3[] results = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                int idx = (_perimeterOrder != null) ? _perimeterOrder[i] : i;
                results[i] = l2w.MultiplyPoint3x4(_templateVerts[idx]);
            }
            return results;
        }

        private void OnTurnEvent()
        {
            if (_isRunning)
            {
                if ((_sourceFilter.transform.position - _lastStampPos).sqrMagnitude <= _sqrSplitDist)
                {
                    ExtrudeCurrentBatch();
                }
                StartNewBatch(ActiveMaterial, renderInitialQuad: true);
            }
        }

        private void ExtrudeCurrentBatch()
        {
            if (_meshChain.Count == 0) return;

            (Mesh activeMesh, _) = _meshChain.Last.Value;

            Vector3[] currentRing = GetCurrentRingWorldPositions();

            if (_lastRingPositions == null || _lastRingPositions.Length != currentRing.Length)
            {
                _lastRingPositions = currentRing;
                return;
            }

            int ringSize = currentRing.Length;
            if (_vCache.Count + (ringSize * 4) >= _limitVerts)
            {
                StartNewBatch(ActiveMaterial, renderInitialQuad: false); 
                (activeMesh, _) = _meshChain.Last.Value;
                return; 
            }

            for (int i = 0; i < ringSize; i++)
            {
                int next = (i + 1) % ringSize;

                Vector3 p1 = _lastRingPositions[i];
                Vector3 p2 = _lastRingPositions[next];
                Vector3 c1 = currentRing[i];
                Vector3 c2 = currentRing[next];

                AddQuadToCache(p1, c1, c2, p2);
            }

            activeMesh.SetVertices(_vCache);
            activeMesh.SetTriangles(_iCache, 0);
            activeMesh.RecalculateBounds();
            activeMesh.RecalculateNormals();

            _lastRingPositions = currentRing;
        }

        private void AddQuadToCache(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            int baseIdx = _vCache.Count;

            _vCache.Add(v0);
            _vCache.Add(v1);
            _vCache.Add(v2);
            _vCache.Add(v3);

            _iCache.Add(baseIdx);
            _iCache.Add(baseIdx + 1);
            _iCache.Add(baseIdx + 2);

            _iCache.Add(baseIdx);
            _iCache.Add(baseIdx + 2);
            _iCache.Add(baseIdx + 3);
        }

        private void Update()
        {
            if (!_isRunning || _meshChain.Count == 0) return;

            Vector3 curPos = _sourceFilter.transform.position;
            
            if ((curPos - _lastStampPos).sqrMagnitude > _sqrSplitDist)
            {
                StartNewBatch(ActiveMaterial, renderInitialQuad: true);
            }

            ExtrudeCurrentBatch();

            foreach (var node in _meshChain)
            {
                if (node.mat != null && node.mesh.vertexCount > 0)
                    Graphics.DrawMesh(node.mesh, Matrix4x4.identity, node.mat, 0);
            }

            _lastStampPos = curPos;
        }

        private int[] CalculateRimOrder(int[] tris, int vCount)
        {
            if (vCount <= 0 || tris == null || tris.Length < 6) return DefaultOrder(vCount);

            var edgeMap = new Dictionary<(int, int), int>();
            for (int i = 0; i < tris.Length; i += 3)
            {
                MarkEdge(tris[i], tris[i + 1]);
                MarkEdge(tris[i + 1], tris[i + 2]);
                MarkEdge(tris[i + 2], tris[i]);
            }

            var adj = new Dictionary<int, List<int>>();
            foreach (var kvp in edgeMap)
            {
                if (kvp.Value == 1)
                {
                    int u = kvp.Key.Item1;
                    int v = kvp.Key.Item2;
                    if (!adj.ContainsKey(u)) adj[u] = new List<int>();
                    if (!adj.ContainsKey(v)) adj[v] = new List<int>();
                    adj[u].Add(v);
                    adj[v].Add(u);
                }
            }

            if (adj.Count < 3) return DefaultOrder(vCount);

            List<int> path = new List<int>();
            int startNode = -1;
            foreach(var k in adj.Keys) { startNode = k; break; }

            int prev = -1;
            int curr = startNode;

            for (int i = 0; i < adj.Count; i++)
            {
                path.Add(curr);
                List<int> neighbors = adj[curr];
                int next = (neighbors.Count > 1 && neighbors[0] == prev) ? neighbors[1] : neighbors[0];
                
                prev = curr;
                curr = next;
                if (path.Count > 1 && curr == startNode) break;
            }

            if (path.Count != vCount) return DefaultOrder(vCount);
            return path.ToArray();

            void MarkEdge(int a, int b)
            {
                int min = Mathf.Min(a, b);
                int max = Mathf.Max(a, b);
                var key = (min, max);
                if (edgeMap.ContainsKey(key)) edgeMap[key]++;
                else edgeMap[key] = 1;
            }
        }

        private int[] DefaultOrder(int c)
        {
            int[] arr = new int[c];
            for (int i = 0; i < c; i++) arr[i] = i;
            return arr;
        }
    }
}