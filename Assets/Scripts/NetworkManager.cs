using UnityEngine;
using System.Collections;

public class NetworkManager : MonoBehaviour
{
    private const string typeName = "UniqueGameName";
    private const string gameName = "zombhe's room";

    private bool isRefreshingHostList = false;
    private HostData[] hostList;

    public GameObject playerPrefab;
    
    //New stuff
	public string playerName;
	[HideInInspector]
	public string tempName;
	
	public bool isConnected;
	
	Chat sChat;
	
	void Start()
	{
		DontDestroyOnLoad(this);
		sChat = GetComponent<Chat>();
	}
    	
	void OnGUI()
	{
		if (!Network.isClient && !Network.isServer)
        {
			if(playerName == "")
			{
				GUI.BeginGroup(new Rect((Screen.width / 2) - 100, (Screen.height / 2) - 25, 200, 50), "");
				tempName = GUI.TextField(new Rect(0, 0, 190, 25), tempName, 15);
				
				if(GUI.Button(new Rect(75, 25, 50, 25), "Next"))
				{
					playerName = tempName;
				}
				
				GUI.EndGroup();
			}
			else
			{
				if(!isConnected)
				{
					if(GUI.Button(new Rect(Screen.width * 0.2f, Screen.height * 0.2f, Screen.width * 0.2f, Screen.height * 0.2f), "Start Server"))
					{
						StartServer();
					}
					
					if(GUI.Button(new Rect(Screen.width * 0.2f, Screen.height * 0.4f, Screen.width * 0.2f, Screen.height * 0.2f), "Refresh Hosts"))
					{
						RefreshHostList();
					}
					
					if (hostList != null)
					{
						for (int i = 0; i < hostList.Length; i++)
						{
							if (GUI.Button(new Rect(Screen.width * 0.5f, Screen.height * 0.2f + (80 * i), Screen.width * 0.2f, Screen.height * 0.12f), hostList[i].gameName))
							{
								JoinServer(hostList[i]);
							}
						}
					}
				}
				else
				{
					if(GUI.Button(new Rect(0, 5, 100, 25), "Disconnect"))
					{
						sChat.SendMessage(playerName + " disconnected!");
						Network.Disconnect();
					}
				}
			}
        }
    }

    private void StartServer()
    {
        Network.InitializeServer(5, 25000, !Network.HavePublicAddress());
        MasterServer.RegisterHost(typeName, gameName);
    }

    void OnServerInitialized()
    {
        SpawnPlayer();
		sChat.SendMessage("Connected to " + gameName + "!");
	}
	
    void Update()
    {
    	NetworkMessageCheck();
		if (isRefreshingHostList && MasterServer.PollHostList().Length > 0)
        {
            isRefreshingHostList = false;
            hostList = MasterServer.PollHostList();
        }
    }

    private void RefreshHostList()
    {
        if (!isRefreshingHostList)
        {
            isRefreshingHostList = true;
            MasterServer.RequestHostList(typeName);
        }
    }

    private void JoinServer(HostData hostData)
    {
        Network.Connect(hostData);
    }

    void OnConnectedToServer()
    {
        SpawnPlayer();
		sChat.SendMessage("Connected to " + gameName + "!");
	}
	
	void OnDisconnectedFromServer()
	{
		sChat.messages.Clear();
	}
	
    private void SpawnPlayer()
    {
        GameObject PlayerInstantiate = Network.Instantiate(playerPrefab, Vector3.up * 1, Quaternion.identity, 0) as GameObject;
        //PlayerInstantiate.transform.name = playerName;
		PlayerInstantiate.GetComponent<NetworkView>().RPC("setPlayerName", RPCMode.AllBuffered, playerName);
    }
    
	void NetworkMessageCheck()
	{
		if(Network.peerType == NetworkPeerType.Disconnected)
		{
			isConnected = false;
		}
		else if(Network.peerType == NetworkPeerType.Client)
		{
			isConnected = true;
		}
		else if(Network.peerType == NetworkPeerType.Server)
		{
			isConnected = true;
		}
	}
}
