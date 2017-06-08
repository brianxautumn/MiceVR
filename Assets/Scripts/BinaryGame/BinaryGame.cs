using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class BinaryGame : MonoBehaviour
{

    public float TargetLength;
    public float GreyLength;
    public float PresentationLength;
    public float TunnelWidth;
    public float TargetDepth;
    public float MazeHeight;
    public float StartLength;
    public float BlackLevel;
    public float WhiteLevel;
    public int TrialDelay;
    private float greyLevel;
    public int PresentationAngle = 45;

    public enum TargetSide
    {
        Left,
        Right
    }

    // game data
    private int presentationIndex = 0;
    private TargetSide targetSide = TargetSide.Left;

    private float wallWidth = 0.01f;

    [SerializeField]
    private GameObject mazeTarget;
    // Maze walls
    private GameObject startWallLeft;
    private GameObject startWallRight;
    private GameObject tunnelBackWall;
    private GameObject presentationWallLeft;
    private GameObject presentationWallRight;
    private GameObject greyWallLeft;
    private GameObject greyWallRight;
    private GameObject targetInnerWallLeft;
    private GameObject targetInnerWallRight;
	private GameObject targetCapWallLeft;
	private GameObject targetCapWallRight;
	private GameObject targetOuterCapWallLeft;
	private GameObject targetOuterCapWallRight;
    private GameObject outerCapWall;
    private GameObject floor;
    private GameObject triggerLeft;
    private GameObject triggerRight;

	[SerializeField]
	private Material presentationMaterial;
    [SerializeField]
    private Material presentationMaterial1;
	[SerializeField]
	private Material presentationMaterial2;
	[SerializeField]
	private Material greyMaterial;
	[SerializeField]
	private Material floorMaterial;

    [SerializeField]
    private GameObject mouse;

	public int runDuration;
	public int numberOfRuns;
	public int numberOfAllRewards;
	public GameObject player;
	public GameObject menuPanel;
	public Image fadeToBlack;
	public Text fadeToBlackText;
	public Text rewardAmountText;
	public Text numberOfDryTreesText;
	public Text numberOfCorrectTurnsText;
	public Text numberOfTrialsText;
	public Text lastAccuracyText;
	public Text timeElapsedText;
	public UDPSend udpSender;
	public MovementRecorder movementRecorder;

	private float rawSpeedDivider;  // Normally 60f; previously 200f
	private float rawRotationDivider;  // Normally 500f; previously -4000f
	private int respawnAmplitude = 2000;
	private int runNumber;
	private float runTime;
	private long frameCounter, previousFrameCounter;
	private System.Collections.Generic.Queue<float> last5Mouse2Y, last5Mouse1Y;
	public string state;
	private bool firstFrameRun;
	private bool playerInWaterTree, playerInDryTree;
	private Loader scenarioLoader;
	private CharacterController characterController;
	private DebugControl debugControlScript;
	private bool timeoutState;

	private int smoothingWindow = 1;  // Amount to smoothen the player movement
	private bool waitedOneFrame = false;  // When mouse hits tree, need to wait a few frames before it turns black, and then pause the game

	private Vector3 startingPos;
	private Quaternion startingRot;
	private Vector3 prevPos;

	private int centralViewVisible;

    // Use this for initialization
    void Start()
    {
        this.startingPos = new Vector3(1.0f, 5.0f, 5.0f);
        this.BuildMaze();

        // mouse.transform.position = new Vector3(1.0f, 5.0f, 5.0f);

        // Now setup experimental controls
		Debug.Log("started!");
		this.frameCounter = this.previousFrameCounter = 0;
		this.runNumber = 1;
		this.last5Mouse1Y = new System.Collections.Generic.Queue<float>(smoothingWindow);
		this.last5Mouse2Y = new System.Collections.Generic.Queue<float>(smoothingWindow);
		this.state = "LoadScenario";
		this.firstFrameRun = false;
		// this.scenarioLoader = GameObject.FindGameObjectWithTag("generator").GetComponent<Loader>();
		this.characterController = GameObject.Find("FPSController").GetComponent<CharacterController>();
		this.debugControlScript = GameObject.Find("FPSController").GetComponent<DebugControl>();
		this.characterController.enabled = false;  // Keeps me from moving the character while typing entries into the form
		Globals.numberOfEarnedRewards = 0;
		Globals.numberOfUnearnedRewards = 0;
		Globals.rewardAmountSoFar = 0;
		Globals.numberOfTrials = 1;  // Start on first trial
		this.timeoutState = false;

        //this.startingPos = this.player.transform.position;
        //this.startingRot = this.player.transform.rotation;
        TeleportToBeginning();

		this.prevPos = this.startingPos;

		// Will this fix the issue where rarely colliding with a wall causes mouse to fly above the wall?  No.
		this.characterController.enableOverlapRecovery = false;

		init();
    }

    private void BuildMaze()
    {
        foreach (Transform child in mazeTarget.transform)
        {
            Destroy(child.gameObject);
        }

		presentationIndex = UnityEngine.Random.Range(0, 2);
        Debug.Log(presentationIndex);
		

        int presentationDegrees;
        int incorrectDegrees;

        if (presentationIndex == 1)
        {
            presentationDegrees = PresentationAngle;
            incorrectDegrees = 0;
        }
        else
        {
			presentationDegrees = 0;
			incorrectDegrees = PresentationAngle;
        }

        float sidePosition = TunnelWidth / 2.0f;
        float forkStart = PresentationLength + GreyLength + StartLength;
        float mazeHeightOffset = MazeHeight / 2.0f;

        BlackLevel = Mathf.Clamp(BlackLevel, 0, 1.0f);
        WhiteLevel = Mathf.Clamp(WhiteLevel, 0, 1.0f);
        greyLevel = (BlackLevel + WhiteLevel) / 2.0f;

        // Generate Maze Here
        tunnelBackWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tunnelBackWall.transform.SetParent(mazeTarget.transform);
        tunnelBackWall.transform.name = "TunnelBackWall";
        tunnelBackWall.transform.localScale = new Vector3(TunnelWidth, MazeHeight, wallWidth);
        tunnelBackWall.transform.localPosition = new Vector3(0, mazeHeightOffset, 0);
        tunnelBackWall.GetComponent<Renderer>().material = Instantiate(greyMaterial);
        tunnelBackWall.GetComponent<Renderer>().material.color = new Color(greyLevel, greyLevel, greyLevel, 1.0f);

        startWallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        startWallLeft.transform.SetParent(mazeTarget.transform);
        startWallLeft.transform.name = "StartWallLeft";
        startWallLeft.transform.localScale = new Vector3(StartLength, MazeHeight, wallWidth);
        startWallLeft.transform.rotation *= Quaternion.Euler(0, 90, 0);
        startWallLeft.transform.localPosition = new Vector3(-sidePosition, mazeHeightOffset, StartLength / 2.0f);
        startWallLeft.GetComponent<Renderer>().material = Instantiate(greyMaterial);
        startWallLeft.GetComponent<Renderer>().material.color = new Color(greyLevel, greyLevel, greyLevel, 1.0f);

        startWallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        startWallRight.transform.SetParent(mazeTarget.transform);
        startWallRight.transform.name = "StartWallRight";
        startWallRight.transform.localScale = new Vector3(StartLength, MazeHeight, wallWidth);
        startWallRight.transform.rotation *= Quaternion.Euler(0, 90, 0);
        startWallRight.transform.localPosition = new Vector3(sidePosition, mazeHeightOffset, StartLength / 2.0f);
        startWallRight.GetComponent<Renderer>().material = Instantiate(greyMaterial);
        startWallRight.GetComponent<Renderer>().material.color = new Color(greyLevel, greyLevel, greyLevel, 1.0f);

        presentationWallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        presentationWallLeft.transform.SetParent(mazeTarget.transform);
        presentationWallLeft.transform.name = "PresentationWallLeft";
        presentationWallLeft.transform.localScale = new Vector3(PresentationLength, MazeHeight, wallWidth);
        presentationWallLeft.transform.rotation *= Quaternion.Euler(0, 90, 0);
        presentationWallLeft.transform.localPosition = new Vector3(-sidePosition, mazeHeightOffset, PresentationLength / 2.0f + StartLength);
        presentationWallLeft.GetComponent<Renderer>().material = Instantiate(presentationMaterial);
        presentationWallLeft.GetComponent<Renderer>().material.SetInt("_Deg", -presentationDegrees);
        presentationWallLeft.GetComponent<Renderer>().material.mainTextureScale = new Vector2(PresentationLength, 1.0f);

        presentationWallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        presentationWallRight.transform.SetParent(mazeTarget.transform);
        presentationWallRight.transform.name = "PresentationWallRight";
        presentationWallRight.transform.localScale = new Vector3(PresentationLength, MazeHeight, wallWidth);
        presentationWallRight.transform.rotation *= Quaternion.Euler(0, 90, 0);
        presentationWallRight.transform.localPosition = new Vector3(sidePosition, mazeHeightOffset, PresentationLength / 2.0f + StartLength);
        presentationWallRight.GetComponent<Renderer>().material = Instantiate(presentationMaterial);
        presentationWallRight.GetComponent<Renderer>().material.SetInt("_Deg", presentationDegrees);
        presentationWallRight.GetComponent<Renderer>().material.mainTextureScale = new Vector2(PresentationLength, 1.0f);

        if(GreyLength > 0){
            greyWallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            greyWallLeft.transform.SetParent(mazeTarget.transform);
            greyWallLeft.transform.name = "GreyWallLeft";
            greyWallLeft.transform.localScale = new Vector3(GreyLength, MazeHeight, wallWidth);
            greyWallLeft.transform.rotation *= Quaternion.Euler(0, -90, 0);
            greyWallLeft.transform.localPosition = new Vector3(-sidePosition, mazeHeightOffset, GreyLength / 2.0f + PresentationLength + StartLength);
            greyWallLeft.GetComponent<Renderer>().material = Instantiate(greyMaterial);
            greyWallLeft.GetComponent<Renderer>().material.color = new Color(greyLevel, greyLevel, greyLevel, 1.0f);

            greyWallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            greyWallRight.transform.SetParent(mazeTarget.transform);
            greyWallRight.transform.name = "GreyWallRight";
            greyWallRight.transform.localScale = new Vector3(GreyLength, MazeHeight, wallWidth);
            greyWallRight.transform.rotation *= Quaternion.Euler(0, 90, 0);
            greyWallRight.transform.localPosition = new Vector3(sidePosition, mazeHeightOffset, GreyLength / 2.0f + PresentationLength + StartLength);
            greyWallRight.GetComponent<Renderer>().material = Instantiate(greyMaterial);
            greyWallRight.GetComponent<Renderer>().material.color = new Color(greyLevel, greyLevel, greyLevel, 1.0f);
        }

        targetInnerWallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetInnerWallLeft.transform.SetParent(mazeTarget.transform);
        targetInnerWallLeft.transform.name = "TargetInnerWallLeft";
        targetInnerWallLeft.transform.localPosition = new Vector3(-sidePosition - TargetDepth / 2.0f, mazeHeightOffset, forkStart);
        targetInnerWallLeft.transform.localScale = new Vector3(TargetDepth, MazeHeight, wallWidth);
        targetInnerWallLeft.GetComponent<Renderer>().material = Instantiate(greyMaterial);
        targetInnerWallLeft.GetComponent<Renderer>().material.color = new Color(greyLevel, greyLevel, greyLevel, 1.0f);

        targetInnerWallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetInnerWallRight.transform.SetParent(mazeTarget.transform);
        targetInnerWallRight.transform.name = "TargetInnerWallLeft";
        targetInnerWallRight.transform.localPosition = new Vector3(sidePosition + TargetDepth / 2.0f, mazeHeightOffset, forkStart);
        targetInnerWallRight.transform.localScale = new Vector3(TargetDepth, MazeHeight, wallWidth);
        targetInnerWallRight.GetComponent<Renderer>().material = Instantiate(greyMaterial);
        targetInnerWallRight.GetComponent<Renderer>().material.color = new Color(greyLevel, greyLevel, greyLevel, 1.0f);

        targetCapWallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetCapWallLeft.transform.SetParent(mazeTarget.transform);
        targetCapWallLeft.transform.name = "TargetCapWallLeft";
        targetCapWallLeft.transform.rotation *= Quaternion.Euler(0, 90, 0);
        targetCapWallLeft.transform.localPosition = new Vector3(-sidePosition - TargetDepth, mazeHeightOffset, forkStart + TargetLength / 2.0f);
        targetCapWallLeft.transform.localScale = new Vector3(TargetLength, MazeHeight, wallWidth);

        targetCapWallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetCapWallRight.transform.SetParent(mazeTarget.transform);
        targetCapWallRight.transform.name = "TargetCapWallRight";
        targetCapWallRight.transform.rotation *= Quaternion.Euler(0, 90, 0);
        targetCapWallRight.transform.localPosition = new Vector3(sidePosition + TargetDepth, mazeHeightOffset, forkStart + TargetLength / 2.0f);
        targetCapWallRight.transform.localScale = new Vector3(TargetLength, MazeHeight, wallWidth);

        targetOuterCapWallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetOuterCapWallLeft.transform.SetParent(mazeTarget.transform);
        targetOuterCapWallLeft.transform.name = "TargetOuterCapWallLeft";
        targetOuterCapWallLeft.transform.localPosition = new Vector3(-sidePosition - TargetDepth / 2.0f, mazeHeightOffset, forkStart + TargetLength);
        targetOuterCapWallLeft.transform.localScale = new Vector3(TargetDepth, MazeHeight, wallWidth);

        targetOuterCapWallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetOuterCapWallRight.transform.SetParent(mazeTarget.transform);
        targetOuterCapWallRight.transform.name = "TargetOuterCapWallRight";
        targetOuterCapWallRight.transform.localPosition = new Vector3(sidePosition + TargetDepth / 2.0f, mazeHeightOffset, forkStart + TargetLength);
        targetOuterCapWallRight.transform.localScale = new Vector3(TargetDepth, MazeHeight, wallWidth);

        outerCapWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        outerCapWall.transform.SetParent(mazeTarget.transform);
        outerCapWall.transform.name = "OuterCapWall";
        outerCapWall.transform.localScale = new Vector3(TunnelWidth, MazeHeight, wallWidth);
        outerCapWall.transform.localPosition = new Vector3(0.0f, mazeHeightOffset, forkStart + TargetLength);
        outerCapWall.GetComponent<Renderer>().material = Instantiate(greyMaterial);
        outerCapWall.GetComponent<Renderer>().material.color = new Color(greyLevel, greyLevel, greyLevel, 1.0f);

        triggerLeft = GameObject.CreatePrimitive(PrimitiveType.Quad);
        triggerLeft.transform.SetParent(mazeTarget.transform);
        triggerLeft.transform.name = "TriggerLeft";
        triggerLeft.transform.rotation *= Quaternion.Euler(0, 90, 0);
        triggerLeft.transform.localPosition = new Vector3(-sidePosition, mazeHeightOffset, forkStart + TargetLength / 2.0f);
        triggerLeft.transform.localScale = new Vector3(TargetLength, MazeHeight, wallWidth);
        triggerLeft.transform.GetComponent<Renderer>().enabled = false;
        BoxCollider leftCollider = triggerLeft.AddComponent<BoxCollider>();
        ColliderData colliderDataLeft = triggerLeft.AddComponent<ColliderData>();

        triggerRight = GameObject.CreatePrimitive(PrimitiveType.Quad);
        triggerRight.transform.SetParent(mazeTarget.transform);
        triggerRight.transform.SetParent(mazeTarget.transform);
        triggerRight.transform.name = "TriggerRight";
        triggerRight.transform.rotation *= Quaternion.Euler(0, 90, 0);
        triggerRight.transform.localPosition = new Vector3(sidePosition, mazeHeightOffset, forkStart + TargetLength / 2.0f);
        triggerRight.transform.localScale = new Vector3(TargetLength, MazeHeight, wallWidth);
        triggerRight.transform.GetComponent<Renderer>().enabled = false;
        BoxCollider rightCollider = triggerRight.AddComponent<BoxCollider>();
        ColliderData colliderDataRight = triggerRight.AddComponent<ColliderData>();

        targetSide = (TargetSide)UnityEngine.Random.Range(0, 2);
        Debug.Log("Target Side is : " + targetSide);
        Debug.Log(presentationDegrees);
        if(targetSide == TargetSide.Left)
        {
            colliderDataLeft.Correct = true;

            targetOuterCapWallLeft.GetComponent<Renderer>().material = Instantiate(presentationMaterial);
            targetOuterCapWallLeft.GetComponent<Renderer>().material.SetInt("_Deg", -presentationDegrees);

            targetCapWallLeft.GetComponent<Renderer>().material = Instantiate(presentationMaterial);
            targetCapWallLeft.GetComponent<Renderer>().material.SetInt("_Deg", -presentationDegrees);

            targetOuterCapWallRight.GetComponent<Renderer>().material = Instantiate(presentationMaterial);
            targetOuterCapWallRight.GetComponent<Renderer>().material.SetInt("_Deg", incorrectDegrees);

            targetCapWallRight.GetComponent<Renderer>().material = Instantiate(presentationMaterial);
            targetCapWallRight.GetComponent<Renderer>().material.SetInt("_Deg", incorrectDegrees);
        }
        else
        {
			colliderDataRight.Correct = true;

			targetOuterCapWallRight.GetComponent<Renderer>().material = Instantiate(presentationMaterial);
			targetOuterCapWallRight.GetComponent<Renderer>().material.SetInt("_Deg", -presentationDegrees);

			targetCapWallRight.GetComponent<Renderer>().material = Instantiate(presentationMaterial);
			targetCapWallRight.GetComponent<Renderer>().material.SetInt("_Deg", -presentationDegrees);

            targetOuterCapWallLeft.GetComponent<Renderer>().material = Instantiate(presentationMaterial);
			targetOuterCapWallLeft.GetComponent<Renderer>().material.SetInt("_Deg", incorrectDegrees);

            targetCapWallLeft.GetComponent<Renderer>().material = Instantiate(presentationMaterial);
            targetCapWallLeft.GetComponent<Renderer>().material.SetInt("_Deg", incorrectDegrees); 
        }

        floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
        floor.transform.SetParent(mazeTarget.transform);
        floor.transform.name = "Floor";
        floor.transform.rotation *= Quaternion.Euler(90, 0, 0);
        float floorLength = TargetLength + PresentationLength + GreyLength + StartLength;
        floor.transform.localScale = new Vector3(TunnelWidth + 2 * TargetDepth, floorLength, 1.0f);
        floor.transform.localPosition = new Vector3(0, 0, floorLength / 2.0f);
        floor.transform.GetComponent<Renderer>().material = floorMaterial;

    }

	public void init()
	{
        this.runDuration = 3600;
        this.numberOfRuns = 333;
        this.numberOfAllRewards = 333;
        this.rawSpeedDivider = 160;
        this.rawRotationDivider = 5000;
        this.centralViewVisible = 0;
        Globals.rewardDur = 33;
        Globals.rewardSize = 2.2f;

		Globals.centralViewVisibleShift = (float)(centralViewVisible * 0.58 / 120);  // 0.45/120

		// trying to avoid first drops of water
		this.udpSender.ForceStopSolenoid();
		this.udpSender.setAmount(Globals.rewardDur);
		this.udpSender.CheckReward();
	}

	// Update is called once per frame
	void Update()
	{

		//Debug.Log ("Framerate: " + 1.0f / Time.deltaTime);
		CatchKeyStrokes();

		// Keep mouse from scaling walls - 
		if (this.player.transform.position.y > this.startingPos.y + 0.1)
		{
			Vector3 tempPos = this.player.transform.position;
			tempPos.y = this.startingPos.y;
			tempPos.x = this.prevPos.x;
			tempPos.z = this.prevPos.z;
			this.player.transform.position = tempPos;
		}
		this.prevPos = this.player.transform.position;

		switch (this.state)
		{
			case "LoadScenario":
				// LoadScenario();
				break;

			case "StartGame":
				StartGame();
				break;

			case "Timeout":
				Timeout();
				break;

			case "Running":
				Run();
				break;

			case "Fading":
				Fade();
				break;

			case "Paused":
				Pause();
				break;

			case "Reset":
				ResetScenario(Color.black);
				break;

			case "Respawn":
				Respawn(Color.black);
				break;

			case "GameOver":
				GameOver();
				break;

			default:
				break;
		}
	}

	private void CatchKeyStrokes()
	{
		if (Input.GetKey(KeyCode.Escape))
			this.state = "GameOver";

		if (!this.state.Equals("LoadScenario") || (this.state.Equals("LoadScenario") && EventSystem.current.currentSelectedGameObject == null))
		{
			if (Input.GetKeyUp(KeyCode.U))
			{
				this.udpSender.SendWaterReward(Globals.rewardDur);
				Globals.numberOfUnearnedRewards++;
				Globals.rewardAmountSoFar += Globals.rewardSize;
				this.rewardAmountText.text = "Reward amount so far: " + Math.Round(Globals.rewardAmountSoFar);
				//this.numberOfUnearnedRewardsText.text = "Number of unearned rewards: " + Globals.numberOfUnearnedRewards.ToString();
			}
			else if (Input.GetKeyUp(KeyCode.T))
			{
				// Mouse is stuck so teleport to beginning
				TeleportToBeginning();
			}
		}
	}

	private void TeleportToBeginning()
	{
        // this.player.transform.position = this.startingPos;
        // this.player.transform.rotation = this.startingRot;
        Debug.Log("TELEPORTING");
        this.player.transform.position = new Vector3(1.0f, 5.0f, 5.0f);
	}

	/*
    * Waits until a tree config is loaded
    * */
	public void LoadScenario()
    {
		this.menuPanel.SetActive(false);
        Debug.Log("Loading Scenario");
        this.state = "StartGame";
	}

	/*
	* Waits for user input to start the game
	* */
	private void StartGame()
	{
		//Debug.Log ("In StartGame()");
		this.fadeToBlack.gameObject.SetActive(true);
		this.fadeToBlack.color = Color.black;
		this.fadeToBlackText.text = "Press SPACE to start";
		//Debug.Log ("waiting for space bar");
		if (Input.GetKeyUp(KeyCode.Space))
		{
			this.runTime = Time.time;
			Globals.gameStartTime = DateTime.Now;
			Debug.Log("Game started at " + Globals.gameStartTime.ToLongTimeString());
			this.movementRecorder.SetRun(this.runNumber);
			this.movementRecorder.SetFileSet(true);
			Color t = this.fadeToBlack.color;
			t.a = 0f;
			this.fadeToBlack.color = t;
			this.fadeToBlackText.text = "";
			this.fadeToBlack.gameObject.SetActive(false);

			this.firstFrameRun = true;
			this.debugControlScript.enabled = true;
			Globals.hasNotTurned = true;
			Globals.numCorrectTurns = 0;
			this.characterController.enabled = true;  // Bring back character movement
			this.state = "Running";

			Globals.InitLogFiles();
			Globals.trialStartTime.Add(DateTime.Now.TimeOfDay);
		}
	}

	/*
    * dry trees timeout state
    * */
	private void Timeout()
	{
		this.fadeToBlack.gameObject.SetActive(true);
		this.fadeToBlack.color = Color.black;

		if (!Globals.timeoutState)
		{
			StartCoroutine(Wait());
			Globals.timeoutState = true;
		}
	}

	IEnumerator Wait()
	{
		// Maria: This is where we change seconds
		yield return new WaitForSeconds(15);

		Color t = this.fadeToBlack.color;
		t.a = 0f;
		this.fadeToBlack.color = t;
		this.fadeToBlackText.text = "";
		this.fadeToBlack.gameObject.SetActive(false);

		this.timeoutState = false;
		this.state = "Running";
	}

	/*
     * Send sync UDP.
     * Get UDP msgs and move the player
     * Send UDP msgs out with (pos, rot, inTree)
     */
	private void Run()
	{
		// send SYNC msg on first frame of every run.
		if (this.firstFrameRun)
		{
			this.udpSender.SendRunSync();
			this.firstFrameRun = false;
		}

		if (Globals.playerInDryTree && !Globals.timeoutState)
		{
			this.state = "Timeout";
		}

		MovePlayer();
		//Debug.Log ("move complete");
		if (this.udpSender.CheckReward())
			this.movementRecorder.logReward(false, true);
		//this.movementRecorder.logReward(this.udpSender.CheckReward());
		//this.movementRecorder.logReward(true);
		this.numberOfTrialsText.text = "Current trial: # " + Globals.numberOfTrials.ToString();
		this.rewardAmountText.text = "Reward amount so far: " + Math.Round(Globals.rewardAmountSoFar).ToString();
		/*
        //this.numberOfEarnedRewardsText.text = "Number of earned rewards: " + Globals.numberOfEarnedRewards.ToString();
        //this.numberOfUnearnedRewardsText.text = "Number of unearned rewards: " + Globals.numberOfUnearnedRewards.ToString();
        this.numberOfDryTreesText.text = "Number of dry trees entered: " + Globals.numberOfDryTrees.ToString();
        if (Globals.numberOfEarnedRewards > 0) {
            this.numberOfCorrectTurnsText.text = "Correct turns: " + 
                Globals.numCorrectTurns.ToString() 
                + " (" + 
                Mathf.Round(((float)Globals.numCorrectTurns / ((float)Globals.numberOfTrials-1)) * 100).ToString() + "%)";
            this.last21AccuracyText.text = "Last 10 accuracy: " + GetLastAccuracy(10) + "%";
        }
        //this.frameCounter++;
        //Debug.Log ("screen updated");
        */

		TimeSpan te = DateTime.Now.Subtract(Globals.gameStartTime);
		this.timeElapsedText.text = "Time elapsed: " + string.Format("{0:D3}:{1:D2}", te.Hours * 60 + te.Minutes, te.Seconds);
		if (Time.time - this.runTime >= this.runDuration)
		{
			// fadetoblack + respawn
			this.movementRecorder.SetFileSet(false);
			this.fadeToBlack.gameObject.SetActive(true);
			this.state = "Fading";
		}
	}

	/*
  * Fade to Black
  * */
	private void Fade()
	{
		Color t = this.fadeToBlack.color;
		t.a += Time.deltaTime;
		this.fadeToBlack.color = t;

		if (this.fadeToBlack.color.a >= .95f)
		{
			this.state = "Reset";
		}
	}

	public void Pause()
	{
		this.rewardAmountText.text = "Reward amount so far: " + Math.Round(Globals.rewardAmountSoFar).ToString();
		//this.numberOfEarnedRewardsText.text = "Number of earned rewards: " + Globals.numberOfEarnedRewards.ToString();
		//this.numberOfUnearnedRewardsText.text = "Number of unearned rewards: " + Globals.numberOfUnearnedRewards.ToString();
		if (Globals.numberOfTrials > 1)
		{
			this.numberOfCorrectTurnsText.text = "Correct turns: " +
				Globals.numCorrectTurns.ToString()
				+ " (" +
				Mathf.Round(((float)Globals.numCorrectTurns / ((float)Globals.numberOfTrials - 1)) * 100).ToString() + "%"
				+ Globals.GetTreeAccuracy() + ")";
			this.lastAccuracyText.text = "Last 20 accuracy: " + Math.Round(Globals.GetLastAccuracy(20) * 100) + "%";
		}

		// NB Hack to get screen to go black before pausing for trialDelay

		if (waitedOneFrame)
		{
            Debug.Log("Sleeping");
			System.Threading.Thread.Sleep(TrialDelay * 1000);
			waitedOneFrame = false;

			float totalEarnedRewardSize = 0;
			float totalRewardSize = 0;
			for (int i = 0; i < Globals.sizeOfRewardGiven.Count; i++)
			{
				totalEarnedRewardSize += (float)System.Convert.ToDouble(Globals.sizeOfRewardGiven[i]);
			}
			//          if (Globals.numberOfEarnedRewards + Globals.numberOfUnearnedRewards >= this.numberOfAllRewards)
			// End game if mouse has gotten more than 1 ml - and send me a message to retrieve the mouse?
			// totalRewardSize = totalEarnedRewardSize + Globals.numberOfUnearnedRewards * Globals.rewardSize;
			Debug.Log("Total reward so far: " + totalRewardSize + "; maxReward = " + Globals.totalRewardSize);
			if (totalRewardSize >= 12)
				this.state = "GameOver";
			else
				this.state = "Respawn";
			// Append to stats file here
			/* NB: removed as we want the mouse to run for a certain number of rewards, not trials?
            if (this.runNumber > this.numberOfRuns)
                this.state = "GameOver";
            else
                this.state = "Respawn";
                */
		}
		else
		{
			waitedOneFrame = true;
		}
	}

	/*
     * Reset all trees
     * */
	public void ResetScenario(Color c)
	{
        this.BuildMaze();

		this.runTime = Time.time;
		this.runNumber++;

		//print(System.DateTime.Now.Second + ":" + System.DateTime.Now.Millisecond);
		this.debugControlScript.enabled = false;

		// NB edit (1 line)
		this.fadeToBlack.gameObject.SetActive(true);
		this.fadeToBlack.color = c;
		this.state = "Paused";

		// Move the player now, as the screen goes to black and the app detects collisions between the new tree and the player 
		// if the player is not moved.
		TeleportToBeginning();
	}

	private void GameOver()
	{
		//Debug.Log ("In GameOver()");
		this.fadeToBlack.gameObject.SetActive(true);
		this.fadeToBlack.color = Color.black;
		this.fadeToBlackText.text = "GAME OVER MUSCULUS!";
		if (Input.GetKeyUp(KeyCode.Escape))
		{
			StartCoroutine(CheckForQ());
		}
	}

	private IEnumerator CheckForQ()
	{
		Debug.Log("Waiting for Q");
		yield return new WaitUntil(() => Input.GetKeyUp(KeyCode.Q));
		Debug.Log("quitting!");
		Globals.WriteStatsFile();
		this.udpSender.close();
		Application.Quit();
	}

	private void MovePlayer()
	{

		if (Globals.newData)
		{
			Globals.newData = false;

			// Keep a buffer of the last 5 movement deltas to smoothen some movement
			if (this.last5Mouse2Y.Count == smoothingWindow)
				this.last5Mouse2Y.Dequeue();
			if (this.last5Mouse1Y.Count == smoothingWindow)
				this.last5Mouse1Y.Dequeue();

			if (Globals.gameTurnControl.Equals("roll"))
				this.last5Mouse1Y.Enqueue(-Globals.sphereInput.mouse1Y);
			else
				this.last5Mouse1Y.Enqueue(Globals.sphereInput.mouse1X);

			this.last5Mouse2Y.Enqueue(Globals.sphereInput.mouse2Y);

			// transform sphere data into unity movement
			//if (this.frameCounter - this.previousFrameCounter > 1)
			//print("lost packets: " + this.frameCounter + "/" + this.previousFrameCounter);
			this.previousFrameCounter = this.frameCounter;

			this.player.transform.Rotate(Vector3.up, Mathf.Rad2Deg * (this.last5Mouse1Y.Average()) / this.rawRotationDivider);

			Vector3 rel = this.player.transform.forward * (this.last5Mouse2Y.Average() / this.rawSpeedDivider);
			//this.player.transform.position = this.player.transform.position + rel;
			this.characterController.Move(rel);
			this.udpSender.SendMousePos(this.player.transform.position);
			this.udpSender.SendMouseRot(this.player.transform.rotation.eulerAngles.y);

			//Debug.Log (this.last5Mouse2Y.Average ());
			//Debug.Log (Time.time * 1000);

			// Send UDP msg out
			//this.udpSender.SendPlayerState(this.player.transform.position, this.player.transform.rotation.eulerAngles.y, Globals.playerInWaterTree, Globals.playerInDryTree);
		}
		else
		{
			//Debug.Log ("no new data");
		}
	}

	public void FlushWater()
	{
		this.udpSender.FlushWater();
	}

    public void Respawn(Color c)
    {
		this.runTime = Time.time;
		this.runNumber++;

		this.fadeToBlack.gameObject.SetActive(true);
		this.fadeToBlack.color = c;
		// this.state = "Paused";

        this.debugControlScript.enabled = false;

		// Move the player now, as the screen goes to black and the app detects collisions between the new tree and the player 
		// if the player is not moved.
		TeleportToBeginning();

		this.runTime = Time.time;
		this.movementRecorder.SetRun(this.runNumber);
		this.movementRecorder.SetFileSet(true);
		Color t = this.fadeToBlack.color;
		t.a = 0f;
		this.fadeToBlack.color = t;
		this.fadeToBlackText.text = "";
		this.fadeToBlack.gameObject.SetActive(false);

		this.firstFrameRun = true;
		this.debugControlScript.enabled = true;

		Globals.hasNotTurned = true;

		Globals.trialStartTime.Add(DateTime.Now.TimeOfDay);

        this.state = "Running";
    }

	public void GiveReward(int rewardDur, bool addToTurns)
	{
		GameObject.Find("UDPSender").GetComponent<UDPSend>().SendWaterReward(rewardDur);
		player.GetComponent<AudioSource>().Play();
		Globals.numberOfEarnedRewards++;
		Globals.sizeOfRewardGiven.Add(Globals.rewardSize / Globals.rewardDur * rewardDur);
		Globals.rewardAmountSoFar += Globals.rewardSize / Globals.rewardDur * rewardDur;

		Globals.hasNotTurned = false;
        ResetScenario(Color.black);
		/*
		if (addToTurns)
		{
			Globals.numCorrectTurns++;
			Globals.firstTurn.Add(this.gameObject.transform.position.x);
			Globals.firstTurnHFreq.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderHFreq());
			Globals.firstTurnVFreq.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderVFreq());
		}


		if (respawn)
		{
			Globals.numberOfTrials++;
			Globals.trialDelay = correctTurnDelay;
			GameObject.Find("GameControl").GetComponent<GameControlScript>().ResetScenario(Color.black);
			Globals.trialEndTime.Add(DateTime.Now.TimeOfDay);
			Globals.WriteToLogFiles();
		}
		*/
	}

	public void WitholdReward()
	{
        ResetScenario(Color.white);
        /*
		Globals.hasNotTurned = false;
		Globals.firstTurn.Add(this.gameObject.transform.position.x);
		Globals.firstTurnHFreq.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderHFreq());
		Globals.firstTurnVFreq.Add(this.gameObject.GetComponent<WaterTreeScript>().GetShaderVFreq());
		Globals.sizeOfRewardGiven.Add(0);
		if (respawn)
		{
			Globals.numberOfTrials++;
			Globals.trialDelay = incorrectTurnDelay;
			GameObject.Find("GameControl").GetComponent<GameControlScript>().ResetScenario(Color.white);
			Globals.trialEndTime.Add(DateTime.Now.TimeOfDay);
		}

		Globals.WriteToLogFiles();
		*/
	}


}
