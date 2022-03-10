using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DatabaseControl;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Networking;

// Essentially just manages the DCF in built database control module.
// https://assetstore.unity.com/packages/tools/network/database-control-free-41337
// TODO: upgrade to DCF pro before release, as current requests are only HTTP (not secure)
public class AccountManager : MonoBehaviour
{
    // log in UI variables
    [SerializeField]
    private InputField LogInUsernameInput;
    [SerializeField]
    private InputField LogInPasswordInput;
    [SerializeField]
    private Text LogInErrorText;

    // public : so game manager can access it 
    public GameObject mainLoadWheel;
    public GameObject userProgressContainer;
    public GameObject mainMenu; 
    public GameObject playerShowcase;
    
    public bool isMainMenuStillLoading = true;

    private GameObject authMenu;
    private GameObject authLoadWheel;

    // register UI variables
    [SerializeField]
    private InputField RegisterUsernameInput;
    [SerializeField]
    private InputField RegisterPassInput;
    [SerializeField]
    private InputField RegisterConfPassInput;
    [SerializeField]
    private Text RegisterErrorText;

    private Button logInButton;
    private Button registerButton;

    public string loggedInUsername;
    private string loggedInPassword;
    private string loggedInUserData;
    private bool isLoggedIn = false;

    public int playerCoins;
    public int playerGems;
    public int playerTrophies;
 
    public static AccountManager instance; // uses instancing to avoid multiple account managers in one scene

    private void OnSceneChanged(Scene current, Scene next)
    {
        /* You may notice there's two different load wheels
         * As much as I'd like to have one load wheel that doesn't destroy on load.
         * If that were the case, I'd have to have an entire canvas dedicated to that load wheel and 
         * that is simply unnecessary. For this reason, each scene has their own dedicated load wheel instance.
         */
        
        
        // auth menu
        if (next.buildIndex == 0)
        {
            authLoadWheel = GameObject.FindGameObjectWithTag("loadwheel");
            authMenu = GameObject.FindGameObjectWithTag("authmenu");
            authLoadWheel.SetActive(false);
        }
        // Main menu 
        if (next.buildIndex == 1)
        {
            mainLoadWheel = GameObject.FindGameObjectWithTag("loadwheel");
            mainMenu = GameObject.FindGameObjectWithTag("mainmenu");
            userProgressContainer = GameObject.FindGameObjectWithTag("userprogresscontainer");
            playerShowcase = GameObject.FindGameObjectWithTag("player_display_obj");
            
            GameObject.FindWithTag("userprogresscontainer").SetActive(true);
            playerShowcase.SetActive(true);

            // show user data in UI
            GameObject.FindGameObjectWithTag("username_text").GetComponent<Text>().text = loggedInUsername;
            GameObject.FindGameObjectWithTag("cash_text").GetComponent<Text>().text = playerCoins.ToString();
            GameObject.FindGameObjectWithTag("bolts_text").GetComponent<Text>().text = playerGems.ToString();
            GameObject.FindGameObjectWithTag("cups_text").GetComponent<Text>().text = playerTrophies.ToString();

        }
        // In game 
        else if (next.buildIndex == 2)
        {
            
        }
    }

    // When the main menu loads, the GetUserData coroutine is called and this is the callback function.
    /*void OnGetUserData()
    {
        
        // It first unwraps the data as all user data is stored in a single string, and then adds the values to their relevant UI components.
        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            string[] data = loggedInUserData.Split(','); // splits data by semi colon to reveal all values

            playerCash = Convert.ToInt32(data[0]);
            playerBolts = Convert.ToInt32(data[1]);
            playerCups = Convert.ToInt32(data[2]);
            
            Debug.Log("Loading finished");
            isMainMenuStillLoading = false;
            

            mainMenu.SetActive(true);
            mainLoadWheel.SetActive(false);
            userProgressContainer.SetActive(true);
            playerShowcase.SetActive(true);

            // show user data in UI
            GameObject.FindGameObjectWithTag("username_text").GetComponent<Text>().text = loggedInUsername;
            GameObject.FindGameObjectWithTag("cash_text").GetComponent<Text>().text = playerCash.ToString();
            GameObject.FindGameObjectWithTag("bolts_text").GetComponent<Text>().text = playerBolts.ToString();
            GameObject.FindGameObjectWithTag("cups_text").GetComponent<Text>().text = playerCups.ToString();
        }
    }*/

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this); // the account manager should exist for all scenes so we can get and set data throughout

        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    // This method is called by the log in button, which then passes to a coroutine.
    public void TriggerLogIn(Button btn)
    {
        logInButton = btn;
        logInButton.interactable = false;

        authMenu.SetActive(false);
        authLoadWheel.SetActive(true);
        StartCoroutine(instance.LogIn(LogInUsernameInput.text, LogInPasswordInput.text));
    }

    // This method is called by the register button, which then passes to a coroutine.
    public void TriggerRegister(Button btn)
    {
        // stops interaction with buttons whilst loading is occuring
        registerButton = btn;
        registerButton.interactable = false;

        

        string registerUsername = RegisterUsernameInput.text.ToLower(); // all usernames are converted to lower case
        string registerPass = RegisterPassInput.text;
        string registerConfPass = RegisterConfPassInput.text;

        if (registerUsername == "" && registerPass == "")
        {
            // nothing inputted
            RegisterErrorText.enabled = true;
            RegisterErrorText.text = "Please enter some data.";
            registerButton.interactable = true;
        }
        else if (registerPass != registerConfPass)
        {
            // passwords don't match
            RegisterErrorText.enabled = true;
            RegisterErrorText.text = "Your passwords don't match.";
            registerButton.interactable = true;

        }
        else if (registerPass.Length < 6)
        {
            // pass too short
            RegisterErrorText.text = "Your password must be 6 characters or longer.";
            registerButton.interactable = true;
        }
        else if (registerUsername.Length < 5 || registerUsername.Length > 11)
        {
            // username too short or long
            RegisterErrorText.text = "Your username must be between 5-11 characters.";
            registerButton.interactable = true;
        }
        else
        {
            // start create account process
            authMenu.SetActive(false);
            authLoadWheel.SetActive(true);
            StartCoroutine(instance.Register(registerUsername, registerPass));
        }
        
    }

    IEnumerator Register(string username, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post("https://cubebrawl.nw.r.appspot.com/register", form))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                // If unsuccessful
                authLoadWheel.SetActive(false);
                authMenu.SetActive(true);
                registerButton.interactable = true;
                RegisterErrorText.text = www.downloadHandler.text;
            }
            else
            {
                Debug.Log("Sign up success");
                
                isLoggedIn = true;
                loggedInUsername = username;
                loggedInPassword = password;
                
                StartCoroutine(instance.GetUserData());
                SceneManager.LoadScene(1); // load main scene*/ 
            }
        }
        
        
    }


    IEnumerator LogIn(string username, string password)
    {
        WWWForm form = new WWWForm();
        form.AddField("username", username);
        form.AddField("password", password);

        using (UnityWebRequest www = UnityWebRequest.Post("https://cubebrawl.nw.r.appspot.com/login", form))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                // If unsuccessful
                authLoadWheel.SetActive(false);
                authMenu.SetActive(true);
                logInButton.interactable = true;
                LogInErrorText.enabled = true;
                LogInErrorText.text = www.downloadHandler.text;
            }
            else
            {
                Debug.Log("Login success");
                
                isLoggedIn = true;
                loggedInUsername = username;
                loggedInPassword = password;
                StartCoroutine(instance.GetUserData());
                SceneManager.LoadScene(1); // load main scene*/ 
            }
        }

    }

    // if the user is logged in, get their data
    IEnumerator GetUserData()
    {
        using (UnityWebRequest www = UnityWebRequest.Get("https://cubebrawl.nw.r.appspot.com/api/user/getData"))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                // If unsuccessful
                
            }
            else
            {
                Debug.Log("Successfully retrieved UserData");
                
                isMainMenuStillLoading = false;
                
                Debug.Log(www.downloadHandler.text);

                String result = www.downloadHandler.text;
                
                string[] data = result.Split(','); // splits data by semi colon to reveal all values

                playerCoins = Convert.ToInt32(data[0]);
                playerGems = Convert.ToInt32(data[1]);
                playerTrophies = Convert.ToInt32(data[2]);
                
                mainMenu.SetActive(true);
                
                GameObject.FindWithTag("mainmenu").SetActive(true);
                //if (mainLoadWheel != null)
                //{
                mainLoadWheel.SetActive(false);
                //}

                //StartCoroutine(instance.GetUserData());
                SceneManager.LoadScene(1); // load main scene*/ 
            }
        }
        
        /*IEnumerator e = DCF.GetUserData(loggedInUsername, loggedInPassword); // << Send request to get the player's data string. Provides the username and password
        while (e.MoveNext())
        {
            yield return e.Current;
        }
        string response = e.Current as string; // << The returned string from the request

        if (response == "Error")
        {
            //There was another error. Automatically logs player out. This error message should never appear, but is here just in case.
            //loggedInUsername = "";
            //loggedInPassword = "";
        }
        else
        {
            //The player's data was retrieved. Goes back to loggedIn UI and displays the retrieved data in the InputField
            loggedInUserData = response;
            OnGetUserData();
        }*/
        
        
    }

    // if the user is logged in, set their data
    IEnumerator SetUserData(string data)
    {
        IEnumerator e = DCF.SetUserData(loggedInUsername, loggedInPassword, data);

        while (e.MoveNext())
        {
            yield return e.Current;
            string output = e.Current as string;

            if (output == "Success")
            {
                // success
                Debug.Log("User data has been altered.");
            }
            else
            {
                // error
                Debug.Log("Error setting user data.");
            }
        }
    }

    // logs out user
    public void LogOut()
    {
        loggedInUsername = "";
        loggedInPassword = "";

        isLoggedIn = false;
    }
}
