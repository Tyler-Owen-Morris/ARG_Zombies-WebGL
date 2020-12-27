using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using System;
using System.Text;

public class FitbitManager : MonoBehaviour {

    private const string _consumerSecret = "1a761965ce558d433ada1aa9b23ab398";
    private const string _clientId = "227YT4";
    private const string _callbackURL = "http://www.argzombie.com/ARGZ_DEV_SERVER/FitbitCallback.php";

    private const string _tokenUrl = "https://api.fitbit.com/oauth2/token";
    private const string _baseGetUrl = "https://api.fitbit.com/1/user/-/";

    private const string _profileUrl = _baseGetUrl + "profile.json/";
    private const string _activityUrl = _baseGetUrl + "activities/";

    private string _distanceUrl = _activityUrl + "distance/date/" + _currentDateTime + "/1d.json";

    private static string _currentDateTime = GetCurrentDate();
    private static string _yesterdayDateTime = GetYesterdayDate();

    void Start ()
    {
        /*
        if (GameManager.instance.fitbit_token_expiration < DateTime.Now && GameManager.instance.fitbit_refresh_token != "")
        {
            //refresh access token, and get updated fitbit data
            RefreshAccessToken();
        } else if (GameManager.instance.fitbit_token_expiration > DateTime.Now && GameManager.instance.fitbit_refresh_token != "")
        {
            //just try to get the fitbit update data
            StartCoroutine(UpdateFitbitData());
        }
        */
        //disable for current build
    }

    public void UserAcceptOrDeny ()
    {
        //we don't have a refresh token so we gotta go through the whole auth process.
        var url =
            "https://www.fitbit.com/oauth2/authorize?response_type=code&client_id=" + _clientId + "&redirect_uri=" +
            WWW.EscapeURL(_callbackURL) +
            "&scope=activity%20nutrition%20heartrate%20location%20profile%20sleep%20weight%20social";
        url += "&state="+GameManager.instance.userId;
        Application.OpenURL(url);
        // print(url);
    }

    public void FetchFitbitData ()
    {
        StartCoroutine(UpdateFitbitData());
    }

    IEnumerator UpdateFitbitData ()
    {
        var headers = new Dictionary<string, string>();
        headers["Authorization"] = "Bearer " + GameManager.instance.fitbit_access_token;

        WWW www = new WWW( _distanceUrl, null, headers);
        yield return www;
        Debug.Log(www.text);

        JsonData distance_json = JsonMapper.ToObject(www.text);
        float todays_distance = float.Parse(distance_json["activities-distance"][0]["value"].ToString());
        HomebaseLevelManager myHomeManager = FindObjectOfType<HomebaseLevelManager>();
        myHomeManager.UpdateDistance(todays_distance);

        /*
        WWW www1 = new WWW(_activityUrl, null, headers);
        yield return www1;
        Debug.Log(www1.text);

        JsonData activity_json = JsonMapper.ToObject(www1.text);

        if (activity_json.Keys.Contains("errors") || distance_json.Keys.Contains("errors"))
        {
            if(activity_json["errors"][0]["errorType"].ToString() == "invalid_token" || distance_json["errors"][0]["errorType"].ToString() == "invalid_token"){
                RefreshAccessToken();
            }
        }
        */
        
    }


    IEnumerator AuthorizeFitbit(string URL)
    {
        WWW www = new WWW(URL);
        yield return www;
        Debug.Log(www.text);

        if (www.error == null)
        {
            yield return new WaitForSeconds(1.5f);
            FindObjectOfType<HomebaseLevelManager>().UpdateDataFromServer();
            /*JsonData authorization_json = JsonMapper.ToObject(www.text);
            if (authorization_json[0].ToString() == "Success")
            {
                GameManager.instance.fitbit_authorization_code = authorization_json[1].ToString();
                Debug.Log(GameManager.instance.fitbit_authorization_code);
            }*/
        }
        else
        {
            Debug.Log(www.error);
        }
    }

    void RefreshAccessToken()
    {
        StartCoroutine(FetchUpdatedAccessToken());
    }

    IEnumerator FetchUpdatedAccessToken()
    {
        Debug.Log("refreshing Token access");
        var plainTextBytes = Encoding.UTF8.GetBytes(_clientId+":"+_consumerSecret);
        var encoded = Convert.ToBase64String(plainTextBytes);

        WWWForm form = new WWWForm();
        form.AddField("grant_type", "refresh_token");
        form.AddField("refresh_token", GameManager.instance.fitbit_refresh_token);

        var headers = form.headers;
        headers["Authorization"] = "Basic " + encoded;

        WWW www = new WWW(_tokenUrl, form.data, headers);
        yield return www;
        Debug.Log(www.text);

        JsonData tokenRefreshJson = JsonMapper.ToObject(www.text);
        if (tokenRefreshJson.Keys.Contains("invalid_token"))
        {
            UserAcceptOrDeny();
        } else if (tokenRefreshJson.Keys.Contains("access_token")){

        }

    }

    //just a utility function to get the correct date format for activity calls that require one
    public static string GetCurrentDate()
    {
        var date = "";
        date += DateTime.Now.Year;
        if (DateTime.Now.Month < 10)
        {
            date += "-" + "0" + DateTime.Now.Month;
        }
        else
        {
            date += "-" + DateTime.Now.Month;
        }

        if (DateTime.Now.Day < 10)
        {
            date += "-" + "0" + DateTime.Now.Day;
        }
        else
        {
            date += "-" + DateTime.Now.Day;
        }
        //date += "-" + 15;
        return date;
    }

    private static string GetYesterdayDate()
    {
        //TODO: DOUBLE CHECK THAT THIS ACTUALLY WORKS FOR JAN 1 of a year (AKA gets Dec 31 of previous year.)
        //Getting yesterday is a bit tricky sometimes. We have to check what day it is and what month even before actually building the string
        //This is because for example, Jan 1st, 2015. The last day would be Dec 31, 2014. This requires us to actually change the whole string
        //compared to if it was intra-month.
        var date = "";
        if (DateTime.Now.Day == 1)
        {
            //we know that we are on the first day of the month, if we are the first day of Jan then we need to go back to the last day of Dec
            if (DateTime.Today.Month == 1)
            {
                date += DateTime.Now.Year - 1;
                date += "-12-31";
                return date;
            }
            //else we aren't Jan so we can just subtract a month and go to the last day of that month.
            else
            {
                date += DateTime.Now.Year
                    + "-" + (DateTime.Today.Month - 1)
                        + "-" + (DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month - 1));
                return date;
            }
        }
        date += DateTime.Now.Year;

        //Months
        if (DateTime.Now.Month < 10)
        {
            date += "-" + "0" + DateTime.Now.Month;
        }
        else
        {
            date += "-" + DateTime.Now.Month;
        }

        //Days
        if (DateTime.Now.Day - 1 < 10)
        {
            date += "-" + "0" + (DateTime.Now.Day - 1);
        }
        else
        {
            date += "-" + (DateTime.Now.Day - 1);
        }
        return date;
    }
}
