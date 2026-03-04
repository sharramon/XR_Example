using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils : MonoBehaviour
{
    public static float FindDistanceOnLocalXZPlane(Transform baseTransform, Transform compareTransform)
    {
        // Convert compareTransform position to baseTransform's local space
        Vector3 localPos = baseTransform.InverseTransformPoint(compareTransform.position);

        Vector3 localXZPos = new Vector3(localPos.x, 0, localPos.z);
        float distance = Vector3.Distance(new Vector3(0, 0, 0), localXZPos);

        return distance;
    }

    public static float FindDistanceOnLocalYZPlane(Transform baseTransform, Transform compareTransform)
    {
        // Convert compareTransform position to baseTransform's local space
        Vector3 localPos = baseTransform.InverseTransformPoint(compareTransform.position);

        Vector3 localXZPos = new Vector3(0, localPos.x, localPos.z);
        float distance = Vector3.Distance(new Vector3(0, 0, 0), localXZPos);

        return distance;
    }

    public static void LookAtOnXZPlane(Transform mainGameObject, Transform targetGameObject)
    {
        Vector3 targetPosition = targetGameObject.position;
        targetPosition.y = mainGameObject.position.y;
        Vector3 direction = targetPosition - mainGameObject.position;

        // Check if the direction is not zero (the objects aren't at the same position).
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            mainGameObject.rotation = lookRotation;
        }
    }

    public static void RotateTowardsOnXZPlane(Transform mainGameObject, Transform targetGameObject, float maxAngle)
    {
        Vector3 targetPosition = targetGameObject.position;
        targetPosition.y = mainGameObject.position.y; // Adjust target position to be on the same horizontal plane
        Vector3 direction = targetPosition - mainGameObject.position;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            // Rotate towards the target rotation by the specified maximum angle
            mainGameObject.rotation = Quaternion.RotateTowards(mainGameObject.rotation, lookRotation, maxAngle);
        }
    }
}
