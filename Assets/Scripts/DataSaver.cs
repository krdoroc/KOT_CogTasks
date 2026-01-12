using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Dhive;
using Newtonsoft.Json;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Linq;
using System.IO; 

enum DataType
{
    Trial,
    TrialTask,
    Session
}
public class DataSaver : MonoBehaviour
{
    private static ConcurrentQueue<Tuple<string, List<OutputParameter>, DataType>> taskQueue = new ConcurrentQueue<Tuple<string, List<OutputParameter>, DataType>>();
    private static bool running = true;
    private static int counter = 0;
    public static DataSaver instance;
    public static string participantTrialId;
    private static DhiveSender sender;
    public static string sessionID;

    public static int GetQueueSize(){
        return taskQueue.Count;
    }
    public static DataSaver Init(){
        sender ??= DhiveSender.GetInstance(participantTrialId);
        return instance;
    }
    public static DataSaver GetInstance(){
        return instance;
    }

    private static async Task SaveData(string taskId, List<Dhive.OutputParameter> outputs)
    {
        Debug.Log($"[Q] Calling save data with outputs:");
        foreach (var output in outputs)
        {
            Debug.Log($"[Q] {JsonConvert.SerializeObject(output)}");
        }
        string trialTask = await sender.NewTrialTask(taskId);
        Debug.Log($"[Q] 2b: Created new trial task: {trialTask}");
        if (string.IsNullOrEmpty(trialTask))
        {
            Debug.Log("[T] ignored: duplicate response");
            throw new Exception("duplicate response -- potential task leak");
        } 
        else
        {
            await sender.SaveParameter(trialTask, outputs);
            Debug.Log($"[Q] 4: Sent data to save for trial task: {trialTask}");
        }
    }

    private static async Task SaveTrialData(List<Dhive.OutputParameter> outputs){
        await sender.SaveTrialParameter(participantTrialId, outputs);
        Debug.Log($"Sent Trial Data.");
    }

    public static void AddDataToSave(string taskId, List<Dhive.OutputParameter> outputs){
        Debug.Log("[Q] 1: Enqueued data!");
        taskQueue.Enqueue(Tuple.Create(taskId, outputs, DataType.TrialTask));
    }

    public static void AddTrialDataToSave(List<Dhive.OutputParameter> outputs){
        taskQueue.Enqueue(Tuple.Create("", outputs, DataType.Trial));
    }
    public static void AddTrialDataToSave(Dhive.OutputParameter output){
        var outputs = new List<OutputParameter>
		{
            output
        };
        taskQueue.Enqueue(Tuple.Create("", outputs, DataType.Trial));
    }
    public static void AddSessionDataToSave(List<Dhive.OutputParameter> outputs){
        taskQueue.Enqueue(Tuple.Create("", outputs, DataType.Session));
    }
    private static async Task SaveSessionData(List<Dhive.OutputParameter> outputs){
        //string sessionID = DataLoader.sessionID;
        await sender.SaveSessionParameter(sessionID, outputs);
        Debug.Log($"Sent Session Data.");
    }
    private static IEnumerator StartConsumer()
    {

            while (running)
            {
                
                // Check if there are tasks in the queue.
                if (taskQueue.TryDequeue(out Tuple<string, List<OutputParameter>, DataType> tuple))
                {
                    Debug.Log("Dequeued a tuple");
                    Task taskRunner = null;
                    try
                    {
                        switch (tuple.Item3)
                        {
                            case DataType.TrialTask:
                                taskRunner = SaveData(tuple.Item1, tuple.Item2);
                                break;
                            case DataType.Trial:
                                taskRunner = SaveTrialData(tuple.Item2);
                                break;
                            case DataType.Session:
                                taskRunner = SaveSessionData(tuple.Item2);
                                break;
                        }
                    }
                    catch(Exception error)
                    {
                        Debug.Log($"[Q] failed save attempt, {error}, for tuple: {tuple}");
                        taskQueue.Enqueue(tuple);
                    }
                    Stopwatch timer = new Stopwatch();
                    timer.Start();
                    while (taskRunner != null)
                    {
                        Debug.Log($"[Q] {timer.ElapsedMilliseconds}");
                        if (timer.ElapsedMilliseconds >= 10000 || taskRunner.IsFaulted)
                        {
                            if (timer.ElapsedMilliseconds >= 10000)
                            {
                                Debug.Log($"[Q] re-queue reason: time elapsed");
                                Debug.Log($"[Q] Is runner also faulted?: {taskRunner.IsFaulted}");
                            }
                            else
                            {
                                Debug.Log($"[Q] re-queue reason: faulted: {taskRunner?.Exception?.Message}");
                            }

                            taskQueue.Enqueue(tuple);
                            taskRunner = null;
                            timer.Stop();
                            timer.Reset();
                            timer = null;
                            yield return new WaitForSecondsRealtime(10);
                        } else if (!taskRunner.IsCompleted)
                        {
                            yield return null;
                        } else if (taskRunner.IsCompletedSuccessfully)
                        {
                            taskRunner = null;
                            timer.Stop();
                            timer.Reset();
                            timer = null;
                        }
                    }
                    timer?.Stop();
                    timer?.Reset();
                    timer = null;
                }
                else{
                }

                yield return null;
            }
        
    }
    void Awake(){
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);  
        }
        else if (instance != this)
        {
            Destroy(gameObject); 
            return;
        }

        StartCoroutine(StartConsumer());
    }
    void Start()
    {

    }

    void OnDestroy()
    {
        // Stop the consumer loop when the object is destroyed.
        Debug.LogError("DataSaver destroyed");
        running = false;
    }
    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
                sender?.DispatchMessageQueue();
        #endif
    }

    public static void AddParticipantTrialId(string participantTrialId)
    {
       DataSaver.participantTrialId = participantTrialId;
       Init();
    }

    // direct data to local save or DHive
    public static void PrepareToSave(List<OutputParameter> outputs, string task_name)
    {
        if (GameManager.save_write_locally)
        {
            WriteToCSV(outputs, task_name);  
        }
        else
        {
            Debug.Log("[Q] Queue size before saving data is " + GetQueueSize());
            
            if (task_name == "session_data") 
            {
                // Use TrialData (generic) instead of TaskData (specific to a game)
                AddTrialDataToSave(outputs); 
            }
            else
            {
                AddDataToSave(GameManager.TaskId, outputs); 
            }
        }
    }

    // function for local saving
    private static void WriteToCSV(List<OutputParameter> outputs, string task_name)
    {
        Debug.Log("[Q] 0: Local Save: " + string.Join(", ", outputs.Select(o => $"{o.Name}: {o.Value}")));

        string directoryPath = Path.Combine(Application.persistentDataPath, "Output");
        string filePath = Path.Combine(directoryPath, GameManager.participantID + "_" + task_name + ".csv");

        // Create the directory if it doesn't exist yet
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        bool fileExists = File.Exists(filePath);

        using (StreamWriter sw = new StreamWriter(filePath, true)) 
        {
            // If file doesn't exist, write headers first
            if (!fileExists)
            {
                string headerLine = "participant_id," + string.Join(",", outputs.Select(o => CleanForCSV(o.Name)));
                sw.WriteLine(headerLine);
            }

            // Write data
            string dataLine = CleanForCSV(GameManager.participantID) + "," + 
                            string.Join(",", outputs.Select(o => CleanForCSV(o.Value.ToString())));
            
            sw.WriteLine(dataLine);
        }
        
        Debug.Log($"[Local Save] Appended to: {filePath}");
    }

    // Helper function to handle commas in data so Excel/R doesn't break
    private static string CleanForCSV(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        
        if (str.Contains(","))
        {
            return "\"" + str + "\""; 
        }
        return str;
    }

}
