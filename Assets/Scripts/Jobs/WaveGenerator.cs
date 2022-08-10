using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class WaveGenerator : MonoBehaviour
{
	[Header("Wave Parameters")]
	[SerializeField] private float _waveScale = default;
	[SerializeField] private float _waveOffsetSpeed = default;
	[SerializeField] private float _waveHeight = default;

	[Header("References and Prefabs")]
	[SerializeField] private MeshFilter _waterMeshFilter = default;

	private Mesh _waterMesh = default;

	private NativeArray<Vector3> _waterVertices = default;
	private NativeArray<Vector3> _waterNormals = default;

	private JobHandle _meshModificationHandle = default;
	private UpdateMeshJob _meshModificationJob = default;

	private void Start()
	{
		_waterMesh = _waterMeshFilter.mesh;
		_waterMesh.MarkDynamic();

		_waterVertices = new NativeArray<Vector3>(_waterMesh.vertices, Allocator.Persistent);
		_waterNormals = new NativeArray<Vector3>(_waterMesh.normals, Allocator.Persistent);
	}

	private void Update()
	{
		_meshModificationJob = new UpdateMeshJob()
		{
			vertices = _waterVertices,
			normals = _waterNormals,
			offsetSpeed = _waveOffsetSpeed,
			time = Time.time,
			scale = _waveScale,
			height = _waveHeight
		};

		_meshModificationHandle = _meshModificationJob.Schedule(_waterVertices.Length, 64);
	}

	private void LateUpdate()
	{
		_meshModificationHandle.Complete();

		_waterMesh.SetVertices(_meshModificationJob.vertices);
		_waterMesh.RecalculateNormals();
	}

	private void OnDestroy()
	{
		_waterVertices.Dispose();
		_waterNormals.Dispose();
	}

	[BurstCompile]
	private struct UpdateMeshJob : IJobParallelFor
	{
		public NativeArray<Vector3> vertices;
		[ReadOnly] public NativeArray<Vector3> normals;

		public float offsetSpeed;
		public float scale;
		public float height;
		public float time;

		public void Execute(int i)
		{
			if(normals[i].z > 0f)
			{
				Vector3 vertex = vertices[i];
				float noiseValue = Noise(vertex.x * scale + offsetSpeed * time, vertex.y * scale + offsetSpeed * time);

				vertices[i] = new Vector3(vertex.x, vertex.y, noiseValue * height + 0.3f);
			}
		}

		private float Noise(float x, float y)
		{
			float2 pos = math.float2(x, y);
			return noise.snoise(pos);
		}
	}
}