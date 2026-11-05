
using UnityEngine;

/// <summary>
/// Adds a reliable carrom-style rebound when the golf ball
/// hits course walls, rails, bumpers or barriers.
/// Attach this component to the GolfBall GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class GolfWallBounce : MonoBehaviour
{
    [Header("Wall Bounce Settings")]
    [SerializeField, Range(0.1f, 1f)]
    private float restitution = 0.82f;

    [SerializeField, Range(0.5f, 1f)]
    private float tangentialRetention = 0.96f;

    [SerializeField]
    private float minimumImpactSpeed = 0.08f;

    [SerializeField]
    private float minimumBounceSpeed = 0.12f;

    [Header("Recognized Bounce Surfaces")]
    [SerializeField]
    private string[] bounceSurfaceNames =
    {
        "Wall",
        "Rail",
        "Bumper",
        "Barrier"
    };

    private Rigidbody rb;

    // Velocity immediately before the physics simulation step.
    private Vector3 velocityBeforePhysics;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (rb != null && !rb.isKinematic)
        {
            velocityBeforePhysics = rb.linearVelocity;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (rb == null || rb.isKinematic)
            return;

        if (!IsBounceSurface(collision.collider))
            return;

        if (velocityBeforePhysics.sqrMagnitude < 0.0001f)
            return;

        Vector3 incomingVelocity = velocityBeforePhysics;

        Vector3 bestNormal = Vector3.zero;
        float strongestApproach = 0f;

        // Find the contact normal that best represents
        // the direction of impact against the wall.
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);

            Vector3 normal = contact.normal;

            // Wall bounce is horizontal.
            normal.y = 0f;

            if (normal.sqrMagnitude < 0.0001f)
                continue;

            normal.Normalize();

            float approach =
                Vector3.Dot(incomingVelocity, normal);

            // Orient normal against incoming velocity.
            if (approach > 0f)
            {
                normal = -normal;
                approach = -approach;
            }

            if (approach < strongestApproach)
            {
                strongestApproach = approach;
                bestNormal = normal;
            }
        }

        float impactSpeed = -strongestApproach;

        if (bestNormal.sqrMagnitude < 0.0001f ||
            impactSpeed < minimumImpactSpeed)
        {
            return;
        }

        // Preserve movement parallel to the wall.
        Vector3 tangentVelocity =
            incomingVelocity -
            Vector3.Project(incomingVelocity, bestNormal);

        tangentVelocity *= tangentialRetention;

        // Calculate opposite-direction rebound.
        float reboundSpeed = Mathf.Max(
            impactSpeed * restitution,
            minimumBounceSpeed
        );

        Vector3 outgoingVelocity =
            tangentVelocity +
            bestNormal * reboundSpeed;

        // Preserve vertical movement.
        outgoingVelocity.y = rb.linearVelocity.y;

        // Apply controlled rebound.
        rb.linearVelocity = outgoingVelocity;

        rb.WakeUp();

        Debug.Log(
            $"[GolfWallBounce] Rebounded from " +
            $"{collision.collider.name}. " +
            $"Impact: {impactSpeed:F2} m/s"
        );
    }

    private bool IsBounceSurface(Collider surface)
    {
        if (surface == null)
            return false;

        Transform current = surface.transform;

        while (current != null)
        {
            foreach (string surfaceName in bounceSurfaceNames)
            {
                if (current.name.IndexOf(
                    surfaceName,
                    System.StringComparison.OrdinalIgnoreCase
                ) >= 0)
                {
                    return true;
                }
            }

            current = current.parent;
        }

        return false;
    }
}
