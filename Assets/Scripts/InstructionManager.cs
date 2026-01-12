using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using Dhive;

public class InstructionManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text Title;
    public TMP_Text Body;
    public Image DisplayImage;
    public Image LargeImage; 
    public Button NextButton;
    public Button RestartButton; 
    public TMP_Text FeedbackMessage; 
    
    [Header("Quiz Prefabs")]
    public GameObject TogglePrefab; 
    public Transform ToggleContainer;

    // Internal State
    private string currentTask;
    private List<string> instruction_pages = new List<string>();
    private List<QuizQuestion> quizQuestions = new List<QuizQuestion>();
    private int currentQuizIndex = 0;
    private string body_text;

    // storing task switching original positions for quiz
    private Vector3 originalBodyPos;
    private Vector3 originalTogglePos;
    private bool positionsCaptured = false;

    // Helper class for quiz data
    private class QuizQuestion
    {
        public string Text;
        public string ImagePath;
        public bool UseLargeImage;
        public Dictionary<string, int> Options; // Option Text -> 1 (Correct) or 0 (Incorrect)

        public QuizQuestion(string text, string img, Dictionary<string, int> opts, bool large = false)
        {
            Text = text; ImagePath = img; Options = opts; UseLargeImage = large;
        }
    }

    void Start()
    {
        // Get the Task Name from the Manager
        currentTask = GameManager.instance.GetTaskName(); 

        string sceneName = SceneManager.GetActiveScene().name;

        UIBridge localBridge = FindObjectOfType<UIBridge>();

        if (localBridge != null)
        {
            Title = localBridge.Title;
            Body = localBridge.Body;
            NextButton = localBridge.NextButton;
            RestartButton = localBridge.RestartButton;
            FeedbackMessage = localBridge.Message;
            DisplayImage = localBridge.Image;
            LargeImage = localBridge.LargeImage;
        }

        if (sceneName == "Instructions")
        {
            SetupInstructionsScene();
        }
        else if (sceneName == "Quiz")
        {
            SetupQuizScene();
        }
    }

    public void OnNextPressed()
    {
        string scene = SceneManager.GetActiveScene().name;
        
        if (scene == "Instructions")
        {
            NextInstructionClick();
        }
        else if (scene == "Quiz")
        {
            NextQuestionClick();
        }
    }

    // 2. Restart handler
    public void OnRestartPressed()
    {
        // Reset logic: force practice mode on and reload instructions
        GameManager.do_practice = true; 
        GameManager.complete_quiz = false;
        GameManager.total_quiz_mistakes = 0;
        SceneManager.LoadScene("Instructions");
    }

    // 3. Toggle handler (Overload for the UIBridge)
    public void CheckAnswer(Toggle t)
    {
        // Only run if the toggle was turned ON (ignore the OFF event)
        if (t.isOn)
        {
            string answerText = t.GetComponentInChildren<TMP_Text>().text;
            CheckAnswer(answerText);
        }
    }

    // =================================================================================================
    // SCENE 1: INSTRUCTIONS LOGIC
    // =================================================================================================

    void SetupInstructionsScene()
    {
        if (currentTask == "TaskSwitching")
        {
            Body.GetComponentInChildren<TMP_Text>().font = GameManager.my_font;
        }

        LoadAllInstructions();

        // Check if we are returning from the quiz (Post-Quiz State)
        bool isPostQuiz = GameManager.instance.IsQuizComplete(); 

        if (isPostQuiz)
        {
            // Show ONLY the final page
            if (RestartButton) RestartButton.gameObject.SetActive(true);
            if (NextButton) NextButton.gameObject.SetActive(false); 

            if (GameManager.do_practice == true)
            {
                 // Normal behavior: Show the final page of instructions
                 if (instruction_pages.Count > 0)
                    Body.text = instruction_pages[instruction_pages.Count - 1];
            }
            else
            {
                Title.text = "";
                Body.alignment = TextAlignmentOptions.Midline;
                GameManager.instance.StartCoroutine(GameManager.instance.StartTask(currentTask));
                if (RestartButton) RestartButton.gameObject.SetActive(false);
            }
        }
        else
        {
            // Show the sequence of pages
            if (RestartButton) RestartButton.gameObject.SetActive(false);
            if (NextButton) NextButton.gameObject.SetActive(true);

            // Setup Header Title
            if (Title) 
            {
                // Note: You might need to adjust how you calculate "Task X of Y" if you moved that logic.
                // For now, we set a generic title or fetch from GM if needed.
                Title.text = "Instructions: " + currentTask; 
            }
            
            // Remove the final page from this list (it is reserved for post-quiz)
            if (instruction_pages.Count > 1)
                instruction_pages.RemoveAt(instruction_pages.Count - 1);

            ShowNextPage();
        }
    }

    void LoadAllInstructions()
    {
        instruction_pages.Clear();

        if (currentTask == "SymbolDigit")
        {
            GameManager.TaskId = DataLoader.GetDatabaseTaskId(GameManager.experimentData, GameManager.symbol_digit_name);

            body_text = "Doing well in this task reflects strong mental agility, which is useful for quick problem-solving in day-to-day activities.\n\n" +
            "In this task you will need to match the symbol to the number.\n\n" +
            "The top of the screen will show a mapping from symbols to numbers. You'll use this mapping to determine which number matches the target symbol at the bottom of the screen.\n\n" +
            "Press the key on your keyboard corresponding to that number.\n\n";
            instruction_pages.Add(body_text);

            body_text = $"After a correct answer the target will change. You have a total of {SymbolDigitGM.time_limit} seconds to enter as many correct responses as possible.\n\n" +
            "You can ONLY answer using the number keys located above the letter keys on your keyboard.\n\n" +
            "<i>When you are ready for the PRACTICE rounds, press \"spacebar\".</i>";
            instruction_pages.Add(body_text);
        }
        else if (currentTask == "NBack")
        {
            GameManager.TaskId = DataLoader.GetDatabaseTaskId(GameManager.experimentData, GameManager.nback_name);

            body_text = "This task shows how well you keep track of recent information, similar to remembering details in a conversation.\n\n" +
            "You first see a starter cue showing a different number in 1-to-3 squares. You must remember the numbers AND which squares (left, middle, right) they're in.\n\n" +
            "You'll do several rounds, one at a time, where a new number appears in only one of the squares, at random. You must respond with the key on your keyboard corresponding to the PREVIOUS number shown in the SAME SQUARE.\n\n" +
            "If the relevant square was blank on the previous round, you must think back over multiple rounds to the most recent number shown in the same square.\n\n" +
            "To do well you must track the previous number shown, in each square, over each round.\n\n";
            instruction_pages.Add(body_text);

            body_text = $"You're scored based on the total number of correct answers and have {Mathf.FloorToInt(NBack.time1)}-{Mathf.FloorToInt(NBack.time3)} seconds to respond while the current number is being displayed: if you're too slow, it's marked incorrect.\n\n" + 
            "In the practice rounds you get feedback after every response (green flash = correct, red flash = incorrect). After practice there is no feedback.\n\n" + 
            "You can ONLY answer using the number keys located above the letter keys on your keyboard.\n\n" +
            "<i>This is the only task where you can practice as many times as you want before starting. When you are ready for the PRACTICE rounds, <i>press \"spacebar\".</i>";
            instruction_pages.Add(body_text);
        }
        else if (currentTask == "StopSignal")
        {
            GameManager.TaskId = DataLoader.GetDatabaseTaskId(GameManager.experimentData, GameManager.sst_name);

            body_text = "This task shows how well you can stop yourself from making rash or impulsive decisions.\n\n" +
            "In this task you must respond to either a left or right pointing arrow by pressing the left arrow or right arrow key, respectively, on your keyboard.\n\n" +
            "You have less than <b>one second</b> to respond and if you are too slow you 'miss' an arrow. Aim to miss as few arrows as possible, ideally 0. \n\n" +
            "Occasionally, <i>an arrow is quickly replaced by a red X, which is the STOP signal</i>. If you see this do not press anything. If you press something by mistake you will 'miss' the stop signal.\n\n" +
            "Your first priority is to respond to arrows as fast as you can. Your second priority is to obey the stop signal if possible, but know that this is very hard and you should expect to miss it 50% of the time.";
            instruction_pages.Add(body_text);

            body_text = $"The task has {StopSignal.no_real_instances*StopSignal.real_blocks} rounds. Most rounds will not show the stop signal.\n\n" +
            "The most important part is to respond as fast as you can and to miss as few arrows as possible. \n\n" +
            "If possible, try to obey the stop signal when it replaces an arrow, but do not 'wait' to see if the stop signal replaces an arrow. This will be penalised by making future rounds harder.\n\n" +
            "You should respond as quickly as possible when seeing an arrow.\n\n" +
            "<i>When you are ready for the PRACTICE rounds, press \"spacebar\".</i>";
            instruction_pages.Add(body_text);
        }
        else if (currentTask == "TaskSwitching")
        {
            GameManager.TaskId = DataLoader.GetDatabaseTaskId(GameManager.experimentData, GameManager.ln_name);

            // NOTE: Accessing static variables from GameManager/TaskSwitching to fill dynamic text
            string eg_text = GameManager.letter_on_left ? "I7" : "7I";
            
            body_text = "This task shows how well you can mentally sort through mixed information, like organizing thoughts in a busy meeting.\n\n" +
            $"In this task you will see a 2x2 grid with four squares. In each round one of the squares will display a letter-number combination, e.g. \"{eg_text}\". The letter will always be upper case.\n\n" +
            "Based on a rule, you will respond to this letter-number combination by pressing either the left arrow or right arrow key on your keyboard.\n\n" +
            "The square displaying the letter-number combination will change each round. You will need to remember which rule applies to which square.";

            instruction_pages.Add(body_text);

            body_text = $"There will be {TaskSwitching.real_block1 + TaskSwitching.real_block2 + TaskSwitching.real_block3} rounds in total and you have a maximum of {Mathf.FloorToInt(TaskSwitching.time_limit)} seconds to respond in each.\n\n" +
            "The square displaying the letter-number combination will change each round. You will need to remember which rule applies to which square.\n\n" +
            $"You are scored based on the speed of your responses and must respond as fast as you can. Accuracy is important as incorrect answers count as taking the maximum {Mathf.FloorToInt(TaskSwitching.time_limit)} seconds.\n\n" +
            "<i>When you are ready for the PRACTICE rounds, press \"spacebar\".</i>";
            instruction_pages.Add(body_text);
        }
        else if (currentTask == "KOT")
        {
            GameManager.TaskId = DataLoader.GetDatabaseTaskId(GameManager.experimentData, GameManager.knapsack_name);

            body_text = "This task shows how well you can make decisions with limited resources, like choosing how to manage your budget, investments, or time.\n\n" + 
					"You will be presented with a set of items, each with a weight and value. Imagine you wanted to put these items in a bag, but the bag has a maximum weight capacity.\n\n" +  
                    "Your task is to select the items that, taken together, achieve the highest possible value without exceeding the weight capacity.\n\n";
            instruction_pages.Add(body_text);

            body_text = "Click on items to select (or de-select) them. Selected items will be highlighted.\n\n" +  
                    "Please use clicking to show your thinking. The moment you think an item is good, click on it to select.\n\n" +  
                    "The moment you change your mind, click on it again to de-select.\n\n";
            instruction_pages.Add(body_text);

            body_text = "The middle of the screen shows the maximum weight capacity.\n\n" +
                    $"A green timer counts down around the middle: you have at most {Mathf.FloorToInt(KnapsackOpt.timeTrial)} seconds per round.\n\n" +
					"Once finished with your selections, <b>press \"Enter\" to submit your answer</b>.\n\n" + 
                    "If time runs out then your current selection will automatically be submitted for you.\n\n";
            instruction_pages.Add(body_text);

            body_text = "Remember the two criteria for your selection of items to be correct.\n\n" +  
                        "1) their combined weight must NOT be larger than the weight capacity.\n\n" +  
                        "2) out of all combinations that are not larger than the weight capacity, your selection must achieve the highest possible value.\n\n" +  
                        "You will now do a quiz to test your understanding. It is critical that you ask the researcher for help if any part of the quiz is unclear.";
            instruction_pages.Add(body_text);
            
            body_text = $"There will be {KnapsackOpt.numberOfInstances} rounds and you have {Mathf.FloorToInt(KnapsackOpt.timeTrial)} seconds to respond in each.\n\n" +
            $"You are paid based on correctness: the amount of time you spend has no effect on your pay.\n\n" +
            $"The practice rounds do not count towards your score. In the real task, you will be paid {KnapsackOpt.pay_p_correct_answer} for each correct answer.\n\n" +
            "<i>When you are ready for the PRACTICE rounds, press \"spacebar\".</i>";
            instruction_pages.Add(body_text);
        }

        if (GameManager.track_quiz_mistakes)
        {
            body_text = $"If you select {GameManager.max_allowed_mistakes} wrong answers throughout this quiz, your participation will be cancelled and you will only be paid the show-up fee.\n\n" +  
            "Please pay attention ask the researcher for help whenever needed.";
            instruction_pages.Insert(instruction_pages.Count - 1, body_text);
        }
    }

    public void NextInstructionClick()
    {
        if (instruction_pages.Count > 0)
        {
            ShowNextPage();
        }
        else
        {
            SceneManager.LoadScene("Quiz");
        }
    }

    void ShowNextPage()
    {
        Body.text = instruction_pages[0];
        instruction_pages.RemoveAt(0);
    }

    // =================================================================================================
    // SCENE 2: QUIZ LOGIC
    // =================================================================================================

    void SetupQuizScene()
    {   
        // Capture default positions 
        if (!positionsCaptured)
        {
            originalBodyPos = Body.transform.localPosition;
            originalTogglePos = TogglePrefab.transform.localPosition;
            positionsCaptured = true;
        }

        // Reset state
        currentQuizIndex = 0;
        if (NextButton) NextButton.interactable = false;
        if (FeedbackMessage) FeedbackMessage.text = "";

        LoadAllQuizQuestions();

        // Show the first question
        DisplayQuestion(currentQuizIndex);
    }

    void LoadAllQuizQuestions()
    {
        quizQuestions.Clear();

        if (currentTask == "SymbolDigit")
        {
            // Q1
            quizQuestions.Add(new QuizQuestion(
                "Which number is the correct response to this target?",
                "Figures/symdig_1",
                new Dictionary<string, int> { 
                    { "1", 1 }, 
                    { "2", 0 }, 
                    { "3", 0 }, 
                    { "4", 0 } }
            ));
            // Q2
            quizQuestions.Add(new QuizQuestion(
                "Which number is the correct response to this target?",
                "Figures/symdig_2",
                new Dictionary<string, int> { 
                    { "1", 0 }, 
                    { "2", 1 }, 
                    { "3", 0 }, 
                    { "4", 0 } }
            ));
        }
        else if (currentTask == "NBack")
        {
            // Q1 (no_quiz 4)
            quizQuestions.Add(new QuizQuestion(
                "You're in the current round, shown in orange. What was the previous number in the same square?",
                "Figures/nback_1",
                new Dictionary<string, int> { 
                    { "\"1\", the prior number on the left", 1 }, 
                    { "\"2\", the current number on the left", 0 }, 
                    { "Some other key", 0 }, 
                    { "Not pressing any key", 0 } }
            ));
            // Q2 (no_quiz 3)
            quizQuestions.Add(new QuizQuestion(
                "You're in the current round, shown in orange. What was the previous number in the same square?",
                "Figures/nback_2",
                new Dictionary<string, int> { 
                    { "\"1\", the starting number on the left", 0 }, 
                    { "\"2\", the prior number on the left", 1 }, 
                    { "\"5\", the current number on the left", 0 }, 
                    { "Some other key", 0 } }
            ));
            // Q3 (no_quiz 2)
            quizQuestions.Add(new QuizQuestion(
                "You're in the current round, shown in orange. What was the previous number in the same square?",
                "Figures/nback_3",
                new Dictionary<string, int> { 
                    { "\"1\", the starting number on the left", 0 }, 
                    { "\"6\", the starting number in the middle", 0 }, 
                    { "\"3\", the prior number on the right", 0 }, 
                    { "\"8\", the prior number in the middle", 0 }, 
                    { "\"5\", the prior number on the left", 1 } }
            ));
            // Q4 (no_quiz 1)
            quizQuestions.Add(new QuizQuestion(
                "You're in the current round, shown in orange. What was the previous number in the same square?",
                "Figures/nback_4",
                new Dictionary<string, int> { 
                    { "\"1\", the starting number on the left", 0 }, 
                    { "\"6\", the starting number in the middle", 0 }, 
                    { "\"3\", the prior number on the right", 1 }, 
                    { "\"8\", the prior number in the middle", 0 }, 
                    { "\"5\", the prior number on the left", 0 } }
            ));
        }
        else if (currentTask == "StopSignal")
        {
            // Q1
            quizQuestions.Add(new QuizQuestion(
                "What is the correct response to this symbol?",
                "Figures/stopsig_left",
                new Dictionary<string, int> { 
                    { "\"Left arrow\" key", 1 }, 
                    { "\"Right arrow\" key", 0 }, 
                    { "\"S\" key", 0 }, 
                    { "Not pressing any key", 0 } }
            ));
            // Q2
            quizQuestions.Add(new QuizQuestion(
                "What is the correct response to this symbol?",
                "Figures/stopsig_stop",
                new Dictionary<string, int> { 
                    { "\"Left arrow\" key", 0 }, 
                    { "\"Right arrow\" key", 0 }, 
                    { "\"S\" key", 0 }, 
                    { "Not pressing any key", 1 } }
            ));
            // Q3
            quizQuestions.Add(new QuizQuestion(
                "What is the correct response to this symbol?",
                "Figures/stopsig_right",
                new Dictionary<string, int> { 
                    { "\"Left arrow\" key", 0 }, 
                    { "\"Right arrow\" key", 1 }, 
                    { "\"S\" key", 0 }, 
                    { "Not pressing any key", 0 } }
            ));
        }
        else if (currentTask == "TaskSwitching")
        {
            // Dynamic logic helpers
            string consonant_key = GameManager.consonant_key;
            string vowel_key = GameManager.vowel_key;
            string odd_key = GameManager.odd_key;
            string even_key = GameManager.even_key;
            bool letterLeft = GameManager.letter_on_left;
            int cong = GameManager.congruence_type;

            // Q1 (no_quiz 4): Top Row (Letter), Consonant/Vowel logic. Example uses "I" (Vowel).
            int correctQ1 = (cong == 0 || cong == 2) ? 0 : 1; // 0=Left, 1=Right (Vowel logic: cong 0/2 is Left, else Right)
            quizQuestions.Add(new QuizQuestion(
                $"If the letter-number pair appears in the TOP ROW, you should respond to the LETTER (in this case, I).\n\n" +
                $"You respond by pressing the \"{consonant_key} arrow\" key if the letter is a consonant.\n\n" +
                $"You respond by pressing the \"{vowel_key} arrow\" key if the letter is a vowel.\n\n" +
                "In this case, I is a vowel so which key should you respond with?",
                letterLeft ? "Figures/taskswitch_inst_1_left" : "Figures/taskswitch_inst_1_right",
                new Dictionary<string, int> { 
                    { "\"Left arrow\" key", correctQ1 == 0 ? 1 : 0 }, 
                    { "\"Right arrow\" key", correctQ1 == 1 ? 1 : 0 } }
            ));

            // Q2 (no_quiz 3): Bottom Row (Number), Odd/Even logic. Example uses "7" (Odd).
            int correctQ2 = (cong == 0 || cong == 3) ? 0 : 1; // Odd logic: cong 0/3 is Left, else Right
            quizQuestions.Add(new QuizQuestion(
                $"If the letter-number pair appears in the BOTTOM ROW, you should respond to the NUMBER (in this case, 7).\n\n" +
                $"You respond by pressing the \"{odd_key} arrow\" key if the number is odd.\n\n" +
                $"You respond by pressing the \"{even_key} arrow\" key if the number is even.\n\n" +
                "In this case, 7 is odd so which key should you respond with?",
                letterLeft ? "Figures/taskswitch_inst_2_left" : "Figures/taskswitch_inst_2_right",
                new Dictionary<string, int> { 
                    { "\"Left arrow\" key", correctQ2 == 0 ? 1 : 0 }, 
                    { "\"Right arrow\" key", correctQ2 == 1 ? 1 : 0 } }
            ));

            // Q3 (no_quiz 2): Consonant logic (Congruence check)
            int correctQ3 = (cong == 1 || cong == 3) ? 0 : 1; // Left if 1/3, Right if 0/2? Wait, original logic:
            // if (cong == 1 || cong == 3) dict["Left..."] = 1. So correct is Left(0). 
            quizQuestions.Add(new QuizQuestion(
                "What is the correct response in this round?",
                letterLeft ? "Figures/taskswitch_1_left" : "Figures/taskswitch_1_right",
                new Dictionary<string, int> { 
                    { "\"Left arrow\" key, letter is a consonant", correctQ3 == 0 ? 1 : 0 }, 
                    { "\"Right arrow\" key, letter is a consonant", correctQ3 == 1 ? 1 : 0 },
                    { "\"Left arrow\" key, number is odd", 0 },
                    { "\"Right arrow\" key, number is odd", 0 }
                }
            ));

            // Q4 (no_quiz 1): Even number logic (Congruence check)
            int correctQ4 = (cong == 1 || cong == 2) ? 0 : 1;
            quizQuestions.Add(new QuizQuestion(
                "What is the correct response in this round?",
                letterLeft ? "Figures/taskswitch_2_left" : "Figures/taskswitch_2_right",
                new Dictionary<string, int> { 
                    { "\"Left arrow\" key, letter is a vowel", 0 }, 
                    { "\"Right arrow\" key, letter is a vowel", 0 },
                    { "\"Left arrow\" key, number is even", correctQ4 == 0 ? 1 : 0 },
                    { "\"Right arrow\" key, number is even", correctQ4 == 1 ? 1 : 0 }
                }
            ));
        }
        else if (currentTask == "KOT")
        {
            // Q1 (no_quiz 21)
            quizQuestions.Add(new QuizQuestion(
                "If your selection of items total weight is exactly equal to the maximum capacity, is your answer correct?",
                null,
                new Dictionary<string, int> { 
                    { "Yes", 0 }, 
                    { "No, it must be below maximum capacity", 0 }, 
                    { "Yes, if those items also give maximum value", 1 }, 
                    { "Not possible to tell", 0 } }
            ));
            // Q2 (no_quiz 20)
            quizQuestions.Add(new QuizQuestion(
                "What is the weight of item 3?", "Figures/kot_1",
                new Dictionary<string, int> { { "7kg", 1 }, 
                { "72kg", 0 }, 
                { "17kg", 0 }, 
                { "Something else", 0 } }
            ));
            // Q3 (no_quiz 19)
            quizQuestions.Add(new QuizQuestion(
                "What is the value of item 5?", "Figures/kot_1",
                new Dictionary<string, int> { 
                    { "$49", 0 }, 
                    { "$43", 0 }, 
                    { "$41", 1 }, 
                    { "41kg", 0 } }
            ));
            // Q4 (no_quiz 18)
            quizQuestions.Add(new QuizQuestion(
                "What is the maximum weight capacity?", "Figures/kot_1",
                new Dictionary<string, int> { 
                    { "100kg", 0 }, 
                    { "72kg", 1 }, 
                    { "49kg", 0 }, 
                    { "Something else", 0 } }
            ));
            // Q5 (no_quiz 17)
            quizQuestions.Add(new QuizQuestion(
                "What shows the remaining time?", "Figures/kot_1",
                new Dictionary<string, int> { 
                    { "The depleting green circle", 1 }, 
                    { "There is no timer", 0 } }
            ));
            // Q6 (no_quiz 16)
            quizQuestions.Add(new QuizQuestion(
                "Which selection of items gives the highest total value without exceeding the weight constraint?", "Figures/kot_2",
                new Dictionary<string, int> { 
                    { "1 and 2", 1 }, 
                    { "1 and 3", 0 }, 
                    { "2 and 3", 0 }, 
                    { "1, 2 and 3", 0 } }
            ));
            // Q7 (no_quiz 15)
            quizQuestions.Add(new QuizQuestion(
                "What is the total value of this selection?", "Figures/kot_2",
                new Dictionary<string, int> { 
                    { "44", 0 }, 
                    { "$88", 1 }, 
                    { "$76", 0 }, 
                    { "$104", 0 } }
            ));
            // Q8 (no_quiz 14)
            quizQuestions.Add(new QuizQuestion(
                "What is the total weight of this selection?", "Figures/kot_2",
                new Dictionary<string, int> { 
                    { "58kg", 0 }, 
                    { "57kg", 0 }, 
                    { "47kg", 1 }, 
                    { "40kg", 0 } }
            ));
            // Q9 (no_quiz 13)
            quizQuestions.Add(new QuizQuestion(
                "Which selection of items gives the highest total value without exceeding the weight constraint?", "Figures/kot_3",
                new Dictionary<string, int> { 
                    { "1 and 2", 0 }, 
                    { "1 and 3", 0 }, 
                    { "2 and 3", 0 }, 
                    { "1, 2 and 3", 1 } }
            ));
            // Q10 (no_quiz 12)
            quizQuestions.Add(new QuizQuestion(
                "What is the total value of this selection?", "Figures/kot_3",
                new Dictionary<string, int> { 
                    { "$86", 0 }, 
                    { "$99", 1 }, 
                    { "$61", 0 }, 
                    { "$51", 0 } }
            ));
            // Q11 (no_quiz 11)
            quizQuestions.Add(new QuizQuestion(
                "What is the total weight of this selection?", "Figures/kot_3",
                new Dictionary<string, int> { 
                    { "25kg", 0 }, 
                    { "16kg", 0 }, 
                    { "39kg", 1 }, 
                    { "37kg", 0 } }
            ));
            // Q12 (no_quiz 10)
            quizQuestions.Add(new QuizQuestion(
                "Which selection of items gives the highest total value without exceeding the weight constraint?", "Figures/kot_4",
                new Dictionary<string, int> { 
                    { "1 and 2", 0 }, 
                    { "1 and 3", 0 }, 
                    { "2 and 3", 1 }, 
                    { "1, 2 and 3", 0 } }
            ));
            // Q13 (no_quiz 9)
            quizQuestions.Add(new QuizQuestion(
                "What is the total value of this selection?", "Figures/kot_4",
                new Dictionary<string, int> { 
                    { "$11", 0 }, 
                    { "$43", 1 }, 
                    { "$36", 0 }, 
                    { "$45", 0 } }
            ));
            // Q14 (no_quiz 8)
            quizQuestions.Add(new QuizQuestion(
                "What is the total weight of this selection?", "Figures/kot_4",
                new Dictionary<string, int> { 
                    { "68kg", 1 }, 
                    { "85kg", 0 }, 
                    { "63kg", 0 }, 
                    { "108kg", 0 } }
            ));
            // Q15 (no_quiz 7) - Uses Large Image
            quizQuestions.Add(new QuizQuestion(
                "How many items has the player selected?", "Figures/kot_5",
                new Dictionary<string, int> { 
                    { "0", 0 }, 
                    { "1", 0 }, 
                    { "2", 0 }, 
                    { "3", 1 }, 
                    { "4", 0 } },
                true
            ));
            // Q16 (no_quiz 6) - Uses Large Image
            quizQuestions.Add(new QuizQuestion(
                "What is the total value of this selection?", "Figures/kot_5",
                new Dictionary<string, int> { 
                    { "$145", 0 }, 
                    { "$206", 1 }, 
                    { "$156", 0 }, 
                    { "$196", 0 } },
                true
            ));
            // Q17 (no_quiz 5) - Uses Large Image
            quizQuestions.Add(new QuizQuestion(
                "What is the total weight of this selection?", "Figures/kot_5",
                new Dictionary<string, int> { 
                    { "165kg", 1 }, 
                    { "156kg", 0 }, 
                    { "155kg", 0 }, 
                    { "166kg", 0 } },
                true
            ));
            // Q18 (no_quiz 4) - Uses Large Image
            quizQuestions.Add(new QuizQuestion(
                "Is the current selection of items correct?", "Figures/kot_5",
                new Dictionary<string, int> { 
                    { "Yes", 0 }, 
                    { "No", 1 } },
                true
            ));
            // Q19 (no_quiz 3) - Uses Large Image + null sprite
            quizQuestions.Add(new QuizQuestion(
                "How do you select an item?", null,
                new Dictionary<string, int> { 
                    { "By clicking on it", 1 }, 
                    { "By using the number pad", 0 } },
                true
            ));
            // Q20 (no_quiz 2) - Uses Large Image + null sprite
            quizQuestions.Add(new QuizQuestion(
                "How do you deselect / unselect an item?", null,
                new Dictionary<string, int> { 
                    { "By clicking on it again", 1 }, 
                    { "Pressing ESC", 0 }, 
                    { "Pressing DELETE", 0 } },
                true
            ));
            // Q21 (no_quiz 1) - Uses Large Image + null sprite
            quizQuestions.Add(new QuizQuestion(
                "How do you submit your selection of items as your final answer?", null,
                new Dictionary<string, int> { 
                    { "Pressing ESC", 0 }, 
                    { "Pressing SPACEBAR", 0 }, 
                    { "Pressing ENTER / RETURN", 1 } },
                true
            ));
        }
    }

    void DisplayQuestion(int index)
    {
        // Clear old toggles
        foreach (Transform child in ToggleContainer)
        {
            if (child.name != "Option") Destroy(child.gameObject); // Keep the template, destroy clones
            else child.gameObject.SetActive(false); // Hide template
        }

        if (index >= quizQuestions.Count)
        {
            FinishQuiz();
            return;
        }

        // reset UI positions
        if (positionsCaptured)
        {
            Body.transform.localPosition = originalBodyPos;
            TogglePrefab.transform.localPosition = originalTogglePos;
        }

        QuizQuestion q = quizQuestions[index];

        // 1. Set Text
        Body.text = q.Text;

        // 2. Set Image
        // If question has no image but is NOT large, we disable both
        if (q.ImagePath == null && !q.UseLargeImage)
        {
            if(DisplayImage) DisplayImage.gameObject.SetActive(false);
            if(LargeImage) LargeImage.gameObject.SetActive(false);
        }
        else
        {
            DisplayImage.gameObject.SetActive(!q.UseLargeImage);
            LargeImage.gameObject.SetActive(q.UseLargeImage);
            
            Image targetImg = q.UseLargeImage ? LargeImage : DisplayImage;
            
            if (!string.IsNullOrEmpty(q.ImagePath))
            {
                targetImg.sprite = Resources.Load<Sprite>(q.ImagePath);
                targetImg.color = Color.white;
            }
            else
            {
                 // Handle the "No Image" case (blue background)
                 Color customColor;
                 if (ColorUtility.TryParseHtmlString("#314D79", out customColor)) targetImg.color = customColor;
                 targetImg.sprite = null;
            }
        }

        if(currentTask == "TaskSwitching")
        {
            Body.GetComponentInChildren<TMP_Text>().font = GameManager.my_font;
            if(currentQuizIndex < 2)  
            {
                // find the Body text object and move it up slightly
                Body.transform.localPosition = originalBodyPos + new Vector3(0, 50, 0);
                // Title.transform.localPosition = Title.transform.localPosition + new Vector3(0, 50, 0);
                TogglePrefab.transform.localPosition = originalTogglePos + new Vector3(0, -430, 0);
                // Message.transform.localPosition = Message.transform.localPosition + new Vector3(0, -270, 0);
            }
        }

        // 3. Spawn Toggles
        Vector3 startPosition = TogglePrefab.transform.localPosition;
        int i = 0;

        foreach (var option in q.Options)
        {
            GameObject newToggle = Instantiate(TogglePrefab, ToggleContainer);
            newToggle.SetActive(true);
            newToggle.GetComponentInChildren<TMP_Text>().text = option.Key;

            newToggle.transform.localPosition = startPosition + new Vector3(0, -90 * i, 0);
            // -----------------------------
            
            if(currentTask == "TaskSwitching")
            {
                newToggle.GetComponentInChildren<TMP_Text>().font = GameManager.my_font;
            }

            Toggle t = newToggle.GetComponent<Toggle>();
            string key = option.Key; 
            t.onValueChanged.AddListener((val) => { if(val) CheckAnswer(key); });

            i++; // Increment counter for the next position
        }
    }

    void CheckAnswer(string selectedAnswer)
    {
        QuizQuestion q = quizQuestions[currentQuizIndex];
        
        // Safety check
        if (!q.Options.ContainsKey(selectedAnswer)) return;

        bool isCorrect = q.Options[selectedAnswer] == 1;

        // save quiz data
        ReportQuizAttempt(
            currentTask, 
            currentQuizIndex + 1, 
            selectedAnswer,
            isCorrect ? 1 : 0,
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        );

        if (isCorrect)
        {
            FeedbackMessage.text = "Correct!";
            FeedbackMessage.color = Color.green;
            NextButton.interactable = true;
        }
        else
        {
            FeedbackMessage.text = "Incorrect!";
            FeedbackMessage.color = Color.red;
            NextButton.interactable = false;
        }
    }
  
    public void NextQuestionClick()
    {
        currentQuizIndex++;
        if (currentQuizIndex < quizQuestions.Count)
        {
            // Reset UI for next Q
            FeedbackMessage.text = "";
            NextButton.interactable = false;
            DisplayQuestion(currentQuizIndex);
        }
        else
        {
            FinishQuiz();
        }
    }

    void FinishQuiz()
    {
        // Tell GameManager the quiz is done
        GameManager.instance.MarkQuizComplete(); 
        // Go back to instruction_pages for the final page
        SceneManager.LoadScene("Instructions");
    }

    public void ReportQuizAttempt(string task, int question_num, string submitted_answer, int is_correct, string datetime)
    {
        // save quiz response
        if (GameManager.save_write_locally)
        {
            var parameters = new List<OutputParameter>
            {
                new OutputParameter("quiz_task", task),
                new OutputParameter("quiz_question", question_num),
                new OutputParameter("quiz_answer", submitted_answer),
                new OutputParameter("quiz_correct", is_correct),
                new OutputParameter("quiz_time_stamp", datetime)
            };

            DataSaver.PrepareToSave(parameters, "quiz");
        }
        else
        {
            var parameters = new List<OutputParameter>
            {
                new("quiz_task", task),
                new("quiz_question", question_num),
                new("quiz_question", submitted_answer),
                new("quiz_correct", is_correct),
                new("quiz_time_stamp", datetime)
            };
            
            DataSaver.AddTrialDataToSave(parameters);  
        }
        
        // if an error, keep track of this
        if (is_correct == 0 && GameManager.track_quiz_mistakes)
        {
            GameManager.instance.TrackQuizMistakes(task);
        }

    }
}
