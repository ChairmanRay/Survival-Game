using UnityEngine;
using System.Collections;

public class Zombie : MonoBehaviour {

	float sightRange = 25;
	float attackRange = 10;
	float moveSpeed = 5;
	Transform target = null;
	NavMeshAgent nav;
	
	void Awake () {
		nav = GetComponent <NavMeshAgent> ();
	}

	void Update () {
		if (target == null && GameObject.FindGameObjectWithTag ("Player")) {
			target = GameObject.FindGameObjectWithTag ("Player").transform;
		} 
		else if (target)
		{
			nav.SetDestination (target.position);
		}
	}
}
