using UnityEngine;

public class PlayerController : MonoBehaviour
{
	private float horizontalInput;
	private float speed = 20.0f;
	private float xRange = 20;
	public GameObject projectilePrefab;


	// Update is called once per frame
	void Update()
	{
		// Check for left and right bounds
		if(transform.position.x < -xRange)
		{
			transform.position = new Vector3(-xRange, transform.position.y, transform.position.z);
		}

		if(transform.position.x > xRange)
		{
			transform.position = new Vector3(xRange, transform.position.y, transform.position.z);
		}

		// Player movement left to right
		horizontalInput = Input.GetAxis("Horizontal");
		transform.Translate(horizontalInput * speed * Time.deltaTime * Vector3.right);


		if(Input.GetKeyDown(KeyCode.Space))
		{
			GameObject pooledProjectile = ObjectPooler.SharedInstance.GetPooledObject();

			if(pooledProjectile != null)
			{
				pooledProjectile.SetActive(true); // activate it
				pooledProjectile.transform.position = transform.position; // position it at player
			}
		}
	}
}
