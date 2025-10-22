using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Clicknext.StylizedLocalVillage.Characters
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Villager : MonoBehaviour
    {
        [SerializeField] float minWaitTime;
        [SerializeField] float maxWaitTime;

        [SerializeField] Transform[] randomPoints;
        [SerializeField] GameObject character;

        private NavMeshAgent mAgent;
        private float mWaitingTime;

        private void Awake()
        {
            character.SetActive(false);
            mAgent = GetComponent<NavMeshAgent>();
            mWaitingTime = Random.Range(minWaitTime, maxWaitTime);
        }

        private void Update()
        {

            if (!mAgent.isOnNavMesh)
                return;

            // villager is reached the target.
            if(mAgent.remainingDistance <= mAgent.stoppingDistance)
            {
                character.SetActive(false);

                // villager is waiting.
                if (mWaitingTime > 0f)
                {
                    mWaitingTime -= Time.deltaTime;
                }
                else
                {
                    character.SetActive(true);
                    RandomPath();
                }
            }
            else
            {
                mWaitingTime = Random.Range(minWaitTime, maxWaitTime);
            }
        }

        private void RandomPath()
        {
            var startPoint = RandomExcept();
            var targetPoint = RandomExcept(startPoint);

            mAgent.transform.SetPositionAndRotation(startPoint.position, startPoint.rotation);
            mAgent.destination = targetPoint.position;
        }

        private Transform RandomExcept(Transform point = null)
        {
            var exceptPoint = randomPoints.Where(each => each != point).ToArray();
            var index = Random.Range(0, exceptPoint.Length);
            return exceptPoint[index];
        }
    }
}
