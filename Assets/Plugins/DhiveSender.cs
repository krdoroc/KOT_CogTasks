using System.Threading.Tasks;
using System.Collections.Generic;
using System.Timers;
using JetBrains.Annotations;
using NativeWebSocket;
using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

// using System.Diagnostics;

namespace Dhive
{
    public class DhiveSender

    {
        private List<String> _trialTasksAllocated;
        public int _retriesDone = 0 ;
        private float _reconnectStartTime = 0;
        private bool _isWaiting = false;
        public bool IsConnected { get; private set; }
        public WebSocketState State => _websocket.State;

        [CanBeNull] private static DhiveSender _instance;
        [CanBeNull] private string _newTrialTaskId;
        private WebSocket _websocket;
        private bool _shouldReconnectWs = true;
        private int _sequenceNumber;
        private Timer _staleTimer;
        private static string _trialId;

        private readonly System.Diagnostics.Stopwatch timer;

        private DhiveSender(string trialId)
        {
            _trialId = trialId;
            IsConnected = false;
            _trialTasksAllocated = new List<string>();
#if UNITY_WEBGL && !UNITY_EDITOR
            var subprotocolsOrHeaders = new List<string>
            {
                "actioncable-v1-json", 
                "actioncable-unsupported", 
                $"Dhive-Trial-Id.{trialId}"
            };
#else
            var subprotocolsOrHeaders = new Dictionary<string, string>
            {
                { "Dhive-Trial-Id", trialId },
                { "Origin", $"https://{WebSocketHandler.BaseURL}" }
            };
#endif

            //Debug.Log($"[{GetType()} Websocket] Setting up WS connection object for {WebSocketHandler.BaseURL}");

            _websocket = new WebSocket($"wss://{WebSocketHandler.BaseURL}/cable", subprotocolsOrHeaders);
            timer = new System.Diagnostics.Stopwatch();
            registerListeners(_websocket);


        }

        private void registerListeners(WebSocket websocket)
        {
            
            websocket.OnOpen += async () =>
            {
                IsConnected = true; 
                timer.Start();
                Debug.Log($"[{nameof(DhiveSender)} Websocket] Connection open!");

                var message = WebSocketHandler.SubscribeToTrialChannel();
                Debug.Log($"[{nameof(DhiveSender)} Websocket] Subscribe: {message}");
                await _websocket.SendText(message);

                message = WebSocketHandler.SubscribeToSessionChannel();
                Debug.Log($"[{nameof(DhiveSender)} Websocket] Subscribe: {message}");
                await websocket.SendText(message);

                _staleTimer = new Timer(1000);
                _staleTimer.Elapsed += HandleStaleMessages;
                _staleTimer.AutoReset = true;
                _staleTimer.Enabled = true;
                _retriesDone = 0;
            };
            websocket.OnClose += async (e) =>
            {
                IsConnected = false;
                Debug.Log($"[{nameof(DhiveSender)} Websocket] Connection closed! Reason: {e}");
                _shouldReconnectWs = e switch
                {
                    WebSocketCloseCode.Abnormal => false,
                    _ => true
                };
                Debug.Log("Websocket connection is... " + IsConnected);
                _staleTimer?.Dispose();
            };
            websocket.OnError += async (e) =>
            {
                Debug.LogError($"[{nameof(DhiveSender)} Websocket] Error! {e}");
                IsConnected = false;
            };
            websocket.OnMessage += (bytes) =>
            {
                _retriesDone = 0;
                IsConnected = true;
                // getting the message as a string
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log($"[{nameof(DhiveSender)} Websocket] InMessage: {message}");

                switch (true)
                {
                    case var _ when WebSocketHandler.IsPingMessage(message):

                        // timer.Stop();
                        var timeSinceLastPingSeconds = timer.ElapsedMilliseconds / 1000;
                        timer.Reset();
                        timer.Start();
                        Debug.Log("Found PING!"); 
                        // Debug.Log($"[Nova] Found ping message: {message}");
                        // Debug.Log($"[Nova] Time Elapsed Since Ping {timeSinceLastPingSeconds}");
                        goto default;
                    case var parsed when WebSocketHandler.ParseWebSocketMessage(message) != null:
                        var parsedMessage = WebSocketHandler.ParseWebSocketMessage(message);
                        Debug.Log($"[{nameof(DhiveSender)} Websocket] ParsedMessage: {parsedMessage}");
                        switch (parsedMessage)
                        {
                            case null:
                            case var _ when parsedMessage.IsWelcomeMessage:
                                break;
                            case var _ when parsedMessage.IsConfirmSubscription:
                                break;
                            case var _ when parsedMessage.IsNewTrialTaskMessage:
                                Debug.Log("[Q] Received a new trial task ID!");
                                _newTrialTaskId = parsedMessage.NewTrialTaskId();
                                Debug.Log($"[Q] NEW: {_newTrialTaskId}");
                                break;
                            case var _ when parsedMessage.IsBroadcastMessage:
                                // Debug.Log($"Broadcast Session Params: {parsedMessage.Message.Broadcast?.Session}");
                                break;
                            case var _ when parsedMessage.IsReceivedMessage:
                                WebSocketHandler.OnMessageReceived(parsedMessage);
                                
                                break;
                            case var _ when parsedMessage.IsErrorMessage:
                                Debug.LogError($"[{nameof(DhiveSender)} Websocket] {parsedMessage.ErrorMessage}");
                                break;
                        }
                        goto default;
                    default:
                        return;
                }
            };
        }
        public static DhiveSender GetInstance(string trialId)
        {
            Debug.Log($"[{nameof(DhiveSender)}] Getting instance for trial {trialId}");
            _trialId ??= trialId;
            return _instance ??= new DhiveSender(_trialId);
        }

        public async Task WaitSeconds(int seconds)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            while (timer.ElapsedMilliseconds < seconds * 1000)
            {
                Task.Yield();
            }
            timer.Stop();
            timer.Reset();
            
        }

    public async Task Reconnect(int maxRetries)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        var subprotocolsOrHeaders = new List<string>
        {
            "actioncable-v1-json", 
            "actioncable-unsupported", 
            $"Dhive-Trial-Id.{_trialId}"
        };
#else
        var subprotocolsOrHeaders = new Dictionary<string, string>
        {
            { "Dhive-Trial-Id", _trialId },
            { "Origin", $"https://{WebSocketHandler.BaseURL}" }
        };
#endif
        
        const int BASE_SECONDS = 5;
        
        if (!_isWaiting)
        {
            Debug.Log($"[Nova] Starting wait period. IsConnected: {IsConnected}, retriesDone: {_retriesDone}");
            _isWaiting = true;
            _reconnectStartTime = Time.time;
            return;
        }
        
        float waitTime = Mathf.Min(BASE_SECONDS * (_retriesDone + 1), 30f);
        Debug.Log($"[Nova] Waiting for {waitTime} seconds");
        float elapsedTime = Time.time - _reconnectStartTime;
        
        if (elapsedTime < waitTime)
        {
            // Still waiting
            return;
        }
        
        // Wait period is over
        _isWaiting = false;
        
        if (!IsConnected && _retriesDone < maxRetries)
        {
            _retriesDone++;
            Debug.Log($"[Nova] Attempt {_retriesDone}/{maxRetries} at {DateTime.Now}");
            
            try 
            {
                try
                {
                    if (_websocket != null)
                    {
                        await _websocket.Close(); // NativeWebSocket uses Close() instead of Dispose()
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to close, reason: {ex}");
                }
                _websocket = new WebSocket($"wss://{WebSocketHandler.BaseURL}/cable", subprotocolsOrHeaders);
                Debug.Log("[Nova] Initiating new connection");
                await _websocket.Connect();
                registerListeners(_websocket);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nova] Connection attempt failed: {ex}");
            }
        }
        
        if (!IsConnected && _retriesDone >= maxRetries)
        {
            Debug.LogError("[Nova] Failed to reconnect after maximum retries");
        }
    }
    public async Task Connect()
        {
            //Debug.Log($"{GetType()} Websocket] State: {_websocket.State} ShouldReconnect? {_shouldReconnectWs}");
            if (_shouldReconnectWs && _websocket.State != WebSocketState.Open)
            {
                //Debug.Log($"[{GetType()} Websocket] Running connection task to {WebSocketHandler.BaseURL} ...");
                await _websocket.Connect();
            }
        }

        public async Task SaveParameter(string trialTaskId, List<OutputParameter> parameters)
        {
            var message = WebSocketHandler.CreateSaveTrialTaskParameterRequest(_trialId, trialTaskId, parameters);
            Debug.Log($"[Q] 3: saving parameters: {message}");
            await _websocket.SendText(message);
        }

        public async Task SaveParameter(string trialTaskId, string name, string value)
        {
            var output = new OutputParameter(name, value);
            await SaveParameter(trialTaskId, output);
        }

        public async Task SaveParameter(string trialTaskId, string name, int value)
        {
            var output = new OutputParameter(name, value);
            await SaveParameter(trialTaskId, output);
        }

        public async Task SaveParameter(string trialTaskId, string name, double value)
        {
            var output = new OutputParameter(name, value);
            await SaveParameter(trialTaskId, output);
        }

        public async Task SaveParameter(string trialTaskId, OutputParameter parameter)
        {
            var parameters = new List<OutputParameter> { parameter };
            await SaveParameter(trialTaskId, parameters);
        }

        public async Task SaveTrialParameter(string trialId, List<OutputParameter> parameters)
        {
            var message = WebSocketHandler.CreateSaveTrialParameterRequest(trialId, parameters);
            await _websocket.SendText(message);
        }

        public async Task SaveTrialParameter(string trialId, OutputParameter parameter)
        {
            var parameters = new List<OutputParameter> { parameter };
            await SaveTrialParameter(trialId, parameters);
        }

        public async Task SaveSessionParameter(string sessionId, List<OutputParameter> parameters)
        {
            var message = WebSocketHandler.CreateSaveSessionParameterRequest(_trialId, sessionId, parameters);
            await _websocket.SendText(message);
        }

        /// <summary>
        /// Creates a new trial task for the current task within the current trial.
        /// A sequence number is automatically generated if no sequence number is provided.
        /// </summary>
        /// <param name="taskId">Current task's id</param>
        /// <param name="customSequenceNumber">Overrides the internal sequence number counter</param>
        /// <returns>Newly created trial task's id. To be used in saving parameters.</returns>
        public async Task<string> NewTrialTask(string taskId, [CanBeNull] string customSequenceNumber = null)
        {
            Debug.Log("[Q] creating a new trial task!");
            _newTrialTaskId = null;

            var selectedSequenceNumber = SelectSequenceNumber(customSequenceNumber);
            var message = WebSocketHandler.CreateNewTrialTaskRequest(taskId, selectedSequenceNumber);
            await _websocket.SendText(message);

            //! NOTE: This creates a busy wait and should not be there.
            while (_newTrialTaskId == null)
            {
                await Task.Yield();
            }
            Debug.Log($"[T] going to return Trialtask id: {_newTrialTaskId}");
            if (_trialTasksAllocated.Contains(_newTrialTaskId))
            {
                
                Debug.Log($"[T] FOUND A DUPE! {_newTrialTaskId}");
                return "";
            }
            else
            {
                _trialTasksAllocated.Add(_newTrialTaskId);
            }
            
            return _newTrialTaskId;
        }

        public async Task LockSession()
        {
            var subscribe = WebSocketHandler.SubscribeToSessionChannel();
            await _websocket.SendText(subscribe);
            var lockMessage = WebSocketHandler.LockSession();
            await _websocket.SendText(lockMessage);
        }
        public void DispatchMessageQueue()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _websocket.DispatchMessageQueue();
#endif
        }

        public async Task Close()
        {
            await _websocket.Close();
        }

        private string SelectSequenceNumber([CanBeNull] string customSequenceNumber)
        {
            return customSequenceNumber ?? (_sequenceNumber++).ToString();
        }

        private async void HandleStaleMessages(System.Object source, ElapsedEventArgs e)
        {
            var staleMessages = WebSocketHandler.GetStaleMessages();

            foreach (var message in staleMessages)
            {
                await _websocket.SendText(message);
            }
        }
    }
}
