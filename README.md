# Knapsack Optimisation Task and cognitive task battery
Knapsack optimisation task, cognitive task battery, and questionnaires used in projects investigating the effects of computational hardness and either age (osf.io/preprints/psyarxiv/qe6uw) or perceived chronic stress (preprint coming soon) on decision-making capacity. 

**⚠️ Important:** This branch allows the task to read/write locally and provides instructions, a comprehension quiz, and practice trials in the knapsack task. These changes appear to work when running the task in the Editor; however, they have not yet been tested with participants nor in compiled builds, either locally or in browser. The build that was deployed in the ageing and perceived chronic stress studies is on the `Main` branch; however, that version only reads/writes data from/to a proprietary private server (referred to below as DHive), which means the software only *runs* for those with DHive access. You can still use the majority of the source code - but anything that reads or writes data on `Main` will need updating for users outside of the University of Melbourne's Centre for Brain, Mind and Markets.

Welcome, brave scientist, to a dangerous quest: navigating this labyrinthine repo and the beastly scripts lurking within. These scripts are no Minotaur, but rather a Chimera: stitching together scripts from half a dozen PhD students and research assistants, most of whom were learning Unity for the first time. The implementations certainty aren’t optimal or pretty, but they work. Let this ReadMe be the map that guides you through the labyrinth as safely as can be. Godspeed.

## Tasks used

Knapsack optimisation task, recall-1-back (working memory), letters and numbers task (set shifting), stop signal task (motor inhibition), digit symbol substitution task (attention and/or processing speed), ICAR (fluid reasoning), and various questionnaires. All built with Unity 3D v2022.3.22f1 (C#) and designed to run in browser via WebGL. 

## Game Controls
The exact controls are all explained within the game. In terms of requirements:

1. Knapsack, questionnaire tasks, and instructions for most cognitive tasks require both a mouse/trackpad and a keyboard.
2. ICAR requires only a mouse/trackpad.
3. All other tasks require only a keyboard.

## Default game behaviour and alternative configurations

By default, the game proceeds in the following sequence:

1. Fully self-contained instructions, practice questions, and deployment of each task. Knapsack is always first, sequence of the remaining tasks is randomised. You can choose to disable the instructions and practice questions for the knapsack if you wish.
2. Questionnaires.
3. Summary of results and completion code (for Prolific participants).

Minor edits in the GameManager script that allow toggling of key features:

1. Reading input parameters from a local file, rather than DHive (set load_read_locally = true).
2. Saving data to a local CSV, rather than DHive (set save_write_locally = true).
3. Skipping the knapsack task (set skip_complex = true).
4. Skipping the instructions, quiz, and practice trials for the knapsack task and going straight to the first trial (set give_complex_instructions = true).
5. Whether to overwrite instructions/quiz/practice settings for the Knapsack based on the ParticipantID (set give_instructions_from_pid = true). If true, then ParticipantIDs which include a "t" or "T" ("f" or "F") will be given (will not be given) Knapsack instructions, a quiz, and practice trials.
6. Allowing participants to choose the sequence of cognitive tasks (set let_participants_choose_tasks = true). Note, by default Questionnaires are always saved for last. If wanting to test the questionnaires only, you can open the CogTaskHome scene and unhide the Questionnaires game object on the Canvas.
7. Allowing the task to run in the Unity Editor or on a local machine, rather than in a browser (set url_for_id = false). If url_for_id is True, the URL hosting the task should be suffixed with "/?id=xxxx", where "xxxx" is replaced by the actual ID.
8. Allowing participants to access the game more than once (set ban_repeat_logins = false).
9. Disable saving of session data (should only be used when testing, set save_session_data = false).
10. Allowing participants to see their accuracy in each task at the end of the game (set give_feedback_at_end = true).
11. Whether to boot a participant from the experiment if they get X number of comprehension questions wrong for any of the tasks (set track_quiz_mistakes = true and max_allowed_mistakes to the desired X).

## Explanation of scripts

- **DataLoader**: Loads all of the experiment, session, and task parameters from DHive for all tasks. The parameters used for the Ageing and Chronic stress experiments are in the `Parameters` folder of the repo. The parameters used for local reading of data are in `Assets/StreamingAssets/Parameters`.
- **DataSaver**: Saves data from all tasks to DHive. Main idea is it adds each data point to a queue, and then processes the queue in parallel to participants progressing through the game. The game only ends and displays the End screen once the save queue has fully emptied, to ensure no data is lost.
- **WebsocketConnector**: I don't fully understand what it does or how important it is, but it does have one key function: it saves the randomisation ID for that session to DHive.
- **GameManager**: Main engine of the game. Handles transitions between tasks, key functions that could apply to multiple tasks (e.g. selecting confidence), reconnection to internet if it drops.
- **BoardManager**: Sets out the visuals and game components for the start of the game and for the knapsack task.
- **BaseComplexTask**: A generic template/script for complex tasks. The Knapsack is of this class, so this should make it easier to add further complex tasks to this Unity build if desired.
- **InstructionManager**: Handles the instructions and quizzes for all tasks.
- **UIBridge**: Ensures the UI elements in each Unity scene are able to be connected to the relevant script (e.g. `GameManager` or `InstructionManager`). Without this, UI connections (and therefore the ability to progress through the game) will break.
- **KnapsackOpt**: Implementation of the knapsack optimisation task.
- **ICAR**: implementation of the International Cognitive Ability Resource, a measure of fluid intelligence / logic. Can handle two versions: the 16-item sample test, or an 8-item variant that only displays progressive matrices. See Dworak et al (2021) for details on the task.
- **NBack**: implementation of the numeric recall-1-back, a measure of working memory. See Wilhelm et al (2013) for details on the task.
- **StopSignal**: implementation of the stop signal task, a measure of motor inhibition. See Verbruggen et al (2019) for details on the task.
- **SymbolDigit**: implementation of the digit symbol substitution task, a measure of attention and/or processing speed. See Trevino et al (2021) for details on the task.
- **TaskSwitching**: implementation of the letters and numbers task (alt: number-letter task), a measure of set shifting. See Miyake et al (2000) for details on the task.
- **Questionnaire**: template that allows for numerous questionnaires to be administered. Can handle likert-style questions with a matrix of statements and options to choose from. Can also handle free-text response questions. See the `Parameters` folder of the repo and look for files ending in `questionnaire_params.csv` to see the exact items administered in the Ageing and Chronic stress studies.

666 is a default/null value often used as a placeholder throughout many of the scripts. 

## Explanation of scenes

- **WebsockectConnection**: Establishes web connection with DHive.
- **PreloadData**: Loads all of the experiment-level data from DHive.
- **LoadTrialData**: Loads all of the session and task-level data from DHive (or from local).
- **Setup**: Displays starting instructions and records `ParticipantID`.
- **KOT**: Knapsack trials.
- **trialGameAnswer**: Deprecated, only relevant for decision variant - which is not implemented here.
- **Confidence**: Select confidence on a discrete integer scale from 0 to 10.
- **InterTrialRest**: Short break in between knapsack trials.
- **InterBlockRest**: Short break in between blocks of knapsack trials.
- **CogTaskHome**: If choosing your own tasks, it displays the menu of tasks to choose from. If at the end of the game, it shows your scores in each task.
- **Instructions**: Instructions for each task.
- **Quiz**: Quiz comprehension questions for each tasks.
- **FinishedCogTask**: Holding screen when someone completes a task, either for the practice set or the real set of trials.
- **ICAR**: ICAR task. Note, the game must be run in browser for this scene to work properly: it will display blank images if run within the Unity editor.
- **NBack**: Recall-1-back task.
- **SymbolDigit**: Digit Symbol Substitution task.
- **StopSignal**: Stop signal task.
- **TaskSwitching**: Letters & Numbers task.
- **Questionnaires**: Questionnaires task.
- **End**: Displays final message that the task has ended and/or the Prolific completion code. 

## Checklist prior to deployment
1. Are the main `GameManager` parameters (`skip_complex`, `let_participants_choose_tasks`, `url_for_id`, `ban_repeat_logins`, `save_session_data`, `give_complex_instructions`, `give_feedback_at_end`, `save_write_locally`, `load_read_locally`, `track_quiz_mistakes`) correctly set?
2. Have you updated the `completion_code` related aspects of the `GameManager` script?

## DHive jargon
A brief explainer for some terms you may encounter and what they refer to.
- Experiment: top level, refers to something which applies to the entire experiment (i.e. across all sessions and participants)
- Session: 2nd level, refers to something which applies to that particular session (i.e. the Participant ID)
- Task: 3rd level: 3rd level, refers to any global variables/parameters relevant to a particular task (e.g. how long a trial lasts in the KOT)
- TrialTask: bottom level, refers to a particular instance/trial from a task 

## Acknowledgements
Thank you to my colleagues in the Centre for Brain, Mind and Markets, particularly Michelle Lee, Juan Pablo Franco, Abhijeet Anand, Nova Tasha, Jedwin Villanueva, and Daniel; who each contributed in some way to developing the tasks or the server. 
