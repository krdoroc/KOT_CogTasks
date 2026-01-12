
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
using Debug = UnityEngine.Debug;

// from Cog Tasks
using UnityEditor;
using TMPro;
using UnityEngine.UI;

//using System.Diagnostics;
using UnityEngine.EventSystems;
using System.Diagnostics;

public class GameManager : MonoBehaviour
{
    // game parameters
    // TODO: ensure these are set correctly
    public static string first_task_scene; // name of the first main scene to load after setup/dataloading
    public static bool give_complex_instructions = true; // whether to give knapsack optimisation task instructions at the start
    public static bool give_instructions_from_pid = true; // whether to give knapsack optimisation task instructions at the start
    public static bool let_participants_choose_tasks = true; // whether participants are allowed to choose which cognitive task to do next
    public static bool give_feedback_at_end = true;
    public static bool skip_complex = false; // whether to skip the complex task (knapsack/TSP) and proceed directly to the cognitive tasks
    public static bool url_for_id = false; // true if participantid is passed in the URL, false if they need to manually input it when in-game
    public static bool ban_repeat_logins = false; // true if participants are not allowed to login more than once
    public static bool save_session_data = false; // whether to save the randomisation id to the server and take that ID out of circulation
    public static bool save_write_locally = true; // whether to save data locally on the participant's machine (T) or to the DHive server (F)
    public static bool load_read_locally = true; // whether to read data locally on the participant's machine (T) or from the DHive server (F)
    public static string file_w_session_params = "Testing and Playground Session.csv"; // name of the CSV file in StreamingAssets containing session parameters
    public static bool track_quiz_mistakes = false; 

    public static GameManager instance = null;  //Game Manager: It is a singleton (i.e. it is always one and the same it is nor destroyed nor duplicated)
    private BoardManager boardScript; //The reference to the script managing the board (interface/canvas).
    private static bool on_load = true; // flag for whether to do something only when the game first loads
    public static float screen_width;
    public static float screen_height;
    public static bool logged_in_before = false; // flag for whether the participant has attempted to login before
    public static TMP_FontAsset my_font;
    public static bool abort_due_to_disconnection = false;  // flag for whether to stop the game and display a message that the participant's internet has been disconnected for too long
    public static int max_reconnection_attempts = 13;  // maximum number of consecutive internet reconnection attempts following identification of disconnection
    
    // Scene & Timing Variables
    public static float tiempo; //Time spent so far on this scene 
    public static float totalTime;  //Total time for these scene
    private static bool showTimer = false;
    //Minimum and maximum for randomized interperiod Time 
    public static float time_iti_min = 5;
    public static float time_iti_max = 9;
    public static string confidence_button_text; // participants asked for confidence after each complex task trial
    public static float time_inter_block_rest = 10; //InterBlock rest time

    //This is the string that will be used as the file name where the data is stored. DeCurrently the date-time is used.
    public static string participantID = "Empty";
    public static string dateID = @System.DateTime.Now.ToString("dd MMMM, yyyy, HH-mm");
    private static System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();  // Stopwatch to calculate time of events.
    private static string initialTimeStamp; // Time at which the stopwatch started. Time of each event is calculated according to this moment.

    // D-Hive variables
    public static readonly string ExperimentId = "76f4e5c5-975b-4220-aa5a-1ec4f61f383a"; //pablo's experiment ID "99630040-71cd-4d89-acd8-3621829b447d";  // 2ac47f83-a59e-4357-965a-70116abe0d23
    public static string participantTrialId;
    public static Experiment experimentData;
    private DhiveReader _reader;
    public static DhiveSender sender;
    public static string TaskId;
    private bool completed;
    private static bool IsIOS;
    private static bool IsAndroid;
    public static bool systemOK;
    public static bool complex_task_completed = false;

    // cognitive task related variables
    public static int randomizationID;  // stores key to the randomised trial sequence
    public static int repeat_nbacks = 0; // count of how often the recall-1-back instructions & do_practice have been repeated
    public static int total_correct = 0; // to track scores displayed to participant at the end
    public static int total_instances = 0; // to track scores displayed to participant at the end
    // each of these _name variables below must match, exactly, the file names uploaded to DHive
    public static string symbol_digit_name = "Digit Symbol Substitution Task"; // if getting a "Task does not exist" error, try changing this the other option out of "Digit Symbol Substitution" or "Digit Symbol Substitution Task"
    public static string knapsack_name = "Knapsack decision task"; // if getting a "Task does not exist" error, try changing this the other option out of "Knapsack optimisation task" or "Knapsack decision task"
    public static string sst_name = "Stop Signal Task"; 
    public static string ln_name = "Letters and Numbers Task"; // if getting a "Task does not exist" error, try changing this the other option out of "Letters and Numbers Task" or "Letters and Numbers task"
    public static string nback_name = "Recall-1-back"; 
    public static string quest_name = "Questionnaires for Assessment"; // if getting a "Task does not exist" error, try changing this the other option out of "Questionnaires for Assessment" or "Ageing_questionnaires"
    private static string task_name = null;
    private static List<string> tasks = new List<string> { "SymbolDigit", "NBack", "StopSignal", "TaskSwitching" };  // { "SymbolDigit", "StopSignal", "NBack", "TaskSwitching", "ICAR" };
    private static List<string> complex_tasks = new List<string> { "KOT" }; // , "KDT", "TSP" };
    public static List<string> completed_tasks = new List<string> { }; // list of completed cognitive tasks
    private static List<string> scores = new List<string> { }; // list of scores to display at the end

    public TMP_Text Title;
    public TMP_Text Body;
    public Button NextButton;
    public Button RestartButton;
    public TMP_Text Message;
    public Image Image;
    public Image LargeImage;
    private static Color newColor;

    public static bool questionnaires_completed = true;  // TODO: edit this back to false when questionnaires are needed
    public static int current_task_number = 1; // stores the sequence of the current task, initialised to 1 as KP is always first
    private static int total_number_of_tasks = complex_tasks.Count + tasks.Count; // total number of cognitive tasks in the study 
    
    // Quiz and instruction related
    public static float instructions_start_time;
    public static bool do_practice = true;
    public static bool complete_quiz = false; // whether the comprehension quiz in the task instructions has been completed
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
    public static int total_quiz_mistakes = 0;
    public static int max_allowed_mistakes = 3; 
    private static bool abort_due_to_performance = false; 
    private static string current_quiz_task; 

    public static BaseComplexTask activeTask;

    // Use this for initialization
    async void Awake()
    {

        if (url_for_id)
        {
            // Get the full URL (including query parameters)
            string url = Application.absoluteURL;

            // Parse the query string to extract the participant ID
            participantID = GetParameterFromUrl(url, "id");  
            if (give_instructions_from_pid)
                ApplyParticipantIDConfig();
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
            return;
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

    // this method extracts whether to give instructions and training from the pid
    private static void ApplyParticipantIDConfig()
    {
        if (string.IsNullOrEmpty(participantID)) return;

        string pid = participantID.ToUpper();

        if (pid.Contains("T"))
        {
            give_complex_instructions = true;
        }
        else if (pid.Contains("F"))
        {
            give_complex_instructions = false;
        }
    }

    //Initializes the complex task scenes
    async Task InitGame()
    {
        // randomise the sequence of cognitive tasks on intialisation
        if (on_load == true)
        {
            tasks = tasks.OrderBy(x => Random.value).ToList();
            on_load = false;
        }

        string scene_name = SceneManager.GetActiveScene().name;
        
        // scene 0: show initial instructions 
        if (scene_name == "Setup")
        {  
            if (activeTask == null)
            {
                activeTask = FindObjectOfType<BaseComplexTask>();
            }

            // Now we are guaranteed to have it (if it exists in the scene)
            if(activeTask != null)
            {
                activeTask.ResetCounters();
            }
            boardScript.setupInitialScreen();

        }
    }

    void SaveInstructionsTime(string task_name, float instructions_time)
    {
        if (save_write_locally)
        {
            var outputs = new List<OutputParameter>
            {
                new OutputParameter("session_metric", $"o_{task_name}_instruction_time"),
                new OutputParameter("session_value", instructions_time.ToString())
            };
            
            // Save to session_data.csv
            DataSaver.PrepareToSave(outputs, "session_data");
        }
        else
        {
            var outputs = new List<OutputParameter>
            {
                new ($"o_{task_name}_instruction_time", instructions_time.ToString()),
            };
            DataSaver.AddTrialDataToSave(outputs);
        }
    }

    // add the whole KP trial's data, including the confidence level, the DHive save queue and then proceed to the next trial
    public void ConfidenceSelection(Button clickedButton)
    {
        // extract rating
        Text buttonText = clickedButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            confidence_button_text = buttonText.text;
        }

        // prepare to save
        if (activeTask != null)
        {
            activeTask.ProcessConfidence();
        }
    }

    // Update is called once per frame
    async void Update()
    {
        string scene_name = SceneManager.GetActiveScene().name;
        if (scene_name == "KOT") // TODO: add KDT and TSP scenes here later
        {
            await activeTask.TaskUpdate();
        }
        else if (scene_name == "InterTrialRest" || scene_name == "InterBlockRest")
        {
            await startTimer(); 
        }

        // if internet disconnection detected, try to reconnect. if reconnection fails after 5min, end the game. ignore if reading/writing locally
        if (!save_write_locally && !load_read_locally && !sender.IsConnected)
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

// TODO: is this needed when not on WebGL?
#if !UNITY_WEBGL || UNITY_EDITOR
        sender?.DispatchMessageQueue();
#endif

    }

    //Takes care of changing between scenes during the complex task
    public static async Task changeToNextScene(List<Vector3> itemClicks, int submitted)
    {
        BoardManager.keysON = false;
        string scene_name = SceneManager.GetActiveScene().name;
        
        // if they're leaving the setup scene, load the KP
        if (scene_name == "Setup")
        {
            if (complex_tasks.Count > 0)
                task_name = complex_tasks[0];

            if (skip_complex)
                give_complex_instructions = false;
            
            if(!skip_complex && give_complex_instructions)
            {
                first_task_scene = "Instructions";
            }
            else
            {
                first_task_scene = "KOT";

                if (!skip_complex)
                {
                    complete_quiz = true;
                }
            }


            if (complex_tasks.Count > 0) 
                complex_tasks.RemoveAt(0);

            SceneManager.LoadScene("LoadTrialData");
        }
        // if they just finished a trial, store everything we want to save as a global variable and then load the confidence scene
        else if (scene_name == "KOT")  // TODO: add KDT and TSP scenes here later
        {
            if (activeTask is KnapsackOpt koTask)
            {
                await koTask.ChangeToConfidenceScene(itemClicks, submitted);
            }
        }
        // if leaving a rest, load the next trial
        else if (scene_name == "InterTrialRest")
        {
            if (activeTask != null) activeTask.ResumeAfterTrialRest();
        }
        else if (scene_name == "InterBlockRest")
        {
            if (activeTask != null) activeTask.ResumeAfterBlockRest();
        }

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

    //Updates the timer (including the graphical representation)
    //If time runs out in the trial or the break scene. It switches to the next scene. 
    async Task startTimer()
    {
        tiempo -= Time.deltaTime;

        if (showTimer) 
        {
            boardScript.updateTimer();
        }
        
        //When the time runs out:
        if (tiempo < 0)
        {
            if (SceneManager.GetActiveScene().name == "InterTrialRest") 
            {
                if (activeTask != null) activeTask.ResumeAfterTrialRest();
            }
            else if (SceneManager.GetActiveScene().name == "InterBlockRest") 
            {
                if (activeTask != null) activeTask.ResumeAfterBlockRest();
            }
        }
    }

    // set participant ID from the input field (if not using URL)
    public static bool SetParticipantId(string value)
    {
        if (value.Equals(string.Empty))
            return false;

        participantID = value;
        if (give_instructions_from_pid)
            ApplyParticipantIDConfig();

        return true;
    }

    // Store that the participant completed the study
    private void ParticipantCompleted()
    {
        completed = true;

        if (save_write_locally)
        {
            var outputs = new List<OutputParameter>
            {
                new OutputParameter("session_metric", "completed"),
                new OutputParameter("session_value", "1")
            };

            // Debug.Log("Saving completed: 1");
            DataSaver.PrepareToSave(outputs, "session_data");
        }
        else
        {
            var output = new OutputParameter("completed", 1);
            completed = true;

            Debug.Log("Saving completed: " + output.Name + " and value: " + output.Value);
            DataSaver.AddTrialDataToSave(output);
        }
    }

    // Store that the participant did not complete the study
    private void ParticipantNotCompleted()
    {
        completed = true;

        if (save_write_locally)
        {
            var outputs = new List<OutputParameter>
            {
                new OutputParameter("session_metric", "completed"),
                new OutputParameter("session_value", "0")
            };

            // Debug.Log("Saving not completed: 0");
            DataSaver.PrepareToSave(outputs, "session_data");
        }
        else
        {
            var output = new OutputParameter("completed", 0);
            completed = true;

            Debug.Log("Saving not completed: " + output.Name + " and value: " + output.Value);
            DataSaver.AddTrialDataToSave(output);
        }
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
        
        if (task_name == "TaskSwitching")
        {
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
        }
        
        if (task_name == "ICAR")
        {
            do_practice = false;
            TaskId = DataLoader.GetDatabaseTaskId(experimentData, "ICAR");
            SceneManager.LoadScene("ICAR");
        }
        else if (task_name == "Questionnaire")
        {
            do_practice = false;
            TaskId = DataLoader.GetDatabaseTaskId(experimentData, quest_name);
            SceneManager.LoadScene("Questionnaires");
        }
        else
        {
            do_practice = true;
            instructions_start_time = Time.time;
            SceneManager.LoadScene("Instructions");
        }
    }

    // if they've finished...
    public void EndNext()
    {
        // the do_practice, take them to a screen that warns them they're about to start the real thing
        if (do_practice == true)
        {
            do_practice = false;
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
    public IEnumerator StartTask(string task)
    {
        if (do_practice == false)
        {
            yield return new WaitForSeconds(2f);
        }

        Title.text = "";

        Body.alignment = TextAlignmentOptions.Midline;
        Body.text = "+";
        Body.fontSize = 100;

        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(task);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UIBridge localBridge = FindObjectOfType<UIBridge>();

        if (localBridge != null)
        {
            // Steal the references from the bridge and give them to GameManager!
            Title = localBridge.Title;
            Body = localBridge.Body;
            NextButton = localBridge.NextButton;
            RestartButton = localBridge.RestartButton;
            Message = localBridge.Message;
            Image = localBridge.Image;
            LargeImage = localBridge.LargeImage;
        }
        
        // scene 1: show KP trial (or if skipping KP, go to landing page for cognitive tasks)
        if (scene.name == "KOT")
        {
            if (skip_complex)
            {
                SceneManager.LoadScene ("CogTaskHome");  
                complex_task_completed = true;
            }
        }

        // scene 3: intertrial rest
        else if (scene.name == "InterTrialRest")
        {
            showTimer = false;
            tiempo = Random.Range(time_iti_min, time_iti_max);
            totalTime = tiempo;
        }

        // scene 4: interblock rest
        else if (scene.name == "InterBlockRest")
        {
            showTimer = true;
            tiempo = time_inter_block_rest;
            totalTime = tiempo;
        }

        // landing page for cognitive tasks
        if (scene.name == "CogTaskHome")
        {
            HandleCogTaskHome();
        }

        else if (scene.name == "FinishedCogTask")
        {
            HandleFinishedCogTask();
        }

        // if at the end of the whole game, display final message
        else if (scene.name == "End")
        {
            HandleEndScene();
        }
    }

    void HandleCogTaskHome()
    {
        // if the KP has just been completed, store the participant's score and mark it as complete
        if (complex_task_completed == false)
        {
            string score = null;
            double temp = (double)total_correct / (double)activeTask.total_complex_instances;
            score = temp.ToString("P0");
            scores.Add(score);

            complex_task_completed = true;
        }
        
        // if there are no remaining cognitive tasks to complete, proceed to the questionnaires
        if (tasks.Count == 0)
        {
            // if the questionnaires are also finished, display the final scores before dusplaying the end screen
            if (questionnaires_completed)
            {
                if (give_feedback_at_end)
                {
                    Title.text = "Cognitive Tasks\nYour Accuracy";
                }
                else{
                    Title.text = "Saving.";
                }
                Body = GameObject.Find("Body").GetComponent<TMP_Text>();
                
                if (skip_complex)
                {
                    for (int i = 0; i < completed_tasks.Count; i++)
                    {
                        if (give_feedback_at_end)
                        {
                            Body.text = Body.text + "\n" + completed_tasks[i] + ": " + scores[i]; //non-KP
                        }
                        GameObject.Find(completed_tasks[i]).GetComponent<Button>().gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (give_feedback_at_end)
                    {
                        Body.text = "Main task: " + scores[0]; //KP
                    }
                    for (int i = 0; i < completed_tasks.Count; i++)
                    {
                        if (give_feedback_at_end)
                        {
                            Body.text = Body.text + "\n" + completed_tasks[i] + ": " + scores[i + 1]; //non-KP
                        }
                        GameObject.Find(completed_tasks[i]).GetComponent<Button>().gameObject.SetActive(false);
                    }
                }
                Body.text = Body.text + "\n\n" + "Your responses are being saved. Your completion code will appear shortly, in the meantime please do not leave this page.";
                StartCoroutine(ProceedToEnd()); // cog tasks used to call QuitOnDelay() here 
            }
            // if the questionnaires aren't finished, proceed to the questionnaires
            else
            {
                TaskId = DataLoader.GetDatabaseTaskId(experimentData, quest_name);
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
                    do_practice = false;
                    TaskId = DataLoader.GetDatabaseTaskId(experimentData, "ICAR");
                    // Debug.Log("Task ID: " + TaskId);
                    SceneManager.LoadScene("ICAR");
                }
                else
                {
                    do_practice = true;
                    instructions_start_time = Time.time;
                    // Debug.Log("start time is " + instructions_start_time);
                    SceneManager.LoadScene("Instructions");
                }
            }
        }
    }
    
    void HandleFinishedCogTask()
    {
        // if they just finished do_practice, give the option to proceed to the main task unless they failed the minimum accuracy hurdle for the recall-1-back - in which case they must repeat the instructions
        if (do_practice == true)
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
                // TODO: revert to the commented version if we have questionnaires
                // NextButton.GetComponentInChildren<TMP_Text>().text = "Proceed to questionnaires";  
                NextButton.GetComponentInChildren<TMP_Text>().text = "Proceed to end";
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
                string temp2 = (total_correct / SymbolDigitGM.time_limit).ToString("F2");
                score = temp2 + " correct responses p/sec";
            }
            scores.Add(score);
        }
    }

    void HandleEndScene()
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
        else if (abort_due_to_performance)
        {
            textComponent.text =
                "Thank you for your participation. Unfortunately, the study has ended early.\n\n" +
                "You have exceeded the maximum number of incorrect answers allowed in the comprehension quizzes.\n\n" +
                "Please let the researcher know you have reached this screen.";
            
            ParticipantNotCompleted();
        }
        // end message for those that successfully completed
        else
        {
            textComponent.text =
            // TODO: change this for Prolific when relevant
            //    "Thank you so much for your participation, you have reached the end of the experiment!\n\n" +
            //    "Please go to Prolific and enter the completion code below to verify your completion.\n\n" +
            //    "COMPLETION CODE (case sensitive): " + completionCode;
            "This part of the experiment is over. \n\n" +
            "Please let the researcher know you have reached this screen.";

            ParticipantCompleted();
        }
    }

    public string GetTaskName()
    {
        return task_name;
    }

    public bool IsQuizComplete()
    {
        return complete_quiz;
    }

    public void MarkQuizComplete()
    {
        complete_quiz = true;
        current_quiz_task = GetTaskName();
    }

    // keeps track of how many incorrect quiz answers are selected and cancels the sesison beyond a threshold
    public void TrackQuizMistakes(string task)
    {
        if (current_quiz_task != task)
        {
            total_quiz_mistakes = 0;
            current_quiz_task = task;
        }

        total_quiz_mistakes++;
        
        if (total_quiz_mistakes >= max_allowed_mistakes)
        {
            abort_due_to_performance = true;
            SceneManager.LoadScene("End");
        }
    }

}
