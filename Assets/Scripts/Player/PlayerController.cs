using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using CnControls;

public class PlayerController : NetworkBehaviour
{
    [SerializeField]
    private Transform playerTransform;
    [SerializeField]
    private Rigidbody rigidBody;
    

    private float adjHorizInput;
    private float adjVertInput;

    private const string MatchPointText = "!!MATCH POINT!!";
    
    // Syncs scale
    [SyncVar(hook = "OnScaleUpdate")]
    public float scale;

    GameManager gameManager;

    [SerializeField]
    Material playerMat;
    
    SensitiveJoystick sensitiveJoystick;

    [SerializeField]
    private float moveSpeed;
    [SerializeField]
    private float scaleMod;

    [SyncVar(hook = "UpdatePlayerScore")] 
    private int playerScore = 0;
    [SyncVar(hook = "UpdateOpponentScore")] 
    private int opponentScore = 0;
    
    
    // Although the initial spawner is handled by Net manager, the respawning isn't.
    // TODO : add this into its own class, clean clutter
    private NetworkStartPosition[] spawnPoints;

    private bool handleJoystick = true;

    // Syncs scale
    private void Awake()
    {
        scale = transform.localScale.x;
    }

    // Only affects the local player, no other players in game. 
    public override void OnStartLocalPlayer()
    {
        GetComponent<MeshRenderer>().material = playerMat; // sets the player colour to blue, which distinguishes from the red enemy
        gameManager = GameObject.FindGameObjectWithTag("netmanager").GetComponent<GameManager>(); // ref game manager
        sensitiveJoystick = GameObject.FindGameObjectWithTag("joystick").GetComponent<SensitiveJoystick>(); // discover the joy stick
        spawnPoints = GameObject.FindObjectsOfType<NetworkStartPosition>(); // gets all spawnpoints
        // to add another spawnpoint: add empty to 'spawnpoints' and call it something like 'spawnpoint3' and add NetworkStartPosition
        // component
    }

    void FixedUpdate ()
    {
        if (!isLocalPlayer)
        {
            return;
        }
        
        if (playerTransform.position.y < -30) // if the player falls below this level, he will die
        {
            Respawn();
        }


        // Handles basic respawn
        rigidBody.AddForce(adjHorizInput * Vector3.right);
        rigidBody.AddForce(adjVertInput * Vector3.forward);

        
    }


    
    private void Respawn()
    {
        if (!isLocalPlayer)
        {
            return;
        }
        
        // spawns at a random spawn point use -1 because count starts at 0
        transform.position = spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;

        rigidBody.velocity = new Vector3(0f, 0f, 0f);
        rigidBody.angularVelocity = new Vector3(0f, 0f, 0f);
        playerTransform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
        StartCoroutine(TempDisableJoystick());
        sensitiveJoystick.HandleRespawn();
        
        CmdHandleDeath();

    }
    
    
    /*
     * The Command tag indicates that the code will be ran on the server
     * Code ran on the server is then executed on both clients
     * This means that, although the code that calls this function is ran client side,
     * The Command function allows it to run on both clients
     *
     * Player (client side) -> Server (server side) -> Player and Opponent (client side)
     */
    [Command]
    public void CmdHandleDeath()
    {
        if (isLocalPlayer)
        {
            playerScore += 1;
            return;
        } 
        
        opponentScore += 1;
    }

    /* Preventative message to stop user falling off the edge
     * temporarily stops handling joystick input for 0.25 of a second
     */
    private IEnumerator TempDisableJoystick()
    {

        handleJoystick = false;
        yield return new WaitForSecondsRealtime(0.25f);
        handleJoystick = true;
    }
    

    private void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (isServer)
        {
            if (transform.localScale.x != scale)
                scale = transform.localScale.x;
        }

        CmdUpdateScale(transform.localScale.x);

        // Polls input from the joystick for movement
        if (handleJoystick)
        {
            adjHorizInput = CnInputManager.GetAxis("Horizontal") * Time.fixedDeltaTime * moveSpeed;
            adjVertInput = CnInputManager.GetAxis("Vertical") * Time.fixedDeltaTime * moveSpeed;
        }
        else
        {
            adjHorizInput = 0f;
            adjVertInput = 0f;
        }


        Debug.Log(gameManager.GetSliderVal());
        playerTransform.localScale = new Vector3(gameManager.GetSliderVal() * 10, playerTransform.localScale.y, playerTransform.localScale.z);
    }

    // Syncs scale
    public void OnScaleUpdate(float newScale)
    {
        scale = newScale;
        transform.localScale = new Vector3(scale, 1, 1);
    }

    public void UpdatePlayerScore(int newScore)
    {
        GameObject.FindWithTag("Score_Player").GetComponent<Text>().text = "" + newScore;

        if (newScore == 4)
        {
            var mpText = GameObject.FindWithTag("Match_Point").GetComponent<Text>();
            
            mpText.color = Color.green;
            mpText.GetComponent<Text>().text = MatchPointText;
        }
        
        
    }

    public void UpdateOpponentScore(int newScore)
    {
        GameObject.FindWithTag("Score_Opp").GetComponent<Text>().text = "" + newScore;
        
        if (newScore == 4)
        {
            var mpText = GameObject.FindWithTag("Match_Point").GetComponent<Text>();
            
            mpText.color = Color.red;
            mpText.GetComponent<Text>().text = MatchPointText;
        }
    }

    // Syncs scale
    [Command]
    public void CmdUpdateScale(float newScale)
    {
        scale = newScale;
    }
}
