// dass, Fatigue, stai-t is broken
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Dhive;
using Random = System.Random;
using System.Text.RegularExpressions;

using System.IO;

// any comments are from karlo changes

public enum LoadingType
{
    ExperimentData = 0,
    SessionData = 1
}

public class DataLoader : MonoBehaviour
{
    // for the pilot sessions:
    // Ageing: e3723d66-83ac-443f-8f7c-cf2af89aef64
    // Chronic: e48498dc-7015-4b8b-81fa-061df10b7c64

    // session ID from preload data and dataloadtrial scenes: pablo: "3d3372fc-ad1e-4e29-91cb-79a639b5525d";  ageing: bd87d5d7-0eaa-4803-93b1-4d2087b2e10a    chronic stress: 97fcce4f-75a4-43a2-bf6d-d91d2cd38753
    private static readonly string ExperimentId = "9b7e32f4-1d9e-47d6-9fea-96eeac7aee8a"; // pablo's exp ID "99630040-71cd-4d89-acd8-3621829b447d";  ageing: 2ac47f83-a59e-4357-965a-70116abe0d23;   chronic stress: 9b7e32f4-1d9e-47d6-9fea-96eeac7aee8a

    [SerializeField] public string sessionID = "";
    [SerializeField] public LoadingType typeOfData;

    private DhiveReader _reader;
    private Text _textObject;
    private bool _completed;
    public static string var_name_prefix;

    private async void Start()
    {
        _textObject = GameObject.Find("ScreenText").GetComponent<Text>();

        // public param in the scene says load experiment data, load that
        if (typeOfData == LoadingType.ExperimentData)
        {
            await LoadDatabaseExperimentParameters();
        }
        // otherwise if it's session data (e.g. knapsack item params), load that
        else if (typeOfData == LoadingType.SessionData)
        {
            await LoadSessionTrialData();
        }
        _completed = true;
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        GameManager.sender?.DispatchMessageQueue();
#endif

        if (!_completed) return;

        switch (typeOfData)
        {
            case LoadingType.ExperimentData:
                SceneManager.LoadScene("Setup");
                break;
            case LoadingType.SessionData:
                SceneManager.LoadScene("WebsocketConnection");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task CreateTrialCogTask()
    {
        _reader = new DhiveReader(ExperimentId);

        var parameters = new List<OutputParameter>
        {
            new ("pID", GameManager.participantID)
        };
        foreach (var parameter in parameters)
        {
            Debug.Log("Parameter: " + parameter.Name + " - " + parameter.Value);
        }
        var newTrial = await _reader.CreateTrial(sessionID, GameManager.participantID, parameters);
        GameManager.participantTrialId = newTrial?.Id;
    }

    // helper function 
    string DecodeUnicodeEscapeSequences(string input)
    {
        return Regex.Replace(input, @"\\u([0-9A-Fa-f]{4})", match =>
        {
            return ((char)Convert.ToInt32(match.Groups[1].Value, 16)).ToString();
        });
    }

    // helper function to remove quotes and spaces from a list of strings
    private List<string> RemoveQuotesAndSpaces(List<string> inputList, bool removeSpaces = true)
    {
        for (var i = 0; i < inputList.Count; i++)
        {
            inputList[i] = inputList[i].Replace("\"", "");
            inputList[i] = inputList[i].Replace("\'", "");
            // if the very first character is a space, remove it
            if (inputList[i][0] == ' ')
            {
                inputList[i] = inputList[i].Substring(1);
            }

            if (removeSpaces)
            {
                inputList[i] = inputList[i].Replace(" ", "");
            }
        }

        return inputList;
    }

    // load the params for the digit symbol task from DHive
    private async Task LoadDigitSymbol(Dhive.ExperimentTask task)
    {
        SymbolDigitGM.digits = task.Parameters.GetStringListParameter($"i_digits");
        SymbolDigitGM.item_n = task.Parameters.GetIntParameter($"i_item_n");
        SymbolDigitGM.time_limit = task.Parameters.GetIntParameter($"i_time_limit");
        SymbolDigitGM.practice_time = (float)task.Parameters.GetIntParameter($"i_practice_time");
        SymbolDigitGM.symbolCues = task.Parameters.GetStringListParameter($"i_symbol_cues");

        List<string> symbolCues = new List<string>();
        foreach (var unicodeString in SymbolDigitGM.symbolCues)
        {
            // Convert the unicode escape sequence to the actual symbol
            string symbol = DecodeUnicodeEscapeSequences(unicodeString);
            symbolCues.Add(symbol);
        }
        SymbolDigitGM.symbolCues = symbolCues;
        symbolCues = null;
        SymbolDigitGM.digits = RemoveQuotesAndSpaces(SymbolDigitGM.digits);
        SymbolDigitGM.symbolCues = RemoveQuotesAndSpaces(SymbolDigitGM.symbolCues);
    }

    // load the params for the stop signal task from DHive
    private async Task LoadStopSignal(Dhive.ExperimentTask task)
    {
        StopSignal.no_real_instances = task.Parameters.GetIntParameter($"i_no_instances");
        StopSignal.real_blocks = task.Parameters.GetIntParameter($"i_no_blocks");
        StopSignal.time_limit = task.Parameters.GetIntParameter($"i_time_limit");
        StopSignal.no_practice_instances = task.Parameters.GetIntParameter($"i_no_practice_instances");
        StopSignal.init_stop_delay = (float)task.Parameters.GetDoubleParameter($"i_init_stop_delay");
        StopSignal.delta_stop_delay = (float)task.Parameters.GetDoubleParameter($"i_delta_stop_delay");
        StopSignal.feedback_time = (float)task.Parameters.GetIntParameter($"i_feedback_time");
        StopSignal.rest_time = (float)task.Parameters.GetIntParameter($"i_rest_time");
    }

    // load the params for the recall-1-back task from DHive
    private async Task LoadR1B(Dhive.ExperimentTask task)
    {
        NBack.time1 = (float)task.Parameters.GetDoubleParameter($"i_time_1");
        NBack.time2 = (float)task.Parameters.GetDoubleParameter($"i_time_2");
        NBack.time3 = (float)task.Parameters.GetDoubleParameter($"i_time_3");
        NBack.real_block1 = task.Parameters.GetIntParameter($"i_block_1");
        NBack.real_block2 = task.Parameters.GetIntParameter($"i_block_2");
        NBack.real_block3 = task.Parameters.GetIntParameter($"i_block_3");
        NBack.practice_block1 = task.Parameters.GetIntParameter($"i_practice_block_1");
        NBack.practice_block2 = task.Parameters.GetIntParameter($"i_practice_block_2");
        NBack.practice_block3 = task.Parameters.GetIntParameter($"i_practice_block_3");
        NBack.digits = task.Parameters.GetStringListParameter($"i_digits");

        NBack.digits = RemoveQuotesAndSpaces(NBack.digits);
    }

    // load the params for the letters and numbers task from DHive
    private async Task LoadTaskSwitch(Dhive.ExperimentTask task)
    {
        TaskSwitching.time_limit = (float)task.Parameters.GetIntParameter($"i_time_limit");
        TaskSwitching.rule_display_time = (float)task.Parameters.GetIntParameter($"i_rule_display_time");
        TaskSwitching.real_block1 = task.Parameters.GetIntParameter($"i_block_1");
        TaskSwitching.real_block2 = task.Parameters.GetIntParameter($"i_block_2");
        TaskSwitching.real_block3 = task.Parameters.GetIntParameter($"i_block_3");
        TaskSwitching.practice_block1 = task.Parameters.GetIntParameter($"i_practice_block_1");
        TaskSwitching.practice_block2 = task.Parameters.GetIntParameter($"i_practice_block_2");
        TaskSwitching.practice_block3 = task.Parameters.GetIntParameter($"i_practice_block_3");
        TaskSwitching.rest_time = (float)task.Parameters.GetIntParameter($"i_rest_time");
        TaskSwitching.practice_stimulus_list = task.Parameters.GetStringListParameter($"i_practice_list");
        TaskSwitching.real_stimulus_list = task.Parameters.GetStringListParameter($"i_switch_congruence_{GameManager.congruence_type}_random_{GameManager.randomizationID}");
        
        TaskSwitching.real_stimulus_list = RemoveQuotesAndSpaces(TaskSwitching.real_stimulus_list);
        TaskSwitching.practice_stimulus_list = RemoveQuotesAndSpaces(TaskSwitching.practice_stimulus_list);
    }

    // load the params for the ICAR task from DHive
    private async Task LoadICAR(Dhive.ExperimentTask task)
    {
        ICAR.time_per_question = (float)task.Parameters.GetDoubleParameter($"i_time_per_question");
        ICAR.is_progressive = task.Parameters.GetStringParameter($"i_is_progressive").ToLower();
        string temp = task.Parameters.GetStringParameter($"i_matrices_only");
        string icar_prefix;
        if (temp.ToLower() == "true")
        {
            ICAR.matrices_only = true;
            icar_prefix = "i_matrices_only_";
        }
        else
        {
            ICAR.matrices_only = false;
            icar_prefix = "i_full_sample_";
        }
        if (ICAR.matrices_only)
        {
            ICAR.total_instances = task.Parameters.GetIntParameter($"i_instance_number_matrices");
        }
        else
        {
            ICAR.total_instances = task.Parameters.GetIntParameter($"i_instance_number_full");
        }

        string[] var_name_suffixes = new string[] { "QuestionID", "hasImage", "QuestionPrompt", "Choices", "CorrectChoiceIndex" };

        // for each integer in instance list length
        for (int i = 1; i < ICAR.total_instances + 1; i++)
        {
            string key = "Question" + i.ToString();
            // add key to the ICAR.loadedData dictionary
            ICAR.i_loadedData[key] = new Dictionary<string, string>();
            ICAR.i_loadedChoices[key] = new List<string> { }; // list of instances to show, taken from input;

            for (int j = 0; j < var_name_suffixes.Length; j++)
            {
                string param_name = icar_prefix + key.ToString() + "_" + var_name_suffixes[j];
                string subkey = var_name_suffixes[j];
                string value = null;
                if (var_name_suffixes[j] == "Choices")
                {
                    List<string> temp2 = task.Parameters.GetStringListParameter($"{param_name}");
                    temp2 = RemoveQuotesAndSpaces(temp2, false);
                    ICAR.i_loadedChoices[key] = temp2;
                }
                else if (var_name_suffixes[j] == "CorrectChoiceIndex")
                {
                    value = task.Parameters.GetIntParameter($"{param_name}").ToString();
                    ICAR.i_loadedData[key][subkey] = value;
                }
                else
                {
                    value = task.Parameters.GetStringParameter($"{param_name}");
                    ICAR.i_loadedData[key][subkey] = value;
                }
            }
        }
    }

    // helper function to process the subquestions for the questionnaires
    private List<string> ProcessSubQuestions(List<string> temp)
    {
        string temp2 = string.Join(", ", temp);
        temp2 = temp2.Replace("  ", " ");
        temp2 = temp2.Replace(", 0", ",0");

        string pattern = "(?<!\\\\)['\'](.*?)(?<!\\\\)['\']";
        Regex regex = new Regex(pattern);
        MatchCollection matches = regex.Matches(temp2);

        temp = new List<string>();
        foreach (Match match in matches)
        {
            string element = match.Value.Trim().Substring(1, match.Value.Length - 2);
            element = element.Replace("\\", ""); // if element contains a \ character, delete it
            temp.Add(element);
        }

        return temp;
    }

    // load the questionnaire params from DHive into a dictionary
    private void populateQuestionnaireDictionaries(Dhive.ExperimentTask task, string key, string var_name_prefix, string[] var_name_suffixes)
    {
        Debug.Log("Key: " + key);  // which questionnaire we're on
        Debug.Log("Var name prefix: " + var_name_prefix);  // whether it's from ageing or chronic stress
        Debug.Log("Var name suffixes: " + string.Join(", ", var_name_suffixes));  // which field of the questionnaire we're on

        Questionnaire.i_questionnaire_data[key] = new Dictionary<string, string>();   // dictionary for the questionnaire data
        Questionnaire.i_sub_questions[key] = null;  // list of subquestions (the statements they have to respond to) for this questionnaire
        Questionnaire.i_choices[key] = null;  // list of choices (the answers they select from) for this questionnaire

        // pre-process the data and then populate the lists and dictionaries
        for (int j = 0; j < var_name_suffixes.Length; j++)
        {
            string param_name = var_name_prefix + key.ToString() + "_" + var_name_suffixes[j];
            string subkey = var_name_suffixes[j];
            string value = null;
            if (var_name_suffixes[j] == "Choices")
            {
                List<string> temp = task.Parameters.GetStringListParameter($"{param_name}");
                Debug.Log("Choices pre-removal: " + string.Join(", ", temp));
                temp = RemoveQuotesAndSpaces(temp, false);
                Debug.Log("Choices post-removal: " + string.Join(", ", temp));
                Questionnaire.i_choices[key] = temp;
            }
            else if (var_name_suffixes[j] == "SubQuestions")
            {
                List<string> temp = task.Parameters.GetStringListParameter($"{param_name}");
                Debug.Log("Sub questions pre-removal: " + string.Join(", ", temp));
                temp = ProcessSubQuestions(temp);
                Debug.Log("Sub questions post-removal: " + string.Join(", ", temp));
                if (key == "QuestionLast")
                {
                    // do nothing
                }
                else
                {
                    Questionnaire.i_sub_questions[key] = temp;
                }
            }
            else
            {
                value = task.Parameters.GetStringParameter($"{param_name}");
                // if value is null, empty, or is just a space, add an empty string to the dictionary
                if (value == null || value == "" || value == " ")
                {
                    // do nothing
                }
                else
                {
                    Questionnaire.i_questionnaire_data[key][subkey] = value;
                }
            }
        }
        // Debug.Log("Checking dict.");
        // foreach (KeyValuePair<string, Dictionary<string, string>> entry in Questionnaire.i_questionnaire_data)
        // {
        //     Debug.Log("Key: " + entry.Key);
        //     // for each subkey in Questionnaire.i_questionnaire_data[key]
        //     foreach (KeyValuePair<string, string> subentry in entry.Value)
        //     {
        //         Debug.Log("Subkey: " + subentry.Key + " Value: " + subentry.Value);
        //     }
        // }
        // Debug.Log("End check");
    }

    // load the params for the questionnaire task from DHive
    private async Task LoadQuest(Dhive.ExperimentTask task)
    {
        Questionnaire.number_of_starting_questions = task.Parameters.GetIntParameter($"i_num_starter_questionnaires");
        int num_real_questions = task.Parameters.GetIntParameter($"i_num_real_questions");
        Questionnaire.numberOfQuestionnaires = Questionnaire.number_of_starting_questions + num_real_questions + 1; // +1 for the last questionnaire
        var_name_prefix = task.Parameters.GetStringParameter($"i_var_name_prefix");

        string[] var_name_suffixes = new string[] { "QuestionID", "MainQuestion", "HasTextInput", "InputMustBeInt", "TextQuestion", "Choices", "SubQuestions" };
        string key = null;

        for (int i = 1; i < Questionnaire.number_of_starting_questions + 1; i++)
        {
            Debug.Log("StarterQuestion" + i.ToString());
            key = "StarterQuestion" + i.ToString();
            populateQuestionnaireDictionaries(task, key, var_name_prefix, var_name_suffixes);
            Questionnaire.questionnaireInstances.Add(key);
        }

        for (int i = 1; i < num_real_questions + 1; i++)
        {
            Debug.Log("RealQuestion" + i.ToString());
            key = "Question" + i.ToString();
            populateQuestionnaireDictionaries(task, key, var_name_prefix, var_name_suffixes);
            Questionnaire.real_questions.Add(key);
        }
        // randomly shuffle the list of real questions
        System.Random random = new System.Random();
        for (int i = Questionnaire.real_questions.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            string temp = Questionnaire.real_questions[i];
            Questionnaire.real_questions[i] = Questionnaire.real_questions[j];
            Questionnaire.real_questions[j] = temp;
        }
        foreach (string question in Questionnaire.real_questions)
        {
            Questionnaire.questionnaireInstances.Add(question);
        }
        Debug.Log("Questionnaire order: " + string.Join(", ", Questionnaire.questionnaireInstances));
        key = "QuestionLast";
        populateQuestionnaireDictionaries(task, key, var_name_prefix, var_name_suffixes);
        Questionnaire.questionnaireInstances.Add(key);

        // // for each key in Questionnaire.i_questionnaire_data
        // foreach (KeyValuePair<string, Dictionary<string, string>> entry in Questionnaire.i_questionnaire_data)
        // {
        //     Debug.Log("Key: " + entry.Key);
        // // for each subkey in Questionnaire.i_questionnaire_data[key]
        // foreach (KeyValuePair<string, string> subentry in entry.Value)
        // {
        //     Debug.Log("Subkey: " + subentry.Key + " Value: " + subentry.Value);
        // }
        // Debug.Log("finished main dict" );
        // foreach (string subentry in Questionnaire.i_choices[entry.Key])
        // {
        //     Debug.Log("Choice: " + subentry);
        // }
        // Debug.Log("finished choices" );
        // foreach (string subentry in Questionnaire.i_sub_questions[entry.Key])
        // {
        //     Debug.Log("Subquestion: " + subentry);
        // }
        // Debug.Log("finished sub questions" );
        // }
    }

    // cycle through the list of tasks and load the params for each one
    private async Task LoadTaskParams()
    {
        foreach (var task in GameManager.experimentData.Tasks)
        {
            if (task.Name == "Digit Symbol Substitution")
            {
                await LoadDigitSymbol(task);
            }
            else if (task.Name == "Stop Signal Task")
            {
                await LoadStopSignal(task);
            }
            else if (task.Name == "Recall-1-back")
            {
                await LoadR1B(task);
            }
            else if (task.Name == "Letters and Numbers task")
            {
                await LoadTaskSwitch(task);
            }
            else if (task.Name == "ICAR")
            {
                await LoadICAR(task);
            }
            else if (task.Name == "Ageing_questionnaires")
            {
                await LoadQuest(task);
            }
            else if (task.Name == "Knapsack optimisation task")
            {
                await LoadKP(task, GameManager.experimentData);
            }
        }
    }

    // load the experiment-level params from the database and store them in GameManager variables
    private async Task LoadDatabaseExperimentParameters()
    {
        // Based on `assignVariables`
        _reader = new DhiveReader(ExperimentId);

        // get the experiment data from the database
        GameManager.experimentData = await _reader.GetExperiment();

        // if the experiment data is null, show an error message
        if (GameManager.experimentData == null)
        {
            _textObject.text = "Experiment cannot be fetched from the server.";
            Debug.LogError("[DHive Reader] Experiment cannot be fetched");
            return;
        }

        Debug.Log("[DHive Reader] Experiment Found. " + GameManager.experimentData.Id);
        _completed = true;
    }

    // load the session-level params from the database, namely the randomisation ID, the instance ordering, and the params for each instance of the KS
    private async Task LoadSessionTrialData()
    {
        _reader = new DhiveReader(ExperimentId);

        var session = await _reader.GetSession(sessionID, GameManager.participantID);

        // if the session data is null, show an error message
        if (session == null)
        {
            _textObject.text = "Session cannot be fetched from the server.";
            Debug.LogError("[DHive Reader] Session cannot be fetched");
            return;
        }

        // if the participant has logged in before, go back to the setup scene
        if(GameManager.ban_repeat_logins)
        {
            if (session.Trials.Count > 0)
            {
                Debug.Log("Participant has logged in before.");
                ParticipantAlreadyLoggedIn();
                return;
            }
        }
        
        // assigns a randomisation ID that hasn't been used yet and generates the instance ordering
        GameManager.randomizationID = await GetLowestAvailableRandomID(session);
        
        // assign the congruence type and whether the letter is on the left or right
        GameManager.congruence_type = GameManager.randomizationID % 4;
        if (GameManager.congruence_type % 2 == 0)
        {
            GameManager.letter_on_left = true;
        }
        else
        {
            GameManager.letter_on_left = false;
        }

        //// TODO: if DataSaver is working correctly with local deployment, then this commented out section can be deleted.
        // // create a new trial in the database
        // GameManager.participantTrialId = await CreateNewTrial(GameManager.randomizationID);
        // DataSaver.AddParticipantTrialId(GameManager.participantTrialId);
        // DataSaver.Init();

        // seems this conditional statement is needed for local deployment. 
        if (GameManager.participantID != "Empty")
        {
            // create a new trial in the database: session x participant
            GameManager.participantTrialId = await CreateNewTrial(GameManager.randomizationID);
            Debug.Log("Participant Trial ID: " + GameManager.participantTrialId);
            DataSaver.AddParticipantTrialId(GameManager.participantTrialId);
            DataSaver.Init();
        }
        else
        {
            Debug.Log("Participant ID has not been set.");
        }

        // load task parameters
        await LoadTaskParams();
        _completed = true;
    }

    // check if the websocket connection is established (not sure if this works)
    public static bool VerifyWebsocketConnection()
    {
        if (GameManager.sender.IsConnected)
        {
            Debug.Log("[DHive Sender] Websocket Connection is established.");
            return true;
        }
        else
        {
            Debug.Log("[DHive Sender] Websocket Connection is not established.");
            return false;
        }

        // _textObject.text = "Connection to remote server is not established. Please try again later or seek assistance.";
        // Debug.LogError("[DHive Sender] Websocket Connection is not established.");
    }

    // if the participant has logged in before, go back to the setup scene
    private void ParticipantAlreadyLoggedIn()
    {
        GameManager.logged_in_before = true;
        SceneManager.LoadScene("Setup");
    }

    // // assigns a randomisation ID that hasn't been used yet, returns that value, and generates the instance ordering
    // private int AssignRandomization(Session session)
    // {
    //     // get the list of randomisations that haven't been assigned to a participant yet
    //     var availableRandomizations = ReadRandomizations(session);

    //     Debug.Log("Available Randomizations count: " + availableRandomizations.Count);

    //     // pick one of the available randomisations
    //     var selected = SelectRandomization(availableRandomizations);

    //     // generate the instance ordering based on the selected randomisation
    //     LoadRandomizationToGame(session, selected);
    //     return selected;
    // }

    // // returns the list of randomisations that haven't been assigned to a participant yet
    // private List<int> ReadRandomizations(Session session)
    // {
    //     var result = new List<int>();

    //     // load all the randomisation IDs from the session
    //     var allRIDs = session.Parameters.GetIntListParameter("all_rIDs");

    //     // go through every randomisation and store each of those that haven't been assigned to a participant yet
    //     foreach (var rID in allRIDs)
    //     {
    //         var assigned = session.Parameters.GetIntParameter($"assigned_rID_{rID}", defaultValue: 0);

    //         if (assigned == 0)
    //         {
    //             result.Add(rID);
    //         }
    //     }

    //     return result;
    // }

    // finds the lowest available randomisation ID that hasn't been assigned to a participant yet
    public async Task<int> GetLowestAvailableRandomID(Session session)
    {
        var allRIDs = session.Parameters.GetIntListParameter("all_rIDs");

        // vars to keep track
        int highest_available_global = allRIDs[allRIDs.Count - 1] + 1; // No IDs above this point are available
        int lowest_unavailable_global = allRIDs[0];

        // vars for iterating
        int lowest_unavailable = allRIDs[0] - 1; // No IDs below this point are unavailable
        int highest_available = allRIDs[allRIDs.Count - 1] + 1; // No IDs above this point are available
        int lowest_available = allRIDs[allRIDs.Count - 1] + 1; // Smallest available rID found so far

        while (lowest_unavailable < highest_available - 1)
        {
            // Calculate the mid-point
            int mid = (lowest_unavailable + highest_available) / 2;
            //Debug.Log($"Checking assigned_rID_{mid}");

            // Query the rID for availability
            var assigned = session.Parameters.GetIntParameter($"assigned_rID_{mid}", defaultValue: 0);

            if (assigned == 0)
            {
                // Mid rID is available
                lowest_available = mid;
                highest_available = mid;  // Now search lower
                //Debug.Log($"Found available rID at {mid}. Setting highest_available to {highest_available}.");
            }
            else
            {
                // Mid rID is unavailable
                lowest_unavailable = mid;  // Now search higher
                Debug.Log($"rID {mid} is unavailable. Setting lowest_unavailable to {lowest_unavailable}.");
            }
        }

        var assigned_check = session.Parameters.GetIntParameter($"assigned_rID_{lowest_available}", defaultValue: 0);
        if (lowest_available < highest_available_global && assigned_check != 1)
        {
            Debug.Log($"lowest available rID is {lowest_available}");
            return lowest_available;  // Return the smallest available rID
        }

        // If no available rID was found
        throw new Exception("No available rID found.");
    }


    // // select a random randomisation from the list of available randomisations
    // private int SelectRandomization(IReadOnlyList<int> randomizations)
    // {
    //     var rand = new Random();

    //     Debug.Log("Randomizations count: " + randomizations.Count);

    //     return randomizations[rand.Next(randomizations.Count)];
    // }

    // // based on the session and the selected randomisation, assign some params to GameManager variables, including the instance ordering
    // private void LoadRandomizationToGame(Session session, int selected)
    // {
    //     // selected is a randomisation ID
    //     var randomization = session.Parameters.GetIntListParameter($"r{selected}_instanceRandomization");

    //     GameManager.instanceRandomization = new int[randomization.Count];
    //     for (var i = 0; i < randomization.Count; i++)
    //     {
    //         GameManager.instanceRandomization[i] = randomization[i] - 1;
    //     }

    //     //Debug.Log("Instance Randomization: " + string.Join(", ", GameManager.instanceRandomization));
    // }

    // create a new trial in the database
    private async Task<string> CreateNewTrial(int randomizationId)
    {
        // store the randomisation and participant IDs for later output
        var parameters = new List<OutputParameter>
        {
            new ("rID", randomizationId),
            new ("pID", GameManager.participantID)
        };
        Debug.Log("Participant ID: " + GameManager.participantID);
        Debug.Log("Randomization ID: " + randomizationId);

        // create a new trial in the database
        var newTrial = await _reader.CreateTrial(sessionID, GameManager.participantID, parameters);

        Debug.Log($"New Trial ID: {newTrial?.Id}");

        return newTrial?.Id;
    }

    // loads the parameters for each instance of the KS
    private async Task LoadKP(Dhive.ExperimentTask task, Experiment experiment)
    {
        GameManager.timeRest1min = Convert.ToSingle(task.Parameters.GetIntParameter("timeRest1min"));
        GameManager.timeRest1max = Convert.ToSingle(task.Parameters.GetIntParameter("timeRest1max"));
        GameManager.timeRest2 = Convert.ToSingle(task.Parameters.GetIntParameter("timeRest2"));
        GameManager.timeTrial = Convert.ToSingle(task.Parameters.GetIntParameter("timeTrial"));
        GameManager.timeOnlyItems = Convert.ToSingle(task.Parameters.GetIntParameter("timeOnlyItems"));
        GameManager.numberOfTrials = task.Parameters.GetIntParameter($"numberOfTrials", -1);
        GameManager.numberOfBlocks = task.Parameters.GetIntParameter($"numberOfBlocks", -1);
        GameManager.numberOfInstances = task.Parameters.GetIntParameter($"numberOfInstances", -1);

        var randomization = task.Parameters.GetIntListParameter($"r{GameManager.randomizationID}_instanceRandomization");  

        GameManager.instanceRandomization = new int[randomization.Count];
        for (var i = 0; i < randomization.Count; i++)
        {
            GameManager.instanceRandomization[i] = randomization[i] - 1;
        }

        // generate the KS instances
        GameManager.ksinstances = new GameManager.KSInstance[GameManager.numberOfInstances];
        GameManager.TaskId = GetDatabaseTaskId(experiment, "Knapsack Optimisation");

        // for each KS param for each instance, assign the values to the GameManager variables
        for (var k = 1; k <= GameManager.numberOfInstances; k++)
        {
            var weightsS = task.Parameters.GetIntListParameter($"i{k}__weights");
            var valuesS = task.Parameters.GetIntListParameter($"i{k}__values");
            var capacityS = task.Parameters.GetIntParameter($"i{k}__capacity");
            var capacityOptS = task.Parameters.GetIntParameter($"i{k}__capacityAtOptimum");
            var profitOptS = task.Parameters.GetIntParameter($"i{k}__profitAtOptimum");
            var itemsOptS = task.Parameters.GetIntListParameter($"i{k}__solutionItems");

            GameManager.ksinstances[k - 1].weights = weightsS.ToArray();
            GameManager.ksinstances[k - 1].values = valuesS.ToArray();
            GameManager.ksinstances[k - 1].capacity = capacityS;
            GameManager.ksinstances[k - 1].capacityOpt = capacityOptS;
            GameManager.ksinstances[k - 1].profitOpt = profitOptS;
            GameManager.ksinstances[k - 1].itemsOpt = itemsOptS.ToArray();

            GameManager.ksinstances[k - 1].id = task.Parameters.GetStringParameter($"i{k}__problemID");
            GameManager.ksinstances[k - 1].type = task.Parameters.GetStringParameter($"i{k}__instanceType");
            GameManager.ksinstances[k - 1].expAccuracy = (float)task.Parameters.GetDoubleParameter($"i{k}__expAccuracy");
        }
    }

    // find the ID for the task in question from the database
    public static string GetDatabaseTaskId(Experiment experiment, string taskName)
    {
        foreach (var task in experiment.Tasks.Where(task => task.Name.Equals(taskName)))
        {
            return task.Id;
        }

        return "";
    }
}




