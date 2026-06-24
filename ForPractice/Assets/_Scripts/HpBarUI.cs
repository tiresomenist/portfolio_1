using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HpBarUI : MonoBehaviour
{
    [SerializeField] private IntEventChannelSO hpEventChannel;
    [SerializeField] private Slider hpSlider;

    private void OnEnable()
    {
        if (hpEventChannel != null) hpEventChannel.Subscribe(UpdateHpUI);
    }

    private void OnDisable()
    {
        if (hpEventChannel != null) hpEventChannel.Unsubscribe(UpdateHpUI);
    }

    private void UpdateHpUI(int newHp)
    {
        hpSlider.value = newHp;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
