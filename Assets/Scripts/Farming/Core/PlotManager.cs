using System.Collections.Generic;
using UnityEngine;

public class PlotManager : MonoBehaviour
{
    private readonly List<SoilPlot> registeredPlots = new List<SoilPlot>();

    void OnEnable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayAdvanced += HandleDayAdvanced;
        }
    }

    void OnDisable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayAdvanced -= HandleDayAdvanced;
        }
    }

    public void RegisterPlot(SoilPlot plot)
    {
        if (!registeredPlots.Contains(plot))
        {
            registeredPlots.Add(plot);
        }
    }



    public void UnregisterPlot(SoilPlot plot)
    {
        if (registeredPlots.Contains(plot))
        {
            registeredPlots.Remove(plot);
        }
    }

    private void HandleDayAdvanced()
    {
        foreach (var plot in registeredPlots)
        {
            plot.AdvanceDay();
        }
    }
}