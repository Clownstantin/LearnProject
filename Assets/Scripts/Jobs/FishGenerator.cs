using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

using random = Unity.Mathematics.Random;

public class FishGenerator : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Transform _waterObject = default;
	[SerializeField] private Transform _objectPrefab = default;

	[Header("Spawn Settings")]
	[SerializeField] private int _amountOfFish = default;
	[SerializeField] private Vector3 _spawnBounds = default;
	[SerializeField] private float _spawnHeight = default;
	[SerializeField] private int _swimChangeFrequency = default;

	[Header("Settings")]
	[SerializeField] private float _swimSpeed = default;
	[SerializeField] private float _turnSpeed = default;

	private Transform _myTransform = default;

	private NativeArray<Vector3> _velocities = default;
	private TransformAccessArray _transformAccessArray = default;

	private JobHandle _positionUpdateHandle = default;
	private PositionUpdateJob _positionUpdateJob = default;

	private void Start()
	{
		_myTransform = transform;

		_velocities = new NativeArray<Vector3>(_amountOfFish, Allocator.Persistent);
		_transformAccessArray = new TransformAccessArray(_amountOfFish);

		float halfBoundsX = _spawnBounds.x * 0.5f;
		float halfBoundsZ = _spawnBounds.z * 0.5f;

		for(int i = 0; i < _amountOfFish; i++)
		{
			float distanceX = Random.Range(-halfBoundsX, halfBoundsX);
			float distanceZ = Random.Range(-halfBoundsZ, halfBoundsZ);

			Vector3 spawnPoint = _myTransform.position + Vector3.up * _spawnHeight + new Vector3(distanceX, 0, distanceZ);
			Transform fishTransform = Instantiate(_objectPrefab, spawnPoint, Quaternion.identity);

			_transformAccessArray.Add(fishTransform);
		}
	}

	private void Update()
	{
		_positionUpdateJob = new PositionUpdateJob()
		{
			objectVelocities = _velocities,
			bounds = _spawnBounds,
			center = _waterObject.position,
			jobDeltaTime = Time.deltaTime,
			time = Time.time,
			swimSpeed = _swimSpeed,
			turnSpeed = _turnSpeed,
			swimChangeFrequency = _swimChangeFrequency,
			seed = System.DateTimeOffset.Now.Millisecond
		};

		_positionUpdateHandle = _positionUpdateJob.Schedule(_transformAccessArray);
	}

	private void LateUpdate() => _positionUpdateHandle.Complete();

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(transform.position + Vector3.up * _spawnHeight, _spawnBounds);
	}

	private void OnDestroy()
	{
		_velocities.Dispose();
		_transformAccessArray.Dispose();
	}

	[BurstCompile]
	private struct PositionUpdateJob : IJobParallelForTransform
	{
		public NativeArray<Vector3> objectVelocities;

		public Vector3 bounds;
		public Vector3 center;

		public float jobDeltaTime;
		public float time;
		public float swimSpeed;
		public float turnSpeed;
		public int swimChangeFrequency;

		public float seed;

		public void Execute(int i, TransformAccess transform)
		{
			Vector3 currentVel = objectVelocities[i];
			var randomGen = new random((uint)(i * time + 1 + seed));

			transform.position += jobDeltaTime * randomGen.NextFloat(0.3f, 1f) * swimSpeed *
								  transform.localToWorldMatrix.MultiplyVector(Vector3.forward);

			if(currentVel != Vector3.zero)
			{
				Quaternion lookRot = Quaternion.LookRotation(currentVel);
				transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, turnSpeed * jobDeltaTime);
			}

			Vector3 currentPos = transform.position;
			bool randomize = true;
			float halfBoundX = bounds.x * 0.5f;
			float halfBoundZ = bounds.z * 0.5f;

			if(currentPos.x > center.x + halfBoundX ||
			   currentPos.x < center.x - halfBoundX ||
			   currentPos.z > center.z + halfBoundZ ||
			   currentPos.z < center.z - halfBoundZ)
			{
				var internalPos = new Vector3(center.x + randomGen.NextFloat(-halfBoundX, halfBoundX) / 1.3f, 0f,
											  center.z + randomGen.NextFloat(-halfBoundZ, halfBoundZ) / 1.3f);

				currentVel = (internalPos - currentPos).normalized;
				objectVelocities[i] = currentVel;

				Quaternion lookRot = Quaternion.LookRotation(currentVel);
				transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, turnSpeed * jobDeltaTime * 2f);

				randomize = false;
			}

			if(randomize && randomGen.NextInt(0, swimChangeFrequency) <= 2)
				objectVelocities[i] = new Vector3(randomGen.NextFloat(-1f, 1f), 0, randomGen.NextFloat(-1f, 1f));
		}
	}
}