﻿using UnityEngine;
using System.Collections;

public class PlayerScript : MonoBehaviour {
	
	public GameObject MyCamera;
	public GameObject MyBody;
	//Weapons and quick slots
	public GameObject PrimaryWeapon;//Quickslot 1
	public GameObject SecondaryWeapon;//Quickslot 2
	public GameObject QuickSlot3;
	public GameObject QuickSlot4;
	public GameObject QuickSlot5;
	public GameObject QuickSlot6;
	
	//Health, hunger, thirst
	public float Health = 100;
	public float HungerLevel = 100;
	public float ThirstLevel = 100;
	
	//GUI crosshairs
	public Texture Crosshair;
	public Texture HitMarker;
	
	public float PlayerSpeedWalk = 5.0f;
	public float PlayerSpeedSprint = 8.0f;
	
	public float gravity = 10.0f;
	public float jumpHeight = 2.0f;
	
	public float maxVelocityChange = 15.0f;
	
	public bool IsJump = true;
	public bool Crouching = false;
	public bool Prone = false;
	private bool WalkingDiagonal = false;
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
		Vector3 syncPosition = Vector3.zero;
		Vector3 syncVelocity = Vector3.zero;
		Quaternion syncRotation = Quaternion.identity;
		
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
		}
		else
		{
			MyCamera.GetComponent<Camera>().enabled = false;
		}
		
		transform.name = ("Player" + GetComponent<NetworkView>().owner);
		
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
		
		if (grounded) 
		{
			if((Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D)) || (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A)) || (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.D)) || (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.A)))
			{
				if(Crouching == false && Prone == false)
				{
					WalkingDiagonal = true;
					//Reduce speed because we are going diagonal
					PlayerSpeedWalk = 3.54f;
					PlayerSpeedSprint = 5.56f;
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
			
			// Apply a force that attempts to reach our target velocity
			Vector3 velocity = GetComponent<Rigidbody>().velocity;
			Vector3 velocityChange = (targetVelocity - velocity);
			velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
			velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
			velocityChange.y = 0;
			GetComponent<Rigidbody>().AddForce(velocityChange, ForceMode.VelocityChange);
			
			// Jumping
			if (Input.GetButton("Jump") && IsJump) 
			{
				GetComponent<Rigidbody>().velocity = new Vector3(velocity.x, CalculateJumpVerticalSpeed(), velocity.z);
			}
			
			//Sprint Movement
			if(Input.GetKey(KeyCode.LeftShift))
			{
				// Calculate how fast we should be moving
				targetVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
				targetVelocity = transform.TransformDirection(targetVelocity);
				targetVelocity *= PlayerSpeedSprint;
				
				// Apply a force that attempts to reach our target velocity
				velocity = GetComponent<Rigidbody>().velocity;
				velocityChange = (targetVelocity - velocity);
				velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
				velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
				velocityChange.y = 0;
				GetComponent<Rigidbody>().AddForce(velocityChange, ForceMode.VelocityChange);
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
			
			if(Prone == false)
			{
				MyBody.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, transform.eulerAngles.z);
				MyCamera.transform.localPosition = new Vector3(0, 1, 0);
			}
			
			if(Crouching == false && Prone == false && WalkingDiagonal == false)
			{
				PlayerSpeedWalk = 5.0f;
				PlayerSpeedSprint = 8.0f;
			}
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
			GUI.Label(new Rect(100, 100, 200, 200),"Health: " + Health);
			GUI.Label(new Rect(100, 80, 200, 200), ("Name: " + transform.name));
			GUI.DrawTexture(new Rect(Screen.width / 2 - 22.5f, Screen.height / 2 - 22.5f, 45, 45), Crosshair, ScaleMode.StretchToFill, true, 10.0F);
		}
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
