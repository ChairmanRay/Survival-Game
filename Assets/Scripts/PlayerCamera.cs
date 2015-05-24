using UnityEngine;
using System.Collections;

public class PlayerCamera : MonoBehaviour {
	
	public Transform MyPlayer;
	
	public Transform FirstPersonCamera;
	public Transform ThirdPersonCamera;
	
	private float rotationX;
	
	public bool FirstPerson = true;
	public bool SwitchSide = true;
	
	public float Xaxis = 1.06f;
	
	void Start ()
	{
		MyPlayer = transform.parent;
	}
	
	void Update ()
	{
		//transform.Rotate(new Vector3(-Input.GetAxis("Mouse Y"), 0, 0) * Time.deltaTime * 200);
		if(GetComponent<NetworkView>().isMine)
		{
			lockedRotation();
			
			if(Input.GetKeyDown(KeyCode.C))
			{
				FirstPerson = !FirstPerson;
			}
			
			if(FirstPerson == true)
			{
				FirstPersonCamera.GetComponent<Camera>().enabled = true;
				ThirdPersonCamera.gameObject.SetActive(false);
			}
			
			if(FirstPerson == false)
			{
				FirstPersonCamera.GetComponent<Camera>().enabled = false;
				ThirdPersonCamera.gameObject.SetActive(true);
				
				ThirdPersonCamera.transform.localPosition = new Vector3(Xaxis, 3f, -2.9f);
				
				if(Input.GetKeyDown(KeyCode.LeftAlt))
				{
					SwitchSide = !SwitchSide;
				}
				
				//On the right side
				if(SwitchSide == true)
				{
					if(Xaxis <= 0.86f)
					{
						Xaxis += 0.8f;
					}
				}
				//On the left side
				if(SwitchSide == false)
				{
					if(Xaxis >= -3f)
					{
						Xaxis -= 0.8f;
					}
				}
			}
		}
	}
	
	void lockedRotation()
	{
		float sensitivityX = 2;
		
		rotationX += Input.GetAxis("Mouse Y") * sensitivityX;
		rotationX = Mathf.Clamp (rotationX, -80, 80);
		
		transform.localEulerAngles = new Vector3(-rotationX, transform.localEulerAngles.y, transform.localEulerAngles.z);
	}
}
