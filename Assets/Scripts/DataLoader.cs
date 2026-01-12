// dass, Fatigue, stai-t is broken


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
using System.Diagnostics;
using Debug = UnityEngine.Debug;

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
    private static readonly string ExperimentId = "76f4e5c5-975b-4220-aa5a-1ec4f61f383a"; // pablo's exp ID "99630040-71cd-4d89-acd8-3621829b447d";  ageing: 2ac47f83-a59e-4357-965a-70116abe0d23;   chronic stress: 9b7e32f4-1d9e-47d6-9fea-96eeac7aee8a
    
    // file names for the different tasks
    string file_ds;
    string file_ss;
    string file_r1b;
    string file_ts;
    string file_icar;
    string file_quest;
    string file_ks;

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

    // load the params for the digit symbol task
    private async Task LoadDigitSymbol(Dhive.ExperimentTask task)
    {
        if (GameManager.load_read_locally)
        {
            var p = ReadCSVParams(file_ds);
            SymbolDigitGM.digits = ParseLocalStringList(p["i_digits"]);
            SymbolDigitGM.item_n = int.Parse(p["i_item_n"]);
            SymbolDigitGM.time_limit = float.Parse(p["i_time_limit"]); // changed to float to be safe
            SymbolDigitGM.practice_time = float.Parse(p["i_practice_time"]);
            SymbolDigitGM.symbolCues = ParseLocalStringList(p["i_symbol_cues"]);
        }
        else
        {
            SymbolDigitGM.digits = task.Parameters.GetStringListParameter($"i_digits");
            SymbolDigitGM.item_n = task.Parameters.GetIntParameter($"i_item_n");
            SymbolDigitGM.time_limit = task.Parameters.GetIntParameter($"i_time_limit");
            SymbolDigitGM.practice_time = (float)task.Parameters.GetIntParameter($"i_practice_time");
            SymbolDigitGM.symbolCues = task.Parameters.GetStringListParameter($"i_symbol_cues");
        }

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

    // load the params for the stop signal task
    private async Task LoadStopSignal(Dhive.ExperimentTask task)
    {
        if (GameManager.load_read_locally)
        {
            var p = ReadCSVParams(file_ss);
            StopSignal.no_real_instances = int.Parse(p["i_no_instances"]);
            StopSignal.real_blocks = int.Parse(p["i_no_blocks"]);
            StopSignal.time_limit = float.Parse(p["i_time_limit"]);
            StopSignal.no_practice_instances = int.Parse(p["i_no_practice_instances"]);
            StopSignal.init_stop_delay = float.Parse(p["i_init_stop_delay"]);
            StopSignal.delta_stop_delay = float.Parse(p["i_delta_stop_delay"]);
            StopSignal.feedback_time = float.Parse(p["i_feedback_time"]);
            StopSignal.rest_time = float.Parse(p["i_rest_time"]);
        }
        else
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
    }

    // load the params for the recall-1-back task
    private async Task LoadR1B(Dhive.ExperimentTask task)
    {
        if( GameManager.load_read_locally)
        {
            var p = ReadCSVParams(file_r1b);
            NBack.time1 = float.Parse(p["i_time_1"]);
            NBack.time2 = float.Parse(p["i_time_2"]);
            NBack.time3 = float.Parse(p["i_time_3"]);
            NBack.real_block1 = int.Parse(p["i_block_1"]);
            NBack.real_block2 = int.Parse(p["i_block_2"]);
            NBack.real_block3 = int.Parse(p["i_block_3"]);
            NBack.practice_block1 = int.Parse(p["i_practice_block_1"]);
            NBack.practice_block2 = int.Parse(p["i_practice_block_2"]);
            NBack.practice_block3 = int.Parse(p["i_practice_block_3"]);
            NBack.digits = ParseLocalStringList(p["i_digits"]);
        }
        else
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
        }

        NBack.digits = RemoveQuotesAndSpaces(NBack.digits);
    }

    // load the params for the letters and numbers task
    private async Task LoadTaskSwitch(Dhive.ExperimentTask task)
    {
        if( GameManager.load_read_locally)
        {
            var p = ReadCSVParams(file_ts);
            TaskSwitching.time_limit = float.Parse(p["i_time_limit"]);
            TaskSwitching.rule_display_time = float.Parse(p["i_rule_display_time"]);
            TaskSwitching.real_block1 = int.Parse(p["i_block_1"]);
            TaskSwitching.real_block2 = int.Parse(p["i_block_2"]);
            TaskSwitching.real_block3 = int.Parse(p["i_block_3"]);
            TaskSwitching.practice_block1 = int.Parse(p["i_practice_block_1"]);
            TaskSwitching.practice_block2 = int.Parse(p["i_practice_block_2"]);
            TaskSwitching.practice_block3 = int.Parse(p["i_practice_block_3"]);
            TaskSwitching.rest_time = float.Parse(p["i_rest_time"]);
            
            TaskSwitching.practice_stimulus_list = ParseLocalStringList(p["i_practice_list"]);
            TaskSwitching.real_stimulus_list = ParseLocalStringList(p[$"i_switch_congruence_{GameManager.congruence_type}_random_{GameManager.randomizationID}"]);
        }
        else
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
        }
        
        TaskSwitching.real_stimulus_list = RemoveQuotesAndSpaces(TaskSwitching.real_stimulus_list);
        TaskSwitching.practice_stimulus_list = RemoveQuotesAndSpaces(TaskSwitching.practice_stimulus_list);
    }

    // load the params for the ICAR task 
    private async Task LoadICAR(Dhive.ExperimentTask task)
    {
        // Store input params here regardless of source
        Dictionary<string, string> localParams = null;

        if (GameManager.load_read_locally)
        {
            localParams = ReadCSVParams(file_icar);
        }

        // global params
        if (GameManager.load_read_locally)
        {
            ICAR.time_per_question = float.Parse(localParams["i_time_per_question"]);
            ICAR.is_progressive = localParams["i_is_progressive"].ToLower();
            
            string temp = localParams["i_matrices_only"];
            if (temp.ToLower() == "true")
            {
                ICAR.matrices_only = true;
            }
            else
            {
                ICAR.matrices_only = false;
            }
        }
        else
        {
            ICAR.time_per_question = (float)task.Parameters.GetDoubleParameter($"i_time_per_question");
            ICAR.is_progressive = task.Parameters.GetStringParameter($"i_is_progressive").ToLower();
            string temp = task.Parameters.GetStringParameter($"i_matrices_only");
            
            if (temp.ToLower() == "true")
            {
                ICAR.matrices_only = true;
            }
            else
            {
                ICAR.matrices_only = false;
            }
        }

        string icar_prefix;
        if (ICAR.matrices_only)
        {
            icar_prefix = "i_matrices_only_";
            if (GameManager.load_read_locally)
            {
                ICAR.total_instances = int.Parse(localParams["i_instance_number_matrices"]);
            }
            else
            {
                ICAR.total_instances = task.Parameters.GetIntParameter($"i_instance_number_matrices");
            }
        }
        else
        {
            icar_prefix = "i_full_sample_";
            if (GameManager.load_read_locally)
            {
                ICAR.total_instances = int.Parse(localParams["i_instance_number_full"]);
            }
            else
            {
                ICAR.total_instances = task.Parameters.GetIntParameter($"i_instance_number_full");
            }
        }
        
        // populate dictionaries
        string[] var_name_suffixes = new string[] { "QuestionID", "hasImage", "QuestionPrompt", "Choices", "CorrectChoiceIndex" };

        for (int i = 1; i < ICAR.total_instances + 1; i++)
        {
            string key = "Question" + i.ToString();
            
            // Initialize the inner dictionary for this question
            if (!ICAR.i_loadedData.ContainsKey(key))
            {
                ICAR.i_loadedData[key] = new Dictionary<string, string>();
            }
            
            // Initialize the list for choices
            ICAR.i_loadedChoices[key] = new List<string> { };

            for (int j = 0; j < var_name_suffixes.Length; j++)
            {
                string subkey = var_name_suffixes[j];
                string param_name = icar_prefix + key.ToString() + "_" + subkey;
                
                if (GameManager.load_read_locally)
                {
                    // --- LOCAL READING LOGIC ---
                    // Check if key exists to prevent crashing on malformed CSVs
                    if (localParams.ContainsKey(param_name))
                    {
                        string rawValue = localParams[param_name];

                        if (subkey == "Choices")
                        {
                            // Use our helper to parse the string "[A, B, C]" into a List
                            List<string> temp2 = ParseLocalStringList(rawValue);
                            ICAR.i_loadedChoices[key] = temp2;
                        }
                        else if (subkey == "CorrectChoiceIndex")
                        {
                            // CSV is already string, just store it directly
                            ICAR.i_loadedData[key][subkey] = rawValue;
                        }
                        else
                        {
                            // Strings like QuestionID, hasImage, QuestionPrompt
                            ICAR.i_loadedData[key][subkey] = rawValue;
                        }
                    }
                }
                else
                {
                    // --- SERVER READING LOGIC (Original) ---
                    string value = null;
                    if (subkey == "Choices")
                    {
                        List<string> temp2 = task.Parameters.GetStringListParameter($"{param_name}");
                        temp2 = RemoveQuotesAndSpaces(temp2, false);
                        ICAR.i_loadedChoices[key] = temp2;
                    }
                    else if (subkey == "CorrectChoiceIndex")
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

    // helper function when loading Local (CSV) and Server (DHive) data for questionnaires
    private void populateQuestionnaireDictionaries(Dhive.ExperimentTask task, string key, string var_name_prefix, string[] var_name_suffixes, Dictionary<string, string> localParams = null)
    {
        Questionnaire.i_questionnaire_data[key] = new Dictionary<string, string>();   // dictionary for the questionnaire data
        Questionnaire.i_sub_questions[key] = null;  // list of subquestions (the statements they have to respond to) for this questionnaire
        Questionnaire.i_choices[key] = null;  // list of choices (the answers they select from) for this questionnaire

        for (int j = 0; j < var_name_suffixes.Length; j++)
        {
            string suffix = var_name_suffixes[j];
            string param_name = var_name_prefix + key + "_" + suffix;

            if (GameManager.load_read_locally)
            {
                // --- LOCAL LOGIC ---
                // We check if the key exists in our CSV dictionary
                if (localParams != null && localParams.ContainsKey(param_name))
                {
                    string rawValue = localParams[param_name];

                    if (suffix == "Choices")
                    {
                        // Parse string list "['A', 'B']" -> List<string>
                        Questionnaire.i_choices[key] = ParseLocalStringList(rawValue);
                    }
                    else if (suffix == "SubQuestions")
                    {
                        if (key != "QuestionLast")
                        {
                            Questionnaire.i_sub_questions[key] = ParseLocalStringList(rawValue);
                        }
                    }
                    else
                    {
                        // Standard strings (QuestionID, MainQuestion, etc.)
                        // Only add if not empty, matching server logic roughly
                        if (!string.IsNullOrWhiteSpace(rawValue))
                        {
                            Questionnaire.i_questionnaire_data[key][suffix] = rawValue;
                        }
                    }
                }
            }
            else
            {
                // --- SERVER LOGIC (Original) ---
                if (suffix == "Choices")
                {
                    List<string> temp = task.Parameters.GetStringListParameter($"{param_name}");
                    temp = RemoveQuotesAndSpaces(temp, false);
                    Questionnaire.i_choices[key] = temp;
                }
                else if (suffix == "SubQuestions")
                {
                    List<string> temp = task.Parameters.GetStringListParameter($"{param_name}");
                    temp = ProcessSubQuestions(temp);

                    if (key != "QuestionLast")
                    {
                        Questionnaire.i_sub_questions[key] = temp;
                    }
                }
                else
                {
                    string value = task.Parameters.GetStringParameter($"{param_name}");
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        Questionnaire.i_questionnaire_data[key][suffix] = value;
                    }
                }
            }
        }
    }

    // load the params for the questionnaire task
    private async Task LoadQuest(Dhive.ExperimentTask task)
    {
        // Setup Parameters (Local vs Server)
        Dictionary<string, string> localParams = null;
        int num_real_questions = 0;

        if (GameManager.load_read_locally)
        {
            localParams = ReadCSVParams(file_quest);

            // Default to "i_chronic_stress_" if missing, just like server logic usually requires a prefix
            var_name_prefix = localParams.ContainsKey("i_var_name_prefix") ? localParams["i_var_name_prefix"] : "i_chronic_stress_";  
            
            Questionnaire.number_of_starting_questions = int.Parse(localParams["i_num_starter_questionnaires"]);
            num_real_questions = int.Parse(localParams["i_num_real_questions"]);
        }
        else
        {
            var_name_prefix = task.Parameters.GetStringParameter($"i_var_name_prefix");
            Questionnaire.number_of_starting_questions = task.Parameters.GetIntParameter($"i_num_starter_questionnaires");
            num_real_questions = task.Parameters.GetIntParameter($"i_num_real_questions");
        }

        Questionnaire.numberOfQuestionnaires = Questionnaire.number_of_starting_questions + num_real_questions + 1; // +1 for QuestionLast
        string[] var_name_suffixes = new string[] { "QuestionID", "MainQuestion", "HasTextInput", "InputMustBeInt", "TextQuestion", "Choices", "SubQuestions" };
        string key = null;

        // Load Starter Questions
        for (int i = 1; i < Questionnaire.number_of_starting_questions + 1; i++)
        {
            key = "StarterQuestion" + i.ToString();
            // Pass localParams (it will be null if we are in server mode, which is fine)
            populateQuestionnaireDictionaries(task, key, var_name_prefix, var_name_suffixes, localParams);
            Questionnaire.questionnaireInstances.Add(key);
        }

        // load remaining Questions
        if (Questionnaire.real_questions == null) Questionnaire.real_questions = new List<string>();
        else Questionnaire.real_questions.Clear();

        for (int i = 1; i < num_real_questions + 1; i++)
        {
            key = "Question" + i.ToString();
            populateQuestionnaireDictionaries(task, key, var_name_prefix, var_name_suffixes, localParams);
            Questionnaire.real_questions.Add(key);
        }

        // Shuffle remaining questions
        System.Random random = new System.Random();
        for (int i = Questionnaire.real_questions.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            string temp = Questionnaire.real_questions[i];
            Questionnaire.real_questions[i] = Questionnaire.real_questions[j];
            Questionnaire.real_questions[j] = temp;
        }

        // Add shuffled questions to the main instance list
        foreach (string question in Questionnaire.real_questions)
        {
            Questionnaire.questionnaireInstances.Add(question);
        }

        // Load Final Question
        key = "QuestionLast";
        populateQuestionnaireDictionaries(task, key, var_name_prefix, var_name_suffixes, localParams);
        Questionnaire.questionnaireInstances.Add(key);
    }

    // cycle through the list of tasks and load the params for each one
    private async Task LoadTaskParams()
    {
        if (GameManager.load_read_locally)
        {
            var config = ReadCSVParams(GameManager.file_w_session_params);
            
            if (config.ContainsKey("file_digit_symbol")) file_ds = config["file_digit_symbol"];
            if (config.ContainsKey("file_stop_signal")) file_ss = config["file_stop_signal"];
            if (config.ContainsKey("file_nback")) file_r1b = config["file_nback"];
            if (config.ContainsKey("file_task_switching")) file_ts = config["file_task_switching"];
            if (config.ContainsKey("file_icar")) file_icar = config["file_icar"];
            if (config.ContainsKey("file_questionnaire")) file_quest = config["file_questionnaire"];
            if (config.ContainsKey("file_knapsack")) file_ks = config["file_knapsack"];
        }

        foreach (var task in GameManager.experimentData.Tasks)
        {
            if (task.Name == GameManager.symbol_digit_name) await LoadDigitSymbol(task);
            else if (task.Name == GameManager.sst_name) await LoadStopSignal(task);
            else if (task.Name == GameManager.nback_name) await LoadR1B(task);
            else if (task.Name == GameManager.ln_name) await LoadTaskSwitch(task);
            else if (task.Name == "ICAR") await LoadICAR(task);
            else if (task.Name == GameManager.quest_name) await LoadQuest(task);
            else if (task.Name == GameManager.knapsack_name) await LoadKP(task, GameManager.experimentData);
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
        if (GameManager.load_read_locally)
        {
            GameManager.randomizationID = GetLowestAvailableRandomIDLocal(); 
        }
        else
        {
            GameManager.randomizationID = await GetLowestAvailableRandomIDServer(session);
        }
        
        
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
    public async Task<int> GetLowestAvailableRandomIDServer(Session session)
    {
        int highest_available_global = session.Parameters.GetIntParameter("max_rID");

        // vars for iterating
        int lowest_unavailable = -1; // No IDs from this point or lower are available
        int highest_available = highest_available_global; // No IDs above this point are available
        int lowest_available = highest_available_global; // Smallest available rID found so far

        while (lowest_unavailable < highest_available - 1)
        {
            int mid = (lowest_unavailable + highest_available) / 2;  // Calculate the mid-point
            var assigned = session.Parameters.GetIntParameter($"assigned_rID_{mid}", defaultValue: 0);  // Query the rID for availability

            if (assigned == 0)
            {
                lowest_available = mid;   // Mid rID is available
                highest_available = mid;  // Now search lower
            }
            else
            {
                lowest_unavailable = mid;  // Mid rID is unavailable, Now search higher
                Debug.Log($"rID {mid} is unavailable. Setting lowest_unavailable to {lowest_unavailable}.");
            }
        }

        var assigned_check = session.Parameters.GetIntParameter($"assigned_rID_{lowest_available}", defaultValue: 0);
        if (lowest_available < highest_available_global && assigned_check != 1)
        {
            Debug.Log($"lowest available rID is {lowest_available}");
            return lowest_available;  // Return the smallest available rID
        }

        throw new Exception("No available rID found.");  // If no available rID was found
    }

    // finds the lowest available randomisation ID that hasn't been assigned to a participant yet (local deployment)
    private int GetLowestAvailableRandomIDLocal()
    {
        var sessionParams = ReadCSVParams(GameManager.file_w_session_params);
        int max_rID = 500; // Default safety
        if (sessionParams.ContainsKey("max_rID"))
        {
            max_rID = int.Parse(sessionParams["max_rID"]);
        }
        
        // Define path for the tracker file (Persistent Data, not StreamingAssets)
        string trackerPath = Path.Combine(Application.persistentDataPath, "Output", "local_session_tracker.csv");
        HashSet<int> usedIDs = new HashSet<int>();

        // Read currently used IDs if file exists
        if (File.Exists(trackerPath))
        {
            string[] lines = File.ReadAllLines(trackerPath);
            // Skip header (index 0)
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                string[] cols = lines[i].Split(',');
                if (cols.Length > 0 && int.TryParse(cols[0], out int id))
                {
                    usedIDs.Add(id);
                }
            }
        }

        // Find lowest available ID
        int assignedID = -1;
        for (int i = 1; i <= max_rID; i++)
        {
            if (!usedIDs.Contains(i))
            {
                assignedID = i;
                break;
            }
        }

        if (assignedID == -1)
        {
            Debug.LogError("All local randomization IDs are exhausted!");
            assignedID = 1; // Fallback or handle error
        }

        // Lock it immediately by appending to the file
        try
        {
            bool fileExists = File.Exists(trackerPath);
            using (StreamWriter sw = new StreamWriter(trackerPath, true))
            {
                if (!fileExists)
                {
                    sw.WriteLine("randomization_id,participant_id,timestamp");
                }
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                sw.WriteLine($"{assignedID},{GameManager.participantID},{timestamp}");
            }
            Debug.Log($"[Local Session] Assigned and locked rID: {assignedID}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Local Session] Failed to save tracker: {e.Message}");
        }

        return assignedID;
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

    // Helper function to load instances dynamically
    private KnapsackOpt.KSInstance[] LoadKnapsackInstancesLocal(Dictionary<string, string> p, int count, string infix)
    {
        // Create the array
        var instances = new KnapsackOpt.KSInstance[count];

        for (var k = 1; k <= count; k++)
        {
            instances[k - 1] = new KnapsackOpt.KSInstance();

            // Construct the prefix dynamically
            // If infix is "", prefix is "i1__"
            // If infix is "_prac", prefix is "i1_prac__"
            string prefix = $"i{k}{infix}__";

            // Parse integer lists manually using our helper
            // Warning: Our helper returns List<string>, so we must convert to int[]
            var wList = ParseLocalStringList(p[$"{prefix}weights"]);
            instances[k - 1].weights = wList.Select(int.Parse).ToArray();

            var vList = ParseLocalStringList(p[$"{prefix}values"]);
            instances[k - 1].values = vList.Select(int.Parse).ToArray();

            var solList = ParseLocalStringList(p[$"{prefix}solutionItems"]);
            instances[k - 1].itemsOpt = solList.Select(int.Parse).ToArray();

            instances[k - 1].capacity = int.Parse(p[$"{prefix}capacity"]);
            instances[k - 1].capacityOpt = int.Parse(p[$"{prefix}capacityAtOptimum"]);
            instances[k - 1].profitOpt = int.Parse(p[$"{prefix}profitAtOptimum"]);
            
            instances[k - 1].id = p[$"{prefix}problemID"];
            instances[k - 1].type = int.Parse(p[$"{prefix}instanceType"]);
            instances[k - 1].expAccuracy = float.Parse(p[$"{prefix}expAccuracy"]);
        }

        return instances;
    }

    private KnapsackOpt.KSInstance[] LoadKnapsackInstancesServer(Dhive.ExperimentTask task, int count, string infix)
    {
        // Create the array
        var instances = new KnapsackOpt.KSInstance[count];

        for (var k = 1; k <= count; k++)
        {
            instances[k - 1] = new KnapsackOpt.KSInstance();

            // Construct the prefix dynamically
            // If infix is "", prefix is "i1__"
            // If infix is "_prac", prefix is "i1_prac__"
            string prefix = $"i{k}{infix}__";

            // Parse integer lists manually using our helper
            // Warning: Our helper returns List<string>, so we must convert to int[]
            var wList = task.Parameters.GetIntListParameter($"{prefix}weights");
            instances[k - 1].weights = wList.ToArray();

            var vList = task.Parameters.GetIntListParameter($"{prefix}values");
            instances[k - 1].values = vList.ToArray();

            var solList = task.Parameters.GetIntListParameter($"{prefix}solutionItems");
            instances[k - 1].itemsOpt = solList.ToArray();

            instances[k - 1].capacity = task.Parameters.GetIntParameter($"{prefix}capacity");
            instances[k - 1].capacityOpt = task.Parameters.GetIntParameter($"{prefix}capacityAtOptimum");
            instances[k - 1].profitOpt = task.Parameters.GetIntParameter($"{prefix}profitAtOptimum");
            
            instances[k - 1].id = task.Parameters.GetStringParameter($"{prefix}problemID");
            instances[k - 1].type = task.Parameters.GetIntParameter($"{prefix}instanceType");
            instances[k - 1].expAccuracy = (float)task.Parameters.GetDoubleParameter($"{prefix}expAccuracy");
        }

        return instances;
    }

    // loads the parameters for each instance of the KS
    private async Task LoadKP(Dhive.ExperimentTask task, Experiment experiment)
    {
        if (GameManager.load_read_locally)
        {
            var p = ReadCSVParams(file_ks);
            
            GameManager.time_iti_min = int.Parse(p["timeRest1min"]);
            GameManager.time_iti_max = int.Parse(p["timeRest1max"]);
            GameManager.time_inter_block_rest = int.Parse(p["timeRest2"]);
            KnapsackOpt.timeTrial = int.Parse(p["timeTrial"]);
            KnapsackOpt.timeOnlyItems = int.Parse(p["timeOnlyItems"]);
            KnapsackOpt.numberOfTrials = int.Parse(p["numberOfTrials"]);
            KnapsackOpt.numberOfBlocks = int.Parse(p["numberOfBlocks"]);
            KnapsackOpt.numberOfInstances = int.Parse(p["numberOfInstances"]);
            
            string randomization = $"r{GameManager.randomizationID}_instanceRandomization";
            List<string> randStringList = ParseLocalStringList(p[randomization]);
            KnapsackOpt.instanceRandomization = new int[randStringList.Count];
            for (int i = 0; i < randStringList.Count; i++)
            {
                KnapsackOpt.instanceRandomization[i] = int.Parse(randStringList[i]) - 1;
            }

            // load real instances
            KnapsackOpt.ksinstances = LoadKnapsackInstancesLocal(p, KnapsackOpt.numberOfInstances, "");
            // load practice instances
            if(GameManager.give_complex_instructions)
            {
                try
                {
                    KnapsackOpt.practice_trials_count = int.Parse(p["num_practice_trials"]);
                    KnapsackOpt.practice_instances = LoadKnapsackInstancesLocal(p, KnapsackOpt.practice_trials_count, "_prac");
                }
                catch (Exception)
                {
                    Debug.Log("No practice instances found in input. Skipping practice mode.");
                    
                    // Set count to 0 -> Triggers the skip logic in KnapsackOpt.StartComplexTask
                    KnapsackOpt.practice_trials_count = 0; 
                    KnapsackOpt.practice_instances = new KnapsackOpt.KSInstance[0];
                }
            }
            else
                KnapsackOpt.practice_trials_count = 0;
        }
        else
        {
            GameManager.time_iti_min = Convert.ToSingle(task.Parameters.GetIntParameter("timeRest1min"));
            GameManager.time_iti_max = Convert.ToSingle(task.Parameters.GetIntParameter("timeRest1max"));
            GameManager.time_inter_block_rest = Convert.ToSingle(task.Parameters.GetIntParameter("timeRest2"));
            KnapsackOpt.timeTrial = Convert.ToSingle(task.Parameters.GetIntParameter("timeTrial"));
            KnapsackOpt.timeOnlyItems = Convert.ToSingle(task.Parameters.GetIntParameter("timeOnlyItems"));
            KnapsackOpt.numberOfTrials = task.Parameters.GetIntParameter($"numberOfTrials", -1);
            KnapsackOpt.numberOfBlocks = task.Parameters.GetIntParameter($"numberOfBlocks", -1);
            KnapsackOpt.numberOfInstances = task.Parameters.GetIntParameter($"numberOfInstances", -1);

            var randomization = task.Parameters.GetIntListParameter($"r{GameManager.randomizationID}_instanceRandomization");  

            KnapsackOpt.instanceRandomization = new int[randomization.Count];
            for (var i = 0; i < randomization.Count; i++)
            {
                KnapsackOpt.instanceRandomization[i] = randomization[i] - 1;
            }

            // load real instances
            KnapsackOpt.ksinstances = LoadKnapsackInstancesServer(task, KnapsackOpt.numberOfInstances, "");
            // load practice instances
            if(GameManager.give_complex_instructions)
            {
                Debug.Log("Booyaka: checking prac");
                try
                {
                    KnapsackOpt.practice_trials_count = task.Parameters.GetIntParameter("num_practice_trials");
                    KnapsackOpt.practice_instances = LoadKnapsackInstancesServer(task, KnapsackOpt.practice_trials_count, "_prac");

                    Debug.Log("Booyaka: practice_trials_count " + KnapsackOpt.practice_trials_count);
                    Debug.Log("Booyaka: practice_instances " + KnapsackOpt.practice_instances);
                }
                catch (Exception)
                {
                    Debug.Log("Booyaka: No practice instances found in input. Skipping practice mode.");
                    
                    // Set count to 0 -> Triggers the skip logic in KnapsackOpt.StartComplexTask
                    KnapsackOpt.practice_trials_count = 0; 
                    KnapsackOpt.practice_instances = new KnapsackOpt.KSInstance[0];
                }
            }
            else
                KnapsackOpt.practice_trials_count = 0;
            
            GameManager.TaskId = GetDatabaseTaskId(experiment, GameManager.knapsack_name);  
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

    // helper function for local CSV reading: reads a CSV file and returns a dictionary of key-value pairs
    private Dictionary<string, string> ReadCSVParams(string filename)
    {
        string path = Path.Combine(Application.streamingAssetsPath, "Parameters", filename);
        var paramsDict = new Dictionary<string, string>();

        if (!File.Exists(path))
        {
            Debug.LogError($"[DataLoader] File not found: {path}");
            return paramsDict;
        }

        string[] lines = File.ReadAllLines(path);
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Robust CSV parsing for "Key,Value" pairs
            // We assume the Key never has a comma, but the Value might (e.g. lists)
            int firstComma = line.IndexOf(',');
            if (firstComma == -1) continue;

            string key = line.Substring(0, firstComma).Trim();
            string rawValue = line.Substring(firstComma + 1).Trim();

            // Handle CSV quoting: If value starts/ends with quotes, remove them and unescape double double-quotes
            if (rawValue.Length >= 2 && rawValue.StartsWith("\"") && rawValue.EndsWith("\""))
            {
                rawValue = rawValue.Substring(1, rawValue.Length - 2).Replace("\"\"", "\"");
            }

            paramsDict[key] = rawValue;
        }
        return paramsDict;
    }

    // helper function for local CSV reading: parses a string representation of a list into a List<string>
    private List<string> ParseLocalStringList(string listString)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(listString) || listString == "[]") return list;

        // Strip outer brackets [ ... ]
        string content = listString.Trim();
        if (content.StartsWith("[") && content.EndsWith("]"))
        {
            content = content.Substring(1, content.Length - 2);
        }

        // Robust parsing of items (handling 'item', "item", and unquoted items)
        bool inQuotes = false;
        char quoteChar = '\0';
        string currentItem = "";

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            if (inQuotes)
            {
                // Handle escaped quotes (e.g. \' or "")
                if (c == '\\' && i + 1 < content.Length && content[i+1] == quoteChar)
                {
                    currentItem += quoteChar;
                    i++; // skip next
                }
                else if (c == quoteChar)
                {
                    // Check for double double-quotes in CSV style ("" -> ")
                    if (quoteChar == '"' && i + 1 < content.Length && content[i+1] == '"')
                    {
                        currentItem += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentItem += c;
                }
            }
            else
            {
                if (c == ',' && !inQuotes)
                {
                    // End of item
                    list.Add(currentItem.Trim());
                    currentItem = "";
                }
                else if ((c == '"' || c == '\'') && string.IsNullOrWhiteSpace(currentItem))
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else
                {
                    currentItem += c;
                }
            }
        }
        // Add last item
        if (!string.IsNullOrWhiteSpace(currentItem) || list.Count > 0)
        {
            list.Add(currentItem.Trim());
        }

        return list;
    }
}




