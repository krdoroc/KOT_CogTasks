
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Random = UnityEngine.Random;
using Dhive;
using JetBrains.Annotations;
using System.Runtime.InteropServices;

// from Cog Tasks
using UnityEditor;
using TMPro;
using UnityEngine.UI;

//using System.Diagnostics;
using UnityEngine.EventSystems;


public class GameManager : MonoBehaviour
{
    // game parameters
    // TODO: ensure these are set correctly
    public static bool let_participants_choose_tasks = true; // whether participants are allowed to choose which cognitive task to do next
    public static bool skip_knapsack = true; // whether to skip the knapsack task and proceed directly to the cognitive tasks
    public static bool url_for_id = false; // true if participantid is passed in the URL, false if they need to manually input it when in-game
    public static bool ban_repeat_logins = false; // true if participants are not allowed to login more than once
    public static bool save_session_data = false; // whether to save the session data to the server


    public static GameManager instance = null;  //Game Manager: It is a singleton (i.e. it is always one and the same it is nor destroyed nor duplicated)
    private BoardManager boardScript; //The reference to the script managing the board (interface/canvas).
    private static bool on_load = true; // flag for whether to do something only when the game first loads
    public static float screen_width;
    public static float screen_height;
    public static bool logged_in_before = false; // flag for whether the participant has attempted to login before
    public static TMP_FontAsset my_font;
    public static bool abort_due_to_disconnection = false;  // flag for whether to stop the game and display a message that the participant's internet has been disconnected for too long
    public static int max_reconnection_attempts = 13;  // maximum number of consecutive internet reconnection attempts following identification of disconnection
    public static int escena; //Current Scene
    public static float tiempo; //Time spent so far on this scene 
    public static float totalTime;  //Total time for these scene
    
    // knapsack related variables
    public static int trial = 0;  //Current knapsack trial initialization
    public static int block = 0;  //Current knapsack block initialization
    public static int globalTrial = 0;  //Total knapsack trial (As if no blocks were used)
    private static bool showTimer;
    public static string confidence_button_text; // participants asked for confidence after each knapsack trial
    public static string items_selected; // which items they've selected
    public static int solution_submitted; // whether a solution has been submitted
    public static List<Vector3> item_clicks; // which items they've selected
    private static bool allowPausing = false;
    //Minimum and maximum for randomized interperiod Time 
    public static float timeRest1min = 5;
    public static float timeRest1max = 9;
    public static float timeRest2 = 10; //InterBlock rest time
    //public static float timeRest1;
    public static float timeTrial = 10; //Time given for each trial (The total time the items are shown -With and without the question-)
    public static float timeOnlyItems = 1; //Time for seeing the KS items without the question

    public static int numberOfTrials; //Total number of trials in each block
    public static int numberOfBlocks;  //Total number of blocks
    public static int numberOfInstances;  //Number of instance file to be considered. From i1.txt to i_.txt..
    public static int randomizationID;  // stores key to the randomised trial sequence
    public static int[] instanceRandomization; //The order of the instances to be presented
    //This is the string that will be used as the file name where the data is stored. DeCurrently the date-time is used.
    public static string participantID = "Empty";
    public static string dateID = @System.DateTime.Now.ToString("dd MMMM, yyyy, HH-mm");
    private static string identifierName;
    private static int questionOn;  //Is the question shown on scene scene 1?
    private static System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();  // Stopwatch to calculate time of events.
    private static string initialTimeStamp; // Time at which the stopwatch started. Time of each event is calculated according to this moment.


    // D-Hive variables
    public static readonly string ExperimentId = "2ac47f83-a59e-4357-965a-70116abe0d23"; //pablo's experiment ID "99630040-71cd-4d89-acd8-3621829b447d";  // 
    public static string participantTrialId;
    public static Experiment experimentData;
    private DhiveReader _reader;
    public static DhiveSender sender;
    public static string TaskId;
    private static string welcomeTaskId;
    public static string currentTrialTask;
    [CanBeNull] private string _clicksTrialTask;
    [CanBeNull] private string _timestampTrialTask;
    [CanBeNull] private string _instancesTrialTask;
    private static int timestampIndex;
    private static int clicksIndex;
    private bool completed;
    private static bool IsIOS;
    private static bool IsAndroid;
    public static bool systemOK;
    //private static string OperatingSystem;
    private static bool kp_completed = false;


    // cognitive task related variables
    public static int repeat_nbacks = 0; // count of how often the recall-1-back instructions & practice have been repeated
    public static int total_correct = 0; // to track scores displayed to participant at the end
    public static int total_instances = 0; // to track scores displayed to participant at the end
    private static string task_name = null;
    private static List<string> tasks = new List<string> { "SymbolDigit", "StopSignal", "NBack", "TaskSwitching", "ICAR" };
    public static List<string> completed_tasks = new List<string> { }; // list of completed cognitive tasks
    private static List<string> scores = new List<string> { }; // list of scores to display at the end
    public TMP_Text Title;
    public TMP_Text Body;
    public Button NextButton;
    public Button RestartButton;
    public TMP_Text Message;
    public Image Image;
    public static bool questionnaires_completed = false;
    public static int current_task_number = 1; // stores the sequence of the current task, initialised to 1 as KP is always first
    // Quiz and instruction related
    public static float instructions_start_time;
    public static bool practice = true;
    private static bool complete_quiz = false; // whether the comprehension quiz in the task instructions has been completed
    private List<string> instructions = new List<string> { };
    private Dictionary<string, int> options_dict = new Dictionary<string, int>();
    private static int no_quiz; // number of screens in the comprehension quiz
    private List<Toggle> temp_toggles;
    // cognitive task specific (leters and numbers)
    private static string eg_text = null;
    public static string vowel_key = null;
    public static string consonant_key = null;
    public static string odd_key = null;
    public static string even_key = null;  
    public static bool letter_on_left = false;  // whether letters are the left or right stimulus
    public static int congruence_type; // int between 0 and 3. congruence_type 0 = vowel response is left, odd response is left; and congruence_type 1 = vowel response is right, odd response is right. congruence_type 2 = vowel response is left, odd response is right; and congruence_type 3 = vowel response is right, odd response is left

    //A structure that contains the parameters of each instance
    public struct KSInstance
    {
        public int capacity;

        public int[] weights;
        public int[] values;

        public string id;
        public string type;
        public float expAccuracy;

        public int profitOpt;
        public int capacityOpt;
        public int[] itemsOpt;
    }

    //An array of all the instances to be uploaded form .txt files.
    public static KSInstance[] ksinstances;// = new KSInstance[numberOfInstances];

    // Use this for initialization
    async void Awake()
    {

        if (url_for_id)
        {
            // Get the full URL (including query parameters)
            string url = Application.absoluteURL;

            // Parse the query string to extract the participant ID
            participantID = GetParameterFromUrl(url, "id");  
        }

        if (!string.IsNullOrEmpty(participantID))
        {
            Debug.Log("Participant ID: " + participantID);
        }
        else
        {
            Debug.Log("No participant ID found in the URL");
        }

        screen_width = Screen.width;
        screen_height = Screen.height;

        var os = new OperatingSystem();

        IsAndroid = os.IsAndroid();
        IsIOS = os.IsIos();

        //Makes the Gama manager a Singleton
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        //Initializes the game		
        boardScript = instance.GetComponent<BoardManager>();
        my_font = Resources.Load<TMP_FontAsset>("Muc-Zeit-Medium SDF");

        // checks the participant is on a desktop and has not already tried to access the game
        if (SceneManager.GetActiveScene().name.Equals("Setup"))
        {
            if (!IsIOS && !IsAndroid)
            {
                // Debug.Log("Participant in a desktop.");
                BoardManager.change_start_text("Welcome");
                if (logged_in_before)
                {
                    BoardManager.change_start_text("The Prolific ID you entered (" + participantID + ") has already been used. Please contact the researcher.");
                }
                systemOK = true;
            }
            else
            {
                BoardManager.change_start_text("Please use a desktop instead");
                systemOK = false;
            }
        }


        await InitGame();
    }

    // This method extracts a query parameter from the URL by name
    string GetParameterFromUrl(string url, string parameterName)
    {
        Uri uri = new Uri(url);
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return queryParams[parameterName];
    }

    //Initializes the knapsack scenes
    async Task InitGame()
    {

        /*
		Scene Order: escena
		0=setup
		1=trial game
		2=trial game answer (doesn't apply to optimisation variant of KP)
		3= intertrial rest
		4= interblock rest
		All other escena's are called by name, not number
		*/

        // randomise the sequence of cognitive tasks on intialisation
        if (on_load == true)
        {
            tasks = tasks.OrderBy(x => Random.value).ToList();
            on_load = false;
        }

        Scene scene = SceneManager.GetActiveScene();
        escena = scene.buildIndex - 1;
        Debug.Log("escena" + escena);
        
        // scene 0: show concise knapsack instructions 
        if (escena == 0)
        {
            block = 1;
            boardScript.setupInitialScreen();

        }
        
        // scene 1: show KP trial (or if skipping KP, go to landing page for cognitive tasks)
        else if (escena == 1)
        {
            if (skip_knapsack)
            {
                SceneManager.LoadScene ("CogTaskHome");  
                kp_completed = true;
            }
            else
            {
                timeTrial = 90f;
                trial++;

                globalTrial = trial + (block - 1) * numberOfTrials;
                showTimer = true;
                questionOn = 0;
                boardScript.SetupScene(1);

                tiempo = timeOnlyItems;
                totalTime = timeOnlyItems;
                confidence_button_text = null;

                // D-Hive: store task id
                TaskId = DataLoader.GetDatabaseTaskId(experimentData, "Knapsack optimisation task");
            }

        }

        // scene 3: intertrial rest
        else if (escena == 3)
        {
            showTimer = false;
            tiempo = Random.Range(timeRest1min, timeRest1max);
            totalTime = tiempo;
        }

        // scene 4: interblock rest
        else if (escena == 4)
        {
            trial = 0;
            block++;
            showTimer = true;
            tiempo = timeRest2;
            totalTime = tiempo;
        }
       
        // if at the end of the whole game, display final message
        else if (SceneManager.GetActiveScene().name == "End")
        {
            GameObject textObject = GameObject.Find("Text");
            TMP_Text textComponent = textObject.GetComponent<TMP_Text>();

            Debug.Log("I am at the end");

            string completionCode;
            // set completion code depending on which experiment is running
            if (DataLoader.var_name_prefix == "i_ageing_")
            {
                Debug.Log("end Ageing code");
                completionCode = "CGN0Y30C";
            }
            else
            {
                Debug.Log("end stress code");
                completionCode = "C1CT7VRN";
            }

            // end message for those with internet disconnections
            if(abort_due_to_disconnection)
            {
                textComponent.text =
                   "Thank you for your participation. Unfortunately, the study has ended early as your internet has been disconnected for over 5 consecutive minutes.\n\n" +
                   "The consent form made clear that a stable and continuous internet connection was required.\n\n" +
                   "Please go to Prolific and return the submission due to technical difficulties.";

                ParticipantNotCompleted();
            }
            // end message for those that successfully completed
            else
            {
                textComponent.text =
                   "Thank you so much for your participation, you have reached the end of the experiment!\n\n" +
                   "Please go to Prolific and enter the completion code below to verify your completion.\n\n" +
                   "COMPLETION CODE (case sensitive): " + completionCode;

                ParticipantCompleted();
            }
        }

        // confidence scene: displayed after each KP trial
        if (SceneManager.GetActiveScene().name == "Confidence")
        {
            // stop the first confidence button from being highlighted when the scene loads
            StartCoroutine(DeselectButtonAfterFrame());
        }

        // landing page for cognitive tasks
        if (SceneManager.GetActiveScene().name == "CogTaskHome")
        {
            // if the KP has just been completed, store the participant's score and mark it as complete
            if (kp_completed == false)
            {
                string score = null;
                double temp = (double)total_correct / (double)numberOfInstances;
                score = temp.ToString("P0");
                scores.Add(score);

                kp_completed = true;
            }
            
            // if there are no remaining cognitive tasks to complete, proceed to the questionnaires
            if (tasks.Count == 0)
            {
                // if the questionnaires are also finished, display the final scores before dusplaying the ened screen
                if (questionnaires_completed)
                {
                    Title.text = "Cognitive Tasks\nYour Accuracy";
                    Body = GameObject.Find("Body").GetComponent<TMP_Text>();
                    if (skip_knapsack)
                    {
                        for (int i = 0; i < completed_tasks.Count; i++)
                        {
                            Body.text = Body.text + "\n" + completed_tasks[i] + ": " + scores[i]; //non-KP
                            GameObject.Find(completed_tasks[i]).GetComponent<Button>().gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        Body.text = "Knapsack: " + scores[0]; //KP
                        for (int i = 0; i < completed_tasks.Count; i++)
                        {
                            Body.text = Body.text + "\n" + completed_tasks[i] + ": " + scores[i + 1]; 
                            GameObject.Find(completed_tasks[i]).GetComponent<Button>().gameObject.SetActive(false);
                        }
                    }
                    Body.text = Body.text + "\n\n" + "Your responses are being saved. Your completion code will appear shortly, in the meantime please do not leave this page.";
                    StartCoroutine(ProceedToEnd()); // cog tasks used to call QuitOnDelay() here 
                }
                // if the questionnaires aren't finished, proceed to the questionnaires
                else
                {
                    TaskId = DataLoader.GetDatabaseTaskId(experimentData, "Ageing_questionnaires");
                    SceneManager.LoadScene("Questionnaires");
                }
            }

            // if there are still cognitive tasks left to complete, proceed to one of those
            else
            {
                complete_quiz = false;
                
                // if they can choose which task to do, disable the option to repeat a previous task
                if (let_participants_choose_tasks)
                {
                    Title.text = "Cognitive Tasks";
                    if (completed_tasks.Count > 0)
                    {
                        for (int i = 0; i < completed_tasks.Count; i++)
                        {
                            GameObject.Find(completed_tasks[i]).GetComponent<Button>().interactable = false;
                        }
                    }
                }

                // otherwise load the next task for them
                else
                {
                    task_name = tasks[0];
                    if (task_name == "ICAR")
                    {
                        practice = false;
                        TaskId = DataLoader.GetDatabaseTaskId(experimentData, "ICAR");
                        // Debug.Log("Task ID: " + TaskId);
                        SceneManager.LoadScene("ICAR");
                    }
                    else
                    {
                        practice = true;
                        instructions_start_time = Time.time;
                        // Debug.Log("start time is " + instructions_start_time);
                        SceneManager.LoadScene("Instructions");
                    }
                }
            }
        }

        if (SceneManager.GetActiveScene().name == "Instructions")
        {
            RestartButton.gameObject.SetActive(false);
            
            // if they have completed the practice questions, warn them they're about to start the real thing
            if (practice == false)
            {
                Title.text = "";
                Body.fontSize = 50;
                Body.text = "<b>You're about to start the real thing.</b>";
                Body.alignment = TextAlignmentOptions.Midline;
                NextButton.gameObject.SetActive(false);

                StartCoroutine(StartTask(task_name));
            }

            // otherwise, display the instructions, comprehension quiz, and practice Qs
            else
            {
                Title.text = "Instructions: Task " + (completed_tasks.Count + 2).ToString() + " of 6";

                if (task_name == null) // test case
                {
                    task_name = "StopSignal";
                }

                AddInstructions(task_name);

                if (complete_quiz == true)
                {
                    RestartButton.gameObject.SetActive(true);
                    Body.text = instructions[^1];
                    NextButton.gameObject.SetActive(false);
                }
                else
                {
                    AddQuiz();
                }
            }

        }

        // display quiz
        if (SceneManager.GetActiveScene().name == "Quiz")
        {
            if (task_name == null) // test case
            {
                task_name = "StopSignal";
                no_quiz = 3;
            }
            GenerateOptions();
        }

        if (SceneManager.GetActiveScene().name == "FinishedCogTask")
        {
            // if they just finished practice, give the option to proceed to the main task unless they failed the minimum accuracy hurdle for the recall-1-back - in which case they must repeat the instructions
            if (practice == true)
            {
                Title.text = "End of Practice";
                NextButton.GetComponentInChildren<TMP_Text>().text = "Start the task";
                if (task_name == "NBack")
                {
                    RestartButton.gameObject.SetActive(true);
                    if (total_correct <= Mathf.RoundToInt(total_instances / 4f) & repeat_nbacks < 2)
                    {
                        Title.text = Title.text + ": you got less than 50% correct and need to repeat";
                        NextButton.gameObject.SetActive(false);
                        repeat_nbacks++;
                    }
                }
                else
                {
                    RestartButton.gameObject.SetActive(false);
                }
            }

            // if they just finished a task, save the scores and invite them to move to the next task
            else
            {
                current_task_number++;
                Title.text = "End of Task";
                RestartButton.gameObject.SetActive(false);

                int temp_index = tasks.IndexOf(task_name);
                tasks.RemoveAt(temp_index);

                if (tasks.Count == 0)
                {
                    NextButton.GetComponentInChildren<TMP_Text>().text = "Proceed to questionnaires";
                }
                else
                {
                    NextButton.GetComponentInChildren<TMP_Text>().text = "Do another task";
                }

                completed_tasks.Add(task_name);

                string score = null;
                if (task_name != "SymbolDigit")
                {
                    double temp = (double)total_correct / (double)total_instances;
                    score = temp.ToString("P0");
                }
                else
                {
                    string temp2 = (total_correct / 90f).ToString("F2");
                    score = temp2 + " correct responses p/sec";
                }
                scores.Add(score);
            }
        }
    }

    // save how long they spent on the instructions and add it to the DHive save queue
    void SaveInstructionsTime(string task_name, float instructions_time)
    {
        var outputs = new List<OutputParameter>
        {
            new ($"o_{task_name}_instruction_time", instructions_time.ToString()),
        };
        DataSaver.AddTrialDataToSave(outputs);
    }

    // prevent the first confidence button from being highlighted when the scene loads
    IEnumerator DeselectButtonAfterFrame()
    {
        yield return null;  // Wait for the end of the first frame
        EventSystem.current.SetSelectedGameObject(null);        // Ensure nothing is selected
    }

    // add the whole KP trial's data, including the confidence level, the DHive save queue and then proceed to the next trial
    public void ConfidenceSelection(Button clickedButton)
    {
        Text buttonText = clickedButton.GetComponentInChildren<Text>();

        if (buttonText != null)
        {
            confidence_button_text = buttonText.text;
        }

        PrepareOutput(items_selected, solution_submitted, timeTrial - tiempo, "", item_clicks);
        SceneManager.LoadScene("InterTrialRest");
    }

    // Update is called once per frame
    async void Update()
    {
        // allow pausing on the KP
        if (escena > 0 && escena < 5)
        {
            await startTimer();
            pauseManager();
        }

        // if internet disconnection detected, try to reconnect. if reconnection fails after 5min, end the game
        if (!sender.IsConnected)
        {
	        Debug.Log("Sender isn't connected, reconnecting");
	        if (sender._retriesDone < max_reconnection_attempts)
	        {
				await sender.Reconnect(max_reconnection_attempts);
	        }
            
            if (!sender.IsConnected && sender._retriesDone >= max_reconnection_attempts)
            {
                abort_due_to_disconnection = true;
                SceneManager.LoadScene("End");
            }
        }

        // if they're on the instructions page, move to the next slide when they press spacebar
        if (SceneManager.GetActiveScene().name == "Instructions" && complete_quiz == true)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                float instructions_end_time = Time.time;
                float instructions_time = instructions_end_time - instructions_start_time;
                SaveInstructionsTime(task_name, instructions_time);
                RestartButton.gameObject.SetActive(false);
                StartCoroutine(StartTask(task_name));
            }
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        sender?.DispatchMessageQueue();
#endif

    }

    //Pauses/Unpauses the game via alt+p. Unpausing takes you directly to next trial
    //Warning! When Unpausing the following happens:
    //If paused/unpaused in scene1 (while items are shown -trial-) then saves the trialInfo with an error: "pause" without information on the items selected.
    //If paused/unpaused on ITI or IBI then it generates a new row in trial Info with an error ("pause"). i.e. there are now 2 rows for the trial.
    private void pauseManager()
    {
        if (allowPausing)
        {
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.P))
            {
                Time.timeScale = (Time.timeScale == 1) ? 0 : 1;
                if (Time.timeScale == 1)
                {
                    errorInScene("Pause");
                }
            }
        }
    }

    //Takes care of changing between scenes during the knapsack task
    public static async Task changeToNextScene(List<Vector3> itemClicks, int submitted)
    {
        BoardManager.keysON = false;
        // if they're leaving the setup scene, load the KP
        if (escena == 0)
        {
            SceneManager.LoadScene("LoadTrialData");
        }
        // if they just finished a trial, store everything we want to save as a global variable and then load the confidence scene
        else if (escena == 1)
        {
            items_selected = extractItemsSelected(itemClicks);
            solution_submitted = submitted;
            item_clicks = itemClicks;
            SceneManager.LoadScene("Confidence");
        }
        // if leaving a rest, load the next trial
        else if (escena == 3)
        {
            changeToNextTrial();
        }
        else if (escena == 4)
        {
            SceneManager.LoadScene("trialGame");
        }

    }

    //Redirects to the next scene depending if the trials or blocks are over.
    private static void changeToNextTrial()
    {
        if (trial < numberOfTrials) //if block is not over, load next trial
        {
            SceneManager.LoadScene("trialGame");
        }
        else if (block < numberOfBlocks)  //if blocks are not over, load interblock rest
        {
            SceneManager.LoadScene("InterBlockRest");
        }
        else //if all trials are done, move onto the cognitive tasks
        {
            current_task_number++;
            SceneManager.LoadScene("CogTaskHome");
        }
    }

    /// In case of an error (e.g. Not enough space to place all items): Skip trial and go to next one.
    public static async void errorInScene(string errorDetails)
    {
        Debug.Log(errorDetails);

        BoardManager.keysON = false;
        PrepareOutput("", 2, timeTrial, errorDetails, new List<Vector3>());
        changeToNextTrial();
    }


    /// Starts the stopwatch. Time of each event is calculated according to this moment.
    public static void setTimeStamp()
    {
        initialTimeStamp = @System.DateTime.Now.ToString("HH-mm-ss-fff");
        stopWatch.Start();
    }

    /// Calculates time elapsed 
    public static string timeStamp()
    {
        long milliSec = stopWatch.ElapsedMilliseconds;
        string stamp = milliSec.ToString();
        return stamp;
    }


    /// Extracts the items that were finally selected based on the sequence of clicks.
    /// <returns>The items selected.</returns>
    /// <param name="itemClicks"> Sequence of clicks on the items.</param>
    private static string extractItemsSelected(List<Vector3> itemClicks)
    {
        List<int> itemsIn = new List<int>();
        foreach (Vector3 clickito in itemClicks)
        {
            if (clickito.y == 1)
            {
                itemsIn.Add(Convert.ToInt32(clickito.x));
            }
            else
            {
                itemsIn.Remove(Convert.ToInt32(clickito.x));
            }
        }

        string itemsInS = "";
        foreach (int i in itemsIn)
        {
            itemsInS = itemsInS + i + ",";
        }
        if (itemsInS.Length > 0)
            itemsInS = itemsInS.Remove(itemsInS.Length - 1);

        return itemsInS;
    }


    //Updates the timer (including the graphical representation)
    //If time runs out in the trial or the break scene. It switches to the next scene. 
    async Task startTimer()
    {
        tiempo -= Time.deltaTime;
        if (showTimer)
        {
            boardScript.updateTimer();
            //			RectTransform timer = GameObject.Find ("Timer").GetComponent<RectTransform> ();
            //			timer.sizeDelta = new Vector2 (timerWidth * (tiempo / timeTrial), timer.rect.height);
        }

        //When the time runs out:
        if (tiempo < 0)
        {
            if (escena == 1 && questionOn == 0)
            {
                //After showing only the items do not change to next scene. Just show the question.
                totalTime = timeTrial - timeOnlyItems;
                tiempo = totalTime;
                boardScript.setQuestion();
                
                questionOn = 1;
            }
            else
            {
                await changeToNextScene(boardScript.itemClicks, 0);
            }

        }
    }

    // set participant ID from the input field (if not using URL)
    public static bool SetParticipantId(string value)
    {
        if (value.Equals(string.Empty))
            return false;

        participantID = value;

        return true;
    }

    // D-Hive: prepare knapsack data for saving to server (DHive Sender) 
    private static void PrepareOutput(string itemsSelected, int submitted, float timeSpent, string error, List<Vector3> clicksList)
    {
        
        var xyCoordinates = BoardManager.getItemCoordinates();

        //Get the instance number for this trial and add 1 because the instanceRandomization is linked to array numbering in C#, which starts at 0;
        var instanceNum = instanceRandomization[globalTrial - 1] + 1;

        var ks = ksinstances[instanceNum - 1];
        //Calculates the capacity and profit selected
        var profitSel = 0;
        var capacitySel = 0;
        var itemSelectedBool = Enumerable.Repeat(0, ks.weights.Length).ToArray();
        if (itemsSelected != "")
        {
            var itemSelectedInt = Array.ConvertAll(itemsSelected.Split(','), int.Parse);
            foreach (var itemS in itemSelectedInt)
            {
                profitSel += ks.values[itemS - 1];
                capacitySel += ks.weights[itemS - 1];
                itemSelectedBool[itemS - 1] = 1;
            }
        }

        var itemsOptTemp = string.Join(",", ks.itemsOpt.Select(p => p.ToString()).ToArray());
        var cOptTemp = ks.capacityOpt;
        var pOptTemp = ks.profitOpt;
        var itemsSelectedBoolS = string.Join(",", itemSelectedBool.Select(p => p.ToString()).ToArray());
        var correctB = (capacitySel <= ks.capacity) && (profitSel == pOptTemp);
        var correct = correctB ? 1 : 0;

        if (correct == 1)
        {
            total_correct++;
        }

        string datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Initialize the solution_to_date list
        var solution_to_date = new List<int[]>();

        // Start with the initial state where no items are selected
        var current_solution = Enumerable.Repeat(0, ks.weights.Length).ToArray();
        solution_to_date.Add((int[])current_solution.Clone()); // Add the initial state

        var click_items = new List<string>();
        var click_in = new List<string>();
        var click_time = new List<string>();
        foreach (var click in clicksList)
        {
            click_items.Add(click.x.ToString());
            click_in.Add(click.y.ToString());
            click_time.Add(click.z.ToString());

            // add solution state
            int itemIndex = (int)click.x - 1; // Assuming click.x is the item number (1-based), convert to 0-based index
            int inOrOut = (int)click.y; // 1 for in, 0 for out

            // Update the current solution based on the click
            current_solution[itemIndex] = inOrOut;

            // Add the updated solution to the solution_to_date list
            solution_to_date.Add((int[])current_solution.Clone()); // Add a copy of the current state
        }

        // create a list of ints to store the profit and capacity of each solution
        var solutionProfits = new List<int>();
        var solutionCapacities = new List<int>();

        foreach (var solution in solution_to_date)
        {
            int solutionProfit = 0;
            int solutionCapacity = 0;

            for (int i = 0; i < solution.Length; i++)
            {
                if (solution[i] == 1) // item is selected
                {
                    solutionProfit += ks.values[i];
                    solutionCapacity += ks.weights[i];
                }
            }

            solutionProfits.Add(solutionProfit);
            solutionCapacities.Add(solutionCapacity);
        }

        var solutionProgressString = string.Join(" | ", solution_to_date.Select(arr => "[" + string.Join(",", arr) + "]"));

        // convert data into a single, named list
        var outputs = new List<OutputParameter>
        {
            new ("TrialInfo__date_time", datetime),
            new ("TrialInfo__block", block),
            new ("TrialInfo__trial", trial),
            new ("TrialInfo__submitted", submitted),
            new ("TrialInfo__timeSpent", timeSpent),
            new ("TrialInfo__instanceNumber", instanceNum),
            new ("TrialInfo__capacity", ks.capacity),
            new ("TrialInfo__itemsSelected", itemsSelectedBoolS),
            new ("TrialInfo__capacitySel", capacitySel),
            new ("TrialInfo__profitSel", profitSel),
            new ("TrialInfo__itemsOpt", itemsOptTemp),
            new ("TrialInfo__capacityOpt", cOptTemp),
            new ("TrialInfo__profitOpt", pOptTemp),
            new ("TrialInfo__correct", correct),
            new ("TrialInfo__xyCoordinates", xyCoordinates),
            new ("TrialInfo__error", error),
            new ("TrialInfo__click_items", click_items),
            new ("TrialInfo__click_in", click_in),
            new ("TrialInfo__click_time", click_time),
            new ("TrialInfo__working_selections", solutionProgressString),
            new ("TrialInfo__working_capacities", solutionCapacities),
            new ("TrialInfo__working_profits", solutionProfits),
            new ("TrialInfo__confidence", confidence_button_text),
			
			// Depends on how InstanceInfo needs to be stored (with the each individual trial vs separately as a whole)
			new ("InstancesInfo__instanceNumber", instanceNum),
            new ("InstancesInfo__c", ks.capacity),
            new ("InstancesInfo__w", ks.weights),
            new ("InstancesInfo__v", ks.values),
            new ("InstancesInfo__id", ks.id),
            new ("InstancesInfo__type", ks.type),
            new ("InstancesInfo__expAccuracy", ks.expAccuracy),
            new ("InstancesInfo__pOpt", ks.profitOpt),
            new ("InstancesInfo__cOpt", ks.capacityOpt),
            new ("InstancesInfo__itemsOpt", ks.itemsOpt)
        };

        SendData(outputs);
    }

    // create a new trialtask for DHive
    private static async Task NewTrialTask()
    {
        currentTrialTask = null;
        TaskId = DataLoader.GetDatabaseTaskId(experimentData, "Knapsack optimisation task");
        currentTrialTask = await sender.NewTrialTask(TaskId);

        while (currentTrialTask == null)
        {
            await Task.Yield();
        }

        if (escena != 0)
        {
            await SaveTimestampToDatabase(escena);
        }
    }

    // store that the participant completed the study and add it to the queue for saving
    private void ParticipantCompleted()
    {
        var output = new OutputParameter("completed", 1);
        completed = true;

        Debug.Log("Saving completed: " + output.Name + " and value: " + output.Value);
        DataSaver.AddTrialDataToSave(output);
    }

    // store that the participant did not complete the study and add it to the queue for saving
    private void ParticipantNotCompleted()
    {
        var output = new OutputParameter("completed", 0);
        completed = true;

        Debug.Log("Saving not completed: " + output.Name + " and value: " + output.Value);
        DataSaver.AddTrialDataToSave(output);
    }

    // prepare knapsack click data for saving to server (DHive Sender) 
    private static void SaveClicks(List<Vector3> clicksList)
    {
        var outputs = new List<OutputParameter>();
        foreach (var click in clicksList)
        {
            outputs.Add(new OutputParameter($"Click__block_{clicksIndex}", GameManager.block));
            outputs.Add(new OutputParameter($"Click__trial_{clicksIndex}", GameManager.trial));
            outputs.Add(new OutputParameter($"Click__item_{clicksIndex}", click.x));
            outputs.Add(new OutputParameter($"Click__In(1)/Out(0)_{clicksIndex}", click.y));
            outputs.Add(new OutputParameter($"Click__time_{clicksIndex}", click.z));
            clicksIndex++;
        }

        SendData(outputs);
    }

    // D-Hive: prepare timestamp data for saving to server (DHive Sender) 
    private static async Task SaveTimestampToDatabase(int eventType)
    {

        var outputs = new List<OutputParameter>
        {
            new ($"Timestamp__block_{timestampIndex}", block),
            new ($"Timestamp__trial_{timestampIndex}", trial),
            new ($"Timestamp__eventType_{timestampIndex}", eventType),
            new ($"Timestamp__elapsedTime_{timestampIndex}", timeStamp())
        };
        timestampIndex++;

        SendData(outputs);
    }

    // if the task is quit, close the DHive connection and record them as having not completed the study
    private async void OnApplicationQuit()
    {
        // Close websocket connection
        if (sender == null) return;

        if (!completed)
        {
            ParticipantNotCompleted();
        }

        await sender.Close();
    }


    // display scores for a set time, then await all data in the save queue to be saved. Once the queue is empty, load the end scene / display completion code and message
    IEnumerator ProceedToEnd()
    {
        yield return new WaitForSeconds(5f);
        Debug.Log("Queue size is " + DataSaver.GetQueueSize());
        while (DataSaver.GetQueueSize() > 0)
        {
            Debug.Log("Waiting for queue to clear and data to be saved...");
            Debug.Log("Queue size is " + DataSaver.GetQueueSize());
            yield return new WaitForSeconds(1f);
        }
        SceneManager.LoadScene("End");
    }

    // when the participant hits the button of which task to do next, load that task
    public void Next(Button button)
    {
        task_name = button.name;
        if (task_name == "ICAR")
        {
            practice = false;
            TaskId = DataLoader.GetDatabaseTaskId(experimentData, "ICAR");
            SceneManager.LoadScene("ICAR");
        }
        else if (task_name == "Questionnaire")
        {
            practice = false;
            TaskId = DataLoader.GetDatabaseTaskId(experimentData, "Ageing_questionnaires");
            SceneManager.LoadScene("Questionnaires");
        }
        else
        {
            practice = true;
            instructions_start_time = Time.time;
            SceneManager.LoadScene("Instructions");
        }
    }

    // proceed from one slide of the instructions to the next. if leaving the final slide, load the quiz
    public void NextInstruction()
    {
        if (instructions.Count > 1)
        {
            Body.text = instructions[0];
            instructions.RemoveAt(0);
        }
        else
        {
            if (task_name == "NBack")
            {
                // skip the quiz
                complete_quiz = true;
                SceneManager.LoadScene("Instructions");
            }
            else
            {
                SceneManager.LoadScene("Quiz");
            }
        }
    }

    // if they've chosen to restart the recall-1-back instructions, do so
    public void RestartInstruction()
    {
        // Debug.Log("Restarting instructions");
        instructions = new List<string> { };
        complete_quiz = false;
        if (task_name == "NBack")
        {
            Debug.Log("Restarting NBack");
            practice = true;
        }
        SceneManager.LoadScene("Instructions");
    }

    // move to the last instruction slide before the practice Qs
    public void ToFinalInstruction()
    {
        if (no_quiz <= 1)
        {
            complete_quiz = true;
            SceneManager.LoadScene("Instructions");
        }
        else
        {
            SceneManager.LoadScene("Quiz");
            no_quiz -= 1;
        }
    }

    // if they've finished...
    public void EndNext()
    {
        // the practice, take them to a screen that warns them they're about to start the real thing
        if (practice == true)
        {
            practice = false;
            SceneManager.LoadScene("Instructions");
        }
        // the main task, move onto the next task
        else
        {
            SceneManager.LoadScene("CogTaskHome");
        }
    }

    // quits the application
    public void Quit()
    {
        Application.Quit();
        string data = System.DateTime.Now + "," + "Quit";
    }

    // display fixation cross and then start the main task
    IEnumerator StartTask(string task)
    {
        if (practice == false)
        {
            yield return new WaitForSeconds(2f);
        }

        Title.text = "";

        Body.alignment = TextAlignmentOptions.Midline;
        Body.text = "+";
        Body.fontSize = 100;

        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(task);
    }

    // display the relevant instructions for the task at hand
    void AddInstructions(string task_name)
    {
        Body.alignment = TextAlignmentOptions.TopLeft;
        Body.fontSize = 36;

        string body_text = null;
        if (task_name == "SymbolDigit")
        {
            TaskId = DataLoader.GetDatabaseTaskId(experimentData, "Digit Symbol Substitution");
            // Debug.Log("Task ID: " + TaskId);

            body_text = "Doing well in this task reflects strong mental agility, which is useful for quick problem-solving in day-to-day activities.\n\n" +
            "In this task you will need to match the symbol to the number.\n\n" +
            "The top of the screen will show a mapping from symbols to numbers. You'll use this mapping to determine which number matches the target symbol at the bottom of the screen.\n\n" +
            "Press the key on your keyboard corresponding to that number.\n\n";
            instructions.Add(body_text);

            body_text = "After a correct answer the target will change. You have a total of 90 seconds to enter as many correct responses as possible.\n\n" +
            "You can ONLY answer using the number keys located above the letter keys on your keyboard.\n\n" +
            "<i>When you are ready for the PRACTICE rounds, press \"spacebar\".</i>";
            instructions.Add(body_text);

        }
        else if (task_name == "NBack")
        {
            TaskId = DataLoader.GetDatabaseTaskId(experimentData, "Recall-1-back");
            // Debug.Log("Task ID: " + TaskId);

            body_text = "This task shows how well you keep track of recent information, similar to remembering details in a conversation.\n\n" +
            "You first see a starter cue showing a different number in 1-to-3 squares. You must remember the numbers AND which squares (left, middle, right) they're in.\n\n" +
            "You'll do several rounds, one at a time, where a new number appears in only one of the squares, at random. You must respond with the key on your keyboard corresponding to the PREVIOUS number shown in the SAME SQUARE.\n\n" +
            "If the relevant square was blank on the previous round, you must think back over multiple rounds to the most recent number shown in the same square.\n\n" +
            "To do well you must track the previous number shown, in each square, over each round.\n\n";
            instructions.Add(body_text);

            body_text = "You're scored based on the total number of correct answers and have 3-4 seconds to respond while the current number is being displayed: if you're too slow, it's marked incorrect.\n\n" + 
            "In the practice rounds you get feedback after every response (green flash = correct, red flash = incorrect). After practice there is no feedback.\n\n" + 
            "You can ONLY answer using the number keys located above the letter keys on your keyboard.\n\n" +
            "<i>This is the only task where you can practice as many times as you want before starting. When you are ready for the PRACTICE rounds, <i>press \"spacebar\".</i>";
            instructions.Add(body_text);

        }
        else if (task_name == "StopSignal")
        {
            TaskId = DataLoader.GetDatabaseTaskId(experimentData, "Stop Signal Task");
            // Debug.Log("Task ID: " + TaskId);

            body_text = "This task shows how well you can stop yourself from making rash or impulsive decisions.\n\n" +
            "In this task you must respond to either a left or right pointing arrow by pressing the left arrow or right arrow key, respectively, on your keyboard.\n\n" +
            "You have less than <b>one second</b> to respond and if you are too slow you 'miss' an arrow. Aim to miss as few arrows as possible, ideally 0. \n\n" +
            "Occasionally, <i>an arrow is quickly replaced by a red X, which is the STOP signal</i>. If you see this do not press anything. If you press something by mistake you will 'miss' the stop signal.\n\n" +
            "Your first priority is to respond to arrows as fast as you can. Your second priority is to obey the stop signal if possible, but know that this is very hard and you should expect to miss it 50% of the time.";
            instructions.Add(body_text);

            body_text = "The task has 200 rounds. Most rounds will not show the stop signal.\n\n" +
            "The most important part is to respond as fast as you can and to miss as few arrows as possible. \n\n" +
            "If possible, try to obey the stop signal when it replaces an arrow, but do not 'wait' to see if the stop signal replaces an arrow. This will be penalised by making future rounds harder.\n\n" +
            "You should respond as quickly as possible when seeing an arrow.\n\n" +
            "<i>When you are ready for the PRACTICE rounds, press \"spacebar\".</i>";
            instructions.Add(body_text);

        }
        else if (task_name == "TaskSwitching")
        {
            TaskId = DataLoader.GetDatabaseTaskId(experimentData, "Letters and Numbers task");

            Body.transform.GetComponent<TMP_Text>().font = my_font;
            Title.transform.GetComponent<TMP_Text>().font = my_font;

            if (letter_on_left == true)
            {
                eg_text = "I7";
            }
            else
            {
                eg_text = "7I";
            }
            if (congruence_type == 0 || congruence_type == 2)
            {
                vowel_key = "left";
                consonant_key = "right";
            }
            else
            {
                vowel_key = "right";
                consonant_key = "left";
            }
            if (congruence_type == 0 || congruence_type == 3)
            {
                odd_key = "left";
                even_key = "right";
            }
            else
            {
                odd_key = "right";
                even_key = "left";
            }

            if (letter_on_left == true)
            {
                Image.sprite = Resources.Load<Sprite>("Figures/taskswitch_inst_1_left");
            }
            else
            {
                Image.sprite = Resources.Load<Sprite>("Figures/taskswitch_inst_1_right");
            }

            if (letter_on_left == true)
            {
                Image.sprite = Resources.Load<Sprite>("Figures/taskswitch_inst_2_left");
            }
            else
            {
                Image.sprite = Resources.Load<Sprite>("Figures/taskswitch_inst_2_right");
            }

            body_text = "This task shows how well you can mentally sort through mixed information, like organizing thoughts in a busy meeting.\n\n" +
            $"In this task you will see a 2x2 grid with four squares. In each round one of the squares will display a letter-number combination, e.g. \"{eg_text}\". The letter will always be upper case.\n\n" +
            "Based on a rule, you will respond to this letter-number combination by pressing either the left arrow or right arrow key on your keyboard.\n\n" +
            "The square displaying the letter-number combination will change each round. You will need to remember which rule applies to which square.";

            instructions.Add(body_text);

            body_text = "There will be 192 rounds in total and you have a maximum of 5 seconds to respond in each.\n\n" +
            "The square displaying the letter-number combination will change each round. You will need to remember which rule applies to which square.\n\n" +
            "You are scored based on the speed of your responses and must respond as fast as you can. Accuracy is important as incorrect answers count as taking the maximum 5 seconds.\n\n" +
            "<i>When you are ready for the PRACTICE rounds, press \"spacebar\".</i>";
            instructions.Add(body_text);
        }
        NextInstruction();
    }

    // flash green (red) for correct (incorrect) responses
    public void Toggle(Toggle toggle)
    {
        if (toggle.isOn)
        {
            if (options_dict[toggle.GetComponentInChildren<TMP_Text>().text] == 1)
            {
                Message.text = "Correct!";
                Message.color = new Color32(0, 255, 0, 255);
                complete_quiz = true;
                NextButton.enabled = true;
            }
            else
            {
                Message.text = "Incorrect!";
                Message.color = new Color32(255, 0, 0, 255);
                NextButton.enabled = false;
            }
        }
    }

    // set the number of quiz questions for each task
    void AddQuiz()
    {
        if (task_name == "SymbolDigit")
        {
            no_quiz = 2;
        }
        else if (task_name == "NBack")
        {
            no_quiz = 4;
        }
        else if (task_name == "StopSignal")
        {
            no_quiz = 3;
        }
        else if (task_name == "TaskSwitching")
        {
            no_quiz = 4;
        }
    }

    // store the locations and assets with which to populate questions and answers for each quiz question
    void GenerateOptions()
    {
        NextButton.enabled = false;
        Message.text = "";

        AddOptions();

        Toggle tgg = GameObject.Find("Option").GetComponent<Toggle>();
        TMP_Text labelText = tgg.GetComponentInChildren<TMP_Text>(); // Get the TMP_Text component for the label
        labelText.text = options_dict.Keys.ElementAt(0);

        if (task_name == "TaskSwitching")
        {
            labelText.font = my_font;
            if (no_quiz > 2)
            {
                // find the Body text object and move it up slightly
                Body.transform.localPosition = Body.transform.localPosition + new Vector3(0, 50, 0);
                // Title.transform.localPosition = Title.transform.localPosition + new Vector3(0, 50, 0);
                tgg.transform.localPosition = tgg.transform.localPosition + new Vector3(0, -430, 0);
                // Message.transform.localPosition = Message.transform.localPosition + new Vector3(0, -270, 0);
            }
        }

        temp_toggles = new List<Toggle>();

        for (int i = 1; i < options_dict.Count; i++)
        {
            Toggle temp_tgg = Instantiate(tgg);
            temp_tgg.transform.SetParent(GameObject.Find("ToggleGroup").transform, false);
            temp_tgg.transform.localScale = Vector3.one;
            temp_tgg.transform.localPosition = tgg.transform.localPosition + new Vector3(0, -90 * i, 0);
            // temp_tgg.GetComponentInChildren<Text>().text = options_dict.Keys.ElementAt(i);

            TMP_Text tempLabelText = temp_tgg.GetComponentInChildren<TMP_Text>(); // Get the TMP_Text for the new toggle
            tempLabelText.text = options_dict.Keys.ElementAt(i);

            if (task_name == "TaskSwitching")
            {
                // Change font for the new toggles
                tempLabelText.font = my_font;
            }

            temp_toggles.Add(temp_tgg);
        }
    }

    // store the questions and answers for each quiz question
    void AddOptions()
    {
        options_dict = new Dictionary<string, int>();

        if (task_name == "SymbolDigit")
        {
            Body.text = "Which number is the correct response to this target?";
            options_dict.Add("1", 0);
            options_dict.Add("2", 0);
            options_dict.Add("3", 0);
            options_dict.Add("4", 0);
            if (no_quiz == 2)
            {
                options_dict["1"] = 1;
                Image.sprite = Resources.Load<Sprite>("Figures/symdig_1");
            }
            else
            {
                options_dict["2"] = 1;
                Image.sprite = Resources.Load<Sprite>("Figures/symdig_2");
            }
        }
        else if (task_name == "NBack")
        {
            Body.text = "You're in the current round, shown in orange. What was the previous number in the same square?";

            if (no_quiz == 4)
            {
                options_dict.Add("\"1\", the prior number on the left", 1);
                options_dict.Add("\"2\", the current number on the left", 0);
                options_dict.Add("Some other key", 0);
                options_dict.Add("Not pressing any key", 0);
                Image.sprite = Resources.Load<Sprite>("Figures/nback_1");
            }
            else if (no_quiz == 3)
            {
                options_dict.Add("\"1\", the starting number on the left", 0);
                options_dict.Add("\"2\", the prior number on the left", 1);
                options_dict.Add("\"5\", the current number on the left", 0);
                options_dict.Add("Some other key", 0);
                Image.sprite = Resources.Load<Sprite>("Figures/nback_2");
            }
            else if (no_quiz == 2)
            {
                options_dict.Add("\"1\", the starting number on the left", 0);
                options_dict.Add("\"6\", the starting number in the middle", 0);
                options_dict.Add("\"3\", the prior number on the right", 0);
                options_dict.Add("\"8\", the prior number in the middle", 0);
                options_dict.Add("\"5\", the prior number on the left", 1);
                Image.sprite = Resources.Load<Sprite>("Figures/nback_3");
            }
            else
            {
                options_dict.Add("\"1\", the starting number on the left", 0);
                options_dict.Add("\"6\", the starting number in the middle", 0);
                options_dict.Add("\"3\", the prior number on the right", 1);
                options_dict.Add("\"8\", the prior number in the middle", 0);
                options_dict.Add("\"5\", the prior number on the left", 0);
                Image.sprite = Resources.Load<Sprite>("Figures/nback_4");
            }
        }
        else if (task_name == "StopSignal")
        {
            Body.text = "What is the correct response to this symbol?";

            options_dict.Add("\"Left arrow\" key", 0);
            options_dict.Add("\"Right arrow\" key", 0);
            options_dict.Add("\"S\" key", 0);
            options_dict.Add("Not pressing any key", 0);

            if (no_quiz == 3)
            {
                options_dict["\"Left arrow\" key"] = 1;
                Image.sprite = Resources.Load<Sprite>("Figures/stopsig_left");
            }
            else if (no_quiz == 2)
            {
                options_dict["Not pressing any key"] = 1;
                Image.sprite = Resources.Load<Sprite>("Figures/stopsig_stop");
            }
            else
            {
                options_dict["\"Right arrow\" key"] = 1;
                Image.sprite = Resources.Load<Sprite>("Figures/stopsig_right");
            }
        }
        else if (task_name == "TaskSwitching")
        {
            Body.transform.GetComponent<TMP_Text>().font = my_font;
            Title.transform.GetComponent<TMP_Text>().font = my_font;

            if (no_quiz == 4)
            {
                Body.text = $"If the letter-number pair appears in the TOP ROW, you should respond to the LETTER (in this case, I).\n\n" +
            $"You respond by pressing the \"{consonant_key} arrow\" key if the letter is a consonant.\n\n" +
            $"You respond by pressing the \"{vowel_key} arrow\" key if the letter is a vowel. You will need to remember these rules before progressing further.\n\n" +
            "In this case, I is a vowel so which key should you respond with?";
                options_dict.Add("\"Left arrow\" key", 0);
                options_dict.Add("\"Right arrow\" key", 0);

                if (congruence_type == 0 || congruence_type == 2)
                {
                    options_dict["\"Left arrow\" key"] = 1;
                }
                else
                {
                    options_dict["\"Right arrow\" key"] = 1;
                }
                if (letter_on_left == true)
                {
                    Image.sprite = Resources.Load<Sprite>("Figures/taskswitch_inst_1_left");
                }
                else
                {
                    Image.sprite = Resources.Load<Sprite>("Figures/taskswitch_inst_1_right");
                }
            }

            else if (no_quiz == 3)
            {
                Body.text = $"If the letter-number pair appears in the BOTTOM ROW, you should respond to the NUMBER (in this case, 7).\n\n" +
            $"You respond by pressing the \"{odd_key} arrow\" key if the number is odd.\n\n" +
            $"You respond by pressing the \"{even_key} arrow\" key if the number is even. You will need to remember these rules before progressing further.\n\n" +
            "In this case, 7 is odd so which key should you respond with?";
                options_dict.Add("\"Left arrow\" key", 0);
                options_dict.Add("\"Right arrow\" key", 0);

                if (congruence_type == 0 || congruence_type == 3)
                {
                    options_dict["\"Left arrow\" key"] = 1;
                }
                else
                {
                    options_dict["\"Right arrow\" key"] = 1;
                }
                if (letter_on_left == true)
                {
                    Image.sprite = Resources.Load<Sprite>("Figures/taskswitch_inst_2_left");
                }
                else
                {
                    Image.sprite = Resources.Load<Sprite>("Figures/taskswitch_inst_2_right");
                }
            }

            else if (no_quiz == 2)
            {
                Body.text = "What is the correct response in this round?";
                options_dict.Add("\"Left arrow\" key, letter is a consonant", 0);
                options_dict.Add("\"Right arrow\" key, letter is a consonant", 0);
                options_dict.Add("\"Left arrow\" key, number is odd", 0);
                options_dict.Add("\"Right arrow\" key, number is odd", 0);

                if (congruence_type == 1 || congruence_type == 3)
                {
                    options_dict["\"Left arrow\" key, letter is a consonant"] = 1;
                }
                else
                {
                    options_dict["\"Right arrow\" key, letter is a consonant"] = 1;
                }
                if (letter_on_left == true)
                {
                    Image.sprite = Resources.Load<Sprite>("Figures/taskswitch_1_left");
                }
                else
                {
                    Image.sprite = Resources.Load<Sprite>("Figures/taskswitch_1_right");
                }
            }
            else
            {
                Body.text = "What is the correct response in this round?";
                options_dict.Add("\"Left arrow\" key, letter is a vowel", 0);
                options_dict.Add("\"Right arrow\" key, letter is a vowel", 0);
                options_dict.Add("\"Left arrow\" key, number is even", 0);
                options_dict.Add("\"Right arrow\" key, number is even", 0);

                if (congruence_type == 1 || congruence_type == 2)
                {
                    options_dict["\"Left arrow\" key, number is even"] = 1;
                }
                else
                {
                    options_dict["\"Right arrow\" key, number is even"] = 1;
                }
                if (letter_on_left == true)
                {
                    Image.sprite = Resources.Load<Sprite>("Figures/taskswitch_2_left");
                }
                else
                {
                    Image.sprite = Resources.Load<Sprite>("Figures/taskswitch_2_right");
                }
            }
        }
    }

    // add data that needs to be saved to the queue for saving to DHive
    private static async void SendData(List<OutputParameter> outputs)
    {
        // var isConnected = sender.TimeSinceLastPingSeconds < 10;
        Debug.Log("Saving: " + string.Join(", ", outputs.Select(o => $"{o.Name}: {o.Value}")));
        Debug.Log("Queue size is " + DataSaver.GetQueueSize());
        DataSaver.AddDataToSave(TaskId, outputs);
    }
}
