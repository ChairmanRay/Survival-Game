using UnityEngine;
using System.Collections;

public class Zombie : MonoBehaviour 
{

	private float sightRange = 10;
	private float attackRange = 3;
	private float attackSpeed = 1;
	private float nextAttack = 0;
	private int attackDamage = 1;
	private Transform targetPlayer = null;
	private NavMeshAgent nav;
	
	void Awake () 
	{
		nav = GetComponent <NavMeshAgent> ();
	}

	void Update () 
	{
		GameObject[] targetList = GameObject.FindGameObjectsWithTag ("Player");
		if (targetPlayer == null && targetList.Length>0) {
			Transform closestPlayer = null;
			float closestRange = float.MaxValue;
			float range;
			foreach (GameObject target in targetList)
			{
				range = Vector3.Distance (this.gameObject.transform.position, target.transform.position);
				if(!closestPlayer || range < closestRange)
				{
					closestPlayer = target.transform;
					closestRange = range;
				}
			}
			targetPlayer = closestPlayer;
		} 
		else if (targetPlayer)
		{
			float range = Vector3.Distance (this.gameObject.transform.position, targetPlayer.position);
			if(range < attackRange)
			{
				if(Time.time > nextAttack)
				{
					nextAttack = Time.time + attackSpeed;
					StartCoroutine("Attack");
				}
			}
			else if(range < sightRange)
				nav.SetDestination (targetPlayer.position);
			else
			{
				nav.SetDestination (this.gameObject.transform.position);
				targetPlayer = null;
			}
		}
	}

	private IEnumerator Attack()
	{
		nav.velocity = new Vector3(0,0,0);
		this.gameObject.transform.localScale = new Vector3(1, 0.9f, 1);
		yield return new WaitForSeconds(0.3f);
		this.gameObject.transform.localScale = new Vector3(1, 1, 1);
	}
}
