using System;
using UnityEngine;

namespace GreenShop
{
    /// <summary>
    /// Раз в заданный интервал начисляет пассивный доход (IncomePerSecond) из GameManager.
    /// Progress01 и SecondsUntilNextTick можно использовать для UI-таймера.
    /// </summary>
    public class IncomeTicker : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float intervalSeconds = 1f;
        [SerializeField] private bool useUnscaledTime;
        [SerializeField] private bool autoPay = true;

        private float elapsed;

        public event Action<double> Ticked;

        /// <summary>Заполненность текущего интервала от 0 до 1.</summary>
        public float Progress01 => intervalSeconds <= 0f ? 0f : Mathf.Clamp01(elapsed / intervalSeconds);

        /// <summary>Сколько секунд осталось до следующей выплаты.</summary>
        public float SecondsUntilNextTick => Mathf.Max(0f, intervalSeconds - elapsed);

        /// <summary>Принудительно выплатить доход за один интервал (например, по кнопке).</summary>
        public void TickNow()
        {
            PayOneTick();
            elapsed = 0f;
        }

        private void Update()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (elapsed < intervalSeconds)
            {
                return;
            }

            int tickCount = Mathf.FloorToInt(elapsed / intervalSeconds);
            elapsed -= tickCount * intervalSeconds;
            PayTicks(tickCount);
        }

        private void PayOneTick()
        {
            PayTicks(1);
        }

        private void PayTicks(int tickCount)
        {
            double income = GameManager.Instance.IncomePerSecond * tickCount;
            if (income <= 0.0)
            {
                return;
            }

            if (autoPay)
            {
                GameManager.Instance.AddCoins(income);
            }

            Ticked?.Invoke(income);
        }
    }
}
