using UnityEngine;
using System;
using ModularTopDown.Locomotion;

public class SoilPlot : MonoBehaviour, IInteractable
{
    
    [SerializeField] private PlotManager plotManager;
    [Header("State Visuals")]
    [SerializeField] private Material untilledMaterial;
    [SerializeField] private Material tilledMaterial;
    [SerializeField] private Material wateredMaterial;
    [Header("Dependencies")]
    [SerializeField] private MeshRenderer plotRenderer;
    [SerializeField] private Transform plantAnchor;
    

    public event Action OnPlotUpdated;

    public bool IsTilled { get; private set; }
    public bool IsWatered { get; private set; }
    public SeedData PlantedSeed { get; private set; }
    public int GrowthProgress { get; private set; }
    public bool IsReadyToHarvest => PlantedSeed != null && GrowthProgress >= PlantedSeed.daysToGrow;

    private GameObject currentPlantVisual;

    void Start()
    {
        plotManager?.RegisterPlot(this);
        SetInitialState();
    }

    void OnDestroy()
    {
        plotManager?.UnregisterPlot(this);
    }

    public string GetInteractionPrompt(ActiveToolSystem toolSystem)
    {
        ItemData activeItem = toolSystem.CurrentActiveItem;

        if (IsReadyToHarvest) return "Thu hoạch";
        if (!IsTilled) return "Xới đất";
        if (IsTilled && PlantedSeed == null && activeItem is SeedData) return $"Gieo hạt {activeItem.itemName}";
        if ((IsTilled || PlantedSeed != null) && !IsWatered) return "Tưới nước";

        return string.Empty;
    }

    public bool Interact(GameObject interactor, ActiveToolSystem toolSystem)
    {
        if (IsReadyToHarvest)
        {
            return TryHarvest(interactor);
        }

        if (!IsTilled)
        {
            return TryTill(interactor); // Truyền interactor vào
        }

        if (PlantedSeed == null && toolSystem.CurrentActiveItem is SeedData seedToPlant)
        {
            return TryPlant(seedToPlant, interactor); // Truyền interactor vào
        }

        if (!IsWatered)
        {
            return TryWater(interactor); // Truyền interactor vào
        }

        return false;
    }

    public void AdvanceDay()
    {
        if (PlantedSeed == null)
        {
            IsWatered = false;
        }
        else if (IsWatered)
        {
            GrowthProgress++;
            IsWatered = false;
            UpdatePlantVisual();
        }

        UpdatePlotVisuals();
        OnPlotUpdated?.Invoke();
    }

    private bool TryTill(GameObject interactor)
    {
        // Thêm dòng này để trigger animation
        interactor.GetComponentInChildren<CharacterAnimator>()?.PlayTargetAnimation("Tilling");

        IsTilled = true;
        UpdatePlotVisuals();
        OnPlotUpdated?.Invoke();
        return true;
    }


    private bool TryWater(GameObject interactor)
    {
        // Thêm dòng này để trigger animation
        interactor.GetComponentInChildren<CharacterAnimator>()?.PlayTargetAnimation("Watering");

        IsWatered = true;
        UpdatePlotVisuals();
        OnPlotUpdated?.Invoke();
        return true;
    }


    private bool TryPlant(SeedData seed, GameObject interactor)
    {
        if (!IsTilled || PlantedSeed != null) return false;

        if (interactor.TryGetComponent<PlayerInventory>(out var inventory))
        {
            if (inventory.RemoveItem(seed, 1))
            {
                PlantedSeed = seed;
                GrowthProgress = 0;
                UpdatePlantVisual();
                OnPlotUpdated?.Invoke();
                return true;
            }
        }
        return false;
    }

    private bool TryHarvest(GameObject interactor)
    {
        if (!IsReadyToHarvest) return false;

        if (interactor.TryGetComponent<PlayerInventory>(out var inventory))
        {
            if (inventory.AddItem(PlantedSeed.cropToYield, 1))
            {
                ClearPlot();
                return true;
            }
        }
        return false;
    }

    private void SetInitialState()
    {
        IsTilled = false;
        IsWatered = false;
        PlantedSeed = null;
        GrowthProgress = 0;
        UpdatePlotVisuals();
    }

    private void ClearPlot()
    {
        if (currentPlantVisual != null)
        {
            Destroy(currentPlantVisual);
        }
        SetInitialState();
        OnPlotUpdated?.Invoke();
    }

    private void UpdatePlotVisuals()
    {
        if (!IsTilled)
        {
            plotRenderer.material = untilledMaterial;
            return;
        }

        plotRenderer.material = IsWatered ? wateredMaterial : tilledMaterial;
    }

    private void UpdatePlantVisual()
    {
        if (currentPlantVisual != null)
        {
            Destroy(currentPlantVisual);
        }

        if (PlantedSeed == null || PlantedSeed.growthStages.Count == 0) return;

        float progressRatio = (float)GrowthProgress / PlantedSeed.daysToGrow;
        int stageIndex = Mathf.FloorToInt(progressRatio * (PlantedSeed.growthStages.Count));
        stageIndex = Mathf.Clamp(stageIndex, 0, PlantedSeed.growthStages.Count - 1);

        GameObject stagePrefab = PlantedSeed.growthStages[stageIndex];
        if (stagePrefab != null)
        {
            currentPlantVisual = Instantiate(stagePrefab, plantAnchor.position, plantAnchor.rotation, plantAnchor);
        }
    }
}