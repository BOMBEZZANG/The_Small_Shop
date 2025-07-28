using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public static TimeManager instance = null;
    
    // 시간 변경 이벤트 (시, 분, 총 분)
    public static event Action<int, int, float> OnTimeChanged;
    
    [Header("Time Settings")]
    [SerializeField] private float timeScale = 60f; // 실제 1초 = 게임 내 60초
    [SerializeField] private int startHour = 6; // 시작 시간 (오전 6시)
    [SerializeField] private int startMinute = 0;
    
    private float totalMinutes;
    private int currentHour;
    private int currentMinute;
    private bool isPaused = false;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 시작 시간 설정
        totalMinutes = startHour * 60 + startMinute;
        UpdateTimeValues();
    }
    
    void Update()
    {
        if (!isPaused)
        {
            // 시간 진행
            totalMinutes += (Time.deltaTime * timeScale) / 60f;
            
            // 24시간 순환
            if (totalMinutes >= 1440) // 24 * 60 = 1440분
            {
                totalMinutes -= 1440;
            }
            
            UpdateTimeValues();
        }
    }
    
    private void UpdateTimeValues()
    {
        currentHour = Mathf.FloorToInt(totalMinutes / 60);
        currentMinute = Mathf.FloorToInt(totalMinutes % 60);
        
        OnTimeChanged?.Invoke(currentHour, currentMinute, totalMinutes);
    }
    
    // 시간 직접 설정
    public void SetTime(int hour, int minute)
    {
        hour = Mathf.Clamp(hour, 0, 23);
        minute = Mathf.Clamp(minute, 0, 59);
        
        totalMinutes = hour * 60 + minute;
        UpdateTimeValues();
    }
    
    // 시간 추가 (휴식, 수면 등)
    public void AdvanceTime(float hours)
    {
        totalMinutes += hours * 60;
        if (totalMinutes >= 1440)
        {
            totalMinutes -= 1440;
        }
        UpdateTimeValues();
    }
    
    // 시간 일시정지/재개
    public void PauseTime(bool pause)
    {
        isPaused = pause;
    }
    
    // 현재 시간대 확인 (아침, 낮, 저녁, 밤)
    public TimeOfDay GetTimeOfDay()
    {
        if (currentHour >= 6 && currentHour < 12) return TimeOfDay.Morning;
        if (currentHour >= 12 && currentHour < 18) return TimeOfDay.Afternoon;
        if (currentHour >= 18 && currentHour < 22) return TimeOfDay.Evening;
        return TimeOfDay.Night;
    }
    
    public int GetHour() => currentHour;
    public int GetMinute() => currentMinute;
    public float GetTotalMinutes() => totalMinutes;
}

public enum TimeOfDay
{
    Morning,    // 06:00 - 11:59
    Afternoon,  // 12:00 - 17:59
    Evening,    // 18:00 - 21:59
    Night       // 22:00 - 05:59
}