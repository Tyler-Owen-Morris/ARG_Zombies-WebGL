using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.UI;
using Facebook.Unity;
using LitJson;

public class LoginManager : MonoBehaviour {

	[SerializeField]
	private Text failedLoginText;
	private int survivorsDrafted = 0;

	public GameObject loginFailedPanel, returnToGameButton;

//	private string registerUrl = "http://localhost/ARGZ_SERVER/register.php";
//	private string playerDataUrl = "http://localhost/ARGZ_SERVER/PlayerData.php";
//	private string loginUrl = "http://localhost/ARGZ_SERVER/login.php";

	private string newSurvivorUrl = GameManager.serverURL+"/create_new_survivor.php";
	private string homebaseLoginURL = GameManager.serverURL+"/HomebaseLoginCheck.php";
	
	// Use this for initialization
	void Start () { 
        if (FB.IsInitialized) {
            FB.ActivateApp();
        } else {
        //Handle FB.Init
            FB.Init(SetInit, OnHideUnity);
        }
        
        
    }
    
    void SetInit () {
        FB.ActivateApp();
        if (FB.IsLoggedIn) {
            Debug.Log ("FB is logged in");
			loginFailedPanel.SetActive (false);
        } else {
            Debug.Log ("FB is not logged in");
			loginFailedPanel.SetActive (true);
        }
        
    }
    
    void OnHideUnity (bool isGameShown) {
        
        if (!isGameShown) {
            Time.timeScale = 0;
        } else {
            Time.timeScale = 1;
        }
    }
    
    public void FBlogin ()  {
        
        List<string> permissions = new List<string>();
        permissions.Add("public_profile");
        //permissions.Add("user_friends");
        //permissions.Add("email");
        
        
        FB.LogInWithReadPermissions (permissions, AuthCallBack);
        
    }
    
    void AuthCallBack (IResult result) {
        
        if (result.Error != null) {
            Debug.Log (result.Error);
        } else {
            
            if (FB.IsLoggedIn) {
                Debug.Log ("FB is logged in");
				loginFailedPanel.SetActive (false);
                FB.API ("/me?fields=id", HttpMethod.GET, UpdateUserId);
		        FB.API ("/me?fields=first_name", HttpMethod.GET, UpdateUserFirstName);
		        FB.API ("/me?fields=last_name", HttpMethod.GET, UpdateUserLastName);

		        //GameManager.instance.ResumeGame();
            } else {
                Debug.Log ("FB is NOT logged in");
				loginFailedPanel.SetActive (false);
            }
        }
    }

    IEnumerator SendLoginToGameServer() {
    	WWWForm form = new WWWForm();
    	form.AddField("id", GameManager.instance.userId);
		form.AddField("login_ts", "12/31/1999 11:59:59");
		form.AddField("client", "web");
        Debug.Log(homebaseLoginURL);
        Debug.Log(GameManager.instance.userId);

    	WWW www = new WWW(homebaseLoginURL, form);
    	yield return www;
    	Debug.Log(www.text);

    	if (www.error == null) {
    		//encode the result into a json object so it can be checked through
    		string homebaseReturnJson = www.text.ToString();
    		JsonData homebaseJson = JsonMapper.ToObject(homebaseReturnJson);

    		if (homebaseJson[0].ToString() == "Success") {
    			//handle the success- load in game data, and go to the game screen
    			Debug.Log(homebaseJson[1].ToString());

				GameManager.instance.wood = (int)homebaseJson[2]["wood"];
                GameManager.instance.metal = (int)homebaseJson[2]["metal"];
				GameManager.instance.lastLogin_ts = homebaseJson[3]["web_login_ts"].ToString();

				GameManager.instance.homebase_lat = float.Parse(homebaseJson[3]["homebase_lat"].ToString());
				GameManager.instance.homebase_lon = float.Parse(homebaseJson[3]["homebase_lon"].ToString());

                GameManager.instance.fitbit_authorization_code = homebaseJson[2]["fitbit_authorization_code"].ToString();
                GameManager.instance.fitbit_access_token = homebaseJson[2]["fitbit_access_token"].ToString();
                GameManager.instance.fitbit_refresh_token = homebaseJson[2]["fitbit_refresh_token"].ToString();
                string expire_time = homebaseJson[2]["fitbit_expire_datetime"].ToString();
                if (expire_time != "" && expire_time != "0000-00-00 00:00:00")
                {
                    GameManager.instance.fitbit_token_expiration = System.DateTime.Parse(expire_time);
                }
                else
                {
                    Debug.Log("player has not sync'ed their fitbit account.");
                }

                GameManager.instance.dataIsInitialized = true;

				SceneManager.LoadScene("02a Homebase");
    		} else if (homebaseJson[0].ToString() == "Failed"){
    			//handle failure- give the user a reason and DO NOT continue to game screen.
    			failedLoginText.text = homebaseJson[1].ToString();
    			loginFailedPanel.SetActive(true);
    		}

    	}else {
    		Debug.Log("WWW error "+ www.error);
    	}

    }

    public void ContinueGame () {
    	if (FB.IsLoggedIn) {
    		if (GameManager.instance.dataIsInitialized == true) {
    			SceneManager.LoadScene("02a Homebase");
    		} else {
    			StartCoroutine(SendLoginToGameServer());
    		}
    	}else {
    		Debug.Log("Cannot resume game, player must be logged into facebook first");
    	}

    }
    
	private void UpdateUserId (IResult result) {
		if (result.Error == null) {
            GameManager.instance.userId = result.ResultDictionary["id"].ToString();
			StartCoroutine(SendLoginToGameServer());
        } else {
            Debug.Log (result.Error);
        }
	}

	private void UpdateUserFirstName(IResult result) {
		if (result.Error == null) {
			GameManager.instance.userFirstName = result.ResultDictionary["first_name"].ToString();
		} else {
			Debug.Log (result.Error);
		}
	}

	private void UpdateUserLastName (IResult result) {
		if (result.Error == null) {
			GameManager.instance.userLastName = result.ResultDictionary["last_name"].ToString();
		}else{
			Debug.Log(result.Error);
		}
	}

	private void UpdateSurvivorDraftWindow (IResult result) {
		if (result.Error == null) {
			//create 4 new player characters from facebook friends results
			for (int i = 0; i < 4; i++) {
				
			}

		}else{
			Debug.Log(result.Error);
		}
	}

	//this is a temporary function to test sending characters to the server.  eventually these choices will be auto-populated from friends, and cycle choices on each pick- creating a Zombie Apocalypse Draft.
	public void ChooseSurvivorToSend (int choice) {
		survivorsDrafted ++;
		if (survivorsDrafted <= 4){
			if (choice == 1) {
				StartCoroutine(SendNewSurvivorToServer("Bill Joe", Random.Range(1,1000000).ToString(), 140, 8));
			} else if (choice == 2) {
				StartCoroutine(SendNewSurvivorToServer("Sally Jesse", Random.Range(1, 1000000).ToString(), 100, 10));
			} else if (choice == 3) {
				StartCoroutine(SendNewSurvivorToServer("Shimbop", Random.Range(1,1000000).ToString(), 90, 12));
			}

			if (survivorsDrafted == 4) {
				//GameManager.instance.ResumeCharacter();
			}
		} else {
			//GameManager.instance.ResumeCharacter();
		}
	}

	IEnumerator SendNewSurvivorToServer (string name, string survivor_id, int stamina, int attack) {
		WWWForm form = new WWWForm();
		form.AddField("id", GameManager.instance.userId);
		form.AddField("login_ts", GameManager.instance.lastLogin_ts);
		form.AddField("client", "web");
		form.AddField("survivor_id", survivor_id); //this will need to actually pull
		form.AddField("name", name);
		form.AddField("base_stam", stamina);
		form.AddField("curr_stam", stamina);
		form.AddField("base_attack", attack);
		form.AddField("weapon_equipped", "none");

		WWW www = new WWW(newSurvivorUrl, form);
		yield return www;
		if (www.error == null) {
			Debug.Log(www.text);
			string jsonReturn = www.text.ToString();
			JsonData jsonResult = JsonMapper.ToObject(jsonReturn);

			Debug.Log (jsonResult[0].ToString());

			//at some point the client will need to recieve the json from the server and report a failed creation.
//			if (jsonResult[0].ToString() == "Success") {
//				Debug.Log(jsonResult[1].ToString());
//			} else {
//				Debug.LogError ("new survivor not added to server error: "+ jsonResult[1].ToString());
//				survivorsDrafted --;
//			}

		}else{
			survivorsDrafted --;
			Debug.Log(www.error);
		}

	}

	public void FakeLoggedInSuccess () {
		if (loginFailedPanel.activeInHierarchy == false){
			loginFailedPanel.gameObject.SetActive(true);

			if (FB.IsLoggedIn == true) {
				//GameManager.instance.ResumeCharacter();
			}

		} else {
			loginFailedPanel.gameObject.SetActive(false);
		}

	}

	public void CloseFailedLoginWindow () {
		loginFailedPanel.SetActive(false);
	}
}
