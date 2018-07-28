using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DatabaseControl;
using UnityEngine.SceneManagement;
using System;

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
    
    private GameObject userProgressContainer;
    private GameObject mainMenu;
    private GameObject loadingBar;
    private GameObject player;
    public bool isMainMenuStillLoading = true;

    // register UI variables
    [SerializeField]
    private InputField RegisterUsernameInput;
    [SerializeField]
    private InputField RegisterPassInput;
    [SerializeField]
    private InputField RegisterConfPassInput;
    [SerializeField]
    private Text RegisterErrorText;

    public string loggedInUsername;
    private string loggedInPassword;
    private string loggedInUserData;
    private bool isLoggedIn = false;

    public int playerCash;
    public int playerBolts;
    public int playerCups;
 
    public static AccountManager instance; // uses instancing to avoid multiple account managers in one scene

    private void OnSceneChanged(Scene current, Scene next)
    {
        // Main menu 
        if (next.buildIndex == 1)
        {
            mainMenu = GameObject.FindGameObjectWithTag("mainmenu");
            userProgressContainer = GameObject.FindGameObjectWithTag("userprogresscontainer");
            loadingBar = GameObject.FindGameObjectWithTag("loadwheel");
            player = GameObject.FindGameObjectWithTag("player_display_obj");

            mainMenu.SetActive(false);
            userProgressContainer.SetActive(false);
            player.SetActive(false);

            StartCoroutine(GetUserData());
        }
        // In game 
        else if (next.buildIndex == 2)
        {
            
        }
    }

    // When the main menu loads, the GetUserData coroutine is called and this is the callback function.
    void OnGetUserData()
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
            loadingBar.SetActive(false);
            userProgressContainer.SetActive(true);
            player.SetActive(true);

            // show user data in UI
            GameObject.FindGameObjectWithTag("username_text").GetComponent<Text>().text = loggedInUsername;
            GameObject.FindGameObjectWithTag("cash_text").GetComponent<Text>().text = playerCash.ToString();
            GameObject.FindGameObjectWithTag("bolts_text").GetComponent<Text>().text = playerBolts.ToString();
            GameObject.FindGameObjectWithTag("cups_text").GetComponent<Text>().text = playerCups.ToString();
        }
    }

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
    public void TriggerLogIn()
    {
        StartCoroutine(instance.LogIn(LogInUsernameInput.text, LogInPasswordInput.text));
    }

    // This method is called by the register button, which then passes to a coroutine.
    public void TriggerRegister()
    {
        if (RegisterPassInput.text != RegisterConfPassInput.text)
        {
            // passwords don't match
            RegisterErrorText.enabled = true;
            RegisterErrorText.text = "Your passwords don't match.";

        }
        else if (RegisterPassInput.text.Length < 6)
        {
            // pass too short
            RegisterErrorText.text = "Your password must be 6 characters or longer.";
        }
        else if (RegisterUsernameInput.text.Length < 5)
        {
            // username too short
            RegisterErrorText.text = "Your username must be 5 characters or longer.";
        }
        else
        {
            // start create account process
            StartCoroutine(instance.Register(RegisterUsernameInput.text, RegisterPassInput.text));
        }
        
    }

    IEnumerator Register(string username, string password)
    {
        // uses HTTP requests to poll for registering user
        // responds to output in real time, hence the need for ienumerator
        IEnumerator e = DCF.RegisterUser(username, password, "250,20,100"); // 250 cash, 20 bolts, 100 cups
        // 10 R refers to bolts, 100 C refers to credits
        while (e.MoveNext())
        {
            yield return e.Current;
        }
        string output = e.Current as string;
        
        if (output == "Success")
        {
            // account created
            isLoggedIn = true;
            loggedInUsername = username;
            loggedInPassword = password;
            StartCoroutine(instance.GetUserData());
            SceneManager.LoadScene(1); // load main scene TODO change this to main menu with shop etc.
        }
        else
        {
            RegisterErrorText.enabled = true;
            if (output == "UserError")
            {
                // username taken
                RegisterErrorText.text = "The username you chose has already been taken.";
            }
            else if (output == "UserShort")
            {
                // username too short
                RegisterErrorText.text = "Your username must be 5 characters or longer.";
            }
            else if (output == "PassShort")
            {
                // password too short
                RegisterErrorText.text = "Your password must be 6 characters or longer.";
            }
            else if (output == "Error")
            {
                // another error
                RegisterErrorText.text = "An error occured.";
            }
        }
        
    }

    private void FixedUpdate()
    {
        // debug just to see if logged in and that
        // TODO remove this before release
        if (isLoggedIn)
        {
            Debug.Log("username : " + loggedInUsername);
            Debug.Log("password : " + loggedInPassword);
        }
    }

    IEnumerator LogIn(string username, string password)
    {
        IEnumerator e = DCF.Login(LogInUsernameInput.text, LogInPasswordInput.text);
        while (e.MoveNext())
        {
            yield return e.Current;
        }
        string output = e.Current as string;
        if (output == "Success")
        {
            // TODO bundle this and the equiv. in register into one method to clean up code
            // log in successful
            isLoggedIn = true;
            loggedInUsername = username;
            loggedInPassword = password;
            StartCoroutine(instance.GetUserData());
            SceneManager.LoadScene(1); // load main scene TODO change this to main menu with shop etc.
        }
        else
        {
            LogInErrorText.enabled = true;
            if (output == "UserError")
            {
                // username doesnt exist
                LogInErrorText.text = "Username or password incorrect.";
            }
            else if (output == "PassError")
            {
                // username exists, wrong pass
                LogInErrorText.text = "Username or password incorrect.";
            }
            else if (output == "Error")
            {
                // misc error
                LogInErrorText.text = "An error occured.";
            }
        }
    }

    // if the user is logged in, get their data
    IEnumerator GetUserData()
    {
        IEnumerator e = DCF.GetUserData(loggedInUsername, loggedInPassword); // << Send request to get the player's data string. Provides the username and password
        while (e.MoveNext())
        {
            yield return e.Current;
        }
        string response = e.Current as string; // << The returned string from the request

        if (response == "Error")
        {
            //There was another error. Automatically logs player out. This error message should never appear, but is here just in case.
            loggedInUsername = "";
            loggedInPassword = "";
        }
        else
        {
            //The player's data was retrieved. Goes back to loggedIn UI and displays the retrieved data in the InputField
            loggedInUserData = response;
            OnGetUserData();
        }
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
