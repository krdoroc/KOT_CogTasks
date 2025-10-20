using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Random = System.Random;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using Dhive;
using System.Threading.Tasks;
public class StopSignal : MonoBehaviour
{
    public static GameManager GM = new GameManager();

    // GameObjects in the scene
    public TMP_Text Arrow;
    public TMP_Text Error;
    public TMP_Text Fixation;
    public Image BlueLeftImage;
    public Image BlueRightImage;
    public Image BlackLeftImage;
    public Image BlackRightImage;
    public Slider slider;

    private bool left = true; 
    private bool stop = true;
    private string response = null; 
    private bool enableKeys = true;
    private float time;
    private float response_time = 0f;
    private bool correct = false;
    private string temp_SSD = null;

    private static int total_correct = 0; 
    private static int instance = 0;
    private static int block = 0;
    private static bool init_task = true;
    private static float stop_delay = 0f;
    private static List<int> block_trials = new List<int>{}; // total number of trials (randomised order for left or right)
    private static List<int> stop_trials = new List<int>{}; // total number of trials (randomised order for stop signals)
    private static int arrows_missed = 0; // trailing/running arrows missed
    private static float total_response_time = 0f; // trailing/running avg. response time
    private static int stops_missed = 0; // trailing/running stop signals missed
    private static int count_go = 0; // trailing/running no. of arrows
    private static int count_stop = 0; // trailing/running no. of stops
    private static int practice_output = 3; // initialised value, will store whether the trial is a practice trial (1) or not (0)


    // Parameters
    public int no_instances;
    public static int no_real_instances = 4; //20; // 20 trials per block
    public static int no_practice_instances = 8; 
    public int practice_blocks = 1; 
    public int no_blocks = 1; 
    public static int real_blocks = 1; //10; // 10 blocks, each with 20 trials, 200 trials in total
    public static float time_limit = 1.5f; // time limit = 1.5s
    public static float rest_time = 10f; // rest time = 30s
    public static float feedback_time = 4f; // how long to display feedback for = 5s
    public static float init_stop_delay = 0.3f; // initial stop signal delay = 0.3s
    public static float delta_stop_delay = 0.05f; // incremental stop signal delay = 0.05s

    private static float start_time;
    private static float max_time = 0f;
    private static float min_time = 40f;


    // Start is called before the first frame update
    async void Start()
    {   
        Debug.Log("StopSig script started");
        GameManager.sender ??= DhiveSender.GetInstance(GameManager.participantTrialId);

        slider.gameObject.SetActive(false);
        slider.value = 1;
        Arrow.text = "";
        Error.text = "";

        BlueLeftImage.gameObject.SetActive(false);
        BlueRightImage.gameObject.SetActive(false);
        BlackLeftImage.gameObject.SetActive(false);
        BlackRightImage.gameObject.SetActive(false);
        
        // set starting params
        if (GameManager.practice == true)
        {
            practice_output = GameManager.practice ? 1 : 0;
            no_instances = no_practice_instances; //5;
            no_blocks = practice_blocks;
        }
        else
        {
            no_instances = no_real_instances; //20;
            no_blocks = real_blocks; //10;
        }

        if (instance == 0)
        {
            if (block == 0)
            {
                stop_delay = init_stop_delay;
            }
                
            GenerateTrialsBlock(no_instances);
        }

        // if you've finished the task, reset everything
        if (block > no_blocks)
        {            
            enableKeys = false;

            Arrow.text = "";
            Error.text = "";
            Fixation.text = "";

            SceneManager.LoadScene("FinishedCogTask");

            GameManager.total_correct = total_correct;
            GameManager.total_instances = no_instances*no_blocks;

            // reset all private static variables
            total_correct = 0; 
            instance = 0;
            block = 0;
            init_task = true;
            stop_delay = 0f;
            block_trials = new List<int>{};
            stop_trials = new List<int>{};
            arrows_missed = 0;
            total_response_time = 0f;
            stops_missed = 0;
            count_go = 0;
            count_stop = 0;
        }
        // otherwise, display the next trial
        else 
        {
            practice_output = GameManager.practice ? 1 : 0;
            if (block_trials[instance] % 2 == 0) // left or right arrow
            {
                left = true; // left arrow
            } 
            else 
            {
                left = false; // right arrow
            }

            if (stop_trials.Contains(block_trials[instance])) // (new version, when stopping is 25% among left and 25% among right)
            {
                stop = true; // present
                temp_SSD = stop_delay.ToString();
            }
            else
            {
                stop = false;
                temp_SSD = null;
            }

            StartCoroutine(FlashCue(left));

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

    // Update is called once per frame
    void Update()
    {   
        {
        #if !UNITY_WEBGL || UNITY_EDITOR
            GameManager.sender?.DispatchMessageQueue();
        #endif
        }

        time += Time.deltaTime;
        // update the timer
        if (instance == no_instances)
        {
            slider.value -= Time.deltaTime/rest_time;
            slider.value = Mathf.Max(0, slider.value);
        }

        // store responses, response times, and whether they were correct
        if (enableKeys)
        {
            response_time = time;
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                response = "left";
                enableKeys = false;
                if (left == true && stop == false)
                {
                    correct = true; 
                }
                start_time = Time.time;
            } 
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                response = "right";
                enableKeys = false;
                if (left == false && stop == false)
                {
                    correct = true; 
                }
                start_time = Time.time;
            }
            else if (time >= time_limit) // time limit = 0.5s
            {
                enableKeys = false;
                response = "no response";

                Arrow.text = "";
                BlueLeftImage.gameObject.SetActive(false);
                BlueRightImage.gameObject.SetActive(false);
                BlackLeftImage.gameObject.SetActive(false);
                BlackRightImage.gameObject.SetActive(false);

                if (stop == true)
                {
                    correct = true;
                }
                start_time = Time.time;
            }

            if (enableKeys == false)
            {
                StartCoroutine(FlashError());
            }
        }
    }

    // display the stimulus / cue
    IEnumerator FlashCue(bool left)
    {
        float time_taken = Time.time - start_time;
        if (time_taken > max_time && time_taken < 5f)
        {
            max_time = time_taken;
        }
        if (time_taken < min_time)
        {
            min_time = time_taken;
        }
        Fixation.text = "+";
        enableKeys = false;
        yield return new WaitForSeconds(0.5f);
        Fixation.text = "";

        enableKeys = true;
        time = 0f;

        // if the stimulus is left, display the left arrow
        if (left == true)
        {
            Arrow.text = "\u2190";
            Arrow.alignment = TextAlignmentOptions.MidlineLeft;

            BlueRightImage.gameObject.SetActive(true);
            BlackRightImage.gameObject.SetActive(true);
        }
        // otherwise, display the right arrow
        else
        {
            Arrow.text = "\u2192";
            Arrow.alignment = TextAlignmentOptions.MidlineRight;

            BlueLeftImage.gameObject.SetActive(true);
            BlackLeftImage.gameObject.SetActive(true);
        }

        // if it's a stop trial, display the stop signal
        if (stop == true)
        {
            StartCoroutine(FlashStop());
        }

        yield return new WaitForSeconds(2f);

        Arrow.text = "";
        BlueLeftImage.gameObject.SetActive(false);
        BlueRightImage.gameObject.SetActive(false);
        BlackLeftImage.gameObject.SetActive(false);
        BlackRightImage.gameObject.SetActive(false);
    }

    // display the stop signal
    IEnumerator FlashStop()
    {
        yield return new WaitForSeconds(stop_delay);

        Arrow.text = "x";
        Arrow.alignment = TextAlignmentOptions.Midline;
        Arrow.color = new Color32(255,0,0,255); // (0,110,255,255); blue text
    }

    // prepare data for saving and display the next trial
    IEnumerator Next()
    {
        Arrow.text = "";
        Error.text = "";

        instance += 1;

        // keep track of which trial types have been displayed
        if (stop == false)
        {
            total_response_time += response_time;
            count_go += 1; 
        }
        else
        {
            count_stop += 1;
        }

        // prepare output for saving
        string DateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
		var outputs = new List<OutputParameter>
		{
			new ("o_StopSignal_date_time", DateTime),
            new ("o_StopSignal_task_order", GameManager.current_task_number.ToString()),
            new ("o_StopSignal_block", block.ToString()),
			new ("o_StopSignal_trial", instance.ToString()),
			new ("o_StopSignal_stop_signal", stop ? "1" : "0"),
            new ("o_StopSignal_SSD", stop_delay.ToString()),
            new ("o_StopSignal_stimulus", left ? "left" : "right"),
            new ("o_StopSignal_response_time", response_time.ToString()),
            new ("o_StopSignal_response", response.ToString()),
            new ("o_StopSignal_correct", correct ? "1" : "0"),
			new ("o_StopSignal_practice_output", practice_output.ToString())
		};

        SendData(DateTime, GameManager.current_task_number, block, instance, stop.ToString(), stop_delay, left, response_time, response, correct.ToString(), practice_output);

        // update the delay for the stop signal
        if (correct == true) 
        {
            total_correct += 1;

            if (stop == true)
            {
                stop_delay += delta_stop_delay;
                stop_delay = Math.Min(time_limit, stop_delay); // max stop signal delay
            }
        } else 
        {
            if (stop == true)
            {
                stop_delay -= delta_stop_delay;
                stop_delay = Math.Max(0f, stop_delay); // min stop signal delay
            }
        }

        yield return new WaitForSeconds(0.3f);

        // if you've finished the block, display feedback and rest
        if (instance == no_instances)
        {   
            float avg_response_time = total_response_time/count_go;
            float prop_stops_missed = (float)stops_missed/(float)count_stop;

            Fixation.fontSize = 50;
            Fixation.text = "Performance so far (all rounds)\n​\n" + 
            "Arrows missed: " + arrows_missed.ToString() + "\n(this should be 0)\n​\n" + 
            "Average response time: " + avg_response_time.ToString("F1")  + "s\n(respond as fast as you can)\n\n​" +
            "Proportion of stop signals missed: " + prop_stops_missed.ToString("P0") + "\n(this should be close to 50%)";

            yield return new WaitForSeconds(feedback_time);

            if (block < no_blocks)
            {
                slider.gameObject.SetActive(true);    
                Fixation.fontSize = 100;
                Fixation.text = "Rest";
                yield return new WaitForSeconds(rest_time - feedback_time);
            }
            instance = 0;
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // if they've made an error, display an error message
    IEnumerator FlashError()
    {
        Arrow.text = "";
        BlueLeftImage.gameObject.SetActive(false);
        BlueRightImage.gameObject.SetActive(false);
        BlackLeftImage.gameObject.SetActive(false);
        BlackRightImage.gameObject.SetActive(false);

        Error.color = new Color32(255,255,255,255); // white text

        if (correct == false)
        {
            if (time >= time_limit)
            {
                Error.text = "TOO SLOW!";
                arrows_missed += 1;
            }
            else
            {
                if (stop == true)
                {
                    Error.text = "Should have stopped!";
                    stops_missed += 1;
                }
                else
                {
                    Error.text = "Wrong key!";
                    arrows_missed += 1;
                }
            }
        }

        yield return new WaitForSeconds(0.5f);
        Error.text = "";
        StartCoroutine(Next());
    }

    // generate a block of trials, ensuring it has requisite left/right/stop trials, randomised order
    void GenerateTrialsBlock(int no_instances)
    {
        Random rng = new Random();

        block_trials = new List<int>();
        stop_trials = new List<int>();
        List<int> left_trials = new List<int>();
        List<int> right_trials = new List<int>();
        
        // Generate a list of indices for trials
        for (int i = 0; i < no_instances; i++)
        {
            if (i % 2 == 0)
            {
                left_trials.Add(i); // Even indices are left trials
            }
            else
            {
                right_trials.Add(i); // Odd indices are right trials
            }
        }

        // Shuffle both left and right trials
        left_trials = left_trials.OrderBy(x => rng.Next()).ToList();
        right_trials = right_trials.OrderBy(x => rng.Next()).ToList();
        
        // Combine left and right trials into a single block
        block_trials = left_trials.Concat(right_trials).ToList();
        block_trials = block_trials.OrderBy(x => rng.Next()).ToList();

        List<int> stop_left_trials;
        List<int> stop_right_trials;

        // if it's practice, then 50% stop trials
        if(GameManager.practice == true)
        {
            stop_left_trials = left_trials.Take(left_trials.Count / 2).OrderBy(x => rng.Next()).ToList(); // 50% stop
            stop_right_trials = right_trials.Take(right_trials.Count / 2).OrderBy(x => rng.Next()).ToList(); // 50% stop
        }
        else // if it's real, then 25% stop trials
        {
            stop_left_trials = left_trials.Take(left_trials.Count / 4).OrderBy(x => rng.Next()).ToList(); // 25% stop
            stop_right_trials = right_trials.Take(right_trials.Count / 4).OrderBy(x => rng.Next()).ToList(); // 25% stop
        }

        // Combine stop trials
        stop_trials = stop_left_trials.Concat(stop_right_trials).ToList();
        stop_trials = stop_trials.OrderBy(x => rng.Next()).ToList();

        block += 1;
    }


    // add data to the queue to be saved
    private static void SendData(string DateTime, int task_number, int block, int instance, string stop, float SSD, bool stimulus, float response_time, string response, string correct, int practice_output)
    {   
        var outputs = new List<OutputParameter>
        {
            new ("o_StopSignal_date_time", DateTime),
            new ("o_StopSignal_task_order", task_number),
            new ("o_StopSignal_block", block),
            new ("o_StopSignal_trial", instance),
            new ("o_StopSignal_stop_signal", stop),
            new ("o_StopSignal_SSD", SSD),
            new ("o_StopSignal_stimulus", stimulus ? "left" : "right"),
            new ("o_StopSignal_response_time", response_time),
            new ("o_StopSignal_response", response),
            new ("o_StopSignal_correct", correct),
            new ("o_StopSignal_practice_output", practice_output)
        };

        Debug.Log("Saving: " + string.Join(", ", outputs.Select(o => $"{o.Name}: {o.Value}")));
        Debug.Log("Queue size is " + DataSaver.GetQueueSize());
        DataSaver.AddDataToSave(GameManager.TaskId, outputs);
    }
}
