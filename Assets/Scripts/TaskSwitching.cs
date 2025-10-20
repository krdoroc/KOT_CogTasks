using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Random = System.Random;
using System.Linq;
using Dhive;
using System.Threading.Tasks;

public class TaskSwitching : MonoBehaviour
{
    public static GameManager GM = new GameManager();

    // define the four quadrants and the message instruction what rule applied to that block
    public GameObject TopLeft;
    public GameObject TopRight;
    public GameObject BottomLeft;
    public GameObject BottomRight;
    public GameObject BlockFocus;
    public Image Rest;
    public Slider slider;

    // some params 
    private static int block_no = 0;
    private static int instance = 0;
    private static int count = 0;
    private static int total_correct = 0;
    private static bool init_task = true;
    private static List<int> block_trials = new List<int>{};
    private static int practice_output = 3; // initialised value, will store whether the trial is a practice trial (1) or not (0)

    private int letter_vowel = 0;  // binary
    private int digit_odd = 0;  // binary
    private int top = 0;  // binary
    private int left = 0;   // binary
    private bool enableKeys = true;
    private GameObject TheCue; 
    private bool correct; 
    private float time = 0.0f; 
    private float response_time = 0.0f;
    private string response = null;
    private string cue_to_display = null;
    private string letter_cue = null;
    private string digit_cue = null;
    private bool first_TL_display = true;
    private bool first_TR_display = true;
    private bool first_BL_display = true;
    private bool first_BR_display = true;
    private static List<string> stimulus_list = new List<string>{};
    public static List<string> real_stimulus_list = new List<string>{};
    public static List<string> practice_stimulus_list = new List<string>{};
    private static int is_congruent = 666;  // binary, whether the trial is congruent i.e. whether both number/letter rules would give the same answer
    public RectTransform border_position;
    public RectTransform errorLetter_position;
    public RectTransform errorDigit_position;
    public GameObject LetterFocus;
    public GameObject DigitFocus;
    public GameObject ErrorLetter;
    public GameObject ErrorDigit;
    private static float vertical_offset;
    private static float horizontal_offset;
    private static int switch_trial = 666;  // binary, whether it's a switch trial (different rule than previous trial)


    // Main parameters, these will get overwritten by the DataLoader
    public List<string> consonants = new List<string>{"G", "K", "M", "R"};
    public List<string> vowels = new List<string>{"A", "E", "I", "U"};
    public List<string> odds = new List<string>{"3", "5", "7", "9"};
    public List<string> evens = new List<string>{"2", "4", "6", "8"};
    public int block1 = 1; //32;
    public int block2 = 1; //32;
    public int block3 = 2; //128;
    public static int real_block1 = 1; //32;
    public static int real_block2 = 1; //32;
    public static int real_block3 = 2; //128;
    public static int practice_block1 = 1; //32;
    public static int practice_block2 = 1; //32;
    public static int practice_block3 = 2; //128;
    public static float time_limit = 5f;
    public static float rest_time = 2; //30f;
    public static float rule_display_time = 3f;

    // Start is called before the first frame update
    async void Start()
    {
        Debug.Log("switch script started");
        GameManager.sender ??= DhiveSender.GetInstance(GameManager.participantTrialId);

        // find the width of the screen
        float horizontal_factor = 47/1920f; // trial and error to find the right factor
        float vertical_factor = 10/1080f;  // trial and error to find the right factor
        horizontal_offset = GameManager.screen_width * horizontal_factor;
        vertical_offset = GameManager.screen_height * vertical_factor;

        // find ErrorLetter Gameobject and change its text to explain the rules for this participant
        string letter_text = "- Letter Task -\n\n" +
                            "Consonant: G, K, M, R\n" +
                            $"Press \"{GameManager.consonant_key} arrow\"\n\n" + 
                            "Vowel: A, E, I, U\n" + 
                            $"Press \"{GameManager.vowel_key} arrow\"\n\n";
        string digit_text = "- Number Task -\n\n" +
                            "Odd: 3, 5, 7, 9\n" +
                            $"Press \"{GameManager.odd_key} arrow\"\n\n" + 
                            "Even: 2, 4, 6, 8\n" + 
                            $"Press \"{GameManager.even_key} arrow\"\n\n";
        // find the text component of the letterfocus gameobject and change its text
        LetterFocus.transform.Find("Cue").GetComponent<TMP_Text>().text = letter_text;
        ErrorLetter.transform.Find("Cue").GetComponent<TMP_Text>().text = letter_text;
        DigitFocus.transform.Find("Cue").GetComponent<TMP_Text>().text = digit_text;
        ErrorDigit.transform.Find("Cue").GetComponent<TMP_Text>().text = digit_text;
        LetterFocus.gameObject.SetActive(false);
        ErrorLetter.gameObject.SetActive(false);
        DigitFocus.gameObject.SetActive(false);
        ErrorDigit.gameObject.SetActive(false);

        // use a serif font so that 7s, 1s and Is are clearly differentiable
        TMP_FontAsset my_font = Resources.Load<TMP_FontAsset>("Muc-Zeit-Medium SDF");

        Rest.enabled = false;
        BlockFocus.gameObject.SetActive(false);

        slider.gameObject.SetActive(false);
        slider.value = 1;
        
        stimulus_list = real_stimulus_list;

        // initialise params
        if (GameManager.practice == true)
        {
            block1 = practice_block1;
            block2 = practice_block2;
            block3 = practice_block3;
            stimulus_list = practice_stimulus_list;
        }
        else
        {
            block1 = real_block1;
            block2 = real_block2;
            block3 = real_block3;
        }

        // if you finished the last trial, end the task and reset params
        if (instance >= block1+block2+block3)
        {
            enableKeys = false;
            SceneManager.LoadScene("FinishedCogTask");

            GameManager.total_correct = total_correct;
            GameManager.total_instances = block1+block2+block3;

            // reset private static variables
            block_no = 0;
            instance = 0;
            count = 0;
            total_correct = 0;
            init_task = true;
            block_trials = new List<int>{};
        } 
        // otherwise display the appropriate cue
        else
        {
            // if first trial
            if (instance == 0) // set trials in blocks
            {
                count = 0;
                block_no += 1;
            } 
            // if the end of block 1
            else if (instance == block1)
            {
                count = 0;
                block_no += 1;
            } 
            // if the end of block 2
            else if (instance == block1+block2)
            {
                count = 0;
                block_no += 1;
                
                first_TL_display = true;
                first_TR_display = true;
                first_BL_display = true;
                first_BR_display = true;
            }
            // if halfway through block 3
            else if (instance == block1+block2+block3/2 & GameManager.practice == false)
            {
                count = 0;
            }

            letter_cue = stimulus_list[instance][0].ToString();
            digit_cue = stimulus_list[instance][1].ToString();

            // block 1 (letters) only at the top
            if (block_no == 1)
            {
                top = 1; // if top == 1, then top quadrant else bottom quadrant    
            }
            // block 2 (numbers) only at the bottom
            else if (block_no == 2)
            {
                top = 0; // if top == 1, then top quadrant else bottom quadrant   
            }
            // block 3 alternating clockwise between all four quadrants
            else if (block_no == 3)
            {
                if (count % 4 == 0 || count % 4 == 1)
                {
                    top = 1;
                }
                else 
                {
                    top = 0;
                }
            }        

            // take the next cue from the input list
            cue_to_display = stimulus_list[instance];

            // if left == 1, then left quadrant else right quadrant
            left = UnityEngine.Random.Range(0, 2);

            // display cue in appropriate quadrant
            if (block_no == 1)
            {
                if (count % 2 == 0)
                {
                    TheCue = TopLeft;
                    if (first_TL_display == true)
                    {
                        AdjustCueFontAndPosition(my_font);
                        first_TL_display = false;
                    }
                }
                else
                {
                    TheCue = TopRight;
                    if (first_TR_display == true)
                    {
                        AdjustCueFontAndPosition(my_font);
                        first_TR_display = false;
                    }
                }
            }
            else if (block_no == 2)
            {
                if (count % 2 == 0)
                {
                    TheCue = BottomLeft;
                    if (first_BL_display == true)
                    {
                        AdjustCueFontAndPosition(my_font);
                        first_BL_display = false;
                    }
                }
                else
                {
                    TheCue = BottomRight;
                    if (first_BR_display == true)
                    {
                        AdjustCueFontAndPosition(my_font);
                        first_BR_display = false;
                    }
                }
            }
            else if (block_no ==3)
            {
                if (count % 4 == 0)
                {
                    TheCue = TopLeft;
                    switch_trial = 1;
                    if (first_TL_display == true)
                    {
                        AdjustCueFontAndPosition(my_font);
                        first_TL_display = false;
                    }
                }
                else if (count % 4 == 1)
                {
                    TheCue = TopRight;
                    switch_trial = 0;
                    if (first_TL_display == true)
                    {
                        AdjustCueFontAndPosition(my_font);
                        first_TL_display = false;
                    }
                }
                else if (count % 4 == 2)
                {
                    TheCue = BottomRight;
                    switch_trial = 1;
                    if (first_TL_display == true)
                    {
                        AdjustCueFontAndPosition(my_font);
                        first_TL_display = false;
                    }
                }
                else
                {
                    TheCue = BottomLeft;
                    switch_trial = 0;
                    if (first_TL_display == true)
                    {
                        AdjustCueFontAndPosition(my_font);
                        first_TL_display = false;
                    }
                }
            }

            // if it's time to rest, do so
            StartCoroutine(RestScreen());

            if (init_task == true)
            {
                if (GameManager.practice == true)
                {
                    practice_output = GameManager.practice ? 1 : 0;
                }
                else
                {
                    practice_output = GameManager.practice ? 1 : 0;
                }
                init_task = false;
            }
        }
    }

    // change to serif font, try to ensure cues are displaying in the right spot
    private void AdjustCueFontAndPosition(TMP_FontAsset font)
    {
        if (GameManager.letter_on_left == true)
        {
            TheCue.transform.Find("CueLeft").GetComponent<TMP_Text>().font = font;
            TheCue.transform.Find("CueLeft").transform.position = new Vector3(TheCue.transform.Find("CueLeft").transform.position.x - horizontal_offset, TheCue.transform.Find("CueLeft").transform.position.y - vertical_offset, TheCue.transform.Find("CueLeft").transform.position.z);
        }
        else
        {
            TheCue.transform.Find("CueRight").GetComponent<TMP_Text>().font = font;
            TheCue.transform.Find("CueLeft").transform.position = new Vector3(TheCue.transform.Find("CueLeft").transform.position.x - horizontal_offset, TheCue.transform.Find("CueLeft").transform.position.y + vertical_offset, TheCue.transform.Find("CueLeft").transform.position.z);
        }
    }

    // Update is called once per frame
    void Update()
    {
        {
        #if !UNITY_WEBGL || UNITY_EDITOR
            GameManager.sender?.DispatchMessageQueue();
        #endif
        }

        if (Rest.enabled == true)
        {
            time = 0f;
        }

        time += Time.deltaTime;

        // display timer if it's a rest
        if (slider.gameObject.activeSelf == true)
        {
            slider.value -= Time.deltaTime/rest_time;
            slider.value = Mathf.Max(0, slider.value);
        }

        // check if response is correct 
        if (enableKeys)
        {
            // record whether the digit is odd and whether the letter is a vowel
            if (odds.Contains(stimulus_list[instance][1].ToString()))
            {
                digit_odd = 1;
            }
            if (vowels.Contains(stimulus_list[instance][0].ToString()))
            {
                letter_vowel = 1;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                response_time = time;
                response = "left";
                enableKeys = false;

                if (top == 1)
                {
                    // check if the current stimulus is a vowel
                    if (letter_vowel == 1)
                    {
                        if (GameManager.congruence_type == 1 || GameManager.congruence_type == 3)
                        {
                            correct = false;
                        }
                        else
                        {
                            correct = true;
                        }
                    }
                    else
                    {
                        if (GameManager.congruence_type == 1 || GameManager.congruence_type == 3)
                        {
                            correct = true;
                        }
                        else
                        {
                            correct = false;
                        }
                    }
                }
                else
                {
                    // check if the current stimulus is an odd number
                    if (digit_odd == 1)
                    {
                        if (GameManager.congruence_type == 1 || GameManager.congruence_type == 2)
                        {
                            correct = false;
                        }
                        else
                        {
                            correct = true;
                        }
                    }
                    else
                    {
                        if (GameManager.congruence_type == 1 ||GameManager.congruence_type == 2)
                        {
                            correct = true;
                        }
                        else
                        {
                            correct = false;
                        }
                    }
                }
                if (correct)
                {
                    StartCoroutine(FlashCorrect());
                }
                else
                {
                    StartCoroutine(FlashError());
                }   
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                response_time = time;
                response = "right";
                enableKeys = false;

                if (top == 1)
                {
                    // check if stimulus_list[0] is in the list of vowels
                    if (letter_vowel == 1)
                    {
                        if (GameManager.congruence_type == 1 || GameManager.congruence_type == 3)
                        {
                            correct = true;
                        }
                        else
                        {
                            correct = false;
                        }
                    }
                    else
                    {
                        if (GameManager.congruence_type == 1 || GameManager.congruence_type == 3)
                        {
                            correct = false;
                        }
                        else
                        {
                            correct = true;
                        }
                    }
                }
                else
                {
                    // check if the current stimulus is an odd number
                    if (digit_odd == 1)
                    {
                        if (GameManager.congruence_type == 1 || GameManager.congruence_type == 2)
                        {
                            correct = true;
                        }
                        else
                        {
                            correct = false;
                        }
                    }
                    else
                    {
                        if (GameManager.congruence_type == 1 || GameManager.congruence_type == 2)
                        {
                            correct = false;
                        }
                        else
                        {
                            correct = true;
                        }
                    }
                }

                // if correct, flash green and move to next trial
                if (correct)
                {
                    StartCoroutine(FlashCorrect());
                }
                // if incorrect, flash red, remind them of the rules, and then move to the next trial
                else
                {
                    StartCoroutine(FlashError());
                }                
            }
        }

        // if they run out of time on a trial, flash red and remind them of the rules
        if (time >= time_limit && enableKeys == true)
        {
            response_time = time;
            response = "no response";
            enableKeys = false;
            correct = false;
            
            StartCoroutine(FlashError());
        }
    }

    // function that takes three arguments: congruence type, whether a letter is a vowel, and whether a digit is odd. it returns nothing, but updates the value of is_congruent
    void CheckCongruence(int congruence_type, int letter_vowel, int digit_odd)
    {
        if (congruence_type == 0 || congruence_type == 1)
        {
            if (letter_vowel == digit_odd)
            {
                is_congruent = 1;
            }
            else
            {
                is_congruent = 0;
            }
        }
        else
        {
            if (letter_vowel == digit_odd)
            {
                is_congruent = 0;
            }
            else
            {
                is_congruent = 1;
            }
        }
    }

    // main engine proceeding through the trials
    IEnumerator Next()
    {
        // no. of instance
        instance += 1; 
        count += 1;

        string task = null;
        if (block_no == 1)
        {
            task = "letter";
        }
        else if (block_no == 2)
        {
            task = "digit";
        } 
        else
        {
            task = "switch";
        }

        string type = null;
        if (top == 1)
        {
            if (letter_vowel == 0)
            {
                type = type + "C";
            } 
            else
            {
                type = type + "V";
            }
        }
        else
        {
            if (digit_odd == 0)
            {
                type = type + "E";
            } 
            else
            {
                type = type + "O";
            }
        }

        CheckCongruence(GameManager.congruence_type, letter_vowel, digit_odd);

        // prepare data for saving
        string DateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
		var outputs = new List<OutputParameter>
		{
			new ("o_TaskSwitching_date_time", DateTime),
            new ("o_TaskSwitching_task_order", GameManager.current_task_number.ToString()),
            new ("o_TaskSwitching_block_no", block_no.ToString()),
			new ("o_TaskSwitching_trial", instance.ToString()),
			new ("o_TaskSwitching_cue_to_display", cue_to_display),
            new ("o_TaskSwitching_is_congruent", is_congruent.ToString()),
            new ("o_TaskSwitching_letter_left", GameManager.letter_on_left ? "1" : "0"),
            new ("o_TaskSwitching_congruence_type", GameManager.congruence_type.ToString()),
            new ("o_TaskSwitching_switch_trial", switch_trial),
            new ("o_TaskSwitching_response_time", response_time.ToString()),
            new ("o_TaskSwitching_response", response.ToString()),
            new ("o_TaskSwitching_correct", correct ? "1" : "0"),
			new ("o_TaskSwitching_practice_output", practice_output.ToString())
		};
        SendData(DateTime, GameManager.current_task_number, instance, block_no, cue_to_display, is_congruent, switch_trial, response_time, response, correct ? "1" : "0", practice_output);

        if (correct == true)
        {
            total_correct += 1;
        }

        TheCue.transform.Find("Image").GetComponent<Image>().color = new Color32(71,71,71,255);
        TheCue.transform.Find("CueLeft").GetComponent<TMP_Text>().text = "";
        TheCue.transform.Find("CueRight").GetComponent<TMP_Text>().text = "";

        yield return new WaitForSeconds(0.1f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // green flash if correct, move to next trial
    IEnumerator FlashCorrect()
    {
        TheCue.transform.Find("Image").GetComponent<Image>().color = new Color32(0,255,0,255);
        yield return new WaitForSeconds(0.2f);

        StartCoroutine(Next());
    }

    // red flash if incorrect, reminder of the rules, move to next trial
    IEnumerator FlashError()
    {
        // flash red 
        GameObject error_cue; 

        TheCue.transform.Find("Image").GetComponent<Image>().color = new Color32(255,0,0,255);
        yield return new WaitForSeconds(0.2f);

        // reminder of the rules
        if (top == 1)
        {
            ErrorLetter.gameObject.SetActive(true);
        }
        else
        {
            ErrorDigit.gameObject.SetActive(true);
        }
        
        yield return new WaitForSeconds(rule_display_time);

        //Destroy(error_cue);
        ErrorLetter.gameObject.SetActive(false);
        ErrorDigit.gameObject.SetActive(false);

        StartCoroutine(Next());
    }

    // if it's a rest screen, rest and display the timer. if we're in between blocks, remind of the rules for that block
    IEnumerator RestScreen()
    {
        enableKeys = false;
        if (instance == 0 || instance == block1 || instance == block1+block2 || (instance == block1+block2+block3/2 & GameManager.practice == false))
        {
            Rest.enabled = true;
            BlockFocus.gameObject.SetActive(true);

            Rest.transform.GetChild(0).GetComponent<TMP_Text>().text = "+";
            yield return new WaitForSeconds(2f);
        
            if (instance == block1+block2)
            {
                if (GameManager.practice == false)
                {
                    slider.gameObject.SetActive(true);
                    Rest.transform.GetChild(0).GetComponent<TMP_Text>().text = "Rest";
                    yield return new WaitForSeconds(rest_time);
                    slider.gameObject.SetActive(false);
                }
                BlockFocus.GetComponent<TMP_Text>().text = "Letters AND Numbers task: all boxes";

                LetterFocus.gameObject.SetActive(true);
                DigitFocus.gameObject.SetActive(true);

                yield return new WaitForSeconds(rule_display_time + 5f);

                // Destroy(error_cue1);
                // Destroy(error_cue2);
                LetterFocus.gameObject.SetActive(false);
                DigitFocus.gameObject.SetActive(false);
            }
            else if (instance == 0 || instance == block1)
            {
                GameObject  error_cue;
                if (top == 1)
                {
                    BlockFocus.GetComponent<TMP_Text>().text = "LETTERS task only: top row";
                    LetterFocus.gameObject.SetActive(true);
                    Rest.transform.GetChild(0).GetComponent<TMP_Text>().text = "";
                }
                else
                {
                    BlockFocus.GetComponent<TMP_Text>().text = "NUMBERS task only: bottom row";
                    DigitFocus.gameObject.SetActive(true);
                    Rest.transform.GetChild(0).GetComponent<TMP_Text>().text = "";
                }

                yield return new WaitForSeconds(rule_display_time + 5f);
                //Destroy(error_cue);
                LetterFocus.gameObject.SetActive(false);
                DigitFocus.gameObject.SetActive(false);
            }
            else if (instance == block1+block2+block3/2)
            {
                if (GameManager.practice == false)
                {
                    slider.gameObject.SetActive(true);
                    Rest.transform.GetChild(0).GetComponent<TMP_Text>().text = "Rest";
                    yield return new WaitForSeconds(rest_time);
                    slider.gameObject.SetActive(false);
                }
            }

            time = 0.0f;
            Rest.enabled = false;
            BlockFocus.gameObject.SetActive(false);
            Rest.transform.GetChild(0).GetComponent<TMP_Text>().text = "+";
        }

        if (GameManager.letter_on_left == true)
        {
            TheCue.transform.Find("CueLeft").GetComponent<TMP_Text>().text = letter_cue;
            TheCue.transform.Find("CueRight").GetComponent<TMP_Text>().text = digit_cue;
        }
        else
        {
            TheCue.transform.Find("CueLeft").GetComponent<TMP_Text>().text = digit_cue;
            TheCue.transform.Find("CueRight").GetComponent<TMP_Text>().text = letter_cue;
        }
        time = 0.0f;
        enableKeys = true;
    }
    
    // add data that needs saving to the save queue
    private static void SendData(string DateTime, int task_number, int instance, int block_no, string cue_to_display, int is_congruent, int switch_trial, float response_time, string response, string correct, int practice_output)
    {   
        var outputs = new List<OutputParameter>
		{
			new ("o_TaskSwitching_date_time", DateTime),
            new ("o_TaskSwitching_task_order", task_number),
            new ("o_TaskSwitching_block_no", block_no),
			new ("o_TaskSwitching_trial", instance),
			new ("o_TaskSwitching_cue_to_display", cue_to_display),
            new ("o_TaskSwitching_is_congruent", is_congruent.ToString()),
            new ("o_TaskSwitching_letter_left", GameManager.letter_on_left ? "1" : "0"),
            new ("o_TaskSwitching_congruence_type", GameManager.congruence_type),
            new ("o_TaskSwitching_switch_trial", switch_trial),
            new ("o_TaskSwitching_response_time", response_time),
            new ("o_TaskSwitching_response", response),
            new ("o_TaskSwitching_correct", correct),
			new ("o_TaskSwitching_practice_output", practice_output)
		};

        Debug.Log("Saving: " + string.Join(", ", outputs.Select(o => $"{o.Name}: {o.Value}")));
        Debug.Log("Queue size is " + DataSaver.GetQueueSize());
        DataSaver.AddDataToSave(GameManager.TaskId, outputs);
    }
}

