using UnityEngine;
using System.Collections;

public class SyncPosRot : MonoBehaviour {
	
	private float lastSynchronizationTime = 0f;
	private float syncDelay = 0f;
	private float syncTime = 0f;
	private Vector3 syncStartPosition = Vector3.zero;
	private Vector3 syncEndPosition = Vector3.zero;
	private Quaternion syncStartRotation = Quaternion.identity;
	private Quaternion syncEndRotation = Quaternion.identity;
	
	private float StartDelay = 30;
	
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
	
	void Start ()
	{
		lastSynchronizationTime = Time.time;
		
		//This fixes when you spawn, the other players guns are at your position
		StartCoroutine(ChangeStartDelay());
	}
	
	void Update ()
	{
		if (GetComponent<NetworkView>().isMine)
		{
			
		}
		else
		{
			SyncedMovement();
		}
	}
	
	private void SyncedMovement()
	{
		syncTime += Time.deltaTime * StartDelay;
		
		GetComponent<Rigidbody>().position = Vector3.Lerp(syncStartPosition, syncEndPosition, syncTime / syncDelay);
		GetComponent<Rigidbody>().rotation = Quaternion.Slerp(syncStartRotation, syncEndRotation, syncTime / syncDelay);
	}
	
	IEnumerator ChangeStartDelay()
	{
		yield return new WaitForSeconds(0.2f);
		
		StartDelay = 1;
	}
}
