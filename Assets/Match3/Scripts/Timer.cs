using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{


    public float timeRemaining = 0;
    public bool timerIsRunning = false;
    public Text timeText;
    public Text botTimeText;
    public bool timeOut = false;
    [SerializeField]
    Image playerCountdownImage;
    [SerializeField]
    Image opponentCountdownImage;

    private void Start()
    {
        // Starts the timer automatically
        timerIsRunning = false;
    }

    /* void Update()
     {
         if (timerIsRunning)
         {
             if (timeRemaining > 0)
             {
                 timeRemaining -= Time.deltaTime;
                 DisplayTime(timeRemaining);
                 timeOut = false;
             }
             else
             {
                 Debug.Log("Time has run out!");
                 timeRemaining = 0;
                 timerIsRunning = false;
                 timeOut = true;
             }
         }
     }*/

    public void Countdown(float duration, Match3Visual.UserState userState)
    {
        // float duration = 3f; // 3 seconds you can change this to
        //to whatever you want
        float totalTime = 0;
        switch (userState)
        {
            case Match3Visual.UserState.PLAYER:
                StartCoroutine(DoCountDownRealPlayer(15));
                break;
            case Match3Visual.UserState.OPPONENT:
                StopAllCoroutines();
                StartCoroutine(DoCountDownRealPlayer(15));
                break;
            case Match3Visual.UserState.BOT:

                StartCoroutine(DoCountDownBotPlayer(15));
                break;

        }
       
    }
    IEnumerator  DoCountDownRealPlayer(float duration)
    {
        float totalTime = 0;
        while (timerIsRunning)
        {
            DisplayTime(timeRemaining);
            if (timeRemaining <= duration)
            {
               
                playerCountdownImage.fillAmount = timeRemaining / 15;
                timeRemaining += Time.deltaTime;
                var integer = (int)totalTime; /* choose how to quantize this */
                /* convert integer to string and assign to text */
                yield return null;
            }
            else
            {
                Debug.Log("Time has run out!");
                timeRemaining = 0;
                timerIsRunning = false;
                timeOut = true;
            }
        }
    }

    IEnumerator DoCountDownBotPlayer(float duration)
    {
        float nRandom = Random.Range(6, 17);
        float totalTime = 0;
        timeRemaining = 0;
        while (timerIsRunning)
        {

            Debug.Log("Random Time" + nRandom);
            timeRemaining += Time.deltaTime;
            DisplayTime(timeRemaining);
            opponentCountdownImage.fillAmount = timeRemaining / 15;
            if (Mathf.FloorToInt(timeRemaining) >= nRandom)
            {
                FindObjectOfType<Match3Visual>().SetUserState(Match3Visual.UserState.BOT);
                Debug.Log("Time Up ...Time Up..Time Up.." + timeRemaining);
                timerIsRunning = false;
            }
                yield return null;
        
       /*     while (timerIsRunning)
        {
            DisplayTime(timeRemaining);
            Debug.Log("timeRemaining......." + Mathf.RoundToInt(timeRemaining));
            Debug.Log("Random Time" + nRandom);
            if (timeRemaining < nRandom)
            {
                timeRemaining += Time.deltaTime;
                DisplayTime(timeRemaining);
                yield return null;
            }
            

            
            if (Mathf.RoundToInt(timeRemaining) >= nRandom)
            {
                Debug.Log("Time Up ......." + timeRemaining);
               
                timerIsRunning = false;
                timeOut = true;

                //   countdownImage.fillAmount = totalTime / duration;

                var integer = (int)totalTime; /* choose how to quantize this */
              //  FindObjectOfType<Match3Visual>().SetUserState(Match3Visual.UserState.BOT);
                /* convert integer to string and assign to text */
                //timeRemaining += duration;
               // yield return null;
           // }
           /* else
            {
                Debug.Log("Time has run out!");
                timeRemaining = 0;
                timerIsRunning = false;
                timeOut = true;
            }*/
        }
    }



    void DisplayTime(float timeToDisplay)
    {
        //timeToDisplay += 1;

        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    public void EnableTimer()
    {
        this.timeRemaining = 0;
        timerIsRunning = true;
       
    }
    public void DisableTimer()
    {
        
        timerIsRunning = false;
    }
}
