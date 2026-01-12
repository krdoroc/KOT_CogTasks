using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Random = System.Random;
using UnityEngine.SceneManagement;

using Dhive;
using System.Threading.Tasks;
// using System.IO;
// using Random = UnityEngine.Random;
// using JetBrains.Annotations;
// using System.Runtime.InteropServices;



public class SymbolDigitGM : MonoBehaviour
{
    public static GameManager GM = new GameManager();

    public GameObject Pair;
    public GameObject CuePair;

    private GameObject cue_pair; 
    private int x;
    private string key; 
    private bool enableKeys = true;
    private bool correct; 
    private Color initial_color = new Color32(71,71,71,255);

    private static float total_time = 0f;
    private static int total_correct = 0;
    private static bool init_task = true;
    private static List<string> symbols;
    private static int previous_x;
    private static int trial_number = 0;
    private static int practice_output = 3; // initialised value, will store whether the trial is a practice trial (1) or not (0)

    // Parameters
    public static int item_n = 9;
    public float max_time = 2f;
    public static float time_limit = 2f;
    public static float practice_time = 2f; //90f; 
    public static List<string> symbolCues = new List<string>{"\u039b", "\u00a7", "\u00b1", "\u00d8", 
    "\u00de", "\u0126", "\u0166", "\u018d", "\u01c1"};
    // use UTF-16 symbols: add \u and the last 4 codes  https://www.fileformat.info/info/charset/UTF-16/list.htm
    public static List<string> digits = new List<string>{"1" , "1", "1", "1", "1", "1", "1", "1", "1"};
    private float vertical_offset_1;
    private float vertical_offset_2;
    private float horizontal_offset;
    private float horizontal_scaler;
    private bool scene_just_loaded = true;

    // Start is called before the first frame update
    async void Start()
    {   
        Debug.Log("Symbol script started");
        GameManager.sender ??= DhiveSender.GetInstance(GameManager.participantTrialId);

        // make sure everything fits nicely on the screen
        if (scene_just_loaded)
        {
            vertical_offset_1 = GameManager.screen_height/4.32f;
            vertical_offset_2 = GameManager.screen_height/5.4f;
            horizontal_offset = GameManager.screen_width/19.2f;
            horizontal_scaler = horizontal_offset*2f;
            scene_just_loaded = false;
        }

        Random rng = new Random();

        if (init_task == true)
        {   
            GameManager.TaskId = DataLoader.GetDatabaseTaskId(GameManager.experimentData, GameManager.symbol_digit_name);
            if (GameManager.do_practice == true)
            {
                practice_output = GameManager.do_practice ? 1 : 0;
            }
            else
            {
                practice_output = GameManager.do_practice ? 1 : 0;
            }
            init_task = false;
            
            symbols = symbolCues.Take(item_n).OrderBy(x => rng.Next()).ToList();
        }
        
        int remainder = item_n % 2; 
        int mid_point = item_n/2;

        if (GameManager.do_practice == true)
        {
            max_time = practice_time;
        }
        else
        {
            max_time = time_limit; 
        }

        for (var i = 0; i < item_n; i++)
        {
            GameObject temp_pair;
            
            if (remainder == 0)
            {
                if (i == mid_point-1)
                {
                    temp_pair = (GameObject)Instantiate(Pair, transform.position + new Vector3(-horizontal_offset, vertical_offset_1, 0), transform.rotation, GameObject.FindGameObjectWithTag("Canvas").transform);
                }
                else if (i == mid_point)
                {
                    temp_pair = (GameObject)Instantiate(Pair, transform.position + new Vector3(horizontal_offset, vertical_offset_1, 0), transform.rotation, GameObject.FindGameObjectWithTag("Canvas").transform);
                } 
                else 
                {
                    temp_pair = (GameObject)Instantiate(Pair, transform.position + new Vector3((i-mid_point)*horizontal_scaler+horizontal_offset, vertical_offset_1, 0), transform.rotation, GameObject.FindGameObjectWithTag("Canvas").transform);
                }
            }
            else
            {
                temp_pair = (GameObject)Instantiate(Pair, transform.position + new Vector3((i-mid_point)*horizontal_scaler, vertical_offset_1, 0), transform.rotation, GameObject.FindGameObjectWithTag("Canvas").transform);
            }

            GeneratePair(temp_pair, symbols[i], digits[i]);
        }
        
        cue_pair = (GameObject)Instantiate(CuePair, transform.position + new Vector3(0, -vertical_offset_2, 0), transform.rotation, GameObject.FindGameObjectWithTag("Canvas").transform);


        x = rng.Next(0, item_n);
        while (x == previous_x)
        {
            x = rng.Next(0, item_n);
        }

        key = digits[x];

        trial_number = trial_number + 1;
        GeneratePair(cue_pair, symbols[x], "");
    }

    // Update is called once per frame
    void Update()
    {
        {
        #if !UNITY_WEBGL || UNITY_EDITOR
            GameManager.sender?.DispatchMessageQueue();
        #endif
        }

        total_time += Time.deltaTime;

        // when time runs out, save what they got up to
        if (total_time >= max_time) // time limit = 90s
        {
            enableKeys = false;
            SceneManager.LoadScene("FinishedCogTask");

            string DateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var outputs = new List<OutputParameter>
            {
                new ("o_SymbolDigit_date_time", DateTime),
                new ("o_SymbolDigit_task_order", GameManager.current_task_number.ToString()),
                new ("o_SymbolDigit_trial_number", trial_number.ToString()),
                new ("o_SymbolDigit_total_time", total_time.ToString()),
                new ("o_SymbolDigit_total_correct", total_correct.ToString()),
                new ("o_SymbolDigit_practice_output", practice_output.ToString())
            };

            SendData(DateTime, GameManager.current_task_number, trial_number, "666", total_time, total_correct, practice_output);

            GameManager.total_correct = total_correct;

            // reset all private static variables
            total_time = 0f;
            total_correct = 0;
            // Debug.Log("reset");
            trial_number = 0;
            init_task = true;
            symbols = new List<string>{};
            previous_x = 0;
        }
        // when they respond, prepare the response for saving and proceed to the next 'trial'
        else if (enableKeys)
        {
            // correct responses
            if (Input.GetKeyDown(key))
            {
                GeneratePair(cue_pair, symbols[x], key);
                enableKeys = false;

                correct = true;
                total_correct += 1;
                StartCoroutine(Flash(cue_pair, correct));

                string DateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var outputs = new List<OutputParameter>
                {
                    new ("o_SymbolDigit_date_time", DateTime),
                    new ("o_SymbolDigit_task_order", GameManager.current_task_number.ToString()),
                    new ("o_SymbolDigit_trial_number", trial_number.ToString()),
                    new ("o_SymbolDigit_total_time", total_time.ToString()),
                    new ("o_SymbolDigit_total_correct", total_correct.ToString()),
                    new ("o_SymbolDigit_practice_output", practice_output.ToString())
                };
            
                SendData(DateTime, GameManager.current_task_number, trial_number, key, total_time, total_correct, practice_output);
            }
            // incorrect responses
            else
            {
                for (var i = 1; i <= item_n; i++)
                {
                    if (Input.GetKeyDown(i.ToString()) && !Input.GetKeyDown(key))
                    {
                        GeneratePair(cue_pair, symbols[x], i.ToString());
                        enableKeys = false;
                        
                        correct = false;
                        StartCoroutine(Flash(cue_pair, correct));

                        break;
                    }
                }
                
            }
        }
    }

    // create and display a symbol-digit pair, i.e. a target symbol mapped to a digit solution
    void GeneratePair(GameObject pair, string sym, string dig)
    {
        for (var j = 0; j < pair.transform.childCount; j++)
        {   
            GameObject obj = pair.transform.GetChild(j).gameObject;
            TMP_Text cue_text = obj.transform.Find("Cue").GetComponent<TMP_Text>();

            if(pair.transform.GetChild(j).name == "Symbol_obj")
            {
                cue_text.text = sym;
            }
            else
            {
                cue_text.text = dig;
            }
        }
    }

    // flash green for correct responses, red for incorrect responses. update stimulus if correct
    IEnumerator Flash(GameObject pair, bool correct)
    {
        GameObject obj = pair.transform.GetChild(1).gameObject;
        Image image = obj.transform.Find("Image").GetComponent<Image>();

        for (var j = 0; j < pair.transform.childCount; j++)
        {   
            obj = pair.transform.GetChild(j).gameObject;
            image = obj.transform.Find("Image").GetComponent<Image>();

            if (!correct)
            {
                image.color = new Color32(255,0,0,255);
            }
            else
            {
                image.color = new Color32(0,255,0,255);
            }
            
        }

        yield return new WaitForSeconds(0.5f);

        enableKeys = true;

        for (var j = 0; j < pair.transform.childCount; j++)
        {  
            obj = pair.transform.GetChild(j).gameObject;
            image = obj.transform.Find("Image").GetComponent<Image>();
            image.color = initial_color;
            GeneratePair(cue_pair, symbols[x], "");
        }

        if (correct)
        {
            previous_x = x;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }


    // add data to the save queue
    private static void SendData(string DateTime, int task_number, int trial_number, string stimulus, float total_time, int total_correct, int practice_output)
    {   
        var outputs = new List<OutputParameter>
        {
            new ("o_SymbolDigit_date_time", DateTime),
            new ("o_SymbolDigit_task_order", GameManager.current_task_number),
            new ("o_SymbolDigit_trial_number", trial_number),
            new ("o_SymbolDigit_stimulus", stimulus),
            new ("o_SymbolDigit_total_time", total_time),
            new ("o_SymbolDigit_total_correct", total_correct),
            new ("o_SymbolDigit_practice_output", practice_output)
        };

        Debug.Log("Saving: " + string.Join(", ", outputs.Select(o => $"{o.Name}: {o.Value}")));
        DataSaver.PrepareToSave(outputs, "SymbolDigit");
    }
}