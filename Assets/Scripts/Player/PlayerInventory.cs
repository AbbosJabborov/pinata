using System;
using System.Collections.Generic;
using Pinata.Candy;
using UnityEngine;

namespace Pinata.Player
{
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [Header("Capacity Settings")]
        [SerializeField] private int maxCapacity = 30;

        private readonly Dictionary<CandyType, int> _candyCounts = new Dictionary<CandyType, int>();

        public event Action<CandyType, int, int> OnCandyAdded;
        public event Action<CandyType, int, int> OnCandyRemoved;
        public event Action<int, int> OnInventoryChanged; // (totalCount, maxCapacity)
        public event Action OnInventoryFull;

        public int MaxCapacity
        {
            get => maxCapacity;
            set
            {
                maxCapacity = value;
                OnInventoryChanged?.Invoke(TotalCount, maxCapacity);
            }
        }

        public int TotalCount
        {
            get
            {
                int sum = 0;
                foreach (var pair in _candyCounts)
                {
                    sum += pair.Value;
                }
                return sum;
            }
        }

        public bool IsFull => TotalCount >= maxCapacity;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            // Pre-seed all candy types to 0
            foreach (CandyType type in Enum.GetValues(typeof(CandyType)))
            {
                _candyCounts[type] = 0;
            }
        }

        public bool TryAddCandy(CandyType type)
        {
            if (IsFull)
            {
                OnInventoryFull?.Invoke();
                return false;
            }

            if (!_candyCounts.ContainsKey(type))
            {
                _candyCounts[type] = 0;
            }

            _candyCounts[type]++;
            int typeCount = _candyCounts[type];
            int total = TotalCount;

            OnCandyAdded?.Invoke(type, typeCount, total);
            OnInventoryChanged?.Invoke(total, maxCapacity);
            return true;
        }

        public bool TryAddCandy(CandyItem candy)
        {
            if (candy == null) return false;
            return TryAddCandy(candy.Type);
        }

        public int RemoveCandies(CandyType type, int count)
        {
            if (!_candyCounts.ContainsKey(type) || _candyCounts[type] <= 0) return 0;

            int toRemove = Mathf.Min(count, _candyCounts[type]);
            _candyCounts[type] -= toRemove;

            int remaining = _candyCounts[type];
            int total = TotalCount;

            OnCandyRemoved?.Invoke(type, remaining, total);
            OnInventoryChanged?.Invoke(total, maxCapacity);
            return toRemove;
        }

        public int GetCount(CandyType type)
        {
            return _candyCounts.TryGetValue(type, out int count) ? count : 0;
        }

        public void ClearInventory()
        {
            foreach (var key in new List<CandyType>(_candyCounts.Keys))
            {
                _candyCounts[key] = 0;
            }
            OnInventoryChanged?.Invoke(0, maxCapacity);
        }
    }
}
