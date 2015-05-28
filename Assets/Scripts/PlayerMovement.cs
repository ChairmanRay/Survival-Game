using UnityEngine;
using System.Collections;


public static class MovementSpeed
{
	public const float walk = 5.0f;
	public const float sprint = 7.0f;
	public const float crouch = 3.0f;
	public const float prone = 1.5f;
	public const float diagonalModifier = 0.707f;
}
	
public class PlayerMovement : MonoBehaviour
{
	public GameObject MyCamera;
	public GameObject MyBody;

	//Walk and run speeds
	private float currentMovementSpeed = 5.0f;
	
	private float gravity = 20.0f;
	private float jumpHeight = 2.0f;
	
	private float maxVelocityChange = 15.0f;

	private enum MovementMode{WALK, SPRINT, CROUCH, PRONE};
	private MovementMode currentMovement = MovementMode.WALK;
	
	private bool walkingDiagonal = false;
	private bool grounded = false;
	
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
		
		GetComponent<Rigidbody>().freezeRotation = true;
		GetComponent<Rigidbody>().useGravity = false;
	}

	
	void Update()
	{
		if (GetComponent<NetworkView>().isMine)
		{
			CheckInput();
		}
		else
		{
			SyncedMovement();
		}
	}
	
	void CheckInput()
	{
		//Lock Z rotation
		transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0);
		//Change rotation based on mouse movement
		transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X"), 0) * Time.deltaTime * 200);

		MyBody.transform.localScale = new Vector3(1, 1, 1);

		//If my player is touching the ground
		if (grounded) {
			if ((Input.GetKey (KeyCode.W) && Input.GetKey (KeyCode.D)) || (Input.GetKey (KeyCode.W) && Input.GetKey (KeyCode.A)) || (Input.GetKey (KeyCode.S) && Input.GetKey (KeyCode.D)) || (Input.GetKey (KeyCode.S) && Input.GetKey (KeyCode.A))) {
				walkingDiagonal = true;
			} else {
				walkingDiagonal = false;
			}
			
			// Calculate how fast we should be moving
			Vector3 targetVelocity = new Vector3 (Input.GetAxis ("Horizontal"), 0, Input.GetAxis ("Vertical"));
			targetVelocity = transform.TransformDirection (targetVelocity);
			targetVelocity *= currentMovementSpeed;
			
			//Apply a force that attempts to reach our target velocity
			Vector3 velocity = GetComponent<Rigidbody> ().velocity;
			Vector3 velocityChange = (targetVelocity - velocity);
			velocityChange.x = Mathf.Clamp (velocityChange.x, -maxVelocityChange, maxVelocityChange);
			velocityChange.z = Mathf.Clamp (velocityChange.z, -maxVelocityChange, maxVelocityChange);
			velocityChange.y = 0;
			GetComponent<Rigidbody> ().AddForce (velocityChange, ForceMode.VelocityChange);
			
			//Jumping
			if (Input.GetButtonDown ("Jump"))
			{
				RaycastHit hit;
				Vector3 DownDir = transform.TransformDirection (Vector3.down);
				if(Physics.Raycast (transform.position, DownDir, out hit, 1.05f))
				{
					//Only jump if you are walking/standing/running
					if(hit.collider.name == "Terrain" && (currentMovement == MovementMode.WALK || currentMovement == MovementMode.SPRINT))
					{
						GetComponent<Rigidbody> ().velocity = new Vector3 (velocity.x, CalculateJumpVerticalSpeed (), velocity.z);
					}
				}
				
				//If you are prone and press jump, stand up
				if (currentMovement == MovementMode.PRONE)
				{
					currentMovement = MovementMode.WALK;
					MyBody.transform.rotation = Quaternion.Euler (0, transform.eulerAngles.y, transform.eulerAngles.z);
					MyCamera.transform.localPosition = new Vector3 (0, 1, 0);
				}
			}
			
			//Walking
			if(currentMovement == MovementMode.WALK)
			{
				if(GetComponent<PlayerScript>().Stamina < 100)
				{
					GetComponent<PlayerScript>().Stamina += (1f * Time.deltaTime); //Sprint will regenerate from 0 to 100 in 2 minutes
				}
				
				//While standing/walking your hunger and thirst goes down slower
				if(GetComponent<PlayerScript>().HungerLevel > 0)
				{
					//This formula will change
					GetComponent<PlayerScript>().HungerLevel -= (0.08f * Time.deltaTime); //Down 1 every 15 seconds
				}
				
				if(GetComponent<PlayerScript>().ThirstLevel > 0)
				{
					//This formula will change
					GetComponent<PlayerScript>().ThirstLevel -= (0.08f * Time.deltaTime); //Down 1 every 15 seconds
				}
			}
			
			//Sprint Movement
			if (Input.GetKey (KeyCode.LeftShift) && (Input.GetKey (KeyCode.W) || Input.GetKey (KeyCode.A) || Input.GetKey (KeyCode.S) || Input.GetKey (KeyCode.D)) && currentMovement == MovementMode.WALK && GetComponent<PlayerScript>().Stamina > 0) 
			{
				currentMovement = MovementMode.SPRINT;
				GetComponent<PlayerScript>().Stamina -= (3.1f * Time.deltaTime); //You will run out of sprint in 1:40
				
				if(GetComponent<PlayerScript>().HungerLevel > 0)
				{
					//This formula will change
					GetComponent<PlayerScript>().HungerLevel -= (0.14f * Time.deltaTime); //Down 1 every 10 seconds
				}
				
				if(GetComponent<PlayerScript>().ThirstLevel > 0)
				{
					//This formula will change
					GetComponent<PlayerScript>().ThirstLevel -= (0.18f * Time.deltaTime); //Down 1 every 8 seconds
				}
				
			} else if (currentMovement == MovementMode.SPRINT) {
				currentMovement = MovementMode.WALK;
			}
			
			//Crouching
			if (Input.GetKey (KeyCode.LeftControl) && currentMovement != MovementMode.PRONE) {
				currentMovement = MovementMode.CROUCH;
			} else if (currentMovement == MovementMode.CROUCH) {
				currentMovement = MovementMode.WALK;
			}
			
			//Going prone
			if (Input.GetKeyDown (KeyCode.Z)) {
				currentMovement = (currentMovement == MovementMode.PRONE) ? MovementMode.WALK : MovementMode.PRONE;
			}
			
			if (currentMovement == MovementMode.PRONE) {
				MyBody.transform.rotation = Quaternion.Euler (90, transform.eulerAngles.y, transform.eulerAngles.z);
				MyCamera.transform.localPosition = new Vector3 (0, 0.4f, 0.6f);
			} else {
				MyBody.transform.rotation = Quaternion.Euler (0, transform.eulerAngles.y, transform.eulerAngles.z);
				MyCamera.transform.localPosition = new Vector3 (0, 1, 0);
			}

			switch (currentMovement) {
			case MovementMode.SPRINT:
				currentMovementSpeed = MovementSpeed.sprint;
				break;
			case MovementMode.CROUCH:
				MyBody.transform.localScale = new Vector3 (1, 0.5f, 1);
				currentMovementSpeed = MovementSpeed.crouch;
				break;
			case MovementMode.PRONE:
				currentMovementSpeed = MovementSpeed.prone;
				break;
			default:
				currentMovementSpeed = MovementSpeed.walk;
				break;
			}
			if (walkingDiagonal) {
				currentMovementSpeed *= MovementSpeed.diagonalModifier;
			}
		}
		//Crouching while jumping
		else if (Input.GetKey (KeyCode.LeftControl) && currentMovement != MovementMode.PRONE) 
		{
			MyBody.transform.localScale = new Vector3 (1, 0.5f, 1);
			currentMovementSpeed = MovementSpeed.crouch;
			currentMovement = MovementMode.CROUCH;
		} 
		else if (currentMovement != MovementMode.CROUCH && currentMovement != MovementMode.PRONE) 
		{
			currentMovement = MovementMode.WALK;
		}

		// We apply gravity manually for more tuning control
		GetComponent<Rigidbody>().AddForce(new Vector3 (0, -gravity * GetComponent<Rigidbody>().mass, 0));
		
		grounded = false;
	}

	//Calculates the jump height
	float CalculateJumpVerticalSpeed() 
	{
		// From the jump height and gravity we deduce the upwards speed 
		// for the character to reach at the apex.
		return Mathf.Sqrt(2 * jumpHeight * gravity);
	}

	void OnCollisionStay () 
	{
		grounded = true;    
	}
	
	void SyncedMovement()
	{
		syncTime += Time.deltaTime;
		
		GetComponent<Rigidbody>().position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
		GetComponent<Rigidbody>().rotation = Quaternion.Slerp(syncStartRotation, syncEndRotation, syncTime / syncDelay);
	}
}
