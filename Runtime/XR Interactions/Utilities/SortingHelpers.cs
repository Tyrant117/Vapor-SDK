﻿using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Burst;
using VaporXR.Interaction;
using VaporXR.Interaction;

namespace VaporXR.Utilities
{
    /// <summary>
    /// Defines a method for calculating the distance between an VXRBaseInteractor and an IXRInteractable.
    /// </summary>
    public interface IInteractorDistanceEvaluator
    {
        /// <summary>
        /// Evaluates the distance between an interactor and an interactable.
        /// </summary>
        /// <param name="attachPoint">The IAttachPoint to use in the calculation.</param>
        /// <param name="interactable">The IXRInteractable to evaluate the distance to.</param>
        /// <returns>The calculated distance as a float.</returns>
        float EvaluateDistance(IAttachPoint attachPoint, Interactable interactable);
    }

    /// <summary>
    /// Utility functions related to sorting.
    /// </summary>
    public static class SortingHelpers
    {
        /// <summary>
        /// Reusable mapping of Interactables to their distance squared from an Interactor (used for sort).
        /// </summary>
        private static readonly Dictionary<Interactable, float> s_InteractableDistanceSqrMap = new Dictionary<Interactable, float>();

        /// <summary>
        /// Used to avoid GC Alloc that would happen if using <see cref="InteractableDistanceComparison"/> directly
        /// as argument to <see cref="List{T}.Sort(Comparison{T})"/>.
        /// </summary>
        private static readonly Comparison<Interactable> s_InteractableDistanceComparison = InteractableDistanceComparison;

        /// <summary>
        /// Evaluates the squared distance between the attachment points of an interactor and an interactable.
        /// </summary>
        public static readonly IInteractorDistanceEvaluator squareDistanceAttachPointEvaluator = new SquareDistanceAttachPointEvaluator();

        /// <summary>
        /// Evaluates the distance based on the interactable's defined method of calculating distance to an interactor.
        /// </summary>
        public static readonly IInteractorDistanceEvaluator interactableBasedEvaluator = new InteractableBasedEvaluator();

        /// <summary>
        /// Evaluates the squared distance to the closest point on the interactable's collider from the interactor's attachment point.
        /// </summary>
        public static readonly IInteractorDistanceEvaluator closestPointOnColliderEvaluator = new ClosestPointOnColliderEvaluator();

        /// <summary>
        /// Sorts a list using an optimized bubble sort algorithm. Useful for arrays to avoid GC Alloc.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list. Must be a value type.</typeparam>
        /// <param name="hits">The list of items to be sorted.</param>
        /// <param name="comparer">The comparer to use for comparing elements.</param>
        public static void Sort<T>(IList<T> hits, IComparer<T> comparer) where T : struct
            => Sort(hits, comparer, hits.Count);

        /// <summary>
        /// Sorts a part of a list using an optimized bubble sort algorithm. Useful for arrays to avoid GC Alloc.
        /// </summary>
        /// <remarks>
        /// This method implements an optimized version of the bubble sort algorithm, which
        /// includes reducing the number of elements to compare in each pass and an early
        /// exit if the list is already sorted. This makes it more efficient for nearly sorted
        /// lists but retains the average and worst-case time complexity of O(n^2).
        /// </remarks>
        /// <typeparam name="T">The type of elements in the list. Must be a value type.</typeparam>
        /// <param name="hits">The list of items to be sorted.</param>
        /// <param name="comparer">The comparer to use for comparing elements.</param>
        /// <param name="count">The number of elements from the start of the list to be sorted.</param>
        public static void Sort<T>(IList<T> hits, IComparer<T> comparer, int count) where T : struct
        {
            if (count <= 1)
                return;

            for (var last = count - 1; last > 0; last--)
            {
                bool swapped = false;
                for (var i = 1; i <= last; i++)
                {
                    var result = comparer.Compare(hits[i - 1], hits[i]);
                    if (result > 0)
                    {
                        (hits[i - 1], hits[i]) = (hits[i], hits[i - 1]);
                        swapped = true;
                    }
                }

                // Exit early if no swaps occurred in this pass.
                if (!swapped)
                    break;
            }
        }

        /// <summary>
        /// Sorts the Interactables in <paramref name="unsortedTargets"/> by distance to the <paramref name="attachPoint"/>,
        /// storing the ordered result in <paramref name="results"/>.
        /// </summary>
        /// <param name="attachPoint">The Interactor to calculate distance against.</param>
        /// <param name="unsortedTargets">The read only list of Interactables to sort. This list is not modified.</param>
        /// <param name="results">The results list to populate with the sorted results.</param>
        /// <remarks>
        /// Clears <paramref name="results"/> before adding to it.
        /// This method is not thread safe.
        /// </remarks>
        public static void SortByDistanceToInteractor(IAttachPoint attachPoint, List<Interactable> unsortedTargets, List<Interactable> results)
        {
            SortByDistanceToInteractor(attachPoint, unsortedTargets, results, interactableBasedEvaluator);
        }

        /// <summary>
        /// Sorts the Interactables in <paramref name="unsortedTargets"/> by distance to the <paramref name="interactor"/>,
        /// storing the ordered result in <paramref name="results"/>.
        /// </summary>
        /// <param name="interactor">The Interactor to calculate distance against.</param>
        /// <param name="unsortedTargets">The read only list of Interactables to sort. This list is not modified.</param>
        /// <param name="results">The results list to populate with the sorted results.</param>
        /// <param name="distanceEvaluator">Custom distance evaluator</param>
        /// <remarks>
        /// Clears <paramref name="results"/> before adding to it.
        /// This method is not thread safe.
        /// </remarks>
        public static void SortByDistanceToInteractor(IAttachPoint attachPoint, List<Interactable> unsortedTargets, List<Interactable> results, IInteractorDistanceEvaluator distanceEvaluator)
        {
            results.Clear();

            if (unsortedTargets.Count == 0)
                return;

            if (unsortedTargets.Count == 1)
            {
                results.Add(unsortedTargets[0]);
                return;
            }

            results.AddRange(unsortedTargets);

            s_InteractableDistanceSqrMap.Clear();

            foreach (var interactable in unsortedTargets)
                s_InteractableDistanceSqrMap[interactable] = distanceEvaluator.EvaluateDistance(attachPoint, interactable);

            results.Sort(s_InteractableDistanceComparison);
        }

        /// <summary>
        /// Sorts the Interactables in <paramref name="interactablesToSort"/> by distance to the <paramref name="interactor"/> in place.
        /// </summary>
        /// <param name="interactor">The Interactor to calculate distance against.</param>
        /// <param name="interactablesToSort">The list of Interactables to sort. This list is will be sorted.</param>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        public static void SortByDistanceToInteractor(Interactor interactor, List<Interactable> interactablesToSort)
        {
            SortByDistanceToInteractor(interactor, interactablesToSort, interactableBasedEvaluator);
        }

        /// <summary>
        /// Sorts the Interactables in <paramref name="interactablesToSort"/> by distance to the <paramref name="interactor"/> in place.
        /// </summary>
        /// <param name="interactor">The Interactor to calculate distance against.</param>
        /// <param name="interactablesToSort">The list of Interactables to sort. This list is will be sorted.</param>
        /// <param name="distanceEvaluator">Custom distance evaluator</param>
        /// <remarks>
        /// This method is not thread safe.
        /// </remarks>
        public static void SortByDistanceToInteractor(Interactor interactor, List<Interactable> interactablesToSort, IInteractorDistanceEvaluator distanceEvaluator)
        {
            if (interactablesToSort.Count <= 1)
                return;

            s_InteractableDistanceSqrMap.Clear();

            foreach (var interactable in interactablesToSort)
            {
                s_InteractableDistanceSqrMap[interactable] = distanceEvaluator.EvaluateDistance(interactor, interactable);
            }

            interactablesToSort.Sort(s_InteractableDistanceComparison);
        }

        static int InteractableDistanceComparison(Interactable x, Interactable y)
        {
            var xDistance = s_InteractableDistanceSqrMap[x];
            var yDistance = s_InteractableDistanceSqrMap[y];
            return xDistance.CompareTo(yDistance);
        }

        /// <summary>
        /// Evaluator that determines the distance based on the interactable's own method of calculating distance to an interactor.
        /// </summary>
        class InteractableBasedEvaluator : IInteractorDistanceEvaluator
        {
            /// <summary>
            /// Evaluates the distance between an interactor and an interactable based on the interactable's defined method.
            /// </summary>
            /// <param name="attachPoint">The attach point to use in the distance calculation.</param>
            /// <param name="interactable">The interactable to which the distance is being calculated.</param>
            /// <returns>The square of the distance between the interactor and the interactable.</returns>
            public float EvaluateDistance(IAttachPoint attachPoint, Interactable interactable)
            {
                return interactable.GetDistanceSqrToInteractor(attachPoint);
            }
        }

        /// <summary>
        /// Evaluator that calculates the squared distance to the closest point on the interactable's collider from the interactor's attachment point.
        /// </summary>
        private class ClosestPointOnColliderEvaluator : IInteractorDistanceEvaluator
        {
            /// <summary>
            /// Evaluates the distance between an interactor and the closest point on an interactable's collider.
            /// </summary>
            /// <param name="attachPoint">The attachPoint to use in the distance calculation.</param>
            /// <param name="interactable">The interactable to which the distance is being calculated.</param>
            /// <returns>The square of the distance between the interactor's attachment point and the closest point on the interactable's collider.</returns>
            public float EvaluateDistance(IAttachPoint attachPoint, Interactable interactable)
            {
                float3 interactorAttachPoint = attachPoint.GetAttachTransform(interactable).position;
                XRInteractableUtility.TryGetClosestPointOnCollider(interactable, interactorAttachPoint, out var distanceInfo);
                return distanceInfo.distanceSqr;
            }
        }


        /// <summary>
        /// Evaluator that calculates the squared distance between the attachment points of an interactor and an interactable.
        /// </summary>
        [BurstCompile]
        private class SquareDistanceAttachPointEvaluator : IInteractorDistanceEvaluator
        {
            /// <summary>
            /// Evaluates the squared distance between the attachment points of an interactor and an interactable.
            /// </summary>
            /// <param name="attachPoint">The attachPoint used in the calculation.</param>
            /// <param name="interactable">The interactable whose attachment point is used in the calculation.</param>
            /// <returns>The square of the distance between the attachment points of the interactor and the interactable.</returns>
            public float EvaluateDistance(IAttachPoint attachPoint, Interactable interactable)
            {
                float3 interactorAttachPoint = attachPoint.GetAttachTransform(interactable).position;
                float3 interactableAttachPoint = interactable.GetAttachTransform(attachPoint).position;
                return SqDistanceToInteractable(interactorAttachPoint, interactableAttachPoint);
            }

            /// <summary>
            /// Calculates the squared distance between two positions.
            /// </summary>
            /// <param name="attachPosition">The first position in the calculation.</param>
            /// <param name="interactablePosition">The second position in the calculation.</param>
            /// <returns>The square of the distance between the two positions.</returns>
            [BurstCompile]
            private static float SqDistanceToInteractable(in float3 attachPosition, in float3 interactablePosition)
            {
                return math.lengthsq(attachPosition - interactablePosition);
            }
        }
    }
}