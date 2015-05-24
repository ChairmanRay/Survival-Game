using UnityEngine;
using System.Collections;

public class NewSync : MonoBehaviour {

	public Transform MyCamera;
	public Vector3 realPosition = Vector3.zero;
	public Quaternion realRotation = Quaternion.identity;
	
	void Update () 
	{
		MyCamera = transform.parent;
		realPosition = transform.position;
		realRotation = MyCamera.transform.rotation;
		
		if(GetComponent<NetworkView>().isMine)
		{
			
		}
		else
		{
			transform.position = Vector3.Lerp(transform.position, realPosition, 0.1f);
			transform.rotation = Quaternion.Lerp(transform.rotation, realRotation, 0.1f);
		}
	}
	
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		if (stream.isWriting)
		{
			stream.Serialize(ref realPosition);
			stream.Serialize(ref realRotation.y);
		}
		else
		{
			stream.Serialize(ref realPosition);
			stream.Serialize(ref realRotation.y);
			transform.rotation = Quaternion.Euler (transform.eulerAngles.x, transform.eulerAngles.y, transform.eulerAngles.z);
		}
	}
}