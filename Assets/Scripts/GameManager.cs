using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using UnityEngine.SceneManagement;

// Handles joining games, and enabling the scale slider value to function properly. :)
// (this is a really integral part of the game now)
public class GameManager : NetworkBehaviour
{
    private float sliderVal = 0.1f;

    NetworkMatch matchMaker;

    Slider slider;
    Button quickPlayBtn;

    AccountManager accManager;

    bool quickPlayInstantiated = false;

    private void Awake()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
        matchMaker = gameObject.AddComponent<NetworkMatch>();
        accManager = GameObject.FindGameObjectWithTag("accmanager").GetComponent<AccountManager>();
    }

    private void OnQuickPlayClick()
    {
        quickPlayBtn.interactable = false; // prevent spamming of clicks, which fucks up the methods
        accManager.mainLoadWheel.SetActive(true);
        accManager.userProgressContainer.SetActive(false);
        accManager.mainMenu.SetActive(false);
        accManager.playerShowcase.SetActive(false);

        Debug.Log("on quick play click");
        matchMaker.ListMatches(0, 10, "", true, 0, 0, OnMatchList);
    }

    private void OnMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matches)
    {
        Debug.Log("onMatchlist");
        
        // checks if any matches exist, if so join the first one. if not create a new match
        if (success && matches.Count > 0)
        {
            Debug.Log("matches found");
            matchMaker.JoinMatch(matches[0].networkId, "", "", "", 0, 0, OnJoinMatch);
        }
        else
        {
            Debug.Log("trying to create a match");
            CreateMatch();
        }
    }

    private void OnJoinMatch(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        if (success)
        {
            MatchInfo hostInfo = matchInfo;
            NetworkManager.singleton.StartClient(hostInfo);
        }
        else
        {
            // if there's an error, re show the menu so the player can select a different option.
            // TODO : display error messages like in auth menu
            quickPlayBtn.interactable = true;
            accManager.userProgressContainer.SetActive(true);
            accManager.mainLoadWheel.SetActive(false);
            accManager.mainMenu.SetActive(true);
            accManager.playerShowcase.SetActive(true);
        }
    }

    private void Update()
    {
        // adds button listener when scene has finished loading
        if (!accManager.isMainMenuStillLoading && SceneManager.GetActiveScene().buildIndex == 1)
        {
            Debug.Log("Load complete.");

            if (!quickPlayInstantiated)
            {
                quickPlayBtn = GameObject.FindGameObjectWithTag("quickplay_btn").GetComponent<Button>();
                Debug.Log(quickPlayBtn.gameObject.name);
                quickPlayBtn.onClick.AddListener(delegate { OnQuickPlayClick(); }); // listens for when quick play button is pressed

                quickPlayInstantiated = true;
            }
            
        }
        
    }

    // basic create match, 
    private void CreateMatch()
    {
        Debug.Log("createMatch");
        // TODO add elo etc.
        matchMaker.CreateMatch(accManager.loggedInUsername, 4, true, "", "", "", 0, 0, OnMatchCreate);
    }

    public void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        if (success)
        {
            Debug.Log("match created successfully" + extendedInfo);

            MatchInfo hostInfo = matchInfo;
            NetworkServer.Listen(hostInfo, 9000);

            NetworkManager.singleton.StartHost(hostInfo);
        }
        else
        {
            Debug.Log("match was not created " + extendedInfo);
        }

        // if there's an error, re show the menu so the player can select a different option.
        // TODO : display error messages like in auth menu
        // TODO : this code is repeated in OnMatchjoin -> enclose into a method
        quickPlayBtn.interactable = true;
        accManager.userProgressContainer.SetActive(true);
        accManager.mainLoadWheel.SetActive(false);
        accManager.mainMenu.SetActive(true);
        accManager.playerShowcase.SetActive(true);
    }

    private void OnSceneChanged(Scene current, Scene next)
    {
        // main menu
        if (next.buildIndex == 1)
        {
            
        }
        // In game 
        if (next.buildIndex == 2)
        {
            slider = GameObject.FindGameObjectWithTag("slider").GetComponent<Slider>(); // pulls scale slider. we are unable to get this as a ref because net manager transcends scenes

            

            slider.onValueChanged.AddListener(delegate { UpdateSliderVal(); }); // listens for when the scale slider increases, and adjusts sliderVal accordingly

            
        }
    }

    public void UpdateSliderVal()
    {
        sliderVal = slider.value;
    }

    public float GetSliderVal()
    {
        if (sliderVal > 0.05f)
        {
            return sliderVal;
        }
        else
        {
            return 0.05f;
        }
    }
}
