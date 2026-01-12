using UnityEngine;
using System;
using System.Threading.Tasks;

/// <summary>
/// The parent class for all major game tasks (KOT, KDT, TSP).
/// GameManager will only ever talk to this class, not the specific implementations.
/// </summary>
public abstract class BaseComplexTask : MonoBehaviour
{
    [Header("Base Task Settings")]
    public string taskID;
    public bool asks_for_confidence = true;
    public bool is_decision_task = false;

	public float current_timer = 0f; 
    protected float time_limit = 0f; 
    protected bool show_timer = false;

    // EVENTS
    public Action OnTaskCompleted;

    // --- CONTRACT METHODS (Must be implemented by Child) ---
    public abstract void StartComplexTask();
	public abstract void ResumeAfterTrialRest();
    public abstract void ResumeAfterBlockRest();
    public abstract void ProcessConfidence();
	protected abstract void OnUpdateTimerUI(float remainingTime);
	public abstract void ResetCounters();  
	public abstract int total_complex_instances { get; }

	// --- SHARED METHODS (Can be overridden) ---
	protected async Task RunTimer()
    {
        if (show_timer)
        {
            current_timer -= Time.deltaTime;
            
            // Call the child's specific UI method
            OnUpdateTimerUI(current_timer);
        }
        await Task.Yield();
    }
	public virtual async Task TaskUpdate() 
    { 
        await RunTimer(); // Default does nothing
    }
	public virtual void FinishComplexTask() 
	{
		OnTaskCompleted?.Invoke();
	}
}