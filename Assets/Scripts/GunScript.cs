using UnityEngine;
using System.Collections;

public class GunScript : MonoBehaviour {
	
	public float MinDamage;
	public float MaxDamage;
	
	public Vector3 LocalHipPosition; //Position of the gun when not aiming
	public Vector3 LocalAimPosition; //Position of the gun when aiming
	public float KickAmount; //How far back does the gun move when shot
	
	public float FOVwhenAiming; //How will the camera zoom
	
	public float Range; //How far does the gun do damage at
	public int BulletsInClip; //How many bullets does this gun hold
	public float FiringSpeed; //How fast does the gun shoot
	
	void Start ()
	{
		
	}
	
	void Update ()
	{
		
	}
}
