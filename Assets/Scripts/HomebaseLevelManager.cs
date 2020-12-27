using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using LitJson;
using UnityEngine.SceneManagement;

public class HomebaseLevelManager : MonoBehaviour {

	//UI elements
	public Text woodText, metalText, cuedWeaponText, sliderClockText, fitbitDistanceText;
	public Slider currentCraftingProgressSlider, fitbitSlider;
	public GameObject QRPanel, garagePanel, weaponCraftPanel, buildingItemCraftPanel, constructionItemCraftPanel, confirmCraftCancelPanel, fitbitDisplayPanel, fitbitLoginButton;
    public Button[] T1_weapons, T2_weapons, T3_weapons;
    public Button T1_craft_button, T2_craft_button, T3_craft_button, cancel_craft_button;
    public FitbitManager myFitbitMgr;

	//numbers for calculating the active weapon
	private DateTime timeActiveWeaponWillComplete, serverTimeNow;
    private TimeSpan serverOffset;
	private string activeWeaponName;
	public int activeWeaponDuration, activeWeaponEntryID, weaponsInCue;
	private bool weaponActivelyBeingCrafted = false;
    private string craftingJsonString;

	private string getwoodURL = GameManager.serverURL+"/Homebase_Getwood.php";
	private string startCraftingURL = GameManager.serverURL+"/Homebase_StartCrafting.php";
	private string getCraftingStatusURL = GameManager.serverURL+"/Homebase_GetCraftingStatus.php";
	private string getStatusURL = GameManager.serverURL+"/Homebase_GetStatus.php";
	private string clientCallingCompletedWeaponURL = GameManager.serverURL+"/Homebase_ClientCallingCraftComplete.php";
    private string cancelCurrentCraftURL = GameManager.serverURL + "/Homebase_CancelCurrentCraft.php";

	void Start () {
		//UpdateTheUI();
		InvokeRepeating("UpdateDataFromServer", 0f, 10f);

        /*
        if (GameManager.instance.fitbit_access_token.Length > 10 && GameManager.instance.fitbit_token_expiration > DateTime.Now)
        {
            fitbitDisplayPanel.SetActive(true);
            fitbitLoginButton.SetActive(false);
            //myFitbitMgr.UserAcceptOrDeny();
           // myFitbitMgr.RefreshTokens();
        } else if (GameManager.instance.fitbit_access_token == "" || GameManager.instance.fitbit_access_token == null)
        {
            fitbitLoginButton.SetActive(true);
            fitbitDisplayPanel.SetActive(false);
        }
        */
	}

	void Update () {
		if (weaponActivelyBeingCrafted == true) {
			if (currentCraftingProgressSlider.gameObject.activeInHierarchy == false ) {
				currentCraftingProgressSlider.gameObject.SetActive(true);
                cancel_craft_button.gameObject.SetActive(true);
			}
			UpdateSliderValue();
		} else {
			currentCraftingProgressSlider.gameObject.SetActive(false);
            cancel_craft_button.gameObject.SetActive(false);
		}
	}

    public void ToggleGaragePanel ()
    {
        if (garagePanel.activeInHierarchy)
        {
            garagePanel.SetActive(false);
        }else
        {
            garagePanel.SetActive(true);
        }
    }

    public void OpenWeaponCraftPanel() {
        weaponCraftPanel.SetActive(true);
        buildingItemCraftPanel.SetActive(false);
        constructionItemCraftPanel.SetActive(false);
    }

    public void OpenBuildingItemCraftPanel() {
        weaponCraftPanel.SetActive(false);
        buildingItemCraftPanel.SetActive(true);
        constructionItemCraftPanel.SetActive(false);
    }

    public void OpenConstructionItemsCraftPanel ()
    {
        constructionItemCraftPanel.SetActive(true);
        weaponCraftPanel.SetActive(false);
        buildingItemCraftPanel.SetActive(false);
    }

    public void CancelCurrentWeaponPressed()
    {
        if (confirmCraftCancelPanel.activeInHierarchy == true)
        {
            confirmCraftCancelPanel.SetActive(false);
        }else
        {
            confirmCraftCancelPanel.SetActive(true);
        }
    }

    public void ConfirmCancelCurrentWeapon()
    {
        confirmCraftCancelPanel.SetActive(false);
        StartCoroutine(CancelCurrentCraft());
    }

	public void QRPanelOpened () {
		QRPanel.GetComponent<QRPanelController>().ConstructAndEncodeQR();
		QRPanel.SetActive(true);
	}

	public void QRPanelClose () {
		QRPanel.SetActive(false);
	}

	void UpdateTheUI () {
		woodText.text = "Wood: " + GameManager.instance.wood.ToString();
        metalText.text = "Metal: " + GameManager.instance.metal.ToString();
		cuedWeaponText.text = "Weapons in cue: " + weaponsInCue.ToString();
        UpdateButtonStatus();

//		constructedKnifeText.text = "Knives completed: "+ GameManager.instance.knife_for_pickup.ToString();
//		constructedClubText.text = "Clubs completed: "+ GameManager.instance.club_for_pickup.ToString();
//		constructedAmmoText.text = "Ammo completed: "+ GameManager.instance.ammo_for_pickup.ToString();
//		constructedGunText.text = "Gun completed: " + GameManager.instance.gun_for_pickup.ToString();
//		activeSurvivorText.text = "Trained Survivors: " + GameManager.instance.active_survivor_for_pickup.ToString();
//		inactiveSurvivorText.text = "Untrained Survivors: " + GameManager.instance.inactive_survivors.ToString();
	}

    public void UpdateDistance(float distance)
    {
        GameManager.instance.fitbit_distance = distance;
        float value = distance / 10.0f;
        fitbitSlider.value = value;
        fitbitDistanceText.text = distance.ToString().Substring(0,3)+" miles";
        Debug.Log("Todays distance: "+distance+" setting slider value to: "+value);
    }

    void UpdateButtonStatus ()
    {
        if (GameManager.instance.crafting_t1 != true)
        {
            foreach (Button btn in T1_weapons)
            {
                btn.interactable = false;
            }
        }else
        {
            //player has the T1 item- turn on the weapons this enables crafting for
            foreach (Button btn in T1_weapons)
            {
                btn.interactable = true;
            }
            T1_craft_button.interactable = false;//player already has this crafting item
        }

        if (GameManager.instance.crafting_t2 != true)
        {
            foreach (Button btn in T2_weapons)
            {
                btn.interactable = false;
            }
        }
        else
        {
            //turn on the T2 weapons
            foreach (Button btn in T2_weapons)
            {
                btn.interactable = true;
            }
            T2_craft_button.interactable = false; //player already has this crafted item
        }

        if (GameManager.instance.crafting_t3 != true)
        {
            foreach (Button btn in T3_weapons)
            {
                btn.interactable = false;
            }
        }
        else
        {
            foreach (Button btn in T3_weapons)
            {
                btn.interactable = true;
            }
            T3_craft_button.interactable = false;
        }

        //now we need to look through the craft cue for the 3 tiered items- if they're being crafted we need to remove the option to craft them.
        Debug.Log(craftingJsonString);

        if (craftingJsonString==null || craftingJsonString == "") //if there's nothing being crafted- then exit, no need to look for tierd items in craft cue
        {
            return;
        }

        JsonData craftingJson = JsonMapper.ToObject(craftingJsonString);
        if (craftingJson[1].Count > 0) 
        {
            for (int i = 0; i < craftingJson[1].Count; i++)
            {
                string myType = craftingJson[1][i]["type"].ToString();
                if (myType == "workbench")
                {
                    T1_craft_button.interactable = false;
                    break;//no reason to continue, there can only be 1 in cue at a time, since the others require the last one complete in order to start
                }else if (myType == "forge")
                {
                    T2_craft_button.interactable = false;
                    break;
                }else if (myType == "lathe")
                {
                    T3_craft_button.interactable = false;
                    break;
                }
            }
        }
    }

	void UpdateSliderValue () {
		TimeSpan timeUntilFinish = (timeActiveWeaponWillComplete + serverOffset) - DateTime.Now;
        //Debug.Log("Server Offset: " + serverOffset);

		//Debug.Log("Weapon completes in "+timeActiveWeaponWillComplete.ToString()+" from now that should be: "+timeUntilFinish.TotalSeconds.ToString());

		float secondsToComplete = activeWeaponDuration*60.0f;
		//Debug.Log("Seconds to complete: "+secondsToComplete.ToString());
		double inverseSliderValue = timeUntilFinish.TotalSeconds / secondsToComplete;
		double sliderValue = 1.0f - inverseSliderValue;
		//Debug.Log("inverse slider value should be " + inverseSliderValue.ToString());
		if (sliderValue <= 1.0f) {
			currentCraftingProgressSlider.value = (float)sliderValue;
		} else {
			currentCraftingProgressSlider.value = 0.0f;
			weaponActivelyBeingCrafted = false;
			StartCoroutine(GetCraftingStatusAndSetCurrentSlider());
		}

		//declare the string to construct from the timespan
		string myClockText = "";
		if (timeUntilFinish.Hours > 0) {
			myClockText += timeUntilFinish.Hours.ToString().PadLeft(2, '0')+":";
			timeUntilFinish = timeUntilFinish - TimeSpan.FromHours(timeUntilFinish.Hours);
		}
		if(timeUntilFinish.Minutes > 0){
			myClockText += timeUntilFinish.Minutes.ToString().PadLeft(2, '0')+":";
			timeUntilFinish = timeUntilFinish - TimeSpan.FromMinutes(timeUntilFinish.Minutes);
		}
		if(timeUntilFinish.Seconds > 0) {
			myClockText += timeUntilFinish.Seconds.ToString().PadLeft(2, '0');
		}
		sliderClockText.text = myClockText;
	}

    void SetServerOffset() {
        serverOffset = DateTime.Now - serverTimeNow;
    }


	public void UpdateDataFromServer () {
		StartCoroutine(GetCraftingStatusAndSetCurrentSlider());
		StartCoroutine(UpdateStatsAndTextFromServer());
	}


	IEnumerator GetCraftingStatusAndSetCurrentSlider () {
		WWWForm form = new WWWForm();
		form.AddField("id", GameManager.instance.userId);
		form.AddField("login_ts", GameManager.instance.lastLogin_ts);
		form.AddField("client", "web");

		WWW www = new WWW( getCraftingStatusURL, form);
		yield return www;
		Debug.Log(www.text);

		if (www.error == null) {
			string returnString = www.text;
            craftingJsonString = returnString;
			JsonData craftingJson = JsonMapper.ToObject(returnString);

			if (craftingJson[0].ToString() == "Success") {

				//process the array with the weapons in progress
				if (craftingJson[1].Count > 0) {
					weaponsInCue = craftingJson[1].Count;
					weaponActivelyBeingCrafted = true;
					DateTime soonestWeaponComplete = DateTime.Parse(craftingJson[1][0]["time_complete"].ToString());
					activeWeaponName = craftingJson[1][0]["type"].ToString();
					activeWeaponEntryID = (int)craftingJson[1][0]["entry_id"];
					activeWeaponDuration = (int)craftingJson[1][0]["duration"];
					for (int i = 0; i < craftingJson[1].Count; i++) {
						//find the soonest weapon to complete, and set that complete time for the slider.
						DateTime myDoneTime = DateTime.Parse(craftingJson[1][i]["time_complete"].ToString());
						if (myDoneTime < soonestWeaponComplete) {
							soonestWeaponComplete = myDoneTime;
							activeWeaponName = craftingJson[1][i]["type"].ToString();
							activeWeaponEntryID = (int)craftingJson[1][i]["entry_id"];
							activeWeaponDuration = (int)craftingJson[1][i]["duration"];
							Debug.Log(activeWeaponDuration.ToString());
						}
					}
					Debug.Log("active weapon complete: "+soonestWeaponComplete.ToString()+" active weapon duration: "+activeWeaponDuration.ToString());
					timeActiveWeaponWillComplete = soonestWeaponComplete;

                    serverTimeNow = DateTime.Parse(craftingJson[3]["NOW()"].ToString()); //we store a NOW() call from the server to calculate offset for slider time.
                    SetServerOffset();

					UpdateTheUI();
				} else {
					weaponsInCue = 0;
					weaponActivelyBeingCrafted = false;
					currentCraftingProgressSlider.gameObject.SetActive(false);

				}


				UpdateTheUI();

			} else if (craftingJson[0].ToString() == "Failed") {
				Debug.Log(craftingJson[1].ToString());
			} else {
				Debug.Log("Attempting to update crafting status- json did not return valid success or failure");
			}
		} else {
			Debug.Log(www.error);
		}
	}

	IEnumerator UpdateStatsAndTextFromServer () {
		WWWForm form = new WWWForm();
		form.AddField("id", GameManager.instance.userId);
		form.AddField("login_ts", GameManager.instance.lastLogin_ts.ToString());
		form.AddField("client", "web");

		WWW www = new WWW(getStatusURL, form);
		yield return www;
		Debug.Log(www.text);

		if (www.error == null) {
			string returnString = www.text;
			JsonData returnJson = JsonMapper.ToObject(returnString);

			if (returnJson[0].ToString() == "Success") {
				GameManager.instance.wood = (int)returnJson[1]["wood"];
                GameManager.instance.metal = (int)returnJson[1]["metal"];
                GameManager.instance.fitbit_authorization_code = returnJson[1]["fitbit_authorization_code"].ToString();
                GameManager.instance.fitbit_access_token = returnJson[1]["fitbit_access_token"].ToString();
                GameManager.instance.fitbit_refresh_token = returnJson[1]["fitbit_refresh_token"].ToString();
                string expire_time = returnJson[1]["fitbit_expire_datetime"].ToString();
                if (expire_time != "" && expire_time != "0000-00-00 00:00:00")
                {
                    GameManager.instance.fitbit_token_expiration = DateTime.Parse(expire_time);
                }else
                {
                    Debug.Log("player has not sync'ed their fitbit account.");
                }
                int t1 = (int)returnJson[1]["craft_t1"];
                int t2 = (int)returnJson[1]["craft_t2"];
                int t3 = (int)returnJson[1]["craft_t3"];
                
                if (t1 == 0)
                {
                    GameManager.instance.crafting_t1 = false;
                }else if (t1 == 1)
                {
                    GameManager.instance.crafting_t1 = true;
                }else
                {
                    Debug.Log("ur fukin T1 crafting isn't storing as a binary on the server -broheim!");
                }
                if (t2== 0)
                {
                    GameManager.instance.crafting_t2 = false;
                }
                else if (t2 == 1)
                {
                    GameManager.instance.crafting_t2 = true;
                }
                else
                {
                    Debug.Log("ur fukin T2 crafting isn't storing as a binary on the server -broheim!");
                }
                if (t3 == 0)
                {
                    GameManager.instance.crafting_t3 = false;
                }
                else if (t3 == 1)
                {
                    GameManager.instance.crafting_t3 = true;
                }
                else
                {
                    Debug.Log("ur fukin T3 crafting isn't storing as a binary on the server -broheim!");
                }

                UpdateTheUI();
			} else if (returnJson[0].ToString() == "Failed") {
				Debug.Log(returnJson[1].ToString());
			} else {
				Debug.Log("Json object did not return a valid json success or failure response");
			}
		} else {
			Debug.Log(www.error);
		}
	}

	public void BackButtonPressed () {
		SceneManager.LoadScene("01a Login");
	}

	public void ConstructWeapon (string wepName) {
		int wood_cost = 0;
        int metal_cost = 0;
		int dur = 0;
		int wep_index =0;
		if (wepName == "shiv") {
			wood_cost = 20;
            metal_cost =10;
			dur = 4;
			wep_index = 1;
			//check if the user has enough currency
			if (GameManager.instance.wood >= wood_cost && GameManager.instance.metal >= metal_cost) {
				GameManager.instance.wood = GameManager.instance.wood - wood_cost;
                GameManager.instance.metal = GameManager.instance.metal - metal_cost;
				weaponsInCue++;
				StartCoroutine(SendCraftStartToServer(wepName, wood_cost, metal_cost, dur, wep_index));
			}
		}
        else if (wepName == "crude club")
        {
            wood_cost = 30;
            metal_cost = 10;
            dur = 15;
            wep_index = 2;
            //check if the user has enough currency
            if (GameManager.instance.wood >= wood_cost && GameManager.instance.metal >= metal_cost)
            {
                GameManager.instance.wood = GameManager.instance.wood - wood_cost;
                GameManager.instance.metal = GameManager.instance.metal - metal_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(wepName, wood_cost, metal_cost, dur, wep_index));
            }
        }
        else if (wepName == "zip gun")
        {
            wood_cost = 10;
            metal_cost = 30;
            dur = 25;
            wep_index = 3;
            //check if the user has enough currency
            if (GameManager.instance.wood >= wood_cost && GameManager.instance.metal >= metal_cost)
            {
                GameManager.instance.wood = GameManager.instance.wood - wood_cost;
                GameManager.instance.metal = GameManager.instance.metal - metal_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(wepName, wood_cost, metal_cost, dur, wep_index));
            }
        }
        else if (wepName == "shank")
        {
            wood_cost = 15;
            metal_cost = 25;
            dur = 8;
            wep_index = 4;
            //check if the user has enough currency
            if (GameManager.instance.wood >= wood_cost && GameManager.instance.metal >= metal_cost)
            {
                GameManager.instance.wood = GameManager.instance.wood - wood_cost;
                GameManager.instance.metal = GameManager.instance.metal - metal_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(wepName, wood_cost, metal_cost, dur, wep_index));
            }
        }
        else if (wepName == "reinforced club")
        {
            wood_cost = 50;
            metal_cost = 20;
            dur = 20;
            wep_index = 5;
            //check if the user has enough currency
            if (GameManager.instance.wood >= wood_cost && GameManager.instance.metal >= metal_cost)
            {
                GameManager.instance.wood = GameManager.instance.wood - wood_cost;
                GameManager.instance.metal = GameManager.instance.metal - metal_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(wepName, wood_cost, metal_cost, dur, wep_index));
            }
        }
        else if (wepName == "zip gun 2.0")
        {
            wood_cost = 30;
            metal_cost = 50;
            dur = 35;
            wep_index = 6;
            //check if the user has enough currency
            if (GameManager.instance.wood >= wood_cost && GameManager.instance.metal >= metal_cost)
            {
                GameManager.instance.wood = GameManager.instance.wood - wood_cost;
                GameManager.instance.metal = GameManager.instance.metal - metal_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(wepName, wood_cost, metal_cost, dur, wep_index));
            }
        }
        else if (wepName == "basic knife")
        {
            wood_cost = 30;
            metal_cost = 70;
            dur = 35;
            wep_index = 7;
            //check if the user has enough currency
            if (GameManager.instance.wood >= wood_cost && GameManager.instance.metal >= metal_cost)
            {
                GameManager.instance.wood = GameManager.instance.wood - wood_cost;
                GameManager.instance.metal = GameManager.instance.metal - metal_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(wepName, wood_cost, metal_cost, dur, wep_index));
            }
        }
        else if (wepName == "hunting knife") {
			wood_cost = 45;
            metal_cost = 90;
			dur = 45;
			wep_index = 10;
            //check if the user has enough currency
            if (GameManager.instance.wood >= wood_cost && GameManager.instance.metal >= metal_cost)
            {
                GameManager.instance.wood = GameManager.instance.wood - wood_cost;
                GameManager.instance.metal = GameManager.instance.metal - metal_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(wepName, wood_cost, metal_cost, dur, wep_index));
            }
        }
        else if (wepName == "deadly bat") {
			wood_cost = 75;
            metal_cost = 25;
			dur = 50;
			wep_index = 8;
            //check if the user has enough currency
            if (GameManager.instance.wood >= wood_cost && GameManager.instance.metal >= metal_cost)
            {
                GameManager.instance.wood = GameManager.instance.wood - wood_cost;
                GameManager.instance.metal = GameManager.instance.metal - metal_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(wepName, wood_cost, metal_cost, dur, wep_index));
            }
        }
        else if (wepName == "sledgehammer") {
			wood_cost = 50;
            metal_cost = 80;
			dur = 120;
			wep_index = 11;
            //check if the user has enough currency
            if (GameManager.instance.wood >= wood_cost && GameManager.instance.metal >= metal_cost)
            {
                GameManager.instance.wood = GameManager.instance.wood - wood_cost;
                GameManager.instance.metal = GameManager.instance.metal - metal_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(wepName, wood_cost, metal_cost, dur, wep_index));
            }
        }
        else if (wepName == ".22 revolver") {
			wood_cost = 55;
            metal_cost = 300;
			dur = 200;
			wep_index = 9;
            //check if the user has enough currency
            if (GameManager.instance.wood >= wood_cost && GameManager.instance.metal >= metal_cost)
            {
                GameManager.instance.wood = GameManager.instance.wood - wood_cost;
                GameManager.instance.metal = GameManager.instance.metal - metal_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(wepName, wood_cost, metal_cost, dur, wep_index));
            }
        }
        else if (wepName == "shotgun") {
			wood_cost = 100;
            metal_cost = 900;
			dur = 560;
			wep_index = 12;
            //check if the user has enough currency
            //check if the user has enough currency
            if (GameManager.instance.wood >= wood_cost && GameManager.instance.metal >= metal_cost)
            {
                GameManager.instance.wood = GameManager.instance.wood - wood_cost;
                GameManager.instance.metal = GameManager.instance.metal - metal_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(wepName, wood_cost, metal_cost, dur, wep_index));
            }
        }
        else if (wepName == "ammo") {
			wood_cost = 5;
            metal_cost = 5;
			dur = 1;
			wep_index = 0;
            //check if the user has enough currency
            if (GameManager.instance.wood >= wood_cost && GameManager.instance.metal >= metal_cost)
            {
                GameManager.instance.wood = GameManager.instance.wood - wood_cost;
                GameManager.instance.metal = GameManager.instance.metal - metal_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(wepName, wood_cost, metal_cost, dur, wep_index));
            }
        }
        else {
			Debug.Log("The string is not being sent correctly from the button");
		}
		UpdateTheUI();
	}

	IEnumerator SendCraftStartToServer (string wep_name, int wd_cst, int mt_cst, int duration, int weapon_index) {
        Debug.Log("sending craft for: " + wep_name + " costing: " + wd_cst + " wood, and " + mt_cst + " metal");
		WWWForm form = new WWWForm();
		form.AddField("id", GameManager.instance.userId);
		form.AddField("login_ts", GameManager.instance.lastLogin_ts);
		form.AddField("client", "web");
		form.AddField("wep_name", wep_name);
		form.AddField("wood_cost", wd_cst);
        form.AddField("metal_cost", mt_cst);
		form.AddField("duration", duration);
		form.AddField("weapon_index", weapon_index);


		WWW www = new WWW( startCraftingURL, form);
		yield return www;

		if (www.error == null) {
			string returnString = www.text;
			Debug.Log(returnString);
			JsonData returnJson = JsonMapper.ToObject(returnString);

			if (returnJson[0].ToString() == "Success") {
                
				Debug.Log(returnJson[1].ToString());
                UpdateTheUI();
			} else if (returnJson[0].ToString() == "Failed") {
				Debug.Log(returnJson[1].ToString());
			} else {
				Debug.Log("json returned something other than success or failure");
			}
		} else {
			Debug.Log(www.error);
		}
		
	}

    public void ConstructBuildingItem(string item_string) {

        if (item_string == "trap")
        {
            int wd_cost = 30;
            int mt_cost = 10;
            int duration = 20;
            int index_no = 0;
            if (GameManager.instance.wood >= wd_cost && GameManager.instance.metal >= mt_cost)
            {
                GameManager.instance.wood -= wd_cost;
                GameManager.instance.metal -= mt_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(item_string, wd_cost, mt_cost, duration, index_no));
            }else
            {
                Debug.Log("Not enough resources to craft");
            }
        } else if (item_string == "barrel")
        {
            int wd_cost = 40;
            int mt_cost = 20;
            int duration = 45;
            int index_no = 0;
            if (GameManager.instance.wood >= wd_cost && GameManager.instance.metal >= mt_cost)
            {
                GameManager.instance.wood -= wd_cost;
                GameManager.instance.metal -= mt_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(item_string, wd_cost, mt_cost, duration, index_no));
            }
            else
            {
                Debug.Log("Not enough resources to craft");
            }
        } else if (item_string == "greenhouse")
        {
            int wd_cost = 40;
            int mt_cost = 50;
            int duration = 120;
            int index_no = 0;
            if ( GameManager.instance.wood >= wd_cost && GameManager.instance.metal >= mt_cost)
            {
                GameManager.instance.wood -= wd_cost;
                GameManager.instance.metal -= mt_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(item_string, wd_cost, mt_cost,duration, index_no));
            }
            else
            {
                Debug.Log("Not enough resources to craft");
            }

        }

    }

    public void ConstructCraftingItem (string item_name)
    {
        if (item_name == "workbench")
        {
            int wd_cost = 500;
            int mt_cost = 250;
            int duration = 240;
            int index_no = -1;
            if (GameManager.instance.crafting_t1!=true && GameManager.instance.wood >= wd_cost && GameManager.instance.metal >= mt_cost)
            {
                GameManager.instance.wood -= wd_cost;
                GameManager.instance.metal -= mt_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(item_name, wd_cost, mt_cost, duration, index_no));
                T1_craft_button.interactable = false;
            }
        }
        else if (item_name == "forge")
        {
            int wd_cost = 1200;
            int mt_cost = 1000;
            int duration = 450;
            int index_no = -1;
            if (GameManager.instance.crafting_t1 == true && GameManager.instance.crafting_t2!=true && GameManager.instance.wood >= wd_cost && GameManager.instance.metal >= mt_cost)
            {
                GameManager.instance.wood -= wd_cost;
                GameManager.instance.metal -= mt_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(item_name, wd_cost, mt_cost, duration, index_no));
                T2_craft_button.interactable = false;
            }
        }
        else if (item_name == "lathe")
        {
            int wd_cost = 2900;
            int mt_cost = 3200;
            int duration = 1000;
            int index_no = -1;
            if (GameManager.instance.crafting_t1 == true && GameManager.instance.crafting_t2 == true && GameManager.instance.crafting_t3!=true && GameManager.instance.wood >= wd_cost && GameManager.instance.metal >= mt_cost)
            {
                GameManager.instance.wood -= wd_cost;
                GameManager.instance.metal -= mt_cost;
                weaponsInCue++;
                StartCoroutine(SendCraftStartToServer(item_name, wd_cost, mt_cost, duration, index_no));
                T3_craft_button.interactable = false;
            }
        }
        //UpdateTheUI();//removed to allow the client to update on its own.  if we call it manually it won't see the craft item in the cue yet, and will turn the tierd craft buttons on
    }

    IEnumerator CancelCurrentCraft ()
    {
        WWWForm form = new WWWForm();
        form.AddField("id", GameManager.instance.userId);
        form.AddField("login_ts", GameManager.instance.lastLogin_ts);
        form.AddField("client", "web");

        WWW www = new WWW(cancelCurrentCraftURL, form);
        yield return www;
        Debug.Log(www.text);

        if (www.error == null)
        {
            JsonData cancelCraftJsonReturn = JsonMapper.ToObject(www.text);
            if (cancelCraftJsonReturn[0].ToString() == "Success")
            {
                craftingJsonString = www.text;
                UpdateTheUI();
                Debug.Log(cancelCraftJsonReturn[3].ToString());//the return has crafting weapons at index 1 and completed weapons index2
            }
            else
            {
                Debug.Log(cancelCraftJsonReturn[1].ToString());
            }

        }else
        {
            Debug.Log(www.error);
        }

        confirmCraftCancelPanel.SetActive(false); //regardless of result- turn off the panel after the reply has occured.
    }
}
