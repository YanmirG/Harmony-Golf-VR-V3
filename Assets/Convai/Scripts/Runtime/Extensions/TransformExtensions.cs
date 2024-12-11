using System.Text.RegularExpressions;
using UnityEngine;

namespace Convai.Scripts.Runtime.Extensions
{
    public static class TransformExtensions
    {
        public static T GetComponentOnChildWithMatchingRegex<T>(this Transform transform, string regexStringPattern)
        {
            // Initialize a variable to store the found SkinnedMeshRenderer.
            T targetComponent = default;

            // Define a regular expression pattern for matching child object names.
            Regex regexPattern = new(regexStringPattern);

            // Iterate through each child of the parentTransform.
            foreach (Transform child in transform)
                // Check if the child's name matches the regex pattern.
                if (regexPattern.IsMatch(child.name))
                {
                    // If a match is found, get the SkinnedMeshRenderer component of the child.
                    targetComponent = child.GetComponent<T>();

                    // If a SkinnedMeshRenderer is found, break out of the loop.
                    if (targetComponent != null) break;
                }

            // Return the found SkinnedMeshRenderer (or null if none is found).
            return targetComponent;
        }
    }
}