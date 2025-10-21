# Knapsack Optimisation Task and cognitive task battery
Knapsack optimisation task, cognitive task battery, and questionnaires used in projects investigating the effects of computational hardness and either age (osf.io/preprints/psyarxiv/qe6uw) or perceived chronic stress (preprint coming soon) on decision-making capacity. 

**⚠️ Important:** These experimental tasks were built to read/write data from/to a proprietary private server (referred to below as DHive), which means the software only *runs* for those with DHive access. We are currently working on creating a flag that allows us to toggle whether to use DHive or local files: once this is complete, we can update this repo with a version that runs locally for all. In the meantime, you can still use the majority of the source code - but anything that reads or writes data will need updating.

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

1. Very sparse Knapsack instructions followed by knapsack optimisation task. No practice questions. After each trial, participants indicate their confidence in the submitted solution. If you want more detailed instructions and practice questions, you'll need to edit the task or append them elsewhere (e.g. in Qualtrics). The instructions we used are stored in the root of this repo.
2. Fully self-contained instructions, practice questions, and deployment of the remaining 5 cognitive tasks. Sequence of tasks is randomised.
3. Questionnaires.
4. Summary of results and completion code (for Prolific participants).

Alternative configurations following minor edits in the `GameManager` script:

1. Skipping the knapsack task (set `skip_knapsack` = true). 
2. Allowing participants to choose the sequence of cognitive tasks (set `let_participants_choose_tasks` = true). Note, by default Questionnaires are always saved for last. If wanting to test the questionnaires only, you can open the CogTaskHome scene and unhide the Questionnaires game object on the Canvas. 
3. Allowing the task to run locally, rather than in a browser (set `url_for_id` = false). If `url_for_id` is True, the URL hosting the task should be suffixed with "/?id=xxxx", where "xxxx" is replaced by the actual ID.
4. Allowing participants to access the game more than once (set `ban_repeat_logins` = false).
5. Disable saving of session data (should only be used when testing, set `save_session_data` = false).

## Explanation of scripts

- **DataLoader**: Loads all of the experiment, session, and task parameters from DHive for all tasks. The parameters used for the Ageing and Chronic stress experiments are in the `Parameters` folder of the repo. This includes every questionnaire asked within Unity and the parameters for the KOT and every cognitive task. 
- **DataSaver**: Saves data from all tasks to DHive. Main idea is it adds each data point to a queue, and then processes the queue in parallel to participants progressing through the game. The game only ends and displays the End screen once the save queue has fully emptied, to ensure no data is lost.
- **GameManager**: Main engine of the game. Handles: the knapsack task, instructions and quizzes for all of the cognitive tasks, transitions between tasks, reconnection to internet if it drops. 
- **BoardManager**: Sets out the visuals and game components for the start of the game and for the knapsack task.
- **ICAR**: implementation of the International Cognitive Ability Resource, a measure of fluid intelligence / logic. Can handle two versions: the 16-item sample test, or an 8-item variant that only displays progressive matrices. See Dworak et al (2021) for details on the task.
- **NBack**: implementation of the numeric recall-1-back, a measure of working memory. See Wilhelm et al (2013) for details on the task.
- **StopSignal**: implementation of the stop signal task, a measure of motor inhibition. See Verbruggen et al (2019) for details on the task.
- **SymbolDigit**: implementation of the digit symbol substitution task, a measure of attention and/or processing speed. See Trevino et al (2021) for details on the task.
- **TaskSwitching**: implementation of the letters and numbers task (alt: number-letter task), a measure of set shifting. See Miyake et al (2000) for details on the task.
- **Questionnaire**: template that allows for numerous questionnaires to be administered. Can handle likert-style questions with a matrix of statements and options to choose from. Can also handle free-text response questions. See the `Parameters` folder of the repo and look for files ending in `questionnaire_params.csv` to see the exact items administered in the Ageing and Chronic stress studies. 
- **WebsocketConnector**: Establishes web connection with DHive and saves the randomisation ID for that session to DHive.

666 is a default/null value often used as a placeholder throughout many of the scripts. 

## Explanation of scenes

- **WebsockectConnection**: Establishes web connection with DHive.
- **PreloadData**: Loads all of the experiment-level data from DHive.
- **LoadTrialData**: Loads all of the session and task-level data from DHive.
- **Setup**: Displays starting instructions and records `ParticipantID`.
- **trialGame**: Knapsack trials.
- **trialGameAnswer**: Deprecated, only relevant for decision variant - which is not implemented here.
- **Confidence**: Select confidence on a discrete integer scale from 0 to 10.
- **InterTrialRest**: Short break in between knapsack trials.
- **InterBlockRest**: Short break in between blocks of knapsack trials.
- **CogTaskHome**: If choosing your own tasks, it displays the menu of tasks to choose from. If at the end of the game, it shows your scores in each task.
- **Instructions**: Instructions for the cognitive tasks (not for the knapsack).
- **Quiz**: Quiz comprehension questions for the cognitive tasks (not for the knapsack).
- **FinishedCogTask**: Holding screen when someone complete a cognitive task, either for the practice set or the real set of trials.
- **ICAR**: ICAR task. Note, the game must be run in browser for this scene to work properly: it will display blank images if run within the Unity editor.
- **NBack**: Recall-1-back task.
- **SymbolDigit**: Digit Symbol Substitution task.
- **StopSignal**: Stop signal task.
- **TaskSwitching**: Letters & Numbers task.
- **Questionnaires**: Questionnaires task.
- **End**: Displays final message that the task has ended and/or the Prolific completion code. 

## Checklist prior to deployment
1. Are the main `GameManager` parameters (`skip_knapsack`, `let_participants_choose_tasks`, `url_for_id`, `ban_repeat_logins`, `save_session_data`) correctly set?
2. Have you updated the `completion_code` related aspects of the `GameManager` script?

## DHive jargon
A brief explainer for some terms you may encounter and what they refer to.
- Experiment: top level, refers to something which applies to the entire experiment (i.e. across all sessions and participants)
- Session: 2nd level, refers to something which applies to that particular session (i.e. the Participant ID)
- Task: 3rd level: 3rd level, refers to any global variables/parameters relevant to a particular task (e.g. how long a trial lasts in the KOT)
- TrialTask: bottom level, refers to a particular instance/trial from a task 

## Acknowledgements
Thank you to my colleagues in the Centre for Brain, Mind and Markets, particularly Michelle Lee, Juan Pablo Franco, Abhijeet Anand, Nova Tasha, Jedwin Villanueva, and Daniel; who each contributed in some way to developing the tasks or the server. 
