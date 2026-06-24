using System;
using UnityEngine;

[CreateAssetMenu(fileName="IntEventChannel", menuName="Events/Int Event Channel")]
public class IntEventChannelSO : ScriptableObject
{
    private Action<int> OnEventRaised;  //구독자들이 등록할 C#액션이벤트

    //발행자가 이벤트를 일으킬 때 호출하는 함수
    public void RaiseEvent(int value)
    {
        OnEventRaised?.Invoke(value);
    }

    public void Subscribe(Action<int> listener) => OnEventRaised += listener;

    public void Unsubscribe(Action<int> listener) => OnEventRaised -= listener;
}
