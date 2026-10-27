using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MimicSpace
{
    /// <summary>
    /// This is a very basic movement script, if you want to replace it
    /// Just don't forget to update the Mimic's velocity vector with a Vector3(x, 0, z)
    /// </summary>
    public class Movement : MonoBehaviour
    {
        [Tooltip("Body Height from ground")]
        [Range(0.5f, 5f)]
        public float height = 0.8f;
        public float speed = 5f;
        public float velocityLerpCoef = 4f;
        [SerializeField] private LayerMask groundMask = ~0;
        private Vector3 velocity = Vector3.zero;
        private Vector3 destination;
        private bool hasDestination;
        private Mimic myMimic;
        private Rigidbody body;

        private void Awake()
        {
            myMimic = GetComponent<Mimic>();
            body = GetComponent<Rigidbody>();
            if (body)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        public void SetDestination(Vector3 destination)
        {
            if (!myMimic)
            {
                Debug.LogError($"{name} cannot move because the Mimic component is missing.", this);
                return;
            }

            myMimic.ResetForCrossing();
            this.destination = destination;
            hasDestination = true;
        }

        public bool IsMoving
        {
            get { return hasDestination; }
        }

        private void Update()
        {
            if (!myMimic)
                return;

            if (!hasDestination)
            {
                velocity = Vector3.zero;
                myMimic.velocity = velocity;
                return;
            }

            Vector3 direction = destination - transform.position;
            direction.y = 0f;
            float remainingDistance = direction.magnitude;

            if (remainingDistance <= 0.05f)
            {
                Vector3 finalPosition = new Vector3(destination.x, transform.position.y, destination.z);
                if (TryGetGroundPosition(finalPosition, out Vector3 finalGroundPosition))
                    finalPosition = finalGroundPosition;

                transform.position = finalPosition;
                velocity = Vector3.zero;
                myMimic.velocity = velocity;
                hasDestination = false;
                return;
            }

            direction /= remainingDistance;
            velocity = Vector3.Lerp(velocity, direction * speed, velocityLerpCoef * Time.deltaTime);

            // Assigning velocity to the mimic to assure great leg placement
            myMimic.velocity = velocity;

            Vector3 nextPosition = Vector3.MoveTowards(
                transform.position,
                new Vector3(destination.x, transform.position.y, destination.z),
                velocity.magnitude * Time.deltaTime);

            if (velocity.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(velocity, Vector3.up);

            if (TryGetGroundPosition(nextPosition, out Vector3 groundedPosition))
                nextPosition.y = groundedPosition.y;

            transform.position = nextPosition;
        }

        private bool TryGetGroundPosition(Vector3 position, out Vector3 groundedPosition)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                position + Vector3.up * 5f,
                Vector3.down,
                20f,
                groundMask,
                QueryTriggerInteraction.Ignore);

            bool foundGround = false;
            float lowestGroundY = float.MaxValue;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform))
                    continue;

                if (hit.point.y < lowestGroundY)
                {
                    lowestGroundY = hit.point.y;
                    foundGround = true;
                }
            }

            if (foundGround)
            {
                groundedPosition = new Vector3(position.x, lowestGroundY + height, position.z);
                return true;
            }

            groundedPosition = position;
            return false;
        }
    }

}