using UnityEngine;
using System.Collections;

public class PlayerScript : MonoBehaviour {
	
	public GameObject MyCamera;
	public GameObject MyBody;
	//Weapons and quick slots
	public GameObject PrimaryWeapon;//Quickslot 1
	private bool PrimaryWeaponActive = true;
	
	public GameObject SecondaryWeapon;//Quickslot 2
	private bool SecondaryWeaponActive = false;
	public GameObject QuickSlot3;
	public GameObject QuickSlot4;
	public GameObject QuickSlot5;
	public GameObject QuickSlot6;
	
	private GameObject LastHitBy = null; //Who was the last person to hit you
	private bool IamDead = false;
	
	private Vector3 fwd; //The direction the raycast will go when you fire
	
	//Health, hunger, thirst, stamina
	public float Health = 100;
	public float Stamina = 100;
	public float HungerLevel = 100;
	public float ThirstLevel = 100;
	
	//GUI crosshairs
	public Texture Crosshair;
	public Texture HitMarker;
	private bool HitPlayer = false;
	
	//Walk and run speeds
	public float PlayerSpeedWalk = 5.0f;
	public float PlayerSpeedSprint = 7.0f;
	
	public float gravity = 10.0f;
	public float jumpHeight = 2.0f;
	
	public float maxVelocityChange = 15.0f;
	
	public bool Crouching = false;
	public bool Prone = false;
	public bool IsSprinting = false;
	
	private bool WalkingDiagonal = false;
	public bool TouchingTheGround = false;
	private bool grounded = false;
	
	public Vector3 targetVelocity = Vector3.zero;
	
	//Everything to do with sync
	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	private Vector3 syncStartPosition = Vector3.zero;
	private Vector3 syncEndPosition = Vector3.zero;
	private Quaternion syncStartRotation = Quaternion.identity;
	private Quaternion syncEndRotation = Quaternion.identity;
	
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		Vector3 syncPosition = transform.position;
		Vector3 syncVelocity = Vector3.zero;
		Quaternion syncRotation = transform.rotation;
		
		if (stream.isWriting)
		{
			syncPosition = GetComponent<Rigidbody>().position;
			stream.Serialize(ref syncPosition);
			
			syncPosition = GetComponent<Rigidbody>().velocity;
			stream.Serialize(ref syncVelocity);
			
			syncRotation = GetComponent<Rigidbody>().rotation;
			stream.Serialize(ref syncRotation);
		}
		else
		{
			stream.Serialize(ref syncPosition);
			stream.Serialize(ref syncVelocity);
			stream.Serialize(ref syncRotation);
			
			syncTime = 0f;
			syncDelay = Time.time - lastSynchronizationTime;
			lastSynchronizationTime = Time.time;
			
			syncEndPosition = syncPosition + syncVelocity * syncDelay;
			syncStartPosition = GetComponent<Rigidbody>().position;
			syncEndRotation = syncRotation;
			syncStartRotation = GetComponent<Rigidbody>().rotation;
		}
	}
	
	void Awake () 
	{
		lastSynchronizationTime = Time.time;
		
		if(GetComponent<NetworkView>().isMine)
		{
			MyCamera.GetComponent<Camera>().enabled = true;
			GetComponent<Rigidbody>().isKinematic = false;
		}
		else
		{
			MyCamera.GetComponent<Camera>().enabled = false;
			GetComponent<Rigidbody>().isKinematic = true;
		}
		
		transform.tag = "Player";
		
		GetComponent<Rigidbody>().freezeRotation = true;
		GetComponent<Rigidbody>().useGravity = false;
	}
	
	void Update()
	{
		if (GetComponent<NetworkView>().isMine)
		{
			InputMovement();
		}
		else
		{
			SyncedMovement();
		}
	}
	
	[RPC]
	void setPlayerName(string playerName)
	{
		transform.name = playerName;
	}
	
	void InputMovement() 
	{
		//Lock Z rotation
		transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0);
		//Change rotation based on mouse movement
		transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X"), 0) * Time.deltaTime * 200);
		PrimaryWeapon.transform.localPosition = new Vector3(0.28f, -0.34f, 0.9f);
		
		//Check to see what my health is at and tell other players
		Health = Mathf.Round(Health * 1f) / 1f;
		GetComponent<NetworkView>().RPC("GetMyHealth", RPCMode.All);
		
		RaycastHit hit;
		
		//If my player is touching the ground
		if (grounded) 
		{
			if((Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D)) || (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A)) || (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.D)) || (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.A)))
			{
				if(Crouching == false && Prone == false && IsSprinting == false)
				{
					WalkingDiagonal = true;
					//Reduce speed because we are going diagonal
					//5 * 0.7071 = 3.54
					PlayerSpeedWalk = 3.54f;
				}
				else if(IsSprinting == true)
				{
					//7 * 0.7071 = 4.95
					PlayerSpeedWalk = 4.95f;
				}
			}
			else
			{
				WalkingDiagonal = false;
			}
			// Calculate how fast we should be moving
			Vector3 targetVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
			targetVelocity = transform.TransformDirection(targetVelocity);
			targetVelocity *= PlayerSpeedWalk;
			
			//Apply a force that attempts to reach our target velocity
			Vector3 velocity = GetComponent<Rigidbody>().velocity;
			Vector3 velocityChange = (targetVelocity - velocity);
			velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
			velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
			velocityChange.y = 0;
			GetComponent<Rigidbody>().AddForce(velocityChange, ForceMode.VelocityChange);
			
			//Jumping
			if(Input.GetButtonDown("Jump")) 
			{
				Vector3 DownDir = transform.TransformDirection(Vector3.down);
				if(Physics.Raycast(transform.position, DownDir, out hit, 1.05f))
				{
					if(hit.collider.name == "Terrain")
					{
						if(WalkingDiagonal == true)
						{
							PlayerSpeedWalk = 3.54f;
						}
						else
						{
							PlayerSpeedWalk = 5;
						}
						
						GetComponent<Rigidbody>().velocity = new Vector3(velocity.x, CalculateJumpVerticalSpeed(), velocity.z);
					}
				}
			}
			
			//Sprint Movement
			if(Input.GetKey(KeyCode.LeftShift) && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)) && Prone == false && Crouching == false && Stamina > 0)
			{
				IsSprinting = true;
				Stamina -= (1.2f * Time.deltaTime); //You will run out of sprint in 1:40
				if(WalkingDiagonal == true)
				{
					PlayerSpeedWalk = 4.95f;
				}
				else
				{
					PlayerSpeedWalk = 7;
				}
				
				if(HungerLevel > 0)
				{
					//This formula will change
					HungerLevel -= (0.14f * Time.deltaTime); //Down 1 every 10 seconds
				}
				
				if(ThirstLevel > 0)
				{
					//This formula will change
					ThirstLevel -= (0.18f * Time.deltaTime); //Down 1 every 8 seconds
				}
			}
			else
			{
				IsSprinting = false;
				
				//While standing/walking your hunger and thirst goes down slower
				if(HungerLevel > 0)
				{
					//This formula will change
					HungerLevel -= (0.08f * Time.deltaTime); //Down 1 every 15 seconds
				}
				
				if(ThirstLevel > 0)
				{
					//This formula will change
					ThirstLevel -= (0.08f * Time.deltaTime); //Down 1 every 15 seconds
				}
			}
			
			if(IsSprinting == false && Stamina < 100)
			{
				//Stamina needs to regenerate slower
				Stamina += (1f * Time.deltaTime); //Sprint will regenerate from 0 to 100 in 2 minutes
			}
			
			//Crouching
			if(Input.GetKey(KeyCode.LeftControl) && Prone == false)
			{
				MyBody.transform.localScale = new Vector3(1, 0.5f, 1);
				PlayerSpeedWalk = 3.0f;
				PlayerSpeedSprint = 3.0f;
				Crouching = true;
			}
			else
			{
				MyBody.transform.localScale = new Vector3(1, 1, 1);
				Crouching = false;
			}
			
			//Going prone
			if(Input.GetKeyDown(KeyCode.Z))
			{
				Prone = !Prone;
			}
			
			if(Prone == true)
			{
				MyBody.transform.rotation = Quaternion.Euler(90, transform.eulerAngles.y, transform.eulerAngles.z);
				MyCamera.transform.localPosition = new Vector3(0, 0.4f, 0.6f);
				PlayerSpeedWalk = 1.5f;
				PlayerSpeedSprint = 1.5f;
			}
			else if(Prone == false)
			{
				MyBody.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, transform.eulerAngles.z);
				MyCamera.transform.localPosition = new Vector3(0, 1, 0);
			}
			
			if(Crouching == false && Prone == false && WalkingDiagonal == false && IsSprinting == false)
			{
				PlayerSpeedWalk = 5.0f;
				PlayerSpeedSprint = 7.0f;
			}
		}
		
		//Left click (firing)
		if(Input.GetKeyDown(KeyCode.Mouse0))// || Input.GetKey(KeyCode.Mouse0))) && weaponData.AmmoInClip >= 1 && weaponData.CanShoot == true)
		{
			if(PrimaryWeaponActive == true)
			{
				fwd = PrimaryWeapon.transform.TransformDirection(Vector3.forward);
			}
			else if(SecondaryWeaponActive == true)
			{
				fwd = SecondaryWeapon.transform.TransformDirection(Vector3.forward);
			}
			
			if(Physics.Raycast(MyCamera.GetComponent<PlayerCamera>().ActiveCamera.transform.position, fwd, out hit, PrimaryWeapon.GetComponent<GunScript>().Range))
			{
				//If we hit a player
				if(hit.collider.tag == "Player")
				{
					HitPlayer = true;
					
					GameObject PlayersBody = hit.collider.gameObject;
					NetworkView targetID = PlayersBody.transform.parent.GetComponent<NetworkView>();
					targetID.RPC("RecievingDamage", RPCMode.All, Random.Range(PrimaryWeapon.GetComponent<GunScript>().MinDamage, PrimaryWeapon.GetComponent<GunScript>().MaxDamage));
					targetID.RPC("LastHitByPlayer", RPCMode.All, gameObject.name);
				}
				
				//If we hit a zombie
				if(hit.collider.name == "Zombie")
				{
					print ("Hit a zombie");
				}
				
				//If we hit the ground
				if(hit.collider.name == "Terrain")
				{
					print ("Hit the ground");
				}
			}
		}
		
		//Right click (aiming)
		if(Input.GetKey(KeyCode.Mouse1))
		{
			PrimaryWeapon.transform.localPosition = PrimaryWeapon.GetComponent<GunScript>().LocalAimPosition;
			MyCamera.GetComponent<Camera>().fieldOfView = PrimaryWeapon.GetComponent<GunScript>().FOVwhenAiming;
		}
		else
		{
			PrimaryWeapon.transform.localPosition = PrimaryWeapon.GetComponent<GunScript>().LocalHipPosition;
			MyCamera.GetComponent<Camera>().fieldOfView = 60;
		}
		
		// We apply gravity manually for more tuning control
		GetComponent<Rigidbody>().AddForce(new Vector3 (0, -gravity * GetComponent<Rigidbody>().mass, 0));
		
		grounded = false;
	}
	
	private void SyncedMovement()
	{
		syncTime += Time.deltaTime;
		
		GetComponent<Rigidbody>().position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
		GetComponent<Rigidbody>().rotation = Quaternion.Slerp(syncStartRotation, syncEndRotation, syncTime / syncDelay);
	}
	
	void OnCollisionStay () 
	{
		grounded = true;    
	}

	[RPC]
	public void GetMyHealth()
	{
		if(Health <= 0)
		{
			IamDead = true;
		}
	}
	
	[RPC]
	public void RecievingDamage(float ThisDamage)
	{
		Health -= ThisDamage;
	}
	
	[RPC]
	public void LastHitByPlayer(string ThisPlayerName)
	{
		print (ThisPlayerName);
		LastHitBy = GameObject.Find(ThisPlayerName);
	}
	
	//Calculates the jump height
	float CalculateJumpVerticalSpeed() 
	{
		// From the jump height and gravity we deduce the upwards speed 
		// for the character to reach at the apex.
		return Mathf.Sqrt(2 * jumpHeight * gravity);
	}
	
	void OnGUI()
	{
		if (GetComponent<NetworkView>().isMine)
		{
			GUI.skin.label.alignment = TextAnchor.UpperLeft;
			//Display your player name
			GUI.Label(new Rect(100, 80, 250, 200), ("Name: " + transform.name));
			//Display your player's health
			GUI.Label(new Rect(100, 100, 200, 200),"Health: " + Mathf.Round(Health * 1f) / 1f);
			//Display your player's stamina
			GUI.Label(new Rect(100, 120, 200, 200),"Stamina: " + (int)Stamina);
			//Display your player's hunger
			GUI.Label(new Rect(100, 140, 200, 200),"Hunger: " + (int)HungerLevel);
			//Display your player's thirst
			GUI.Label(new Rect(100, 160, 200, 200),"Thirst: " + (int)ThirstLevel);
			
			//See who you were killed by
			if(IamDead == true)
			{
				GUI.skin.label.alignment = TextAnchor.MiddleCenter;
				GUI.Label(new Rect(Screen.width / 2 - 140, Screen.height / 2 - 100, 280, 200),"<size=20>You are dead\n" + "Killed by: " + LastHitBy.transform.name + "</size>");
				GUI.skin.label.alignment = TextAnchor.UpperLeft;
			}
			
			//Crosshair texture
			GUI.DrawTexture(new Rect(Screen.width / 2 - 22.5f, Screen.height / 2 - 22.5f, 45, 45), Crosshair, ScaleMode.StretchToFill, true, 10.0F);
			//Create a crosshair so you know you hit someone
			if(HitPlayer == true)
			{
				GUI.DrawTexture(new Rect(Screen.width / 2 - 12.5f, Screen.height / 2 - 12.5f, 25, 25), HitMarker, ScaleMode.StretchToFill, true, 10.0F);
				StartCoroutine(HitPlayerWait());
			}
		}
	}
	
	IEnumerator HitPlayerWait()
	{
		yield return new WaitForSeconds(0.1f);
		
		HitPlayer = false;
	}
	
	void OnApplicationQuit()
	{
		if (GetComponent<NetworkView>().isMine)
		{
			Network.Destroy(gameObject);
			Network.Disconnect();
		}
	}
}
