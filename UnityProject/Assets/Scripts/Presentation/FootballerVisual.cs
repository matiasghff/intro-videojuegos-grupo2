using UnityEngine;

namespace UltimaJugada
{
    /// <summary>Presentation follows the controller's position without changing gameplay or contacts.</summary>
    public sealed class FootballerVisual : MonoBehaviour
    {
        private Transform leftLeg, rightLeg;
        private Vector3 lastPosition;
        private float stride;

        public void Initialize(Transform left, Transform right)
        {
            leftLeg = left;
            rightLeg = right;
            lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            Vector3 displacement = transform.position - lastPosition;
            displacement.y = 0f;
            if (displacement.sqrMagnitude > 0.00001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(displacement), Time.unscaledDeltaTime * 14f);
                stride += displacement.magnitude * 9f;
                float swing = Mathf.Sin(stride) * 25f;
                leftLeg.localRotation = Quaternion.Euler(swing, 0f, 0f);
                rightLeg.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            }
            else
            {
                leftLeg.localRotation = Quaternion.identity;
                rightLeg.localRotation = Quaternion.identity;
            }
            lastPosition = transform.position;
        }
    }
}
