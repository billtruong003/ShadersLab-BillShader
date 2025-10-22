using Clicknext.StylizedLocalVillage.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

namespace Clicknext.StylizedLocalVillage.Characters
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Farmer : MonoBehaviour
    {
        public event Action OnPicked = delegate { };

        [SerializeField] private Animator animator;
        [SerializeField] private Transform attachedPoint;
        [SerializeField] private GameObject bag;
        [SerializeField] private GameObject movePoint;
        [SerializeField] private float runSpeed;
        [SerializeField] private float walkSpeed;

        public Transform AttachedPoint => attachedPoint;

        private NavMeshAgent mAgent;
        private int mLayer;
        private Vector3 mPreviousPosition;
        private bool mIsAction;
        private Vector3 destination;

#pragma warning disable IDE0052 // Remove unread private members
        private Coroutine mPickingRoutine;
#pragma warning restore IDE0052 // Remove unread private members

        private readonly Dictionary<Item, int> mBackpack = new();

        private void Awake()
        {
            mAgent = GetComponent<NavMeshAgent>();
            mLayer = LayerMask.NameToLayer(LayerType.Ground.ToString());
            mLayer = ~mLayer;
            mPreviousPosition = transform.position;
            movePoint.transform.SetParent(transform.parent);
        }

#if (!UNITY_EDITOR)
        private float touchCooldown;
#endif

        private void Update()
        {
#if(UNITY_EDITOR)
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            if (Input.GetMouseButtonDown(0))
                CheckRaycast(Input.mousePosition, MoveTo);
#else
            touchCooldown -= Time.deltaTime;
            touchCooldown = Math.Clamp(touchCooldown, 0, 1f);

            if (Input.touches.Length > 0)
            {
                var touch = Input.touches[0];
                var isOverUI = EventSystem.current.IsPointerOverGameObject(touch.fingerId);
                if (isOverUI)
                    touchCooldown = 0.25f;

                if (!isOverUI && touchCooldown <= 0f)
                    CheckRaycast(touch.position, MoveTo);
            }
#endif
        }

        private void MoveTo(Vector3 point)
        {
            mAgent.SetDestination(point);
            destination = point;
        }

        private void FixedUpdate()
        {
            var speed = GetSpeed();
            bag.SetActive(mBackpack.Count > 0);
            animator.SetFloat("speed", speed);
            animator.SetBool("isBackpack", bag.activeInHierarchy);
            mAgent.speed = mIsAction ? 0f: bag.activeInHierarchy ? walkSpeed : runSpeed;

            if (movePoint)
            {
                movePoint.transform.position = destination;
                movePoint.SetActive(speed > 0f);
            }
        }

        private void CheckRaycast(Vector3 position, Action<Vector3> OnRaycast)
        {
            Ray ray = Camera.main.ScreenPointToRay(position);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mLayer))
                OnRaycast(hit.point);
        }

        public void Pick(Transform location, Item type, int value)
        {
            mPickingRoutine ??= StartCoroutine(Picking(location));

            if (!mBackpack.ContainsKey(type))
                mBackpack.Add(type, value);
            else
                mBackpack[type] += value;
        }

        public int GetItemByType(Item type)
        {
            int amount = 0;
            if (mBackpack.ContainsKey(type))
            {
                amount = mBackpack[type];
                mBackpack.Remove(type);
            }

            return amount;
        }

        private IEnumerator Picking(Transform location)
        {
            if (location)
            {
                mAgent.transform.LookAt(location.position);
                var eulerAngles = mAgent.transform.eulerAngles;
                    eulerAngles.x = 0f;
                    eulerAngles.z = 0f;
                mAgent.transform.eulerAngles = eulerAngles;
            }

            animator.SetTrigger("pick");
            mIsAction = true;
            yield return new WaitForSeconds(2f);
            mIsAction = false;

            mPickingRoutine = null;

        }

        private float GetSpeed()
        {
            var currentPosition = new Vector3(transform.position.x, 0f, transform.position.z);
            var lastPosition = new Vector3(mPreviousPosition.x, 0f, mPreviousPosition.z);
            var magnitude = Mathf.Abs((currentPosition - lastPosition).magnitude);
            mPreviousPosition = transform.position;
            return magnitude;
        }
    }
}
