using UnityEngine;
using System.Collections;


public class PlayerMovement : MonoBehaviour
{
	public GameObject MyCamera;
	public GameObject MyBody;

	//Walk and run speeds
	private float PlayerSpeedWalk = 5.0f;
	private float PlayerSpeedSprint = 7.0f;
	
	private float gravity = 10.0f;
	private float jumpHeight = 2.0f;
	
	private float maxVelocityChange = 15.0f;
	
	public bool Crouching = false;
	public bool Prone = false;
	public bool IsSprinting = false;
	
	private bool WalkingDiagonal = false;
	private bool TouchingTheGround = false;
	private bool grounded = false;
	
	//private Vector3 targetVelocity = Vector3.zero;
	
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
		RaycastHit hit;

		//Lock Z rotation
		transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0);
		//Change rotation based on mouse movement
		transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X"), 0) * Time.deltaTime * 200);
		
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
			if(Input.GetKey(KeyCode.LeftShift) && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)) && Prone == false && Crouching == false /*&& Stamina > 0*/)
			{
				IsSprinting = true;
				//Stamina -= (1.2f * Time.deltaTime); //You will run out of sprint in 1:40
				if(WalkingDiagonal == true)
				{
					PlayerSpeedWalk = 4.95f;
				}
				else
				{
					PlayerSpeedWalk = 7;
				}

			}
			else
			{
				IsSprinting = false;
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
