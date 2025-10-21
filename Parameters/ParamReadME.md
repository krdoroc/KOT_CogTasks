# Parameter explanation

## Knapsack Optimisation Task

- timeRest1min: int; minimum length of inter-trial-interval in seconds
- timeRest1max: int; maximum length of inter-trial-interval in seconds
- timeRest2: int; length of inter-block rest in seconds
- timeTrial: int; length of a trial in seconds
- timeOnlyItems: int; length in seconds during which only the items are visible and not the constraints. 
- numberOfTrials: int; number of trials in each block
- numberOfBlocks: int; number of blocks
- numberOfInstances: int; total number of trials, i.e. the product of numberOfTrials and numberOfBlocks
- iX_ prefix: X is the instance identifier
- capacity suffix: int; total weight capacity of the instance
- capacityAtOptimum suffix: int; weight accrued in the optimal solution
- expAccuracy suffix: float; expected mean accuracy, as predicted by the model used to sample instances
- instanceType suffix: int; the number of items in the instance
- problemID suffix: str; unique identifier for the instance
- profitAtOptimum suffix: int; value achieved in the optimal solution
- solutionItems suffix: list of int; each element is 1 if the item is in the optimal solution, 0 otherwise
- values suffix: list of int; each element is the value of a given item
- weights suffix: list of int; each element is the weight of a given item
- instanceRandomization suffix: list of int; the sequence in which instances appear. Each participant is allocated one unique randomisation. Each element stores the instance number and its index the order of appearance.

## Digit Symbol Substitution Task

- i_digits: list of str; each of the digits that are paired with a symbol
- i_item_n: int; number of digit-symbol pairs
- i_time_limit: int; time limit in seconds
- i_symbol_cues: list of str; unicode for each of the symbols that are paired with a digit
- i_practice_time: int; time limit for the practice round in seconds

## Progressive matrices (ICAR)

- i_is_progressive: bool; TRUE if matrix questions should be in ascending order of difficulty
- i_matrices_only: bool; TRUE if only matrix questions should be asked, rather than non-matrix questions from the sample ICAR test
- i_instance_number_matrices: int; the number of progressive matrices trials
- i_instance_number_full: int; the number of trials on the ICAR sample test
- i_time_per_question: float, the average time limit in seconds for each trial (the actual time limit is cumulative across all trials, rather than enforced on a per-trial basis)
- i_full_sample prefix: questions on the full ICAR sample test
- i_matrices_only prefix: questions that only include progressive matrices
- QuestionID suffix: str; unique ICAR question identifier
- hasIage suffix: bool; TRUE if the question requires an image to be loaded
- QuestionPrompt suffix: str; the question being asked
- Choices suffix: list of str; the possible answers that could be selected
- CorrectChoiceIndex suffix: int; which index of `Choices` is the correct solution. Indexing starts from 0

## Number-letter task

- i_time_limit: int; time limit per trial in seconds
- i_rule_display_time: int; time in seconds for which a reminder of the correct rules are displayed after each incorrect response
- i_block_1: int; number of trials in block 1 (letters only)
- i_block_2: int; number of trials in block 2 (numbers only)
- i_block_3: int; number of trials in block 4 (letters and numbers)
- i_no_practice_instances: int; number of practice trials
- i_practice_block_1: int; number of letter-only practice trials 
- i_practice_block_2: int; number of number-only practice trials 
- i_practice_block_3: int; number of numbers and letters practice trials 
- i_rest_time: int; rest time in seconds in between blocks, and halfway between block 3
- i_practice_list: list of str; the stimuli used for the practice trials
- i_switch_congruence_X_random_Y: list of str; the stimuli used for the real trials. Each participant was assigned a congruence value of between 0 and 3 and then randomly assigned a stimuli list for that level of congruence. Congruence values dictated which keys were correct for vowels/consonants and odds/evens

## Recall-1-Back

- i_time_1: float; maximum time per trial in block 1, in seconds
- i_time_2: float; maximum time per trial in block 2, in seconds
- i_time_3: float; maximum time per trial in block 3, in seconds
- i_block_1: int; number of trials in block 1
- i_block_2: int; number of trials in block 2
- i_block_3: int; number of trials in block 3
- i_digits: list of str; the possible digits participants could be asked to recall
- i_no_practice_instances: int; the total number of practice trials
- i_practice_block_1: int; the number of practice trials in block 1
- i_practice_block_2: int; the number of practice trials in block 2
- i_practice_block_3: int; the number of practice trials in block 3

## Stop Signal Task

- i_no_instances: int; number of trials per block
- i_no_blocks: int;	number of blocks
- i_time_limit: int; max time per trial in seconds
- i_init_stop_delay: float;	starting value for the stop signal delay (SSD)
- i_delta_stop_delay: float; the amount by which the SSD changed (positively or negatively) after each trial
- i_no_practice_instances: int; number of practice trials
- i_feedback_time: int;	the amount of time in seconds that participants saw their progress to date, i.e. how many arrows they had missed and what fraction of stop signals they managed to obey
- i_rest_time: int; total time to rest in between blocks, in seconds

## Questionnaires

- i_num_real_questions: int; number of questionnaires to administer
- i_num_starter_questionnaires: int; number of starting questions to administer. These are in addition to those listed in real_questions and they always occur prior to real_questions
- i_var_name_prefix: str; i_ageing_ for the healthy cognitive ageing project or i_chronic_stress for the perceived chronic stress project
- QuestionID suffix: str; name given to the questionnaire
- MainQuestion suffix: str; the main question/prompt for the questionnaire
- Choices suffix: list of str; the possible options that could be selected, e.g. Below Average, Average, Above average
- SubQuestions suffix: list of str; the specific items/statements that make up the questionnaire
- HasTextInput suffix: bool; TRUE if there was a free text component, FALSE if it was purely a multiple choice / likert scale

## Session Params

- all_rIDs: list of int; the possible randomisation IDs each participant could be allocated. A participant was allocated one, and this determined, for example, which knapsack randomisation of instances they received. 
