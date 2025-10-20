using System;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using TMPro;


// This Script (a component of Game Manager) Initializes the Borad (i.e. screen).
public class BoardManager : MonoBehaviour 
{

	//Resoultion width and Height
	//CAUTION! Modifying this does not modify the Screen resolution. This is related to the unit grid on Unity.
	public static int resolutionWidth = 1600;//800;
	public static int resolutionHeight = 900;//600;

	// //Number of Columns and rows of the grid (the possible positions of the items).
	// public static int columns = 16;
	// public static int rows = 12;

	//The item radius. This is used to avoid superposition of items.
	//public static float KSItemRadius = 1.5f;

	//Timer width
	//public static float timerWidth =400;

	//66 to remove
	//The method to be used to place items randomly on the grid.
	//1. Choose random positions from full grid. It might happen that no placement is found and the trial will be skipped.
	//2. Choose randomly out of 10 positions. A placement is guaranteed
	//public static int randomPlacementType =1;

	//Prefab of the item interface configuration
	public static GameObject KSItemPrefab;

	//A canvas where all the board is going to be placed
	private GameObject canvas;
	//The possible positions of the items;
	private List<Vector3> gridPositions = new List<Vector3>();

	//Weights and value vectors for this trial. CURRENTLY SET UP TO ALLOW ONLY INTEGERS.
	//ws and vs must be of the same length
	private int[] ws;
	private int[] vs;

	//If randomization of buttons:
	//1: No/Yes 0: Yes/No
	public static int randomYes;//=Random.Range(0,2);

	private String question;

	//Should the key be working?
	public static bool keysON = false;

	//These variables shouldn't be modified. They just state that the area of the value part of the item and the weight part are assumed to be 1.
	private static float minAreaBill = 1f;
	private static float minAreaWeight = 1f;

	//The total area of all the items. Separated by the value part and the weighy part. A good initialization for this variables is the number of items plus 1.
	//public static int totalAreaComponent = 5;
	public static float totalAreaBill = 11.5f; // edit this to change the area of the bill
	public static int totalAreaWeight = 13; // edit this to change the area of the weight
	public static int item_size = 90; // edit this to change the default size of the items

	// The list of all the button clicks on items. Each event contains the following information:
	// ItemNumber (a number between 1 and the number of items. It corresponds to the index in the weight's (and value's) vector.)
	// Item is being selected In/Out (1/0) 
	// Time of the click with respect to the beginning of the trial 
	public List <Vector3> itemClicks =  new List<Vector3> ();

	//private GameObject start0 = GetComponent<Start>();
	private static GameObject start;
	private static GameObject screen_warning;
	private Button startButton;
	public static int starting_screen = 0;


	//Structure with the relevant parameters of an item.
	//gameItem: is the game object
	//coorValue1: The coordinates of one of the corners of the encompassing rectangle of the Value Part of the Item. The coordinates are taken relative to the center of the item.
	//coorValue2: The coordinates of the diagonally opposite corner of the Value Part of the Item.
	//coordWeight1 and coordWeight2: Same as before but for the weight part of the item.
	//botncitoW: button attached to the weight
	//botncitoV: button attached to the Value (Bill)
	//itemNumber: a number between 1 and the number of items. It corresponds to the index in the weight's (and value's) vector.
	private struct Item
	{
		public GameObject gameItem;
		public Vector2 coordValue1;
		public Vector2 coordValue2;
		public Vector2 coordWeight1;
		public Vector2 coordWeight2;
		public Vector2 center;
		public Button botoncitoW;
		public Button botoncitoV;
		public int itemNumber;
	}

	//The items for the scene are stored here.
	private static Item[] items;



	//This Initializes the GridPositions which are the possible places where the items will be placed.
	void InitialiseList()
	{
		gridPositions.Clear ();

		int radius = 300; // edit this to change how far apart the items are
		float width2height = 1.5f;
		for (int i = 0; i < ws.Length; i++)
		{
			// Generate a new item every this many radians...
			double radian_separation = (360f / ws.Length * Math.PI) / 180;
			gridPositions.Add(new Vector2((float)Math.Sin(radian_separation * i) * radius * width2height,
				(float)Math.Cos(radian_separation * i) * radius));
		}

		// totalAreaBill = totalAreaBill - (int)Math.Round(ws.Length * 0.15);
		// totalAreaWeight = totalAreaWeight - (int)Math.Round(ws.Length * 0.15);

	}

	//Call only for visualizing grid in the Canvas.
	void seeGrid(){
		GameObject hangerpref = (GameObject)Resources.Load ("Hanger");
		for (int ss=0;ss<gridPositions.Count;ss++){
			GameObject hanger = Instantiate (hangerpref, gridPositions[ss], Quaternion.identity) as GameObject;
			canvas=GameObject.Find("Canvas");
			hanger.transform.SetParent (canvas.GetComponent<Transform> (),false);
			hanger.transform.position = gridPositions[ss];
		}
	}

	//Initializes the instance for this trial:
	//1. Sets the question string 
	//2. The weight and value vectors are uploaded
	//3. The instance prefab is uploaded
	void setKSInstance(){
		
		int randInstance = GameManager.instanceRandomization[GameManager.globalTrial-1];

//		Text Quest = GameObject.Find("Question").GetComponent<Text>();
//		String question = "Can you obtain at least $" + GameManager.ksinstances[randInstance].profit + " with at most " + GameManager.ksinstances[randInstance].capacity +"kg?";
//		Quest.text = question;

		//question = "Can you pack $" + GameManager.ksinstances[randInstance].profit + " if your capacity is " + GameManager.ksinstances[randInstance].capacity +"kg?";
		//question = "$" + GameManager.ksinstances[randInstance].profit + System.Environment.NewLine + GameManager.ksinstances[randInstance].capacity +"kg?";
		question = " Max: " + System.Environment.NewLine + GameManager.ksinstances[randInstance].capacity +"kg ";

		ws = GameManager.ksinstances [randInstance].weights;
		vs = GameManager.ksinstances [randInstance].values;

		KSItemPrefab = (GameObject)Resources.Load ("KSItem");
		// take the KSItem prefab and set its vector3 scale
		KSItemPrefab.transform.localScale = new Vector3(item_size, item_size, item_size);
	}

	//Shows the question on the screen
	public void setQuestion(){
		Text Quest = GameObject.Find("Question").GetComponent<Text>();
		Quest.text = question;
    }


	/// <summary>
	/// Instantiates an Item and places it on the position from the input
	/// </summary>
	/// <returns>The item structure</returns>
	/// The item placing here is temporary; The real placing is done by the placeItem() method.
        Item generateItemOLD(int itemNumber, Vector2 randomPosition)
    {

        //Instantiates the item and places it.
        GameObject instance = Instantiate(KSItemPrefab, randomPosition, Quaternion.identity) as GameObject;

        canvas = GameObject.Find("Canvas");
        instance.transform.SetParent(canvas.GetComponent<Transform>(), false);

        //Setting the position in a separate line is importatant in order to set it according to global coordinates.
        instance.transform.position = randomPosition;

        //instance.GetComponentInChildren<Text>().text = ws[itemNumber]+ "Kg \n $" + vs[itemNumber];

        //Gets the subcomponents of the item 
        GameObject bill = instance.transform.Find("Bill").gameObject;
        GameObject weight = instance.transform.Find("Weight").gameObject;

        //Sets the Text of the items
        bill.GetComponentInChildren<Text>().text = "$" + vs[itemNumber];
        weight.GetComponentInChildren<Text>().text = "" + ws[itemNumber] + "kg";

        // This calculates area accrding to approach 1
        //		float areaItem1 = minAreaBill + (totalAreaBill - vs.Length * minAreaBill) * vs [itemNumber] / vs.Sum ();
        //		float scale1 = Convert.ToSingle (Math.Sqrt (areaItem1) - 1);
        //		bill.transform.localScale += new Vector3 (scale1, scale1, 0);
        //		float areaItem2 = minAreaWeight + (totalAreaWeight - ws.Length * minAreaWeight) * ws [itemNumber] / ws.Sum ();
        //		float scale2 = Convert.ToSingle (Math.Sqrt (areaItem2) - 1);
        //		weight.transform.localScale += new Vector3 (scale2, scale2, 0);

        // Calculates the area of the Value and Weight sections of the item accrding to approach 2 and then Scales the sections so they match the corresponding area.
        //Area Approach 2 calculation general idea:
        //The total area is constant. The area is divided among the items propotional to the ratio between the value (weight) and the sum of all the values (weights) of the items. 
        //Afterwards a constant area is substracted (or added) from all items in order to make the area of the minimum item equal to the minimum area defined, mantianing the total area constant.
        // Equations: 1. area_i = c + (totalArea-numberOfItems*c)*(value_i/sum(value_i)) 2. min(area_i)=minimumAreaDefined
		// comment out or edit the following lines to remove scaling and make all items the same size
        // float adjustmentBill = (minAreaBill - totalAreaBill * vs.Min() / vs.Sum()) / (1 - vs.Length * vs.Min() / vs.Sum());
        // float areaItem1 = adjustmentBill + (totalAreaBill - vs.Length * adjustmentBill) * vs[itemNumber] / vs.Sum();
        // float scale1 = Convert.ToSingle(Math.Sqrt(areaItem1) - 1);
		// bill.transform.localScale += new Vector3(scale1, scale1, 0);

		// float adjustmentWeight = (minAreaWeight - totalAreaWeight * ws.Min() / ws.Sum()) / (1 - ws.Length * ws.Min() / ws.Sum());
        // float areaItem2 = adjustmentWeight + (totalAreaWeight - ws.Length * adjustmentWeight) * ws[itemNumber] / ws.Sum();
        // float scale2 = Convert.ToSingle(Math.Sqrt(areaItem2) - 1);
		// weight.transform.localScale += new Vector3(scale2, scale2, 0);

		//Using the scaling results it calculates the coordinates (with respect to the center of the item) of the item.
		//		float weightH = weight.GetComponent<BoxCollider2D> ().size.y;
		//		float weightW = weight.GetComponent<BoxCollider2D> ().size.x;
		//		float valueH = bill.GetComponent<BoxCollider2D> ().size.y;
		//		float valueW = bill.GetComponent<BoxCollider2D> ().size.x;
		float weightH = weight.GetComponent<BoxCollider2D>().size.y * weight.transform.localScale.y;
        float weightW = weight.GetComponent<BoxCollider2D>().size.x * weight.transform.localScale.x;
        float valueH = bill.GetComponent<BoxCollider2D>().size.y * bill.transform.localScale.y;
        float valueW = bill.GetComponent<BoxCollider2D>().size.x * bill.transform.localScale.x;

        Item itemInstance = new Item();
        itemInstance.gameItem = instance;
        //		itemInstance.coordValue1=new Vector2(-valueW*(1+scale1)/2,0);
        //		itemInstance.coordValue2=new Vector2(valueW*(1+scale1)/2,valueH*(1+scale1));
        //		itemInstance.coordWeight1=new Vector2(-weightW*(1+scale2)/2,0);
        //		itemInstance.coordWeight2=new Vector2(weightW*(1+scale2)/2,-weightH*(1+scale2));

        itemInstance.coordValue1 = new Vector2(-valueW / 2, 0);
        itemInstance.coordValue2 = new Vector2(valueW / 2, valueH);
        itemInstance.coordWeight1 = new Vector2(-weightW / 2, 0);
        itemInstance.coordWeight2 = new Vector2(weightW / 2, -weightH);

        itemInstance.botoncitoV = bill.GetComponent<Button>();
        itemInstance.botoncitoW = weight.GetComponent<Button>();

        //Goes from 1 to numberOfItems
        itemInstance.itemNumber = itemNumber + 1;

        return (itemInstance);

    }

	/// <summary>
	/// Places the item on the input position and assigns the button press listener to the item.
	/// </summary>
	void placeItem(Item itemToLocate, Vector2 position){
		//Setting the position in a separate line is importatant in order to set it according to global coordinates.
		itemToLocate.gameItem.transform.localPosition = position;

		//itemToLocate.botoncito.onClick.AddListener(setElementSelectionButtons);
		itemToLocate.botoncitoV.onClick.AddListener(delegate{setElementSelectionButtons(itemToLocate);});
		itemToLocate.botoncitoW.onClick.AddListener(delegate{setElementSelectionButtons(itemToLocate);});

	}

	//Returns a random position from the grid and removes the item from the list.
    Vector2 RandomPosition()
    {
        // int randomIndex = Random.Range(0, gridPositions.Count);
        int randomIndex = 0;
        Vector2 randomPosition = gridPositions[randomIndex];
        gridPositions.RemoveAt(randomIndex);
        return randomPosition;
    }

	// Places all the objects from the instance (ws,vs) on the canvas. 
	// Returns TRUE if all items where positioned, FALSE otherwise.
	private bool LayoutObjectAtRandom()
	{
		int objectCount = ws.Length;
		items = new Item[objectCount];

		// randomise the location of the items
		List<int> objectCountList = Enumerable.Range(0, objectCount).ToList();
		objectCountList = objectCountList.OrderBy(x => Random.value).ToList();

		foreach (int i in objectCountList)
		{
			bool objectPositioned = false;

			Item itemToLocate = generateItemOLD(i, new Vector2(-2000, -2000));
			while (!objectPositioned && gridPositions.Count > 0)
			{
				Vector2 randomPosition = RandomPosition();
				//Instantiates the item and places it.
				placeItem(itemToLocate, randomPosition);
				//itemToLocate.gameItem.transform.localPosition = randomPosition;
				//itemToLocate.center = new Vector2(itemToLocate.gameItem.transform.localPosition.x,
				//	itemToLocate.gameItem.transform.localPosition.y);

				items[i] = itemToLocate;
				objectPositioned = true;
			}

			if (!objectPositioned)
			{
				Debug.Log("Not enough space to place all items... " +
					"ran out of randomPositions");
				return false;
			}
		}
		return true;
	}

	/// Macro function that initializes the Board
	public void SetupScene(int sceneToSetup)
	{

		itemClicks.Clear(); // Fix for item Clicks to be correct TODO check
		setKSInstance();

		//If the bool returned by LayoutObjectAtRandom() is false, then retry again:
		//Destroy all items. Initialize list again and try to place them once more.
		int nt = 0;
		bool itemsPlaced = false;
		while (nt < 10 && !itemsPlaced)
		{
			GameObject[] items1 = GameObject.FindGameObjectsWithTag("Item");

			foreach (GameObject item in items1)
			{
				Destroy(item);
			}

			InitialiseList();
			seeGrid();
			itemsPlaced = LayoutObjectAtRandom();
			nt++;
		}
		keysON = true;

	}

	//Checks if positioning an item in the new position generates an overlap.
	//Returns: TRUE if there is an overlap. FALSE Otherwise.
	bool objectOverlapsQ(Vector3 pos, Item item)
	{
		Vector2 posxy = new Vector3 (pos.x, pos.y);
		bool overlapValue = Physics2D.OverlapArea (item.coordValue1+posxy, item.coordValue2+posxy);
		bool overlapWeight = Physics2D.OverlapArea (item.coordWeight1+posxy, item.coordWeight2+posxy);

		return overlapValue || overlapWeight;
	}

	//Updates the timer rectangle size accoriding to the remaining time.
	public void updateTimer(){
		// timer = GameObject.Find ("Timer").GetComponent<RectTransform> ();
		// timer.sizeDelta = new Vector2 (timerWidth * (GameManager.tiempo / GameManager.totalTime), timer.rect.height);
		Image timer = GameObject.Find ("Timer").GetComponent<Image> ();
		timer.fillAmount = GameManager.tiempo / GameManager.totalTime;
	}

	//Sets the triggers for pressing the corresponding keys
	private async Task setKeyInput()
	{
		if (GameManager.escena == 1)
		{
			await ChangeFromKpScene();
		}
	}

	private async Task ChangeFromKpScene()
	{
		if (Input.GetKeyDown (KeyCode.Return)) 
		{
			await GameManager.changeToNextScene (itemClicks, 1);
		} 
	}

	/// The action to be taken when a button is pressed: Toggles the light and adds the click to itemClicks
	/// <param name="itemToLocate"> item clicked </param>
	private void setElementSelectionButtons(Item itemToLocate){

		int itemN = itemToLocate.itemNumber;

		Light myLight = itemToLocate.gameItem.GetComponent<Light> ();
		myLight.enabled = !myLight.enabled;

		int itemIn=(myLight.enabled)? 1 : 0 ;

		itemClicks.Add (new Vector3 (itemN, itemIn , GameManager.timeTrial - GameManager.tiempo));
	}

	// Initializes the starting instructions, Start Button and the Input Field for the Participant ID
	public void setupInitialScreen(){

		//Button
		start = GameObject.Find("Start");
		start.SetActive (false);

		screen_warning = GameObject.Find("ScreenWarning");
		if(GameManager.logged_in_before)
		{
			screen_warning.gameObject.SetActive(false);
		}
		// screen_warning.gameObject.SetActive(false); // edit / comment out this for URL parameter. URL should end in ?id=>PID< (delete the <>)

		startButton = start.GetComponent<Button>();
		startButton.onClick.AddListener(delegate { onStartButton(); });

		var pID = GameObject.Find ("ParticipantID").GetComponent<InputField>();

		var se = new InputField.EndEditEvent();

        if (GameManager.systemOK)
		{
			se.AddListener(value => SubmitPid(value));//,start));
			pID.onEndEdit = se;
		} 

		if(GameManager.logged_in_before)
		{
			pID.gameObject.SetActive(false);
		}
	}

	// Submits the Prolific ID and activates the Start Button
	private static void SubmitPid(string inputParticipantID)//, GameObject start)
	{
		Debug.Log("KD: entered suvmitpid");
		if (starting_screen == 0)
		{
			screen_warning.gameObject.SetActive(true);
		}
		else
		{
			GameObject iinf = GameObject.Find("InputInfo");
			Text Tiinf = iinf.GetComponent<Text>();
			var pID = GameObject.Find("ParticipantID");
			pID.SetActive (false);

			//Set Participant ID
			if (!GameManager.SetParticipantId(inputParticipantID))
			{
				Tiinf.text = "Wrong Prolific ID, please try again:";
				pID.GetComponent<InputField>().Select();
				pID.GetComponent<InputField>().text = "";
				pID.SetActive(true);
				return;
			}
			GameManager.participantID = inputParticipantID;

			//Activate Start Button and listener
			start.SetActive (true);
			Tiinf.text = "Click the Start button to begin.";
			keysON = false;
		}
	}

	public static void change_start_text(string new_text)
    {
		GameObject iinf = GameObject.Find("InputInfo");
		Text Tiinf = iinf.GetComponent<Text>();
		Tiinf.text = new_text;
	}


	// Returns the coordinates of the items in the format (x1,y1)(x2,y2)...
	public static string getItemCoordinates(){
		var coordinates = "";
		foreach (var it in items)
		{
			coordinates = coordinates + "(" + it.center.x + "," + it.center.y + ")";
		}
		return coordinates;
	}

	private async void Update () {
		
		if (keysON) {
			await setKeyInput ();
		}
		
		// progress through the concise KP instructions on the starting screen
		if (GameManager.escena == 0 && starting_screen <= 2)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
				if (GameManager.skip_knapsack || starting_screen >= 2)
				{
					// proceed to start
					screen_warning.gameObject.SetActive(false);
					starting_screen = 2;
					if (GameManager.url_for_id)
					{
						onStartButton();
					}
				}
                else if (starting_screen == 0)
				{
					// display first instruction
					TMP_Text screen_warning_text = screen_warning.GetComponentInChildren<TMP_Text>();
					screen_warning_text.text = "This task shows how well you can make decisions with limited resources, like choosing how to manage your budget, investments, or time.\n\n" + 
					"Your task is to select the items that, taken together, maximise your value without exceeding the weight capacity. Click on items to select (or de-select) them.\n\n" +  
					"<b>Press the \"spacebar\" to continue.</b>";
					starting_screen++;
				}
				else if (starting_screen == 1)
				{
					// display second instruction
					TMP_Text screen_warning_text = screen_warning.GetComponentInChildren<TMP_Text>();
					screen_warning_text.text = "Please use clicking to show your thinking. The moment you think an item is good, click on it to select. The moment you change your mind, click on it again to de-select.\n\n" +  
					"Once finished with your selections, <b>press \"Enter\" to submit your answer.</b>\n\n" +
					"You can earn a bonus of up to $4 USD based on your performance in this task.\n\n" + 
					"<b>Press the \"spacebar\" to begin.</b>";
					starting_screen++;
				}
            }
		}
		// 	// Detect Ctrl+V or Cmd+V for pasting
        // else if (GameManager.escena == 0)
		// {
		// 	if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand)) && Input.GetKeyDown(KeyCode.V))
		// 	{
		// 		PasteFromClipboard();
		// 	}
		// }
	}

	// // Function to manually paste from the system clipboard
    // void PasteFromClipboard()
    // {
    //     string clipboardText = GUIUtility.systemCopyBuffer; // Get clipboard content
	// 	var pID = GameObject.Find ("ParticipantID").GetComponent<InputField>();
    //     pID.text += clipboardText; // Append clipboard content to input field's existing text
    // }

	private async void onStartButton()
    {
		// Debug.Log("You've clicked the button!");
		GameManager.setTimeStamp();

		await GameManager.changeToNextScene(new List<Vector3>(), 0);

		
	}

}