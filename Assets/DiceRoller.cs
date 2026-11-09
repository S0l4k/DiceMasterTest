using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class DiceRoller : MonoBehaviour
{
    private Rigidbody rb;
    // Maps target result (1-6) to the local direction of the face that must touch the ground
    private Dictionary<int, Vector3> bottomFaceDirections;
    private bool isRolling = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        InitializeDiceFaces();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && !isRolling)
        {
            int targetResult = Random.Range(1, 7);
            Debug.Log($"Target result: {targetResult}");
            RollDice(targetResult);
        }
    }

    void InitializeDiceFaces()
    {
        // Dictionary maps which number should appear on top to which local face direction must touch the ground
        bottomFaceDirections = new Dictionary<int, Vector3>
        {
            { 1, Vector3.down },
            { 6, Vector3.up },
            { 2, Vector3.back },
            { 5, Vector3.forward },
            { 3, Vector3.left },
            { 4, Vector3.right }
        };
    }

    void RollDice(int targetResult)
    {
        isRolling = true;

        transform.position = new Vector3(0, 5, 0);
        transform.rotation = Quaternion.Euler(Random.Range(0, 360f), Random.Range(0, 360f), Random.Range(0, 360f));
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.centerOfMass = Vector3.zero;

        Vector3 targetDownDirection = bottomFaceDirections[targetResult];
        Vector3 globalTargetDown = transform.TransformDirection(targetDownDirection);

        // Directed force pushes dice away from target face, so gravity naturally pulls it down
        Vector3 randomForce = Random.insideUnitSphere * 2f;
        Vector3 directedForce = -globalTargetDown * 3f;

        rb.AddForce(randomForce + directedForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 2.5f, ForceMode.Impulse);

        StartCoroutine(ApplyCheat(targetResult));
    }

    // Multi-phase cheat: gradually shifts center of mass to avoid sudden physics jitter
    private IEnumerator ApplyCheat(int targetResult)
    {
        yield return new WaitForSeconds(0.3f);

        Vector3 targetDownDirection = bottomFaceDirections[targetResult];

        rb.centerOfMass = targetDownDirection * 0.6f;
        rb.angularDamping = 2.5f;

        yield return new WaitUntil(() => rb.angularVelocity.magnitude < 0.5f && rb.linearVelocity.magnitude < 0.5f);

        rb.centerOfMass = targetDownDirection * 0.8f;
        rb.angularDamping = 4f;

        yield return new WaitUntil(() => rb.angularVelocity.magnitude < 0.2f && rb.linearVelocity.magnitude < 0.2f);

        yield return StartCoroutine(SmoothFinalSettle(targetDownDirection));

        rb.angularDamping = 0.5f;
        rb.centerOfMass = Vector3.zero;

        int actualResult = GetTopFace();
        Debug.Log($"Actual result: {actualResult} | Target: {targetResult} | {(actualResult == targetResult ? "OK" : "MISMATCH")}");

        isRolling = false;
    }

    // Gradually increases center of mass offset and damping to smoothly settle dice on target face
    IEnumerator SmoothFinalSettle(Vector3 targetDownDirection)
    {
        float settleDuration = 0.5f;
        float elapsed = 0f;

        float startCenterOfMass = 0.8f;
        float startAngularDamping = 4f;

        float endCenterOfMass = 0.85f;
        float endAngularDamping = 6f;

        float torqueStrength = 0.3f;

        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / settleDuration;

            float currentCenterOfMass = Mathf.Lerp(startCenterOfMass, endCenterOfMass, t);
            float currentAngularDamping = Mathf.Lerp(startAngularDamping, endAngularDamping, t);

            rb.centerOfMass = targetDownDirection * currentCenterOfMass;
            rb.angularDamping = currentAngularDamping;

            rb.AddTorque(targetDownDirection * torqueStrength, ForceMode.Force);

            if (rb.angularVelocity.magnitude < 0.05f && rb.linearVelocity.magnitude < 0.05f)
            {
                break;
            }

            yield return null;
        }

        yield return new WaitUntil(() => rb.angularVelocity.magnitude < 0.05f && rb.linearVelocity.magnitude < 0.05f);
    }

    // Calculates which face is pointing up by comparing local face normals with global up vector using dot product
    int GetTopFace()
    {
        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
        int[] values = { 1, 6, 2, 5, 3, 4 };

        float maxDot = -1f;
        int result = 1;

        for (int i = 0; i < directions.Length; i++)
        {
            float dot = Vector3.Dot(transform.TransformDirection(directions[i]), Vector3.up);
            if (dot > maxDot)
            {
                maxDot = dot;
                result = values[i];
            }
        }

        return result;
    }
}