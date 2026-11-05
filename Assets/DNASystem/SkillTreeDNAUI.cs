using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkillTreeDNAUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI dnaText;

    DNAWallet m_Wallet;

    void OnEnable()
    {
        AttachWallet();
    }

    void Start()
    {
        AttachWallet();
    }

    void OnDisable()
    {
        if (m_Wallet != null)
            m_Wallet.OnDNAChanged -= Refresh;

        m_Wallet = null;
    }

    void AttachWallet()
    {
        if (!DNAWallet.HasInstance || m_Wallet == DNAWallet.Instance)
            return;

        if (m_Wallet != null)
            m_Wallet.OnDNAChanged -= Refresh;

        m_Wallet = DNAWallet.Instance;
        m_Wallet.OnDNAChanged += Refresh;
        Refresh(m_Wallet.CurrentDNA);
    }

    void Refresh(int currentDNA)
    {
        dnaText.text = $"DNA {currentDNA}";
    }
}
