using UnityEngine;
using System.Collections;

public class Zombie : MonoBehaviour 
{

	private float sightRange = 10;
	private float attackRange = 3;
	private float attackSpeed = 1;
	private float nextAttack = 0;
	private int attackDamage = 1;
	private Transform target = null;
	private PlayerHealth targetHealth = null;
	private NavMeshAgent nav;
	
	void Awake () 
	{
		nav = GetComponent <NavMeshAgent> ();
	}

	void Update () 
	{
		if (target == null && GameObject.FindGameObjectWithTag ("Player")) {
			target = GameObject.FindGameObjectWithTag ("Player").transform;
			targetHealth = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<PlayerHealth>();
		} 
		else if (target)
		{
			float range = Vector3.Distance (this.gameObject.transform.position, target.position);
			if(range < attackRange)
			{
				if(Time.time > nextAttack)
				{
					nextAttack = Time.time + attackSpeed;
					StartCoroutine("Attack");
				}
			}
			else if(range < sightRange)
				nav.SetDestination (target.position);
			else
				nav.SetDestination (this.gameObject.transform.position);
		}
	}

	private IEnumerator Attack()
	{
		nav.velocity = new Vector3(0,0,0);
		this.gameObject.transform.localScale = new Vector3(1, 0.9f, 1);
		yield return new WaitForSeconds(0.3f);
		targetHealth.TakeDamage (attackDamage);
		this.gameObject.transform.localScale = new Vector3(1, 1, 1);
	}
}
