using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent (typeof (NetworkView))]
public class Chat : MonoBehaviour
{
	public Rect chatPosition;

	public string message;
	public List<string> messages;
	public GUIStyle MessageStyle;
	
	public float LineHeight;
	
	NetworkManager NetworkM;
	
	public bool ChatActive = false;
	
	public bool ProximityChat = true; //Send to players in my area
	public bool ServerChat = false; //Send to all players on the server
	public bool PartyChat = false; //Send to all players in my party
	
	void Start()
	{
		chatPosition.y = Screen.height - 200;
		NetworkM = GetComponent<NetworkManager>();
	}
	
	void Update()
	{
		if(messages.Count > (int)chatPosition.height / 18)
		{
			messages.RemoveAt(0);
		}
		
		if (GetComponent<NetworkView>().isMine)
		{
			if(Input.GetKeyDown(KeyCode.F1))
			{
				ProximityChat = true;
				ServerChat = false;
				PartyChat = false;
			}
			else if(Input.GetKeyDown(KeyCode.F2))
			{
				ServerChat = true;
				ProximityChat = false;
				PartyChat = false;
			}
			else if(Input.GetKeyDown(KeyCode.F3))
			{
				PartyChat = true;
				ProximityChat = false;
				ServerChat = false;
			}
		}
	}
	
	void OnGUI()
	{
		if(NetworkM.isConnected)
		{
			GUI.BeginGroup(chatPosition, "");
			GUI.Box(new Rect(0, -30, chatPosition.width, chatPosition.height), "");
			for (var i = 0; i < messages.Count; i++)
			{
				GUI.Label(new Rect(5, (i * -0) + (15 * i), chatPosition.width - 10, 30), "<size=10>" + messages[i] + "</size>", MessageStyle);
			}
			
			if(Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return))
			{
				if(message != "")
				{
					LineHeight = MessageStyle.CalcHeight(new GUIContent(message), chatPosition.width - 10);
					print (LineHeight);
					if(LineHeight == 22)
					{
						print (message + " is a big message");
					}
					if(ProximityChat == true)
					{
						string tempMessage = ("<color=green>[PROXIMITY] </color>" + NetworkM.playerName + ": " + message);
						SendMessage(tempMessage);
					}
					else if(ServerChat == true)
					{
						string tempMessage = ("<color=cyan>[SERVER] </color>" + NetworkM.playerName + ": " + message);
						SendMessage(tempMessage);
					}
					else if(PartyChat == true)
					{
						string tempMessage = ("<color=yellow>[PARTY] </color>" + NetworkM.playerName + ": " + message);
						SendMessage(tempMessage);
					}
				}
				
				ChatActive = !ChatActive;
			}
			
			if(ChatActive == true)
			{
				GUI.SetNextControlName("TextField");
				message = GUI.TextField(new Rect(0, chatPosition.height - 30, chatPosition.width - 1, 25), message);
				GUI.FocusControl("TextField");
			}
			GUI.EndGroup();
		}
	}
	
	public void SendMessage(string msg)
	{
		GetComponent<NetworkView>().RPC ("ReciveMessage", RPCMode.All, msg);
		message = "";
	}
	
		
	[RPC]
	public void ReciveMessage(string msg)
	{
		messages.Add(msg);
	}
}
