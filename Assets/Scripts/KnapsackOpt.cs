using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; 
using System;
using System.Linq;
using System.Threading.Tasks;
using Dhive; 
using UnityEngine.UI;

public class KnapsackOpt : BaseComplexTask
{
    private static bool allowPausing = false; // TOCON: choose whether pausing is allowed in knapsack task

    public static KnapsackOpt instance;
    private BoardManager boardScript; // Reference to the board

    public static int trial = 0;  //Current knapsack trial initialization
    public static int block = 0;  //Current knapsack block initialization
    public static string items_selected; // which items they've selected
    public static int solution_submitted; // whether a solution has been submitted
    public static List<Vector3> item_clicks; // which items they've selected
    public static float timeTrial = 10; //Time given for each trial (The total time the items are shown -With and without the question-)
    public static float timeOnlyItems = 1; // Time for seeing the KS items without the question
    public static string pay_p_correct_answer = "$0.125";

    public static int numberOfTrials; //Total number of trials in each block
    public static int numberOfBlocks;  //Total number of blocks
    public static int numberOfInstances;  //Number of instance file to be considered. From i1.txt to i_.txt..
    public static int[] instanceRandomization; //The order of the instances to be presented
    private static int questionOn;  //Is the question shown on scene scene 1?
    public static float current_total_time = 0f;

    private static int practice_output = 3; 
    public static int practice_trials_count = 0; // How many practice trials?
    public static KSInstance[] practice_instances; // Holds the practice data loaded by DataLoader
    // Backups for the real data
    private static KSInstance[] real_instances_backup; 
    private static int[] real_randomization_backup;
    private static int real_trials_backup = 0;   // To store the real configuration
    private static int real_blocks_backup = 0;
    
    //A structure that contains the parameters of each instance
    public struct KSInstance
    {
        public int capacity;

        public int[] weights;
        public int[] values;

        public string id;
        public int type;
        public float expAccuracy;

        public int profitOpt;
        public int capacityOpt;
        public int[] itemsOpt;
    }

    //An array of all the instances to be uploaded form .txt files.
    public static KSInstance[] ksinstances; // = new KSInstance[numberOfInstances];

    private static bool isTaskActive = false; // Is this task currently running?

    void Awake()
    {
        if (instance == null) 
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return; 
        }
        boardScript = GetComponent<BoardManager>(); // Grab the neighbor script
        GameManager.activeTask = this; 
        DontDestroyOnLoad(this.gameObject);
    }

    // Call this when the Knapsack Task starts
    public override void StartComplexTask()
    {
        isTaskActive = true;

        if (GameManager.do_practice && practice_trials_count == 0)
        {
            GameManager.do_practice = false;
        }

        if (GameManager.do_practice)
        {
             // Backup the real settings if we haven't already
             if (real_instances_backup == null) real_instances_backup = ksinstances;
             if (real_randomization_backup == null) real_randomization_backup = instanceRandomization;
             if (real_trials_backup == 0) real_trials_backup = numberOfTrials;
             if (real_blocks_backup == 0) real_blocks_backup = numberOfBlocks;

             // Set to Practice Mode
             ksinstances = practice_instances;
             numberOfTrials = practice_trials_count;
             numberOfBlocks = 1; 

             instanceRandomization = Enumerable.Range(0, practice_trials_count).ToArray();
        }
        else
        {
             // Restore Real Settings
             if (real_instances_backup != null) ksinstances = real_instances_backup;
             if (real_randomization_backup != null) instanceRandomization = real_randomization_backup;
             if (real_trials_backup != 0) numberOfTrials = real_trials_backup;
             if (real_blocks_backup != 0) numberOfBlocks = real_blocks_backup;
             
             // Reset score for the real task
             GameManager.total_correct = 0; 
        }
        practice_output = GameManager.do_practice ? 1 : 0;
        SetupTrial(); 
    }

    // Formerly part of InitGame
    public void SetupTrial()
    {
        // 1. Check if we are skipping
        if (GameManager.skip_complex)
        {
            FinishComplexTask();
            return;
        }

        // 2. Setup the trial
        trial++;
        show_timer = true;
        
        // Tell BoardManager to draw the screen
        boardScript.SetupScene(1);
        bool success = boardScript.SetupScene(1);

        // If it failed (e.g. items couldn't fit), skip this trial immediately
        if (!success)
        {
            HandleCriticalError("Board Generation Failed: Items could not fit");
            return; 
        }

        current_timer = timeTrial;
        time_limit = timeTrial;

        // if we plan to display the question from the start, do so
        if (timeOnlyItems <= 0)
        {
            boardScript.setQuestion();
            questionOn = 1;
        }
        else
        {
            questionOn = 0;
        }
        
        // D-Hive ID
        GameManager.TaskId = DataLoader.GetDatabaseTaskId(GameManager.experimentData, GameManager.knapsack_name);
    }

    // The Update Loop specifically for Knapsack
    public override async Task TaskUpdate()
    {
        if (!isTaskActive) return;

        // Run the timer
        await base.TaskUpdate();
        
        // if we weren't displaying the question and item-only time runs out, start displaying the question
        if (questionOn == 0 && current_timer <= (timeTrial - timeOnlyItems))
        {
            boardScript.setQuestion();
            questionOn = 1;
        }
        // if time runs out, proceed to next scene
        if (current_timer <= 0)
        {
            isTaskActive = false; 
            await ChangeToConfidenceScene(boardScript.itemClicks, 0);
        }
        
        // Check for pause
        if (allowPausing)
        {
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.P))
            {
                Time.timeScale = (Time.timeScale == 1) ? 0 : 1;
                if (Time.timeScale == 1) PrepareOutput("", 2, timeTrial, "Pause", new List<Vector3>(), practice_output);
            }
        }

        // if they submit manually, proceed to next scene
        if (BoardManager.keysON && Input.GetKeyDown(KeyCode.Return) && SceneManager.GetActiveScene().name == "KOT")
        {
            await ChangeToConfidenceScene(boardScript.itemClicks, 1); // 1 means "Submitted manually"
        }
    }

    protected override void OnUpdateTimerUI(float remainingTime)
    {
        if (boardScript != null)
        {
            boardScript.updateTimer(remainingTime, time_limit);
        }
    }

    // Formerly changeToNextScene
    public async Task ChangeToConfidenceScene(List<Vector3> itemClicks, int submitted)
    {
        BoardManager.keysON = false;
        items_selected = extractItemsSelected(itemClicks);
        solution_submitted = submitted;
        item_clicks = itemClicks;
        isTaskActive = false;
        SceneManager.LoadScene("Confidence");
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
        if (scene.name == "KOT") 
        {
            StartComplexTask();
        }
    }

    public override void ResumeAfterTrialRest()
    {
        SceneManager.LoadScene("KOT");
    }

    public override void ResumeAfterBlockRest()
    {
        block++;
        trial = 0;
        SceneManager.LoadScene("KOT");

        // TOCON: relevant code from GameManager that may be helpful later
        // showTimer = true;
        // KnapsackOpt.trial = 0;
        // KnapsackOpt.block++;
        // tiempo = time_inter_block_rest;
        // totalTime = tiempo;
    }

    public override void ProcessConfidence()
    {
        PrepareOutput(items_selected, solution_submitted, timeTrial - current_timer, "", item_clicks, practice_output);
        NextTrial();
    }

    public void NextTrial()
    {
        if (trial < numberOfTrials) 
        {
            SceneManager.LoadScene("InterTrialRest");
        }
        else if (block < numberOfBlocks)  
        {
            SceneManager.LoadScene("InterBlockRest");
        }
        else 
        {
            FinishComplexTask();
        }
    }

    public override void FinishComplexTask()
    {
        isTaskActive = false;

        if (GameManager.skip_complex)
        {
             GameManager.current_task_number++;
             SceneManager.LoadScene("CogTaskHome");
             return; 
        }
        
        if (GameManager.do_practice)
        {
            // If practice is done, go to the "End of Practice" screen
            SceneManager.LoadScene("FinishedCogTask");
            ResetCounters(); 
        }
        else
        {
            // If real task is done, mark as complete and go to the next task
            GameManager.current_task_number++;
            SceneManager.LoadScene("CogTaskHome");
        }
    }

    // D-Hive: prepare knapsack data for saving to server (DHive Sender) 
    public void PrepareOutput(string itemsSelected, int submitted, float timeSpent, string error, List<Vector3> clicksList, int practice_output)
    {   
        int globalIndex = (block - 1) * numberOfTrials + (trial - 1);
        var xyCoordinates = BoardManager.getItemCoordinates();

        //Get the instance number for this trial and add 1 because the instanceRandomization is linked to array numbering in C#, which starts at 0;
        var instanceNum = instanceRandomization[globalIndex] + 1;

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
            GameManager.total_correct++;
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
            new ("TrialInfo__confidence", GameManager.confidence_button_text),
            new ("TrialInfo__practice_output", practice_output),
			
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
    
    /// Extracts the items that were finally selected based on the sequence of clicks.
    /// <returns>The items selected.</returns>
    /// <param name="itemClicks"> Sequence of clicks on the items.</param>
    private string extractItemsSelected(List<Vector3> itemClicks)
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

    // add data that needs to be saved to the queue for saving to DHive
    private async void SendData(List<OutputParameter> outputs)
    {
        // var isConnected = sender.TimeSinceLastPingSeconds < 10;
        Debug.Log("Saving: " + string.Join(", ", outputs.Select(o => $"{o.Name}: {o.Value}")));
        DataSaver.PrepareToSave(outputs, "Knapsack");
    }

    // Call this from anywhere (like BoardManager) if a critical error occurs
    public void HandleCriticalError(string errorDetails)
    {
        Debug.LogError("Critical Knapsack Error: " + errorDetails);
        
        // Save the error so you know why this trial is missing data
        // We pass 0 for submitted/time because the trial was aborted
        PrepareOutput("", 2, 0, errorDetails, new List<Vector3>(), practice_output);
        
        // Force skip to the next trial
        NextTrial();
    }

    public override void ResetCounters() 
    {
        block = 1;
        trial = 0;
    }
    public override int total_complex_instances 
    { 
        get { return numberOfInstances; } 
    }
}