using System.Collections.Generic;
using NUnit.Framework;
using Runtime.Domain.Tracing;
using Runtime.Services.Tracing;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class StrokeProgressTrackerTests
    {
        [Test]
        public void ForwardMovementAdvancesProgress()
        {
            StrokeProgressTracker tracker = CreateLineTracker();

            Assert.That(
                tracker.BeginPointer(Vector2.zero).Status,
                Is.EqualTo(TraceSampleStatus.Reacquired));

            TraceSampleResult result = tracker.Sample(new Vector2(0.2f, 0f));

            Assert.That(result.Status, Is.EqualTo(TraceSampleStatus.Advanced));
            Assert.That(tracker.ProgressDistance, Is.EqualTo(0.2f).Within(0.001f));
        }

        [Test]
        public void OutsideCorridorPausesWithoutResettingProgress()
        {
            StrokeProgressTracker tracker = CreateLineTracker();
            tracker.BeginPointer(Vector2.zero);
            tracker.Sample(new Vector2(0.2f, 0f));

            TraceSampleResult result = tracker.Sample(new Vector2(0.25f, 0.2f));

            Assert.That(
                result.Status,
                Is.EqualTo(TraceSampleStatus.PausedOutsideCorridor));
            Assert.That(tracker.ProgressDistance, Is.EqualTo(0.2f).Within(0.001f));
        }

        [Test]
        public void ReverseMovementPausesWithoutRollingBackProgress()
        {
            StrokeProgressTracker tracker = CreateLineTracker();
            tracker.BeginPointer(Vector2.zero);
            tracker.Sample(new Vector2(0.2f, 0f));

            TraceSampleResult result = tracker.Sample(new Vector2(0.14f, 0f));

            Assert.That(result.Status, Is.EqualTo(TraceSampleStatus.PausedReverse));
            Assert.That(tracker.ProgressDistance, Is.EqualTo(0.2f).Within(0.001f));
        }

        [Test]
        public void ReacquireBehindFrontCanContinueForward()
        {
            StrokeProgressTracker tracker = CreateLineTracker();
            tracker.BeginPointer(Vector2.zero);
            tracker.Sample(new Vector2(0.2f, 0f));
            tracker.Sample(new Vector2(0.2f, 0.2f));

            Assert.That(
                tracker.Sample(new Vector2(0.18f, 0f)).Status,
                Is.EqualTo(TraceSampleStatus.Reacquired));

            TraceSampleResult result = tracker.Sample(new Vector2(0.25f, 0f));

            Assert.That(result.Status, Is.EqualTo(TraceSampleStatus.Advanced));
            Assert.That(tracker.ProgressDistance, Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void PointerCannotStartAheadOfCurrentFront()
        {
            StrokeProgressTracker tracker = CreateLineTracker();

            TraceSampleResult result = tracker.BeginPointer(new Vector2(0.2f, 0f));

            Assert.That(result.Status, Is.EqualTo(TraceSampleStatus.PausedAhead));
            Assert.That(tracker.ProgressDistance, Is.Zero);
        }

        [Test]
        public void InputTolerancePaddingAcceptsSmallMissOutsideBaseCorridor()
        {
            StrokeProgressTracker tracker = CreateLineTracker(
                corridorWidth: 0.1f,
                inputTolerancePadding: 0.025f);

            TraceSampleResult result =
                tracker.BeginPointer(new Vector2(0f, 0.07f));

            Assert.That(result.Status, Is.EqualTo(TraceSampleStatus.Reacquired));
        }

        [Test]
        public void EndInsetBecomesTheCompletionTarget()
        {
            StrokeProgressTracker tracker = CreateLineTracker(endInset: 0.1f);
            tracker.BeginPointer(Vector2.zero);
            tracker.Sample(new Vector2(0.2f, 0f));
            tracker.Sample(new Vector2(0.4f, 0f));
            tracker.Sample(new Vector2(0.6f, 0f));
            tracker.Sample(new Vector2(0.8f, 0f));

            TraceSampleResult result =
                tracker.Sample(new Vector2(0.9f, 0f));

            Assert.That(result.Status, Is.EqualTo(TraceSampleStatus.Completed));
            Assert.That(tracker.TargetDistance, Is.EqualTo(0.9f).Within(0.001f));
        }

        [Test]
        public void StartInsetDefinesMascotAndInitialProgressPosition()
        {
            StrokeProgressTracker tracker = CreateLineTracker(startInset: 0.1f);

            TraceSampleResult result =
                tracker.BeginPointer(new Vector2(0.1f, 0f));

            Assert.That(result.Status, Is.EqualTo(TraceSampleStatus.Reacquired));
            Assert.That(result.ProgressDistance, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(result.Position.x, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(tracker.NormalizedProgress, Is.Zero.Within(0.001f));
        }

        [Test]
        public void ClosedPathDoesNotJumpToEndAtSharedSeam()
        {
            TraceStrokeDefinition stroke = new(
                "closed",
                true,
                new List<TraceBezierKnot>
                {
                    new(Vector2.zero, Vector2.zero, Vector2.zero),
                    new(Vector2.right, Vector2.zero, Vector2.zero)
                });
            stroke.SetBakedData(
                new List<Vector2>
                {
                    Vector2.zero,
                    Vector2.right,
                    Vector2.one,
                    Vector2.up,
                    Vector2.zero
                },
                new List<float> { 0f, 1f, 2f, 3f, 4f });

            StrokeProgressTracker tracker = new(
                stroke,
                0.2f,
                0.05f,
                0.25f,
                0.001f,
                0.01f);

            TraceSampleResult result = tracker.BeginPointer(Vector2.zero);

            Assert.That(result.Status, Is.EqualTo(TraceSampleStatus.Reacquired));
            Assert.That(tracker.ProgressDistance, Is.Zero);
            Assert.That(tracker.IsCompleted, Is.False);
        }

        private static StrokeProgressTracker CreateLineTracker(
            float corridorWidth = 0.1f,
            float inputTolerancePadding = 0f,
            float endInset = 0f,
            float startInset = 0f)
        {
            TraceStrokeDefinition stroke = new(
                "line",
                false,
                new List<TraceBezierKnot>
                {
                    new(Vector2.zero, Vector2.zero, Vector2.zero),
                    new(Vector2.right, Vector2.zero, Vector2.zero)
                });
            stroke.SetBakedData(
                new List<Vector2> { Vector2.zero, Vector2.right },
                new List<float> { 0f, 1f });

            return new StrokeProgressTracker(
                stroke,
                corridorWidth,
                0.05f,
                0.3f,
                0.005f,
                0.01f,
                inputTolerancePadding,
                endInset,
                startInset);
        }
    }
}
