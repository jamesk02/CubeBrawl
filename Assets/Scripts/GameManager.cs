using System;
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

    private MatchInfo currentMatchInfo = null;
    Button quickPlayBtn;
    private bool isHost = false;
    private GameObject scoreMenu;

    AccountManager accManager;

    bool quickPlayInstantiated = false;
    
    // uses singleton design pattern to avoid multiple account managers in one scene
    // for more info: https://www.journaldev.com/1377/java-singleton-design-pattern-best-practices-examples
    private static GameManager instance; 

    private void Awake()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
        matchMaker = gameObject.AddComponent<NetworkMatch>();
        accManager = GameObject.FindGameObjectWithTag("accmanager").GetComponent<AccountManager>();
        scoreMenu = GameObject.Find("ScoreMenu");
        scoreMenu.GetComponent<CanvasGroup>().alpha = 0f;
        scoreMenu.GetComponent<CanvasGroup>().blocksRaycasts = false;
        scoreMenu.GetComponent<CanvasGroup>().interactable = false;


        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this); // the account manager should exist for all scenes so we can get and set data throughout

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

    public void QuitMatch(GameState gameState)
    {
        if (isHost)
        {
            try
            {
                NetworkManager.singleton.StopHost();
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
            
        }
        
        StartCoroutine(instance.UpdateStats(gameState));

        // So either a win or loss, bring up menu showing game stats and ELO adjustments
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
            currentMatchInfo = matchInfo;
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
    
    IEnumerator UpdateStats(GameState gameState)
    {
        int trophiesAdj = 0;
        int coinsAdj = 0;
        int gemsAdj = 0;
        
        WWWForm form = new WWWForm();
        if (gameState == GameState.WIN)
        {
            trophiesAdj = 25;
            coinsAdj = 500;

            if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
            {
                gemsAdj = 1;
            }
        }
        else if (gameState == GameState.LOSS)
        {
            trophiesAdj = -25;
            coinsAdj = 200;
        }
        
        form.AddField("trophiesAdj", trophiesAdj);
        form.AddField("gemsAdj", gemsAdj);
        form.AddField("coinsAdj", coinsAdj);
        
        using (UnityWebRequest www = UnityWebRequest.Post("https://cubebrawl.nw.r.appspot.com/api/user/setData", form))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                // If unsuccessful

            }
            else
            {
                Debug.Log("Successfully updated Player Stats");

                Debug.Log(www.downloadHandler.text);

                String result = www.downloadHandler.text;
                
                Debug.Log("Result: " + result);

                string[] data = result.Split(','); // splits data by semi colon to reveal all values

                int playerCoins = Convert.ToInt32(data[0]);
                int playerGems = Convert.ToInt32(data[1]);
                int playerTrophies = Convert.ToInt32(data[2]);

                // mainMenu.SetActive(true);

                GameObject.FindWithTag("mainmenu").SetActive(true);
                
                // show user data in UI
                GameObject.FindGameObjectWithTag("cash_text").GetComponent<Text>().text = playerCoins.ToString();
                GameObject.FindGameObjectWithTag("bolts_text").GetComponent<Text>().text = playerGems.ToString();
                GameObject.FindGameObjectWithTag("cups_text").GetComponent<Text>().text = playerTrophies.ToString();

                scoreMenu.GetComponent<CanvasGroup>().alpha = 1f;
                scoreMenu.GetComponent<CanvasGroup>().blocksRaycasts = true;
                scoreMenu.GetComponent<CanvasGroup>().interactable = true;

                Text scoreText = GameObject.Find("Score_Text").GetComponent<Text>();
                Text scoreIndicText = GameObject.Find("Score_Indic_Text").GetComponent<Text>();
                Text statsUpdateText = GameObject.Find("Stats_Update_Text").GetComponent<Text>();

                if (gameState == GameState.WIN)
                {
                    scoreText.text = "Victory";
                    scoreIndicText.text = "";
                    statsUpdateText.text = "";
                }
                else
                {
                    scoreText.text = "Defeat";
                    scoreIndicText.text = "";
                    statsUpdateText.text = "";
                }
                


                //if (mainLoadWheel != null)
                //{
                //mainLoadWheel.SetActive(false);
                //}
            }
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
            currentMatchInfo = matchInfo;
            isHost = true;
            NetworkServer.Listen(hostInfo, 9000);
            
            NetworkManager.singleton.StartHost(hostInfo);
        }
        else
        {
            Debug.Log("match was not created " + extendedInfo);
        }

        // if there's an error, re show the menu so the player can select a different option
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
