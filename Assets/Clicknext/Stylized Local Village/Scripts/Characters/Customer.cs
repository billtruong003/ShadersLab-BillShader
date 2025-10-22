using Clicknext.StylizedLocalVillage.Entities;
using Clicknext.StylizedLocalVillage.Units;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Clicknext.StylizedLocalVillage.Characters
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Customer : MonoBehaviour
    {
        public event Action OnSpawn = delegate { };
        public event Action OnUnspawn = delegate { };
        public event Action<Customer, Product, int> OnBought = delegate { };
        public Transform AttachedPoint => attachedPoint;

        [SerializeField] Transform attachedPoint;
        [SerializeField] float buyingTime;
        [SerializeField] float waitTime;

        [SerializeField] int minBuyingCount;
        [SerializeField] int maxBuyingCount;
        [SerializeField] Transform[] points;

        [SerializeField] GameObject character;
        [SerializeField] Animator animator;

        private NavMeshAgent mAgent;
        private Shelf[] shelves;

        private void Awake()
        {
            character.SetActive(false);
            mAgent = GetComponent<NavMeshAgent>();
            shelves = FindObjectsByType<Shelf>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private IEnumerator Start()
        {
            yield return new WaitUntil(() => mAgent.isOnNavMesh);
            yield return new WaitForSeconds(UnityEngine.Random.Range(1f,waitTime));
            yield return Spawn();
        }

        private IEnumerator Spawn()
        {
            var startPoint = RandomPoint();
            mAgent.transform.SetPositionAndRotation(startPoint.position, startPoint.rotation);
            character.SetActive(true);
            OnSpawn();

            yield return new WaitForEndOfFrame();
            yield return Buy();
        }

        private IEnumerator Buy()
        {
            var shelf = RandomItemPoint();
            yield return GoTo(shelf.transform);

            var amount = shelf.Remove(RandomBuyingCount());
            OnBought(this, shelf.Product, amount);

            var isSuccess = amount > 0;
            animator.SetTrigger(isSuccess? "buy": "look");
            yield return new WaitForSeconds(buyingTime);

            if (isSuccess && IsBonus())
                yield return Buy();
            else
                yield return GoBack();
        }

        private IEnumerator GoBack()
        {
            yield return GoTo(RandomPoint());
            character.SetActive(false);
            OnUnspawn();

            yield return new WaitForSeconds(UnityEngine.Random.Range(1f, waitTime));
            yield return Spawn();
        }

        private IEnumerator GoTo(Transform destination)
        {
            mAgent.SetDestination(destination.position);
            animator.SetBool("isWalking", true);
            yield return new WaitUntil(() => IsPathReady());
            yield return new WaitUntil(() => mAgent.remainingDistance <= mAgent.stoppingDistance);
            mAgent.transform.rotation = destination.rotation;
            animator.SetBool("isWalking", false);
        }

        private Transform RandomPoint() => points[UnityEngine.Random.Range(0, points.Length)];

        private Shelf RandomItemPoint() => shelves[UnityEngine.Random.Range(0, shelves.Length)];

        private int RandomBuyingCount() => UnityEngine.Random.Range(minBuyingCount, maxBuyingCount + 1);

        private bool IsBonus() => UnityEngine.Random.Range(0f, 100f) <= 30f;

        public bool IsPathReady()
        {
            if (mAgent.pathPending ||
                mAgent.pathStatus == NavMeshPathStatus.PathInvalid ||
                mAgent.path.corners.Length == 0)
                return false;
            return true;
        }
    }
}
