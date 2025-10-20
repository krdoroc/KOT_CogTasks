using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Dhive;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine.Networking;
using System.Collections;

public class ICAR : MonoBehaviour
{
    public Canvas StartCanvas;  // shows the instructions / intro for the task
    public Canvas TextCanvas;  // shows the text-based questions
    public Canvas ImageCanvas;  // shows the image-based questions (e.g. progressive matrices)
    public Canvas EndCanvas;  // end of task screen
    private GameObject questionPromptObject;  // text of the question
    private GameObject questionCounter;  // which question we're up to 
    private GameObject[] choiceObjects;  // possible answers 
    private GameObject userPrompt;  // pop up that they need to select an answer before they can continue
    private GameObject timer;
    private GameObject startText;  // text on the StartCanvas
    private int currentQuestionIndex = 1;  // which question we're currently on
    public RawImage rawImage;
    List<int> questionOrder = new List<int>();
    private float lastNextQuestionButtonClickTime = 0f; // Stores the time of the last next question button click
    private float countdownDuration; // Duration of the countdown in seconds
    private bool hasBegun = false; // Whether the countdown has begun
    private float remainingTime; // Time left in the countdown
    public static int total_instances;
    private string[][] all_outputs;  //initialise a 2D array to store all outputs
    public string participant_id = "P001";  // default value
    private string question_id;
    private string question_type; // output variable to store the question type
    public static List<int> instance_list = new List<int>{}; // list of instances to show, taken from input
    private string path_prefix; // path prefix to load the question data from
    public static float time_per_question = 60f; // time per question in seconds
    public static bool matrices_only = true; // whether to only show matrices questions or the full sample test
    private bool is_correct; // whether the answer is correct
    private string correctChoiceIndex; // correct choice index
    public static Dictionary<string, Dictionary<string, string>> i_loadedData = new Dictionary<string, Dictionary<string, string>>(); // dictionary to store the loaded / input data
    public static Dictionary<string, List<string>> i_loadedChoices = new Dictionary<string, List<string>>(); // dictionary to store the Choice data
    public static string is_progressive = "true";  // whether the matrices should get progressive more difficult or whether they should be randomly presented

    // Start is called before the first frame update
    async void Start()
    {
        Debug.Log("ICAR script started");
        GameManager.sender ??= DhiveSender.GetInstance(GameManager.participantTrialId);

        GameManager.total_correct = 0;
        countdownDuration = time_per_question * total_instances; 
        
        all_outputs = new string[total_instances][];

        // Hide all canvases until user presses start
        StartCanvas.gameObject.SetActive(true);
        TextCanvas.gameObject.SetActive(false);
        ImageCanvas.gameObject.SetActive(false);
        EndCanvas.gameObject.SetActive(false);
        startText = StartCanvas.transform.Find("StartText").gameObject;
        TimeSpan timeSpan = TimeSpan.FromSeconds(countdownDuration);
        
        startText.GetComponent<TMP_Text>().text = "Instructions: Task " + (GameManager.completed_tasks.Count + 2).ToString() + " of 6\n\n" + 
                                                    "This task shows how well you can solve novel problems without much prior knowledge.\n\n" + 
                                                    "You will have a total of " +
                                                    timeSpan.Minutes +
                                                    " minutes to complete a quiz with " + total_instances.ToString() + " logic and reasoning questions. There are NO practice questions and once you submit an answer, you cannot go back and change it.\n\n" + 
                                                    "Each question will be harder than the one before.\n\n" +
                                                    "<i>When ready, press the \"spacebar\" to continue.</i>";
        // wait for the spacebar to be pressed
        StartCoroutine(WaitForKeyDown(KeyCode.Space));
    }
    
    // proceed past the instructions when the spacebar is pressed
    IEnumerator WaitForKeyDown(KeyCode keyCode)
    {
        // Debug.Log("pressed space");
        while (!Input.GetKeyDown(keyCode))
        {
            yield return null;
        }
        BeginQuestioning();
    }

    public void BeginQuestioning()
    {
        // present in ascending order of difficulty
        if (is_progressive == "true")
        {
            questionOrder = new List<int>{5, 6, 2, 3, 7, 1, 8, 4};
        }
        // or randomly shuffle the instances
        else
        {
            // Temporary array of integers from 1 to number of instances
            List<int> _questionOrder = new List<int>();
            for (int i = 1; i <= total_instances; i++)
            {
                _questionOrder.Add(i);
            }

            // Shuffle the temporary array using Random.Range
            for (int i = 0; i <= (total_instances-1); i++)
            {
                // Get random element from _questionOrder
                int randomIndex = UnityEngine.Random.Range(0, _questionOrder.Count);
                int temp = _questionOrder[randomIndex];
                
                // Add temp to questionOrder
                questionOrder.Add(temp);

                // Remove temp from _questionOrder
                _questionOrder.RemoveAt(randomIndex);
            }

            // Delete _questionOrder to free up memory
            _questionOrder = null;
        }

        StartCanvas.gameObject.SetActive(false);
        float currentTime = Time.time; // Get the current time
        lastNextQuestionButtonClickTime = currentTime;
        remainingTime = countdownDuration; // Initialize countdown timer
        hasBegun = true; // Set the countdown to begin

        if (matrices_only)
        {
            path_prefix = "matrices_only";
        }
        else
        {
            path_prefix = "full_sample";
        }
        // load the first question
        LoadQuestionData(path_prefix, $"Question{questionOrder[currentQuestionIndex - 1]}");
    }

    void Update()
    {   
        {
        #if !UNITY_WEBGL || UNITY_EDITOR
            GameManager.sender?.DispatchMessageQueue();
        #endif
        }

        if (!hasBegun)
        {
            return;
        }

        // update the timer
        if (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            // if they've run out of time, end the task
            if (remainingTime <= 0)
            {
                remainingTime = 0;
                // Handle countdown finished (e.g., show EndCanvas)
                EndCanvas.gameObject.SetActive(true);

                GameManager.total_instances = total_instances;
                string DateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                SendData(DateTime, GameManager.current_task_number, question_id, currentQuestionIndex, questionOrder[currentQuestionIndex - 1], question_type, 666f, 666, correctChoiceIndex, "0");  // but save the trial they were on, to ensure at least one trial is saved
                SceneManager.LoadScene("FinishedCogTask");
            }
            // Optionally, update a UI element with the formatted time
            UpdateTimerUI(remainingTime);
        }
        else
        {
            // Handle countdown finished (e.g., show EndCanvas)
            ImageCanvas.gameObject.SetActive(false);
            TextCanvas.gameObject.SetActive(false);
            EndCanvas.gameObject.SetActive(true);

            GameManager.total_instances = total_instances;
            SceneManager.LoadScene("FinishedCogTask");
        }
    }
    
    private void UpdateTimerUI(float timeInSeconds)
    {
        string formattedTime = FormatTime(timeInSeconds);
        if (timer != null)
            timer.GetComponent<TMP_Text>().text = "Time remaining: " + formattedTime;
    }

    // // load the image aspect of the questions (local only)
    // private Texture2D LoadTextureFromFile(string filePath)
    // {
    //     Texture2D texture = new Texture2D(2, 2);
    //     byte[] fileData = System.IO.File.ReadAllBytes(filePath);
    //     texture.LoadImage(fileData);
    //     return texture;
    // }

    // load the image using UnityWebRequest
    private IEnumerator LoadTexture(string filePath)
    {
        // Use UnityWebRequest to fetch the image
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(filePath);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Failed to load image: " + filePath + " Error: " + www.error);
        }
        else
        {
            // Get the texture from the downloaded data
            Texture2D texture = DownloadHandlerTexture.GetContent(www);

            // Apply the texture to your RawImage or other UI element
            rawImage.texture = texture;
            // Debug.Log("Image loaded successfully from: " + filePath);
        }
    }

    // display the question data onto the scene
    void LoadQuestionData(string path_prefix, string fileName)
    {   
        // store the correct answer, question type, and quesiton ID
        correctChoiceIndex = i_loadedData[fileName]["CorrectChoiceIndex"];  
        question_type = i_loadedData[fileName]["QuestionID"].Substring(0, 2);
        question_id = i_loadedData[fileName]["QuestionID"];
    
        // If the question has an image, load it
        if (i_loadedData[fileName].ContainsKey("hasImage") && i_loadedData[fileName]["hasImage"] == "TRUE") 
        {   
            TextCanvas.gameObject.SetActive(false);
            ImageCanvas.gameObject.SetActive(true);
            questionPromptObject = ImageCanvas.transform.Find("ImageQuestionPrompt").gameObject;
            questionCounter = ImageCanvas.transform.Find("QuestionCounter").gameObject;
            choiceObjects = new GameObject[8];
            for (int i = 0; i < 8; i++)
            {   
                choiceObjects[i] = ImageCanvas.transform.Find("AnswerGroup/AnswerToggle" + (i).ToString()).gameObject;
            }
            userPrompt = ImageCanvas.transform.Find("UserPrompt").gameObject;
            userPrompt.gameObject.SetActive(false);

            timer = ImageCanvas.transform.Find("Timer").gameObject;

            // // Load the image (local only)
            // string imagePath = "Assets/StreamingAssets/ICAR/" + path_prefix + "/" + i_loadedData[fileName]["QuestionID"] + ".jpeg";
            // Texture2D image = LoadTextureFromFile(imagePath);
            // if (image != null)
            // {
            //     rawImage.texture = image;     
            // }
            // else
            // {
            //     Debug.LogError("Failed to load image: " + i_loadedData[fileName]["QuestionID"]);
            // }

            // Set the correct path based on the platform
            string imagePath = "";

            #if UNITY_WEBGL
                // For WebGL, use the streaming assets URL
                imagePath = Application.streamingAssetsPath + "/ICAR/" + path_prefix + "/" + i_loadedData[fileName]["QuestionID"] + ".jpeg";
            #elif UNITY_EDITOR
                // In the editor, you can access files directly without the "file://" prefix
                imagePath = "file://" + Application.streamingAssetsPath + "/ICAR/" + path_prefix + "/" + i_loadedData[fileName]["QuestionID"] + ".jpeg";
            #else
                // For standalone platforms (Windows, Mac, etc.), use the "file://" prefix
                imagePath = "file://" + Application.streamingAssetsPath + "/ICAR/" + path_prefix + "/" + i_loadedData[fileName]["QuestionID"] + ".jpeg";
            #endif

            // Start the coroutine to load the texture from the path
            StartCoroutine(LoadTexture(imagePath));

        }
        //If the question does not have an image, load it
        else 
        {   
            TextCanvas.gameObject.SetActive(true);
            ImageCanvas.gameObject.SetActive(false);
            questionPromptObject = TextCanvas.transform.Find("TextQuestionPrompt").gameObject;
            questionCounter = TextCanvas.transform.Find("QuestionCounter").gameObject;
            choiceObjects = new GameObject[8];
            for (int i = 0; i < 8; i++)
            {   
                choiceObjects[i] = TextCanvas.transform.Find("AnswerGroup/AnswerToggle" + (i).ToString()).gameObject;
            }
            userPrompt = TextCanvas.transform.Find("UserPrompt").gameObject;
            userPrompt.gameObject.SetActive(false);
            timer = TextCanvas.transform.Find("Timer").gameObject;

        }
        
        // Print the question number
        if (questionCounter != null)
            questionCounter.GetComponent<TMP_Text>().text = "Question " + currentQuestionIndex.ToString() + " of " + total_instances.ToString();

        // Set the question prompt
        if (questionPromptObject != null)
            questionPromptObject.GetComponent<TMP_Text>().text = i_loadedData[fileName]["QuestionPrompt"];

        // Set the choices
        for (int i = 0; i < choiceObjects.Length; i++)
        {   
            if (i < i_loadedChoices[fileName].Count && choiceObjects[i] != null)
            {
                choiceObjects[i].SetActive(true);
                // Reset the toggle state of the choice object
                choiceObjects[i].GetComponent<Toggle>().isOn = false;
                choiceObjects[i].GetComponentInChildren<TMP_Text>().text = i_loadedChoices[fileName][i];
            }
            else
            {
                choiceObjects[i].SetActive(false);
            }
        }
    }

    // if they try to progress without answering, warn them they must answer the question to progress. if they've answered the question, store their response and prepare it for saving
    public void NextQuestion()
    {   
        // A list to keep track of outputs
        List<string> output = new List<string>();

        // Check if at least one choice is selected
        bool isAnyChoiceSelected = false;
        int whichChoiceSelected = -1;
        foreach (var choiceObject in choiceObjects)
        {
            if (choiceObject.GetComponent<Toggle>().isOn)
            {
                isAnyChoiceSelected = true;
                whichChoiceSelected = System.Array.IndexOf(choiceObjects, choiceObject);
                break; // Exit the loop early if a selected choice is found
            }
        }

        // If no choice is selected, display the user prompt and return early
        if (!isAnyChoiceSelected)
        {
            // Display the user prompt
            userPrompt.gameObject.SetActive(true);

            // Create a new material instance for the userPrompt and change its face color to red
            Material newMaterial = new Material(userPrompt.GetComponent<TextMeshProUGUI>().fontSharedMaterial);
            userPrompt.GetComponent<TextMeshProUGUI>().fontMaterial = newMaterial;
            
            return; // Return early
        }
        
        float currentTime = Time.time; // Get the current time
        float response_time = currentTime - lastNextQuestionButtonClickTime; // Calculate the time difference
        lastNextQuestionButtonClickTime = currentTime; // Update the last click time

        is_correct = ( whichChoiceSelected.ToString() == correctChoiceIndex );
        // output.Add( is_correct.ToString() );
        string DateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        
        // prepare data for adding to the save queue
        SendData(DateTime, GameManager.current_task_number, question_id, currentQuestionIndex, questionOrder[currentQuestionIndex - 1], question_type, response_time, whichChoiceSelected, correctChoiceIndex, is_correct ? "1" : "0");

        if (is_correct)
        {
            GameManager.total_correct++;
        }

        // if they just finished the last question, end the task
        if (currentQuestionIndex == total_instances)
        {
            GameManager.total_instances = total_instances;
            SceneManager.LoadScene("FinishedCogTask");
        }
        // otherwise load the next question
        else
        {
            currentQuestionIndex++;
            LoadQuestionData(path_prefix, $"Question{questionOrder[currentQuestionIndex - 1]}");
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(timeInSeconds);
        return string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
    }

    // store data as a list and add it to the saving queue
    private static void SendData(string DateTime, int task_number, string question_identifier, int trial, int instance_id, string question_type, float response_time, int response, string solution, string correct)
    {   
        var outputs = new List<OutputParameter>
		{
			new ("o_ICAR_date_time", DateTime),
            new ("o_ICAR_task_order", task_number),
            new ("o_ICAR_question_id", question_identifier),
            new ("o_ICAR_trial", trial),
			new ("o_ICAR_instance_id", instance_id),
            new ("o_ICAR_question_type", question_type),
			new ("o_ICAR_response_time", response_time),
            new ("o_ICAR_choice_selected", response),
            new ("o_ICAR_solution", solution),
            new ("o_ICAR_correct", correct)
		};

        Debug.Log("[Q] 0: Saving: " + string.Join(", ", outputs.Select(o => $"{o.Name}: {o.Value}")));
        Debug.Log("[Q] Queue size before saving data is " + DataSaver.GetQueueSize());
        DataSaver.AddDataToSave(GameManager.TaskId, outputs);
    }
}
