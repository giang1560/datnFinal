using UnityEngine;
using System.Collections;

/// <summary>
/// Xoay Rubik dựa trên vector di chuyển của player
/// Logic: Dựa vào vector AB (từ vị trí cũ đến vị trí mới)
/// </summary>
public class VectorBasedRotator : MonoBehaviour
{
    [Header("References")]
    public Transform rubikCube;

    [Header("Settings")]
    public float rotationSpeed = 3f;
    public float angleThreshold = 10f; // Độ lệch góc chấp nhận để coi là vuông góc

    private Coroutine currentRotation;

    /// <summary>
    /// Xoay dựa trên vector di chuyển trong world space
    /// </summary>
    public void RotateBasedOnMovement(Vector3 fromPos, Vector3 toPos, System.Action onComplete = null)
    {
        // Vector AB trong world space
        Vector3 moveDir = (toPos - fromPos).normalized;

        // Trục world
        Vector3 worldX = Vector3.right;   // (1, 0, 0)
        Vector3 worldZ = Vector3.forward; // (0, 0, 1)
        Vector3 worldY = Vector3.up;      // (0, 1, 0)

        // Tính góc với các trục
        float angleWithX = Vector3.Angle(moveDir, worldX);
        float angleWithZ = Vector3.Angle(moveDir, worldZ);
        float angleWithY = Vector3.Angle(moveDir, worldY);

        Vector3 rotationAxis = Vector3.zero;
        float rotationAngle = 0f;

        // ─────────────────────────────────────────────
        // LOGIC: Kiểm tra vuông góc với trục nào
        // ─────────────────────────────────────────────

        // Vuông góc với OZ (góc gần 90°)
        if (Mathf.Abs(angleWithZ - 90f) < angleThreshold)
        {
            rotationAxis = Vector3.forward; // Xoay quanh trục Z

            // Kiểm tra cùng chiều hay ngược chiều OX
            float dotX = Vector3.Dot(moveDir, worldX);

            if (dotX > 0) // Cùng chiều OX
            {
                rotationAngle = 90f;
            }
            else // Ngược chiều OX
            {
                rotationAngle = -90f;
            }

            Debug.Log($"[VectorRotator] Vuông góc OZ → Xoay Z {rotationAngle}°");
        }
        // Vuông góc với OX (góc gần 90°)
        else if (Mathf.Abs(angleWithX - 90f) < angleThreshold)
        {
            rotationAxis = Vector3.right; // Xoay quanh trục X

            // Kiểm tra cùng chiều hay ngược chiều OZ
            float dotZ = Vector3.Dot(moveDir, worldZ);

            if (dotZ > 0) // Cùng chiều OZ
            {
                rotationAngle = -90f;
            }
            else // Ngược chiều OZ
            {
                rotationAngle = 90f;
            }

            Debug.Log($"[VectorRotator] Vuông góc OX → Xoay X {rotationAngle}°");
        }
        // Vuong góc với OY (góc gần 90°)
        else if (Mathf.Abs(angleWithY - 90f) < angleThreshold)
        {
            rotationAxis = Vector3.up; // Xoay quanh trục Y

            // Kiểm tra cùng chiều hay ngược chiều OZ
            float dotZ = Vector3.Dot(moveDir, worldZ);

            if (dotZ > 0) // Cùng chiều OZ
            {
                rotationAngle = 90f;
            }
            else // Ngược chiều OZ
            {
                rotationAngle = -90f;
            }

            Debug.Log($"[VectorRotator] Vuông góc OY → Xoay Y {rotationAngle}°");
        }
        else
        {
            Debug.LogWarning($"[VectorRotator] Vector không vuông góc với cả OX và OZ! angleX={angleWithX:F1}° angleZ={angleWithZ:F1}°");
            onComplete?.Invoke();
            return;
        }

        // Thực hiện xoay
        if (currentRotation != null)
            StopCoroutine(currentRotation);

        currentRotation = StartCoroutine(RotateByAxisRoutine(rotationAxis, rotationAngle, onComplete));
    }
    
    /// <summary>
    /// Xoay quanh trục theo world space
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="angle"></param>
    /// <param name="onComplete"></param>
    /// <returns></returns>
    IEnumerator RotateByAxisRoutine(Vector3 axis, float angle, System.Action onComplete)
    {
        Quaternion startRotation = rubikCube.rotation;

        // WORLD SPACE rotation
        Quaternion targetWorldRotation =
            Quaternion.AngleAxis(angle, axis.normalized) * startRotation;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * rotationSpeed;
            rubikCube.rotation = Quaternion.Slerp(startRotation, targetWorldRotation, t);
            yield return null;
        }

        rubikCube.rotation = targetWorldRotation;
        onComplete?.Invoke();
        currentRotation = null;
    }

        public void ResetRotation()
    {
        if (currentRotation != null)
            StopCoroutine(currentRotation);

        rubikCube.rotation = Quaternion.identity;
        currentRotation = null;

        Debug.Log("[Rotator] Reset rotation → Identity");
    }
}