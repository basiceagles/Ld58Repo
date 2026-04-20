using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GamePhaseManager : MonoBehaviour
{
    public float goodPhaseDuration = 420f;
    public float stormDuration = 180f;
    public float breakCheckInterval = 20f;
    public WeatherFader weatherFader;
    public float currentTimer;
    public bool isStormActive = false;
    public int stormCount = 0;
    public int breaksThisStorm = 0;

    private void Start()
    {
        StartCoroutine(MainGameLoop());
    }

    [ContextMenu("Debug/Skip Good Phase")]
    public void SkipGoodPhase()
    {
        if (!isStormActive)
        {
            currentTimer = 0.1f;
        }
    }

    private IEnumerator MainGameLoop()
    {
        while (true)
        {
            isStormActive = false;
            if (weatherFader != null)
            {
                weatherFader.FadeToBasic();
            }
            
            currentTimer = goodPhaseDuration;
            while (currentTimer > 0)
            {
                currentTimer -= Time.deltaTime;
                yield return null;
            }

            stormCount++;
            isStormActive = true;
            breaksThisStorm = 0;
            if (weatherFader != null)
            {
                weatherFader.FadeToFoggy();
            }

            CheckForFinalDestruction();

            float stormTimer = stormDuration;
            StartCoroutine(StormRoutine());

            while (stormTimer > 0)
            {
                currentTimer = stormTimer;
                stormTimer -= Time.deltaTime;
                yield return null;
            }
        }
    }

    private IEnumerator StormRoutine()
    {
        int totalChecks = 9; 
        bool anyBreakThisStorm = false;

        for (int i = 1; i <= totalChecks; i++)
        {
            if (!isStormActive)
            {
                break;
            }
            yield return new WaitForSeconds(breakCheckInterval);

            // Получаем список исправных антенн
            List<AntennaController> healthyAntennas = GetHealthyAntennas();
            int maxBreaksAllowed = GetMaxBreaksLimit(stormCount);

            if (healthyAntennas.Count == 0 || breaksThisStorm >= maxBreaksAllowed)
            {
                continue;
            }

            float breakChance = GetBreakChance(stormCount);

            if (i == totalChecks && !anyBreakThisStorm)
            {
                breakChance = 1.0f;
            }

            if (Random.value < breakChance)
            {
                int targetIndex = Random.Range(0, healthyAntennas.Count);
                if (healthyAntennas[targetIndex] != null)
                {
                    healthyAntennas[targetIndex].Break();
                    breaksThisStorm++;
                    anyBreakThisStorm = true;
                }
            }
        }
    }

    private void CheckForFinalDestruction()
    {
        AntennaController[] all = FindObjectsOfType<AntennaController>();
        foreach (var a in all)
        {
            if (a != null && a.IsBroken)
            {
                if (Random.value < 0.5f)
                {
                    a.DestroyToAsh();
                }
            }
        }
    }

    private List<AntennaController> GetHealthyAntennas()
    {
        AntennaController[] all = FindObjectsOfType<AntennaController>();
        List<AntennaController> list = new List<AntennaController>();
        foreach (var a in all)
        {
            // Шторм ломает только те антенны, которые ВКЛЮЧЕНЫ и ИСПРАВНЫ
            if (a != null && !a.IsBroken && !a.IsDestroyed && a.IsPowered)
            {
                list.Add(a);
            }
        }
        return list;
    }

    private int GetMaxBreaksLimit(int stormIdx)
    {
        if (stormIdx <= 2)
        {
            return 2;
        }
        if (stormIdx <= 4)
        {
            return 3;
        }
        if (stormIdx <= 7)
        {
            return 4;
        }
        return 5;
    }

    private float GetBreakChance(int stormIdx)
    {
        float chance = 0.08f + (stormIdx * 0.04f);
        return Mathf.Clamp(chance, 0.12f, 0.48f);
    }

    [ContextMenu("Debug/Force Start Storm")]
    public void ForceStartStorm()
    {
        StopAllCoroutines();
        StartCoroutine(ForceStormRoutine());
    }

    private IEnumerator ForceStormRoutine()
    {
        stormCount++;
        isStormActive = true;
        breaksThisStorm = 0;
        if (weatherFader != null)
        {
            weatherFader.FadeToFoggy();
        }
        yield return StartCoroutine(StormRoutine());
        StartCoroutine(MainGameLoop()); 
    }

    [ContextMenu("Debug/Break Random Antenna")]
    public void ForceBreakRandomAntenna()
    {
        List<AntennaController> healthy = GetHealthyAntennas();
        if (healthy.Count > 0)
        {
            healthy[Random.Range(0, healthy.Count)].Break();
        }
    }
}
