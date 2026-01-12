using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Dhive;
using System.Threading.Tasks;
using System.Linq;

public class NBack : MonoBehaviour
{

    // GameObject for each cue/stimulus
    public GameObject Cue1;  
    public GameObject Cue2;
    public GameObject Cue3;
    public GameObject ErrorBox;  // error message that pops up after an incorrect response during practice
    public GameObject StarterBox;  // message that pops up at the start of each block to remind the participant of the task
    public TMP_Text InitialText;
    public bool pop_up_flag_is_for_error = true;
    private static int a;  // index of the previous digit in position 0
    private static int b;  // index of the previous digit in position 1
    private static int c;   // index of the previous digit in position 2
    private static int prev_a;  // previous stimulus in each position
    private static int prev_b;
    private static int prev_c; 
    private static int instance = 0;
    private static int total_correct = 0;
    private static int load = 1;
    private static int prev_load = 0; 
    private static float time_between_cues = 0f;
    private static bool init_task = true;
    private static int practice_output = 3; // initialised value, will store whether the trial is a practice trial (1) or not (0)
    private static List<string> starter_cue = new List<string>{"", "", ""};

    private static int x; // index of the current digit
    private int stimulus_position; 
    private bool enableKeys = true;
    private bool correct = false; 
    private float time = 0f; 
    private float response_time = 0f; 
    private string inputkey = null;
    private string prevkey = null;
    private static List<int> stimulus_positions = new List<int>();
    private static bool block_2_randomisation_completed = false;
    private static bool block_3_randomisation_completed = false;

    // Parameters (these will all get overwritten)
    public static float time1 = 2.5f;
    public static float time2 = 2.9f;
    public static float time3 = 3.5f;
    public static int real_block1 = 2; //2;
    public static int real_block2 = 5; //5;
    public static int real_block3 = 5; //5;
    public int block1 = 1; //2;
    public int block2 = 1; //5;
    public int block3 = 1; //5;
    public static int practice_block1 = 1; //2;
    public static int practice_block2 = 1; //5;
    public static int practice_block3 = 1; //5;
    public static List<string> digits = new List<string>{"1", "1", "3", "4", "5", "6", "7", "8", "9"};

    // Start is called before the first frame update
    async void Start()
    {   
        Debug.Log("Nback script started");
        // find the errorbox object on the canvas
        ErrorBox = GameObject.Find("ErrorBox");
        ErrorBox.gameObject.SetActive(false);

        StarterBox = GameObject.Find("StarterBox");
        StarterBox.gameObject.SetActive(false);

        GameManager.sender ??= DhiveSender.GetInstance(GameManager.participantTrialId);

        // set initial parameters
        if (GameManager.do_practice == true)
        {
            block1 = practice_block1;
            block2 = practice_block2;
            block3 = practice_block3;
        }
        else
        {
            block1 = real_block1; 
            block2 = real_block2; 
            block3 = real_block3; 
        }

        if (instance >= block1+block2+block3)
        {
            enableKeys = false;
            SceneManager.LoadScene("FinishedCogTask");

            GameManager.total_correct = total_correct;
            GameManager.total_instances = instance;

            // reset all private static variables
            instance = 0;
            total_correct = 0;
            load = 1;
            prev_load = 0; 
            time_between_cues = 0f;
            init_task = true;
            block_2_randomisation_completed = false;
            block_3_randomisation_completed = false;
            a = 666;
            b = 666;
            c = 666;
        } 
        else
        {   
            if (instance == 0)
            {
                time_between_cues = time1;
            }
            else if (instance == block1)
            {
                load = 2;
                time_between_cues = time2;
                
                if (block_2_randomisation_completed == false)
                {
                    stimulus_positions = new List<int>();
                    stimulus_positions = GenerateStimulusPositions(stimulus_positions, block2, 2);
                    stimulus_positions = RandomizeStimulusPositions(stimulus_positions);
                }

                block_2_randomisation_completed = true;
            }
            else if (instance == block1+block2)
            {
                load = 3; 
                time_between_cues = time3; 

                if (block_3_randomisation_completed == false)
                {
                    stimulus_positions = new List<int>();
                    stimulus_positions = GenerateStimulusPositions(stimulus_positions, block3, 3);
                    stimulus_positions = RandomizeStimulusPositions(stimulus_positions);
                }
                block_3_randomisation_completed = true;
            }

            x = UnityEngine.Random.Range(0, digits.Count); // current random digit
            stimulus_position = SelectStimulusPosition(stimulus_positions, stimulus_position, instance); // select random position

            time = 0f;
            inputkey = null;

            StartCoroutine(Fixation());

            if (init_task == true)
            {
                if (GameManager.do_practice == true)
                {
                    practice_output = GameManager.do_practice ? 1 : 0;
                }
                else
                {
                    practice_output = GameManager.do_practice ? 1 : 0;
                    starter_cue = new List<string>{"", "", ""};
                }
                init_task = false;
            }
        }        
    }

    // select the position (left / middle / right) of the stimulus 
    private int SelectStimulusPosition(List<int> stimulusPositions, int random_position, int instance)
    {
        int index = 666;

        // if it's the first block, always choose the first position
        if (instance < block1)
        {
            random_position = 0;
            return random_position;
        }
        // if it's the third block, choose from the stimulusPositions list
        else if (instance >= (block1+block2))
        {
            index = instance - (block1 + block2);
            random_position = stimulusPositions[index];
            return random_position;
        }
        // if it's the second block, choose from the stimulusPositions list
        else
        {
            index = instance - (block1);
            random_position = stimulusPositions[index];
            return random_position;
        }
    }

    // draw which positions the stimuli will appear in
    private List<int> GenerateStimulusPositions(List<int> stimulusPositions, int block, int blockNumber)
    {
        float minimumNumber = Mathf.Floor(block / blockNumber);
        int pos = 666;
        
        for (var i = 0; i < minimumNumber; i++)
        {
            stimulusPositions.Add(0);
            stimulusPositions.Add(1);
            if (blockNumber == 3)
            {
                stimulusPositions.Add(2);
            }
        }

        if (stimulusPositions.Count != block)
        {
            for (var i = 0; i < (block - stimulusPositions.Count + 1); i++)
            {
                if (blockNumber == 3)
                {
                    pos = UnityEngine.Random.Range(0, 2);
                }
                else
                {
                    pos = UnityEngine.Random.Range(0, 1);
                }
                stimulusPositions.Add(pos);
            }
        }

        return stimulusPositions;
    }

    private List<int> RandomizeStimulusPositions(List<int> stimulus_positions)
    {
        for (int i = stimulus_positions.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int temp = stimulus_positions[i];
            stimulus_positions[i] = stimulus_positions[j];
            stimulus_positions[j] = temp;
        }

        return stimulus_positions;
    }   

    // Update is called once per frame
    void Update()
    {
        {
        #if !UNITY_WEBGL || UNITY_EDITOR
            GameManager.sender?.DispatchMessageQueue();
        #endif
        }

        time += Time.deltaTime;
        
        // Flash the cue and check if the correct key is pressed
        if (prev_load == load && enableKeys)
        {   
            if (stimulus_position == 0)
            {
                Flash(Cue1, a);
            }
            else if (stimulus_position == 1)
            {
                Flash(Cue2, b);
            }
            else if (stimulus_position == 2)
            {
                Flash(Cue3, c);
            }

            // if they take too long to respond, show feedback if practice. move onto the next trial
            if (time >= time_between_cues)
            {
                enableKeys = false;
                correct = false;

                if (GameManager.do_practice == true)
                {
                    if (stimulus_position == 0)
                    {
                        Cue1.transform.Find("Image").GetComponent<Image>().color = new Color32(255,0,0,255);
                    }
                    else if (stimulus_position == 1)
                    {
                        Cue2.transform.Find("Image").GetComponent<Image>().color = new Color32(255,0,0,255);
                    }
                    else if (stimulus_position == 2)
                    {
                        Cue3.transform.Find("Image").GetComponent<Image>().color = new Color32(255,0,0,255);
                    }
                    pop_up_flag_is_for_error = true;
                    ErrorBox.gameObject.SetActive(true);
                    // find the text component of errorbox and change the text
                    ErrorBox.transform.Find("Text").GetComponent<TMP_Text>().text = "Too slow! You needed to respond more quickly with " + prevkey + ", the PREVIOUS number shown in THIS square.\n\n" + 
                    "Press the spacebar to continue.";
                    StartCoroutine(WaitForSpacebar(pop_up_flag_is_for_error));
                }
                else
                {
                    StartCoroutine(Next());
                }
            }
        }
    }

    // display the current message until the spacebar is pressed
    private IEnumerator WaitForSpacebar(bool is_error = true)
    {
        // Wait until the spacebar is pressed
        while (!Input.GetKeyDown(KeyCode.Space))
        {
            yield return null; // Wait for the next frame
        }

        if (is_error)
        {
            ErrorBox.gameObject.SetActive(false);
            // Proceed with the next part of the task
            StartCoroutine(Next());
        }
        else
        {
            StarterBox.gameObject.SetActive(false);
            StartCoroutine(ResetCue());
        }
    }

    // display the fixation cross in each box or, if it's the first trial in the block, the reminder cue for which boxes to track
    IEnumerator Fixation()
    {   
        enableKeys = false;      
        Cue1.transform.Find("Cue").GetComponent<TMP_Text>().text = "+";
        
        if (load > 1)
        {
            Cue2.transform.Find("Cue").GetComponent<TMP_Text>().text = "+";
        }
        
        if (load > 2)
        {
            Cue3.transform.Find("Cue").GetComponent<TMP_Text>().text = "+";
        }

        yield return new WaitForSeconds(0.5f);
        enableKeys = true;

        if (prev_load != load)
        {
            enableKeys = false;
            StarterBox.gameObject.SetActive(true);
            // find the text component of StarterBox and change the text
            string edit_text_0 = "Track the ";
            string edit_text_1 = ".\n\n Press the spacebar to continue.";
            string edit_text = "";
            if (load == 1)
            {
                edit_text = edit_text_0 + "number shown in the left square only" + edit_text_1;
            }
            else if (load == 2)
            {
                edit_text = edit_text_0 + "numbers shown in the left and middle squares" + edit_text_1;
            }
            else
            {
                edit_text = edit_text_0 + "numbers shown in all three squares" + edit_text_1;
            }
            pop_up_flag_is_for_error = false;
            StarterBox.transform.Find("Text").GetComponent<TMP_Text>().text = edit_text;
            StartCoroutine(WaitForSpacebar(pop_up_flag_is_for_error));
        }
        else if (prev_load == load)
        {
            enableKeys = true;
            StartCoroutine(PresentCue());
        }
    }

    // display a random stimulus in the current box, making sure it's different from the previous stimulus in the same position or the other positions
    void RandomCue(GameObject cue, int pos_0, int pos_1, int pos_2)
    {
        x = UnityEngine.Random.Range(0, digits.Count);

        // make sure the current digit is different from the previous digit in the same position or the digits in the other positions
        while (x == pos_0 || x == pos_1 || x == pos_2)
        {
            x = UnityEngine.Random.Range(0, digits.Count);
        }

        cue.transform.Find("Cue").GetComponent<TMP_Text>().text = digits[x];
    }

    // display select and display the cue in the correct position
    IEnumerator PresentCue()
    {
        Cue1.transform.Find("Cue").GetComponent<TMP_Text>().text = "";
        Cue2.transform.Find("Cue").GetComponent<TMP_Text>().text = "";
        Cue3.transform.Find("Cue").GetComponent<TMP_Text>().text = "";

        yield return new WaitForSeconds(0.3f);

        time = 0f;
        if (stimulus_position == 0)
        {
            RandomCue(Cue1, a, b, c);
        }
        else if (stimulus_position == 1)
        {
            RandomCue(Cue2, a, b, c);
        }
        else
        {   
            RandomCue(Cue3, a, b, c);
        }
    }

    // set the Starter Cue for this block
    IEnumerator ResetCue()
    {   
        InitialText.text = "Starter Cue";
        //InitialText.transform.position = InitialText.transform.position + new Vector3(-400, 0, 0);
        // if load is 1, apend text to intitaltext.text

        Cue1.transform.Find("Cue").GetComponent<TMP_Text>().text = digits[x];

        a = x;
        starter_cue[0] = digits[a].ToString(); // starter cue in left box

        // starter cue in middle box
        if (load > 1)
        {
            x = UnityEngine.Random.Range(0, digits.Count);
            // make sure the digit in position 1 is different from the one in position 0
            while (x == a)
            {
                x = UnityEngine.Random.Range(0, digits.Count);
            }
            Cue2.transform.Find("Cue").GetComponent<TMP_Text>().text = digits[x];
            b = x;
            starter_cue[1] = digits[b].ToString();

            //InitialText.transform.position = InitialText.transform.position + new Vector3(+200, 0, 0);
        }
        // starter cue in right box
        if (load > 2)
        {
            x = UnityEngine.Random.Range(0, digits.Count);
            // make sure the digit in position 2 is different from the one in positions 0 and 1
            while (x == a || x == b)
            {
                x = UnityEngine.Random.Range(0, digits.Count);
            }
            Cue3.transform.Find("Cue").GetComponent<TMP_Text>().text = digits[x];
            c = x;
            starter_cue[2] = digits[c].ToString();

            //InitialText.transform.position = InitialText.transform.position + new Vector3(+200, 0, 0);
        }     

        yield return new WaitForSeconds(time_between_cues + 0.3f);
        prev_load = load; 
        InitialText.text = "";

        // prepare the starter cue for saving
        string DateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var outputs = new List<OutputParameter>
		{
			new ("o_RecallNBack_date_time", DateTime),
            new ("o_RecallNBack_task_order", GameManager.current_task_number.ToString()),
            new ("o_RecallNBack_trial", instance),
			new ("o_RecallNBack_load", load),
            new ("o_RecallNBack_stimulus", string.Join(";", starter_cue).ToString()),
            new ("o_RecallNBack_stimulus_position", "666"),
            new ("o_RecallNBack_solution", "666"),
			new ("o_RecallNBack_response_time", "666"),
            new ("o_RecallNBack_response", "666"),
            new ("o_RecallNBack_correct", "666"),
			new ("o_RecallNBack_practice_output", "3")
		};
        SendData(DateTime, GameManager.current_task_number, instance, load, string.Join(";", starter_cue).ToString(), 666, "666", 666, "666", "666", 3);
    
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // move to the next trial
    IEnumerator Next()
    {   
        
        response_time = time;
        enableKeys = false;

        // remove all cues
        Cue1.transform.Find("Cue").GetComponent<TMP_Text>().text = "";
        Cue2.transform.Find("Cue").GetComponent<TMP_Text>().text = "";
        Cue3.transform.Find("Cue").GetComponent<TMP_Text>().text = "";

        // update the previous digit in the correct position
        if (stimulus_position == 0)
        {
            a = x;
        }
        else if (stimulus_position == 1)
        {
            b = x;
        }
        else
        {
            c = x; 
        }
        
        yield return new WaitForSeconds(0.5f);
        
        prev_load = load; 
        instance += 1; 

        if (correct == true)
        {
            total_correct += 1;
        }

        if (inputkey == null)
        {
            inputkey = "no response";
        }

        // create a list storing the current digit and its position
        List<string> current = new List<string>{digits[x], stimulus_position.ToString()};

        // prepare the output for saving
        string DateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var outputs = new List<OutputParameter>
		{
			new ("o_RecallNBack_date_time", DateTime),
            new ("o_RecallNBack_task_order", GameManager.current_task_number.ToString()),
            new ("o_RecallNBack_trial", instance),
			new ("o_RecallNBack_load", load),
            new ("o_RecallNBack_stimulus", digits[x].ToString()),
            new ("o_RecallNBack_stimulus_position", stimulus_position.ToString()),
            new ("o_RecallNBack_solution", prevkey.ToString()),
			new ("o_RecallNBack_response_time", response_time.ToString()),
            new ("o_RecallNBack_response", inputkey.ToString()),
            new ("o_RecallNBack_correct", correct ? "1" : "0"),
			new ("o_RecallNBack_practice_output", practice_output.ToString())
		};
        SendData(DateTime, GameManager.current_task_number, instance, load, digits[x], stimulus_position, prevkey, response_time, inputkey, correct ? "1" : "0", practice_output);
        

        enableKeys = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Flash the cue and check if the correct key is pressed
    void Flash(GameObject cue, int previous)
    {
        prevkey = digits[previous];
     
        // if the correct key was pressed
        if (Input.GetKeyDown(digits[previous].ToLower()))
        {
            inputkey = digits[previous];
            enableKeys = false;
            correct = true;

            // if practice, give feedback 
            if (GameManager.do_practice == true)
            {
                cue.transform.Find("Image").GetComponent<Image>().color = new Color32(0,255,0,255);
            }
            
            // save the output and move to the next trial
            StartCoroutine(Next());
        }
        // if the wrong key was pressed
        else
        {
            for (var i = 0; i < digits.Count; i++)
            {
                if (Input.GetKeyDown(digits[i].ToLower()) && !Input.GetKeyDown(digits[previous].ToLower()))
                {
                    inputkey = digits[i];
                    enableKeys = false;
                    correct = false;

                    // if practice, give feedback. either way, proceed to the next trial
                    if (GameManager.do_practice == true)
                    {        
                        cue.transform.Find("Image").GetComponent<Image>().color = new Color32(255,0,0,255);
                        pop_up_flag_is_for_error = true;
                        ErrorBox.gameObject.SetActive(true);
                        // find the text component of errorbox and change the text
                        ErrorBox.transform.Find("Text").GetComponent<TMP_Text>().text = "Incorrect! You needed to respond with " + prevkey + ", the PREVIOUS number shown in THIS square.\n\n" + 
                        "Press the spacebar to continue.";
                        StartCoroutine(WaitForSpacebar(pop_up_flag_is_for_error));
                    }
                    else
                    {
                        StartCoroutine(Next());
                        break;
                    }
                }
            }
        }
    }

    // add the data to the Save queue
    private static void SendData(string DateTime, int task_number, int instance, int load, string stimulus, int stimulus_position, string solution, float response_time, string response, string correct, int practice_output)
    {   
        var outputs = new List<OutputParameter>
		{
			new ("o_RecallNBack_date_time", DateTime),
            new ("o_RecallNBack_task_order", GameManager.current_task_number),
            new ("o_RecallNBack_trial", instance),
			new ("o_RecallNBack_load", load),
            new ("o_RecallNBack_stimulus", stimulus),
            new ("o_RecallNBack_stimulus_position", stimulus_position),
            new ("o_RecallNBack_solution", solution),
			new ("o_RecallNBack_response_time", response_time),
            new ("o_RecallNBack_response", response),
            new ("o_RecallNBack_correct", correct),
			new ("o_RecallNBack_practice_output", practice_output)
		};

        Debug.Log("Saving: " + string.Join(", ", outputs.Select(o => $"{o.Name}: {o.Value}")));
        DataSaver.PrepareToSave(outputs, "NBack");
    }
}
