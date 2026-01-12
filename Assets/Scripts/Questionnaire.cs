using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Dhive;
using System.Threading.Tasks;

public class Questionnaire : MonoBehaviour
{
    public static int numberOfQuestionnaires;
    public static List<string> questionnaireInstances = new List<string>(); // list of all instances of questionnaires to complete
    private string[] input_files;
    private int current_questionnaire = 0;
    private string current_instance_number;
    private int max_questions_per_screen = 7; // maximum number of questions to display on the screen at once. if more than this, it will be split over multiple screens
    private int number_of_screens_needed = 1; // for a given question
    private int current_screen = 1;
    private int verical_offset = 110; // change this to adjust the vertical spacing between questions
    private QuestionnaireClass questionnaire;
    private static int questions_to_display = 0;
    private static int num_subquestions = 0;
    private int starting_instance; // for a given question
    private int ending_instance;
    private GameObject warningText; // canvas object
    private GameObject newQuestionText; // canvas object
    private GameObject startingText; // canvas object
    private GameObject lastQuestion; // canvas object
    private GameObject questionGroup; // canvas object
    private GameObject savingMessageText; // canvas object
    private GameObject continueButton; // canvas object
    private GameObject TextInput; // canvas object
    public static int number_of_starting_questions;
    private List<int> subquestionIndices;
    private string questionnaire_type = "age"; // possible values: age vs ChronicStress
    private Dictionary<string, Dictionary<int, string>> questionnaire_responses = new Dictionary<string, Dictionary<int, string>>(); // dictionary to store key: question number, value: dictionary of key: subquestion number, value: int choice selected
    private Dictionary<string, Dictionary<string, string>> all_questionnaire_data = new Dictionary<string, Dictionary<string, string>>();  // top level key: question number, second level key: field name, value: field value
    private static string[] sub_questions;
    private static string[] choices;
    private bool passed_first_screen = false;

    // input data stored 
    public static Dictionary<string, Dictionary<string, string>> i_questionnaire_data = new Dictionary<string, Dictionary<string, string>>(); 
    public static Dictionary<string, List<string>> i_sub_questions = new Dictionary<string, List<string>>();
    public static Dictionary<string, List<string>> i_choices = new Dictionary<string, List<string>>();
    public static List<string> real_questions = new List<string>();

    // Start is called before the first frame update
    async void Start()
    {
        Debug.Log("Quest script started");
        // find the vertical screen size and adjust the vertical offset accordingly
        float screenHeight = Screen.height;
        verical_offset = (int) Math.Round(screenHeight / 10.5);

        // deactivate objects on the canvas that we don't want to display at the start
        warningText = GameObject.Find("WarningText");
        warningText.SetActive(false);
        newQuestionText = GameObject.Find("NewQuestion");
        newQuestionText.SetActive(false);
        lastQuestion = GameObject.Find("LastQuestion");
        lastQuestion.SetActive(false);
        TextInput = GameObject.Find("TextInput");
        TextInput.SetActive(false);
        continueButton = GameObject.Find("ContinueButton");
        continueButton.SetActive(false);
        savingMessageText = GameObject.Find("SavingMessage");
        savingMessageText.SetActive(false);	

        GameManager.sender ??= DhiveSender.GetInstance(GameManager.participantTrialId);

        // // store the number of questionnaires
        numberOfQuestionnaires = number_of_starting_questions + real_questions.Count + 1;

        // display the starting instructions for the questionnaires
        Intro();

        // load the first questionnaire
        current_instance_number = questionnaireInstances[current_questionnaire];
        LoadQuestionFirstTime();
    }

    // display the starting instructions for the questionnaires
    void Intro()
    {
        startingText = GameObject.Find("StartingText");
        TMP_Text startingTextChild = startingText.GetComponentInChildren<TMP_Text>();
        startingTextChild.GetComponent<TMP_Text>().text = "You will now be presented with " + numberOfQuestionnaires.ToString() + " questionnaires.\n\n" + 
        "Pay close attention: the scale may change for each new questionnaire, and there will be attention checks that could affect your payment.\n\n" +
        "Only the attention checks can affect your payment, so answer truthfully and work quickly without thinking too much or checking your answers.\n\n" + 
        "Your responses have a big impact on how we interpret your performance on the tasks, so we thank you for your honesty. This is the last part of the study and should take 10 minutes.\n\n" + 
        "DO NOT resize your browser window or change monitors. <i>When ready, press the \"spacebar\" to continue.</i>";
        
        // wait for the spacebar to be pressed
        StartCoroutine(WaitForKeyDown(KeyCode.Space));
    }

    // proceed past the instructions when the spacebar is pressed
    IEnumerator WaitForKeyDown(KeyCode keyCode)
    {
        while (!Input.GetKeyDown(keyCode))
        {
            yield return null;
        }
        passed_first_screen = true;
        startingText.SetActive(false);
        TextInput.SetActive(true);
        continueButton.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        {
        #if !UNITY_WEBGL || UNITY_EDITOR
            GameManager.sender?.DispatchMessageQueue();
        #endif
        }
    }

    // record which choice was selected for a particular subquestion
    void OnToggleValueChanged(bool isOn, Toggle toggle)
    {
        if (isOn)
        {
            // store the choice selected
            string response = toggle.transform.Find("Label").GetComponent<TMP_Text>().text;
            
            // store the question being answered
            string subquestion_number = toggle.transform.parent.Find("Label").GetComponent<TMP_Text>().text;
            int subquestionIndex = int.Parse(subquestion_number);
            
            // update the dictionary
            questionnaire_responses[i_questionnaire_data[current_instance_number]["QuestionID"]][subquestionIndex] = response;
        }
    }

    // record the text input for a particular question
    public void addTextInput()
    {
        // store the text input
        TMP_InputField inputField = TextInput.GetComponent<TMP_InputField>();
        string text = inputField.text;
        questionnaire_responses[i_questionnaire_data[current_instance_number]["QuestionID"]][666] = text;
    }

    // check whether all questions on the screen have been answered
    bool AreAllQuestionsAnswered()
    {
        // Iterate through the relevant subquestions
        for (int i = starting_instance-1; i < ending_instance; i++)
        {
            // if they haven't all been answered, display a warning and return false
            if (questionnaire_responses[i_questionnaire_data[current_instance_number]["QuestionID"]][subquestionIndices[i]] == choices.Length.ToString())
            {
                warningText.SetActive(true);
                return false;
            }
        }

        // if there is a required text input, check if it has been filled in
        if (i_questionnaire_data[current_instance_number]["HasTextInput"] == "TRUE" && i_questionnaire_data[current_instance_number]["InputMustBeInt"] == "TRUE")
        {
            int number;
            TMP_InputField inputField = TextInput.GetComponent<TMP_InputField>();
            string text = inputField.text;
            if (!int.TryParse(text, out number))
            {
                warningText.SetActive(true);
                warningText.GetComponent<TMP_Text>().text = "Please enter a number without any spaces or non-numeric characters";
                return false;
            }
        }

        // otherwise hide the warning and return true
        warningText.SetActive(false); 
        return true;
    }

    // delete all of the temporary objects created for the current set of questions from the canvas
    void ClearCurrentQuestions(bool end_question)
    {
        warningText.GetComponent<TMP_Text>().text = "Please select one answer to each statement before clicking 'Continue'";
        
        GameObject[] tempObjects = GameObject.FindGameObjectsWithTag("delete_end_screen");
        
        if (end_question)
        {
            TextInput.SetActive(false);
            tempObjects = tempObjects.Concat(GameObject.FindGameObjectsWithTag("delete_end_question")).ToArray();
            number_of_screens_needed = 1;
        }
        foreach (GameObject temp in tempObjects)
        {
            Destroy(temp);
        }
    }

    // turn off all the selectable toggles, turn on the unselctable fake toggle. The fake toggle is a hacky implementation, a toggle that is off screen and cannot be selected. It is used to ensure that none of the visible toggle are preselected.
    void ResetToggles()
    {
        Toggle toggleFake = GameObject.Find("ToggleFake").GetComponent<Toggle>();
        toggleFake.isOn = true;
    }

    // prepare the responses for saving
    async Task StoreResponses()
    {
        string datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var mostRecentKey = questionnaire_responses.Keys.LastOrDefault();

        // for the current questionnaire, iterate through the subquestions and store the responses 
        foreach (var subkey in questionnaire_responses[mostRecentKey].Keys)
        {
            string subquestion = subkey.ToString();
            string response = questionnaire_responses[mostRecentKey][subkey];
            string question_id = i_questionnaire_data[current_instance_number]["QuestionID"].ToString();
            SendData(datetime, question_id, subquestion, response);
        }
    }

    // proceed to the next set of questions if all questions have been answered
    public async void OnContinueButtonClicked()
    {
        // check all questions on the screen have been answered
        if (AreAllQuestionsAnswered())
        {
            if (i_questionnaire_data[current_instance_number]["HasTextInput"] == "TRUE")
            {
                addTextInput();
            }
            ResetToggles();

            // if a questionnaire has been completed
            if (current_screen > number_of_screens_needed || current_questionnaire == numberOfQuestionnaires - 1)
            {
                savingMessageText.SetActive(true);
                // save responses 
                await StoreResponses();
                
                // clear the current questions from the canvas
                ClearCurrentQuestions(true);
                current_screen = 1;
                current_questionnaire++;

                savingMessageText.SetActive(false);

                // if there are more questionnaires to complete, load the next one, otherwise load the end screen
                if (current_questionnaire < numberOfQuestionnaires)
                {
                    current_instance_number = questionnaireInstances[current_questionnaire];
                    LoadQuestionFirstTime();
                }
                else
                {
                    GameManager.questionnaires_completed = true;
                    SceneManager.LoadScene("CogTaskHome");
                }
            }
            // if a questionnaire is not yet finished, load the next set of questions in that questionnaire
            else
            {
                ClearCurrentQuestions(false);  // clear the subquestions they've answered so far
                LoadStatements(questions_to_display);  // display the next set of subquestions
            }
        }
    }

    // hide the new question text after a short delay
    IEnumerator OnlyShowQuestion()
    {
        yield return new WaitForSeconds(1f);
        newQuestionText.SetActive(false);
    }

    // set up the canvas template objects to display the first set of questions in a questionnaire
    void LoadQuestionFirstTime()
    {   
        // if it's the last questionnaire, disable the QuestionGroup and activate the LastQuestion
        if (current_questionnaire == numberOfQuestionnaires - 1)
        {
            questionGroup = GameObject.Find("QuestionGroup");
            questionGroup.SetActive(false);
            lastQuestion.SetActive(true);
            starting_instance = 1;
            ending_instance = 1;
            num_subquestions = 1;
            subquestionIndices = new List<int> {0};
        }
        else
        {
            sub_questions = i_sub_questions[current_instance_number].ToArray();

            // store the index of each subquestion 
            subquestionIndices = new List<int>();
            for (int i = 0; i < sub_questions.Length; i++)
            {
                int index = Array.IndexOf(sub_questions, sub_questions[i]);
                subquestionIndices.Add(index);
            }

            // if current_instance_number starts with the string Question
            if (current_instance_number.StartsWith("Question"))
            {
                // shuffle the order of subquestions
                System.Random random = new System.Random();
                subquestionIndices = subquestionIndices.OrderBy(x => random.Next()).ToList();
            }

            num_subquestions = sub_questions.Length;
        }

        // alert participants that they are about to start a new question
        newQuestionText.SetActive(true);
        StartCoroutine(OnlyShowQuestion());

        // sub_questions = all_questionnaire_data[current_instance_number]["SubQuestions"].Split('^');
        // choices = all_questionnaire_data[current_instance_number]["Choices"].Split('^');
        choices = i_choices[current_instance_number].ToArray();
        // store the length of the choices array and the length of the subquestions array
        int num_choices = choices.Length;

        // add a key to the dictionary based on QuestionID
        questionnaire_responses.Add(i_questionnaire_data[current_instance_number]["QuestionID"], new Dictionary<int, string>());
        // for the current QuestionID key, populate its value (itself a dictionary) with keys equal to subquestion number and default value equal to num_choices
        for (int i = 0; i <= num_subquestions - 1; i++)
        {
            questionnaire_responses[i_questionnaire_data[current_instance_number]["QuestionID"]].Add(i, num_choices.ToString());
        }

        // find and update the main parts of the question and answer for this questionnaire on the canvas
        GameObject MainQuestion = GameObject.Find("MainQuestion"); // Main Question
        TMP_Text mainQuestionText = MainQuestion.GetComponent<TMP_Text>();
        mainQuestionText.GetComponent<TMP_Text>().text = i_questionnaire_data[current_instance_number]["MainQuestion"];
        GameObject ScalePoint = GameObject.Find("ScalePoint"); // likert descriptions
        ScalePoint.GetComponent<TMP_Text>().text = choices[0];
        GameObject ScalePointEnd = GameObject.Find("ScalePointEnd");
        ScalePointEnd.GetComponent<TMP_Text>().text = choices[num_choices-1];
        GameObject Toggle = GameObject.Find("Toggle"); // toggle for each choice
        GameObject ToggleCopy;
        GameObject Answer = GameObject.Find("Answer"); // answer object
        Answer.transform.Find("Label").GetComponent<TMP_Text>().text = subquestionIndices[0].ToString();
        
        // find the x_position of each end of the likert scale and the average distance between options
        float x_position_start = ScalePoint.transform.position.x;
        float x_position_end = ScalePointEnd.transform.position.x;
        float distance = (x_position_end - x_position_start) / (num_choices-1);

        // for the first subquestion, populate each of the remaining likert choices and an accompanying toggle
        for (int i = 0; i <= num_choices-2; i++)
        {
            if (i == 0)
            {
                ToggleCopy = Toggle;
            }
            else
            {
                // populate choices
                GameObject ScalePointCopy = Instantiate(ScalePoint, new Vector3(ScalePoint.transform.position.x + i*distance, ScalePoint.transform.position.y, ScalePoint.transform.position.z), Quaternion.identity);
                ScalePointCopy.GetComponent<TMP_Text>().text = choices[i];
                ScalePointCopy.transform.SetParent(GameObject.Find("MainQuestion").transform);
                ScalePointCopy.tag = "delete_end_question"; // add tag to delete object later

                // ensure the duplicates scale to screen size the same way as the original object
                RectTransform rectTransform = ScalePointCopy.GetComponent<RectTransform>();
                rectTransform.localScale = ScalePoint.GetComponent<RectTransform>().localScale;

                // populate toggles
                ToggleCopy = Instantiate(Toggle, new Vector3(Toggle.transform.position.x + i*distance, Toggle.transform.position.y, Toggle.transform.position.z), Quaternion.identity);
                ToggleCopy.transform.SetParent(GameObject.Find("Answer").transform);
                ToggleCopy.tag = "delete_end_question";
                ToggleCopy.transform.Find("Background").tag = "delete_end_question"; // add tag to delete object later

                rectTransform = ToggleCopy.GetComponent<RectTransform>();
                rectTransform.localScale = Toggle.GetComponent<RectTransform>().localScale;
            }
            ToggleCopy.transform.Find("Label").GetComponent<TMP_Text>().text = i.ToString(); // add a label to the toggle to display the index of choice number
            // add a listener to the toggle to record the choice selected
            Toggle toggleComponent = ToggleCopy.GetComponent<Toggle>();
            toggleComponent.onValueChanged.AddListener((bool isOn) => OnToggleValueChanged(isOn, toggleComponent));
        }
        // set up the last toggle and the fake toggle appropriately
        ToggleCopy = GameObject.Find("ToggleEnd");
        ToggleCopy.transform.Find("Label").GetComponent<TMP_Text>().text = (num_choices-1).ToString();
        Toggle toggleEnd = ToggleCopy.GetComponent<Toggle>();
        toggleEnd.onValueChanged.AddListener((bool isOn) => OnToggleValueChanged(isOn, toggleEnd));

        ToggleCopy = GameObject.Find("ToggleFake");
        ToggleCopy.transform.Find("Label").GetComponent<TMP_Text>().text = (num_choices).ToString();

        // if there is a text question, there is less space for likert choices, so display fewer questions per screen
        if (i_questionnaire_data[current_instance_number]["HasTextInput"] == "TRUE")
        {
            max_questions_per_screen = 4;
        }
        else
        {
            max_questions_per_screen = 7;
        }
        
        // if there are more subquestions than can fit on one screen, calculate how many screens are needed and how many questions to display on each
        if (num_subquestions > max_questions_per_screen)
        {
            number_of_screens_needed = Mathf.CeilToInt((float)num_subquestions / max_questions_per_screen);
        }
        questions_to_display = Mathf.CeilToInt((float)num_subquestions / number_of_screens_needed);
        
        // if it's not the last questionnaire, load the remaining subquestions for this questionnaire
        if (current_questionnaire != numberOfQuestionnaires - 1)
        {
            LoadStatements(questions_to_display);
        }
    }

    // used for checking that dictionaries are populating correctly
    string PrintDictionary(Dictionary<int, string> dict)
    {
        string result = "";
        foreach (var kvp in dict)
        {
            result += $"Key: {kvp.Key}, Value: {kvp.Value}\n";
        }
        return result;
    }

    // duplicate the answer template and populate it with the next set of subquestions
    void LoadStatements(int questions_to_display)
    {   
        // set the range of subquestions to display
        starting_instance = 1;
        ending_instance = questions_to_display;
        if (current_screen != 1)
        {
            starting_instance = (questions_to_display * (current_screen - 1)) + 1;
            ending_instance = Math.Min(questions_to_display * current_screen, num_subquestions);
        }

        // populate the first statement
        GameObject Statement = GameObject.Find("Statement");
        Statement.GetComponent<TMP_Text>().text = sub_questions[subquestionIndices[starting_instance-1]];

        // populate answers
        GameObject Answer = GameObject.Find("Answer");
        if (current_screen != 1)
        {
            Answer.transform.Find("Label").GetComponent<TMP_Text>().text = subquestionIndices[starting_instance-1].ToString();
        }

        // for each SubQuestion, populate the text, make a copy of the Answer object, display the copied object below the previous Answer object, and set the text to the SubQuestion
        for (int i = starting_instance; i < ending_instance; i++)
        {
            // duplicate the answer object
            GameObject AnswerCopy = Instantiate(Answer, new Vector3(Answer.transform.position.x, Answer.transform.position.y - verical_offset*(i - starting_instance + 1), Answer.transform.position.z), Quaternion.identity);
            AnswerCopy.transform.SetParent(GameObject.Find("QuestionGroup").transform);
            AnswerCopy.tag = "delete_end_screen"; // add tag to delete object later
            
            // label it and add the subquestion text
            GameObject StatementCopy = AnswerCopy.transform.Find("Statement").gameObject;
            StatementCopy.GetComponent<TMP_Text>().text = sub_questions[subquestionIndices[i]];
            AnswerCopy.transform.Find("Label").GetComponent<TMP_Text>().text = subquestionIndices[i].ToString();

            // ensure the duplicates scale to screen size the same way as the original object
            RectTransform rectTransform = AnswerCopy.GetComponent<RectTransform>();
            rectTransform.localScale = Answer.GetComponent<RectTransform>().localScale;

            // Add listeners to each Toggle within the AnswerCopy
            Toggle[] toggles = AnswerCopy.GetComponentsInChildren<Toggle>();
            foreach (Toggle toggle in toggles)
            {
                toggle.onValueChanged.AddListener((bool isOn) => OnToggleValueChanged(isOn, toggle));
            }
        }

        if (i_questionnaire_data[current_instance_number]["HasTextInput"] == "TRUE" && current_screen == 1)
        {
            questionnaire_responses[i_questionnaire_data[current_instance_number]["QuestionID"]].Add(666, ""); // add 666 as a key for text input
            
            TMP_InputField inputField = TextInput.GetComponent<TMP_InputField>();
            inputField.text = "";
            
            GameObject InputQuestion = TextInput.transform.Find("InputQuestion").gameObject;
            InputQuestion.GetComponent<TMP_Text>().text = i_questionnaire_data[current_instance_number]["TextQuestion"];

            if (passed_first_screen == false)
            {
                TextInput.SetActive(false);
            }
            else
            {
                TextInput.SetActive(true);
            }
        }
        current_screen = current_screen + 1;
    }
        
    // check the dictionary of responses to ensure it is being populated correctly
    void check_dict (Dictionary<string, Dictionary<int, string>> dict)
    {
        Debug.Log("start full check");
        if (dict != null && dict.Count > 0)
        {
            foreach (var key in dict.Keys)
            {
                Debug.Log("key is " + key);
                foreach (var subkey in dict[key].Keys)
                {
                    Debug.Log("subkey is ");
                    Debug.Log(subkey);
                    Debug.Log("value is ");
                    Debug.Log(dict[key][subkey]);
                }
            }
        }
        else
        {
            Debug.Log("The dictionary is empty.");
        }
        Debug.Log("end full check");
    }

    void check_dict_2 (Dictionary<string, Dictionary<string, string>> dict)
    {
        Debug.Log("start full check");
        if (dict != null && dict.Count > 0)
        {
            foreach (var key in dict.Keys)
            {
                Debug.Log("key is " + key);
                foreach (var subkey in dict[key].Keys)
                {
                    Debug.Log("subkey is ");
                    Debug.Log(subkey);
                    Debug.Log("value is ");
                    Debug.Log(dict[key][subkey]);
                }
            }
        }
        else
        {
            Debug.Log("The dictionary is empty.");
        }
        Debug.Log("end full check");
    }

    // add the data to the queue to be saved
    private static void SendData(string DateTime, string question_id, string subquestion, string response)
    {   
        var outputs = new List<OutputParameter>
        {
            new ("o_" + DataLoader.var_name_prefix.Substring(2) + "Questionnaire_date_time", DateTime),
            new ("o_" + DataLoader.var_name_prefix.Substring(2) + "Questionnaire_question_id", question_id),
            new ("o_" + DataLoader.var_name_prefix.Substring(2) + "Questionnaire_subquestion", subquestion),
            new ("o_" + DataLoader.var_name_prefix.Substring(2) + "Questionnaire_response", response)
        };

        Debug.Log("Saving: " + string.Join(", ", outputs.Select(o => $"{o.Name}: {o.Value}")));
        DataSaver.PrepareToSave(outputs, "Questionnaires");
    }
}

[System.Serializable]
public class QuestionnaireClass
{   
    public string QuestionID;
    public string MainQuestion;
    public string[] Choices;
    public string[] SubQuestions;
    public bool HasTextInput;
    public bool InputMustBeInt;
    public string TextQuestion;
}

public class DynamicCanvasScaler : MonoBehaviour
{
    public CanvasScaler canvasScaler; // Reference to the CanvasScaler

    void Update()
    {
        AdjustCanvasScaler();
    }

    void AdjustCanvasScaler()
    {
        // float screenWidth = Screen.width;
        // float screenHeight = Screen.height;
        float aspectRatio = GameManager.screen_width / GameManager.screen_height;

        Debug.Log($"Screen width: {GameManager.screen_width}, Screen height: {GameManager.screen_height}, Aspect ratio: {aspectRatio}");
        // Adjust matchWidthOrHeight dynamically based on the screen's aspect ratio
        canvasScaler.matchWidthOrHeight = aspectRatio >= 1 ? 0f : 1f;  // Adjust this ratio as needed
    }
}

