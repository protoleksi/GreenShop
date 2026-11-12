using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GreenShop
{
    /// <summary>
    /// HUD валюты: монеты, доход в секунду и заполнение таймера до следующей выплаты.
    /// Ссылки на TMP_Text/Image назначаются в инспекторе.
    /// </summary>
    public class CurrencyHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text coinsLabel;
        [SerializeField] private TMP_Text incomeLabel;
        [SerializeField] private Image incomeFill;

        [SerializeField] private IncomeTicker incomeTicker;

        [SerializeField] private string coinsFormat = "{0}";
        [SerializeField] private string incomeFormat = "+{0}/сек";

        private GameManager gameManager;

        private void Start()
        {
            gameManager = GameManager.Instance;

            if (gameManager != null)
            {
                gameManager.CoinsChanged += OnCoinsChanged;
                gameManager.IncomeChanged += OnIncomeChanged;

                OnCoinsChanged(gameManager.Coins);
                OnIncomeChanged(gameManager.IncomePerSecond);
            }
            else
            {
                Debug.LogWarning("[CurrencyHud] GameManager не найден.");
            }

            if (incomeTicker == null)
            {
                incomeTicker = FindAnyObjectByType<IncomeTicker>();
            }
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.CoinsChanged -= OnCoinsChanged;
                gameManager.IncomeChanged -= OnIncomeChanged;
            }
        }

        private void Update()
        {
            if (incomeFill != null && incomeTicker != null)
            {
                incomeFill.fillAmount = incomeTicker.Progress01;
            }
        }

        private void OnCoinsChanged(double coins)
        {
            if (coinsLabel != null)
            {
                coinsLabel.text = string.Format(coinsFormat, NumberFormat.Compact(coins));
            }
        }

        private void OnIncomeChanged(double incomePerSecond)
        {
            if (incomeLabel != null)
            {
                incomeLabel.text = string.Format(incomeFormat, NumberFormat.Compact(incomePerSecond));
            }
        }
    }
}
