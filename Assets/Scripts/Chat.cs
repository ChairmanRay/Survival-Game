using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent (typeof (NetworkView))]
public class Chat : MonoBehaviour
{
	public Rect chatPosition;
	public List<string> messages;
	public GUIStyle MessageStyle;

	private NetworkManager NetworkM;
	private string message;
	private float LineHeight;
	private bool ChatActive = false;
	private enum ChatModes {PROXIMITY, SERVER, PARTY};
	private ChatModes chatMode = ChatModes.PROXIMITY;
	
	void Start()
	{
		chatPosition.y = Screen.height - 200;
		NetworkM = GetComponent<NetworkManager>();
	}
	
	void Update()
	{		
		if (GetComponent<NetworkView>().isMine)
		{
			if(Input.GetKeyDown(KeyCode.F1))
			{
				chatMode = ChatModes.PROXIMITY;
			}
			else if(Input.GetKeyDown(KeyCode.F2))
			{
				chatMode = ChatModes.SERVER;
			}
			else if(Input.GetKeyDown(KeyCode.F3))
			{
				chatMode = ChatModes.PARTY;
			}
		}
	}
	
	void OnGUI()
	{
		if(NetworkM.isConnected)
		{
			GUI.BeginGroup(chatPosition, "");
			GUI.Box(new Rect(0, 0, chatPosition.width, chatPosition.height), "");
			float messageHeight = 0;
			for (var i = 0; i < messages.Count; i++)
			{
				GUI.Label(new Rect(5, messageHeight, chatPosition.width - 10, 30), "<size=10>" + messages[i] + "</size>", MessageStyle);
				messageHeight += MessageStyle.CalcHeight(new GUIContent(messages[i]), chatPosition.width - 10);
				while(messageHeight > chatPosition.height-25)
				{
					messageHeight -= MessageStyle.CalcHeight(new GUIContent(messages[0]), chatPosition.width - 10);
					messages.RemoveAt(0);
				}
			}
			
			if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
			{
				if(!String.IsNullOrEmpty(message))
				{
					switch (chatMode) 
					{
						case ChatModes.PROXIMITY:
							message = "<color=green>[PROXIMITY] </color>" + NetworkM.playerName + ": " + message;
							break;
						case ChatModes.SERVER:
							message = "<color=cyan>[SERVER] </color>" + NetworkM.playerName + ": " + message;
							break;
						case ChatModes.PARTY:
							message = "<color=yellow>[PARTY] </color>" + NetworkM.playerName + ": " + message;
							break;
						default:
							break;
					}
					SendMessage(message);
					
				}
				message = "";
				ChatActive = !ChatActive;
			}
			if(ChatActive == true)
			{
				GUI.SetNextControlName("TextField");
				message = GUI.TextField(new Rect(0, chatPosition.height-25, chatPosition.width - 1, 25), message);
				GUI.FocusControl("TextField");
			}
			GUI.EndGroup();
		}
	}
	
	public void SendMessage(string msg)
	{
		if (String.IsNullOrEmpty (msg))
			return;
		GetComponent<NetworkView>().RPC ("ReceiveMessage", RPCMode.All, msg);
	}

	[RPC]
	private void ReceiveMessage(string msg)
	{
		messages.Add(msg);
	}
}
