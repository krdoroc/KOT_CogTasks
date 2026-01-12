using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Dhive;
using UnityEngine.SceneManagement;


public class WebsocketConnector : MonoBehaviour
{
    [SerializeField] public string sessionID = "";
    private async void Start()
    {
        StartCoroutine(Wait());
        Debug.Log("setup trialid");
        GameManager.sender = DhiveSender.GetInstance(GameManager.participantTrialId);
        Debug.Log($"New Trial ID: {GameManager.participantTrialId}");
        await GameManager.sender.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        GameManager.sender?.DispatchMessageQueue();
#endif
    }

    IEnumerator Wait()
    {
        yield return new WaitForSeconds(2.5f);
        SceneManager.LoadScene(GameManager.first_task_scene);
    }

    private async void OnDestroy()
    {
        await WriteRandomizationToServer(GameManager.randomizationID);
    }

    // private async Task WriteRandomizationToServer(int randomizationID)
    // {
    //     var parameters = new List<OutputParameter>
    //     {
    //         new($"assigned_rID_{randomizationID}", 1)
    //     };
        
    //     Debug.Log($"Saving randomization ID to server: {randomizationID}");
    //     DataSaver.sessionID = sessionID;
    //     if(GameManager.save_session_data)
    //     {
    //         DataSaver.AddSessionDataToSave(parameters);  
    //     }
    // }

    private async Task WriteRandomizationToServer(int randomizationID)
    {
        if (GameManager.save_write_locally)
        {
            // Standardize headers to match GameManager's session_data format
            var parameters = new List<OutputParameter>
            {
                new OutputParameter("session_metric", $"assigned_rID"),
                new OutputParameter("session_value", randomizationID.ToString())
            };

            // Use the central saver
            DataSaver.PrepareToSave(parameters, "session_data");
        }
        else
        {
            var parameters = new List<OutputParameter>
            {
                new($"assigned_rID_{randomizationID}", 1)
            };
            
            if(GameManager.save_session_data)
            {
                DataSaver.AddSessionDataToSave(parameters);  
            }
        }

        Debug.Log($"Saving randomization ID: {randomizationID}");
        DataSaver.sessionID = sessionID;
    }
}
