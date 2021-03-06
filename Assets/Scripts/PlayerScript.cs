﻿using UnityEngine;
using System.Collections;

public class PlayerScript : MonoBehaviour {
	
	public GameObject MyCamera;
	//public GameObject MyBody;
	//public GameObject MyHead;
	//Weapons and quick slots
	private bool LookingAtLoot = false;
	public GameObject LootObject;
	public float LootTimer;
	
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
	
	public Vector3 targetVelocity = Vector3.zero;
		
	void Awake () 
	{		
		if(GetComponent<NetworkView>().isMine)
		{
			MyCamera.GetComponent<Camera>().enabled = true;
		}
		else
		{
			MyCamera.GetComponent<Camera>().enabled = false;
		}
		
		transform.tag = "Player";
	}
	
	void Update()
	{
		if (GetComponent<NetworkView>().isMine)
		{
			InputMovement();
		}
	}
	
	[RPC]
	void setPlayerName(string playerName)
	{
		transform.name = playerName;
	}
	
	void InputMovement() 
	{
		PrimaryWeapon.transform.localPosition = new Vector3(0.28f, -0.34f, 0.9f);
		
		//Check to see what my health is at and tell other players
		Health = Mathf.Round(Health * 1f) / 1f;
		GetComponent<NetworkView>().RPC("GetMyHealth", RPCMode.All);
		
		if(Input.GetKeyDown(KeyCode.I))
		{
			//Open inventory
		}
		
		RaycastHit hit;
		
		if(PrimaryWeaponActive == true)
		{
			fwd = PrimaryWeapon.transform.TransformDirection(Vector3.forward);
		}
		else if(SecondaryWeaponActive == true)
		{
			fwd = SecondaryWeapon.transform.TransformDirection(Vector3.forward);
		}
		
		//Check to see what we are looking at
		if(Physics.Raycast(MyCamera.GetComponent<PlayerCamera>().ActiveCamera.transform.position, fwd, out hit, 4))
		{
			if(hit.collider.tag == "Loot")
			{
				LookingAtLoot = true;
				LootObject = hit.collider.gameObject;
			}
			else
			{
				LookingAtLoot = false;
				LootObject = null;
			}
		}
		
		//Picking up loot
		if(Input.GetKey(KeyCode.E) && LookingAtLoot == true)
		{
			LootTimer += (1.5f * Time.deltaTime);
			//It will take you 2 seconds to pick up an item
			if(LootTimer >= 2)
			{
				//Play "pick up sound" so you know you picked up the loot
				//...
				Transform MyInventoryObjects = transform.FindChild("MyInventoryObjects");
				LootObject.transform.parent = MyInventoryObjects.transform; //Parent loot to the player
				LootObject.GetComponent<Loot>().enabled = false; //Turn of its loot script
				LootObject.GetComponent<Renderer>().material = LootObject.GetComponent<Loot>().RegularMaterial; //Put its material back to normal
				LootObject.transform.localPosition = new Vector3(0, 0, 0); //Center it in the player
				LootObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); //Make its scale small
				LootObject = null;
			}
		}

		//Left click (firing)
		if(Input.GetKeyDown(KeyCode.Mouse0))// || Input.GetKey(KeyCode.Mouse0))) && weaponData.AmmoInClip >= 1 && weaponData.CanShoot == true)
		{
			if(Physics.Raycast(MyCamera.GetComponent<PlayerCamera>().ActiveCamera.transform.position, fwd, out hit, PrimaryWeapon.GetComponent<GunScript>().Range))
			{
				//If we hit a player's body
				if(hit.collider.tag == "Player")
				{
					HitPlayer = true;
					
					GameObject PlayersBody = hit.collider.gameObject;
					NetworkView targetID = PlayersBody.transform.parent.GetComponent<NetworkView>();
					targetID.RPC("RecievingDamage", RPCMode.All, Random.Range(PrimaryWeapon.GetComponent<GunScript>().MinDamage, PrimaryWeapon.GetComponent<GunScript>().MaxDamage));
					targetID.RPC("LastHitByPlayer", RPCMode.All, gameObject.name);
				}
				
				//If we hit a player's head
				if(hit.collider.tag == "PlayerHead")
				{
					HitPlayer = true;
					print ("Hit head");
					
					GameObject PlayersHead = hit.collider.gameObject;
					NetworkView targetID = PlayersHead.transform.parent.parent.GetComponent<NetworkView>();
					targetID.RPC("RecievingDamage", RPCMode.All, Random.Range(PrimaryWeapon.GetComponent<GunScript>().MinDamage * 1.2f, PrimaryWeapon.GetComponent<GunScript>().MaxDamage * 1.2f));
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
