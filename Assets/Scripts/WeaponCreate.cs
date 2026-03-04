using System.Collections;
using UnityEngine;

namespace FullMetal
{
    public class WeaponCreate_DirectionOnly : MonoBehaviour
    {
        [Header("Hands")]
        [SerializeField] private Transform m_leftHandTransform;
        [SerializeField] private Transform m_rightHandTransform;

        [Header("Create VFX (optional)")]
        [SerializeField] private GameObject m_createEffect;
        [SerializeField] private float m_createHandDistance = 0.20f;

        [Header("Weapons")]
        [SerializeField] private GameObject m_frontWeapon;
        [SerializeField] private GameObject m_backWeapon;

        [Header("Timing")]
        [SerializeField] private float m_timeoutSeconds = 10f;
        [SerializeField] private float m_refactoryPeriod = 0.5f;

        [SerializeField] private float m_createLockSeconds = 0.2f;

        [Header("Replace Behavior")]
        [Tooltip("If true, destroy the previously spawned weapon on the target hand before spawning a new one.")]
        [SerializeField] private bool m_replaceExistingWeapon = true;

        private bool m_isRefactory;
        private bool m_isCurrentlyCreating;

        private Transform m_mainHand;
        private Transform m_offHand;

        private Coroutine m_failCoroutine;
        private GameObject m_instantiatedCreateEffect;
        private float m_createLockUntil;

        // Track last weapon per hand so we can replace cleanly.
        private GameObject m_leftHandWeapon;
        private GameObject m_rightHandWeapon;

        private void Start()
        {
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            StopAllCoroutines();
        }

        private void Update()
        {
            if (!m_isCurrentlyCreating) return;
            if (Time.time < m_createLockUntil) return;

            if (m_mainHand == null || m_offHand == null) return;

            if (!CheckHandDistance()) return;

            var choice = GetDirectionChoice();
            if (choice == DirectionChoice.None) return;

            var prefab = (choice == DirectionChoice.Front) ? m_frontWeapon : m_backWeapon;
            CreateWeapon(prefab);

            m_createLockUntil = Time.time + m_createLockSeconds;
        }

        private void SubscribeEvents()
        {
            EventManager.Instance._tagTouchedEvent += CreateObjectTrigger;
        }

        private void UnsubscribeEvents()
        {
            if (EventManager.Instance == null) return;
            EventManager.Instance._tagTouchedEvent -= CreateObjectTrigger;
        }

        private void CreateObjectTrigger(string tag, bool isLeft, GameObject parentObject)
        {
            if (tag != "Back") return;
            CreateWeaponTrigger(isLeft, parentObject);
        }

        private void CreateWeaponTrigger(bool isLeft, GameObject handObject)
        {
            if (m_isRefactory) return;
            StartCoroutine(StartRefactoryPeriod());

            if (m_isCurrentlyCreating)
            {
                // Cancel if the same off-hand triggers again
                bool triggeredBySameOffHand =
                    (m_offHand != null) &&
                    ((m_offHand == m_leftHandTransform) == isLeft);

                if (triggeredBySameOffHand)
                {
                    ResetCreationState();
                    return;
                }

                // Otherwise restart
                if (m_failCoroutine != null) StopCoroutine(m_failCoroutine);
                if (m_instantiatedCreateEffect != null) Destroy(m_instantiatedCreateEffect);
            }

            m_isCurrentlyCreating = true;
            m_failCoroutine = StartCoroutine(CountdownToFail());

            // Assign hands (off-hand is the triggering hand)
            if (isLeft)
            {
                m_offHand = m_leftHandTransform;
                m_mainHand = m_rightHandTransform;
            }
            else
            {
                m_offHand = m_rightHandTransform;
                m_mainHand = m_leftHandTransform;
            }

            if (m_createEffect != null && handObject != null)
                SpawnCreateEffect(isLeft, handObject.transform);
        }

        private IEnumerator StartRefactoryPeriod()
        {
            m_isRefactory = true;
            yield return new WaitForSeconds(m_refactoryPeriod);
            m_isRefactory = false;
        }

        private IEnumerator CountdownToFail()
        {
            yield return new WaitForSeconds(m_timeoutSeconds);
            ResetCreationState();
        }

        private void SpawnCreateEffect(bool isLeft, Transform handTransform)
        {
            m_instantiatedCreateEffect = Instantiate(m_createEffect);

            if (isLeft)
                m_instantiatedCreateEffect.transform.position = handTransform.position + handTransform.right * 0.06f;
            else
                m_instantiatedCreateEffect.transform.position = handTransform.position - handTransform.right * 0.06f;

            m_instantiatedCreateEffect.transform.SetParent(handTransform, true);
            //m_instantiatedCreateEffect.transform.localEulerAngles = new Vector3(0, 0, -90);
        }

        private bool CheckHandDistance()
        {
            float distance = Vector3.Distance(m_mainHand.position, m_offHand.position);
            return distance >= m_createHandDistance;
        }

        private enum DirectionChoice { None, Front, Back }

        private DirectionChoice GetDirectionChoice()
        {
            Vector3 mainToOff = (m_offHand.position - m_mainHand.position).normalized;
            Vector3 forward = m_mainHand.forward.normalized;

            float angle = Vector3.Angle(forward, mainToOff);

            if (angle < 60f) return DirectionChoice.Front;
            if (angle > 120f) return DirectionChoice.Back;
            return DirectionChoice.None;
        }

        private void CreateWeapon(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Weapon prefab is null.");
                ResetCreationState();
                return;
            }

            // Replace the existing weapon on whichever hand we're spawning onto.
            if (m_replaceExistingWeapon)
            {
                if (m_mainHand == m_leftHandTransform)
                {
                    if (m_leftHandWeapon != null) Destroy(m_leftHandWeapon);
                    m_leftHandWeapon = null;
                }
                else if (m_mainHand == m_rightHandTransform)
                {
                    if (m_rightHandWeapon != null) Destroy(m_rightHandWeapon);
                    m_rightHandWeapon = null;
                }
            }

            GameObject weapon = Instantiate(prefab);

            // Parent first, then set local transforms for consistent attachment.
            weapon.transform.SetParent(m_mainHand, false);
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
            weapon.transform.localScale = Vector3.one;
            weapon.SetActive(true);

            // Store reference so next spawn can replace it.
            if (m_mainHand == m_leftHandTransform) m_leftHandWeapon = weapon;
            else if (m_mainHand == m_rightHandTransform) m_rightHandWeapon = weapon;

            ResetCreationState();
        }

        private void ResetCreationState()
        {
            m_isCurrentlyCreating = false;

            if (m_failCoroutine != null)
            {
                StopCoroutine(m_failCoroutine);
                m_failCoroutine = null;
            }

            if (m_instantiatedCreateEffect != null)
            {
                Destroy(m_instantiatedCreateEffect);
                m_instantiatedCreateEffect = null;
            }

            m_mainHand = null;
            m_offHand = null;
        }
    }
}