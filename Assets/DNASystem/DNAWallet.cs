using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DNAWallet : MonoBehaviour
{
    [SerializeField, Min(0)] int startingDNA;

    public static DNAWallet Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    public int CurrentDNA { get; private set; }

    public event Action<int> OnDNAChanged;

    void Awake()
    {
        Instance = this;
        CurrentDNA = startingDNA;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AddDNA(int amount)
    {
        if (amount <= 0)
            return;

        CurrentDNA += amount;
        OnDNAChanged?.Invoke(CurrentDNA);
    }

    public bool CanSpendDNA(int amount)
    {
        return amount >= 0 && CurrentDNA >= amount;
    }

    public bool TrySpendDNA(int amount)
    {
        if (!CanSpendDNA(amount))
            return false;

        if (amount == 0)
            return true;

        CurrentDNA -= amount;
        OnDNAChanged?.Invoke(CurrentDNA);
        return true;
    }
}
