using UnityEngine;

public class StateMachine : MonoBehaviour
{
    public virtual BaseState DefaultState() => new BaseState(this);
    public BaseState currentState;
    public BaseState bufferedState;

    public event System.Action deathEvent;

    protected virtual void Awake()
    {
    }
    protected virtual void Start()
    {
        currentState = DefaultState();
        currentState.OnEnter();
    }
    protected virtual void Update()
    {
        currentState.Update();
    }
    protected virtual void FixedUpdate()
    {
        currentState.FixedUpdate();
    }
    //protected virtual void OnCollisionEnter2D(Collision2D collision)
    //{
    //    currentState.OnCollisionEnter2D(collision);
    //}
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        currentState.OnTriggerEnter2D(collision);
    }
    public virtual void ChangeState(BaseState newState)
    {
        currentState.OnExit();
        newState.OnEnter();
        currentState = newState;
    }
    public void Die()
    {
        deathEvent?.Invoke();
        Destroy(gameObject);
    }
}