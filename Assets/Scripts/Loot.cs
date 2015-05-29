using UnityEngine;
using System.Collections;

public class Loot : MonoBehaviour {

	private float range = 10.0f;
	
	private Transform t;
	private Transform player;
	
	public Material RegularMaterial;
	public Material OutlineMaterial;
	
	private void Awake()
	{
		t = this.transform;
	}
	
	void Update()
	{
		player = GameObject.FindGameObjectWithTag("Player").transform;
		if (Distance() < 15)
		{
			//Player is in range
			//print(player.name + " is " + Distance().ToString() + " units from " + t.name);
			GetComponent<Renderer>().material = OutlineMaterial;
		}
		else
		{
			//Player is out of range
			GetComponent<Renderer>().material = RegularMaterial;
		}
	}
	
	private float Distance()
	{
		return Vector3.Distance(t.position, player.position);
	}
}
