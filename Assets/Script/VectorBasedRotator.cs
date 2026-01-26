using UnityEngine;
using System.Collections;

/// <summary>
/// Xoay Rubik dựa trên vector di chuyển của player
/// Logic: Dựa vào vector AB (từ vị trí cũ đến vị trí mới)
/// ✅ UPDATED: Thêm logic đặc biệt cho Teleport
/// </summary>
public class VectorBasedRotator : MonoBehaviour
{
    [Header("References")]
    public Transform rubikCube;

    [Header("Settings")]
    public float rotationSpeed = 3f;
    public float angleThreshold = 10f;
    public float verticalThreshold = 0.9f; // |AB.y| > 0.9 → rotate X 180°

    private Coroutine currentRotation;

    /// <summary>
    /// Xoay dựa trên vector di chuyển trong world space
    /// </summary>
    public void RotateBasedOnMovement(Vector3 fromPos, Vector3 toPos, System.Action onComplete = null)
    {
        Vector3 moveDir = (toPos - fromPos).normalized;

        // Trục world
        Vector3 worldX = Vector3.right;
        Vector3 worldZ = Vector3.forward;
        Vector3 worldY = Vector3.up;

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
            rotationAxis = Vector3.forward;
            float dotX = Vector3.Dot(moveDir, worldX);

            if (dotX > 0)
                rotationAngle = 90f;
            else
                rotationAngle = -90f;

            Debug.Log($"[VectorRotator] Vuông góc OZ → Xoay Z {rotationAngle}°");
        }
        // Vuông góc với OX (góc gần 90°)
        else if (Mathf.Abs(angleWithX - 90f) < angleThreshold)
        {
            rotationAxis = Vector3.right;
            float dotZ = Vector3.Dot(moveDir, worldZ);

            if (dotZ > 0)
                rotationAngle = -90f;
            else
                rotationAngle = 90f;

            Debug.Log($"[VectorRotator] Vuông góc OX → Xoay X {rotationAngle}°");
        }
        // Vuông góc với OY (góc gần 90°)
        else if (Mathf.Abs(angleWithY - 90f) < angleThreshold)
        {
            rotationAxis = Vector3.up;
            float dotZ = Vector3.Dot(moveDir, worldZ);

            if (dotZ > 0)
                rotationAngle = 90f;
            else
                rotationAngle = -90f;

            Debug.Log($"[VectorRotator] Vuông góc OY → Xoay Y {rotationAngle}°");
        }
        else
        {
            Debug.LogWarning($"[VectorRotator] Vector không vuông góc với trục! angleX={angleWithX:F1}° angleZ={angleWithZ:F1}°");
            onComplete?.Invoke();
            return;
        }

        // Thực hiện xoay
        if (currentRotation != null)
            StopCoroutine(currentRotation);

        currentRotation = StartCoroutine(RotateByAxisRoutine(rotationAxis, rotationAngle, onComplete));
    }

    [ContextMenu("Rotate UpRight")]
    public void RotateUpRight()
    {
        if (isRotating) return;

        currentRotation = StartCoroutine(RotateByAxisRoutine(Vector3.right, 90f, null));
    }

    [ContextMenu("Rotate DownRight")]
    public void RotateDownRight()
    {
        if (isRotating) return;

        currentRotation = StartCoroutine(RotateByAxisRoutine(Vector3.right, -90f, null));
    }

    [ContextMenu("Rotate UpLeft")]
    public void RotateUpLeft()
    {
        if (isRotating) return;

        currentRotation = StartCoroutine(RotateByAxisRoutine(Vector3.forward, -90f, null));
    }

    [ContextMenu("Rotate DownLeft")]
    public void RotateDownLeft()
    {
        if (isRotating) return;

        currentRotation = StartCoroutine(RotateByAxisRoutine(Vector3.forward, 90f, null));
    }

    [ContextMenu("Rotate Left")]
    public void RotateLeft()
    {
        if (isRotating) return;

        currentRotation = StartCoroutine(RotateByAxisRoutine(Vector3.up, 90f, null));
    }

    [ContextMenu("Rotate Right")]
    public void RotateRight()
    {
        if (isRotating) return;

        currentRotation = StartCoroutine(RotateByAxisRoutine(Vector3.up, -90f, null));
    }

    /// <summary>
    /// ✅ MỚI: Logic xoay đặc biệt cho Teleport
    /// 1. Tính vector AB = normalize(B - A)
    /// 2. Nếu |AB.y| > 0.9 → xoay X 180°
    /// 3. Chiếu lên mặt phẳng XZ: proj = normalize((AB.x, 0, AB.z))
    /// 4. Tính dot với ±X, ±Z
    /// 5. Hướng có dot lớn nhất quyết định góc xoay
    /// </summary>
    public void RotateForTeleport(Vector3 fromPosForwardVector, Vector3 toPosForwardVector, System.Action onComplete = null)
    {
        Debug.Log("[Teleport] Calculating rotation based on forward vectors:");
        Debug.Log($"[Teleport] From forward: {fromPosForwardVector}, To forward: {toPosForwardVector}");
        // 1️⃣ Tính vector AB
        Vector3 AB = (toPosForwardVector - fromPosForwardVector).normalized;

        // Kiểm tra xem 2 Vector cùng phương ngược hướng không
        float dot = Vector3.Dot(fromPosForwardVector.normalized, toPosForwardVector.normalized);
        Debug.Log($"[Teleport] AB Vector: {AB}, Dot product: {dot}");
        if (dot < -0.99f)
        {
            if (currentRotation != null)
                StopCoroutine(currentRotation);

            currentRotation = StartCoroutine(RotateByAxisRoutine(Vector3.right, 180f, onComplete));
            return;

        }

        // 3️⃣ Chiếu lên mặt phẳng XZ
        Vector3 proj = new Vector3(AB.x, 0f, AB.z).normalized;

        // 4️⃣ Tính dot product với 4 hướng cơ bản
        float dotPosX = Vector3.Dot(proj, Vector3.right);    // +X
        float dotNegX = Vector3.Dot(proj, Vector3.left);     // -X
        float dotPosZ = Vector3.Dot(proj, Vector3.forward);  // +Z
        float dotNegZ = Vector3.Dot(proj, Vector3.back);     // -Z

        // 5️⃣ Tìm hướng có dot lớn nhất
        float maxDot = Mathf.Max(dotPosX, dotNegX, dotPosZ, dotNegZ);

        Vector3 rotationAxis = Vector3.zero;
        float rotationAngle = 0f;

        if (Mathf.Approximately(maxDot, dotPosX))
        {
            // Hướng +X → Xoay Z 90°
            rotationAxis = Vector3.forward;
            rotationAngle = 90f;
            Debug.Log("[Teleport] Direction: +X → Rotate Z +90°");
        }
        else if (Mathf.Approximately(maxDot, dotNegX))
        {
            // Hướng -X → Xoay Z -90°
            rotationAxis = Vector3.forward;
            rotationAngle = -90f;
            Debug.Log("[Teleport] Direction: -X → Rotate Z -90°");
        }
        else if (Mathf.Approximately(maxDot, dotPosZ))
        {
            // Hướng +Z → Xoay X -90°
            rotationAxis = Vector3.right;
            rotationAngle = -90f;
            Debug.Log("[Teleport] Direction: +Z → Rotate X -90°");
        }
        else if (Mathf.Approximately(maxDot, dotNegZ))
        {
            // Hướng -Z → Xoay X +90°
            rotationAxis = Vector3.right;
            rotationAngle = 90f;
            Debug.Log("[Teleport] Direction: -Z → Rotate X +90°");
        }

        // 6️⃣ Thực hiện xoay
        if (currentRotation != null)
            StopCoroutine(currentRotation);

        currentRotation = StartCoroutine(RotateByAxisRoutine(rotationAxis, rotationAngle, onComplete));
    }

    bool isRotating = false;

    /// <summary>
    /// Xoay quanh trục theo world space
    /// </summary>
    IEnumerator RotateByAxisRoutine(Vector3 axis, float angle, System.Action onComplete)
    {
        isRotating = true;
        Quaternion startRotation = rubikCube.rotation;
        Quaternion targetWorldRotation = Quaternion.AngleAxis(angle, axis.normalized) * startRotation;

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
        isRotating = false;
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