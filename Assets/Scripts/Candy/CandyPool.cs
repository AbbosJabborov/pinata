using System.Collections.Generic;
using UnityEngine;

namespace Pinata.Candy
{
    public class CandyPool : MonoBehaviour
    {
        public static CandyPool Instance { get; private set; }

        [System.Serializable]
        public struct CandyPrefabEntry
        {
            public CandyType type;
            public GameObject prefab;
            public int initialPoolSize;
        }

        [SerializeField] private List<CandyPrefabEntry> candyEntries = new List<CandyPrefabEntry>();

        private readonly Dictionary<CandyType, Queue<CandyItem>> _pools = new Dictionary<CandyType, Queue<CandyItem>>();
        private readonly Dictionary<CandyType, GameObject> _prefabMap = new Dictionary<CandyType, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePools();
        }

        private void InitializePools()
        {
            foreach (var entry in candyEntries)
            {
                if (entry.prefab == null) continue;
                _prefabMap[entry.type] = entry.prefab;

                Queue<CandyItem> queue = new Queue<CandyItem>();
                for (int i = 0; i < entry.initialPoolSize; i++)
                {
                    CandyItem item = CreateNewItem(entry.type, entry.prefab);
                    queue.Enqueue(item);
                }
                _pools[entry.type] = queue;
            }
        }

        private CandyItem CreateNewItem(CandyType type, GameObject prefab)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            CandyItem item = obj.GetComponent<CandyItem>();
            if (item == null) item = obj.AddComponent<CandyItem>();
            return item;
        }

        public CandyItem Spawn(CandyType type, Vector3 position, Quaternion rotation)
        {
            CandyItem item = null;

            if (_pools.TryGetValue(type, out var queue) && queue.Count > 0)
            {
                item = queue.Dequeue();
            }
            else if (_prefabMap.TryGetValue(type, out var prefab))
            {
                item = CreateNewItem(type, prefab);
            }

            if (item != null)
            {
                item.transform.position = position;
                item.transform.rotation = rotation;
                item.gameObject.SetActive(true);
                item.ResetState();
            }

            return item;
        }

        public void Despawn(CandyItem item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            item.ResetState();
            item.transform.SetParent(transform);

            if (_pools.TryGetValue(item.Type, out var queue))
            {
                queue.Enqueue(item);
            }
            else
            {
                var newQueue = new Queue<CandyItem>();
                newQueue.Enqueue(item);
                _pools[item.Type] = newQueue;
            }
        }

        public void ReturnCandy(CandyItem item) => Despawn(item);
    }
}
