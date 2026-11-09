using System;
using static ZooWorld.Tests.SerializedFields;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Definitions;
using ZooWorld.Motion;
using ZooWorld.Movement;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class MovementStrategyTests
    {
        private const float Step = 0.25f;
        private const float Tolerance = 0.00001f;
        private readonly List<MovementProfile> _profiles = new List<MovementProfile>();

        [TearDown]
        public void TearDown()
        {
            foreach (MovementProfile profile in _profiles)
            {
                Object.DestroyImmediate(profile);
            }

            _profiles.Clear();
        }

        [Test, Description("Линейная стартует с заданной скоростью, сохраняя вертикальную")]
        public void LinearStartsAtConfiguredPlanarSpeedAndPreservesVerticalVelocity()
        {
            var motor = new Motor { Velocity = new Vector3(0, -2, 0) };
            using IMovementStrategy strategy = Profile<LinearMovementProfile>().CreateStrategy(Context(motor, new Vector3(5, 4, 0)));
            strategy.FixedTick(Step);
            AssertVector(motor.Velocity, new Vector3(3, -2, 0));
        }

        [Test, Description("Разворот до первого тика откладывает движение и меняет направление")]
        public void LinearSteeringBeforeFirstTickDefersMotionAndReplacesInitialDirection()
        {
            var motor = new Motor();
            using IMovementStrategy strategy = Profile<LinearMovementProfile>().CreateStrategy(Context(motor, Vector3.right));
            strategy.SteerInward(Vector3.back);
            AssertVector(motor.Velocity, Vector3.zero);
            strategy.FixedTick(Step);
            AssertVector(motor.Velocity, new Vector3(0, 0, -3));
        }

        [Test, Description("Линейная сохраняет отскок в холде, затем восстанавливает скорость")]
        public void LinearPreservesReboundDuringHoldThenRestoresConfiguredSpeed()
        {
            var motor = new Motor();
            using IMovementStrategy strategy = Profile<LinearMovementProfile>().CreateStrategy(Context(motor, Vector3.right));
            strategy.FixedTick(Step);
            motor.Velocity = new Vector3(-1.5f, 0, 1);
            strategy.FixedTick(Step);
            AssertVector(motor.Velocity, new Vector3(-1.5f, 0, 1));
            strategy.FixedTick(Step);
            Vector3 reboundDirection = new Vector3(-1.5f, 0, 1).normalized;
            float restored = new Vector3(motor.Velocity.x, 0, motor.Velocity.z).magnitude;
            Assert.That(restored, Is.GreaterThan(new Vector3(-1.5f, 0, 1).magnitude));
            Assert.That(restored, Is.LessThan(3));
            for (var i = 0; i < 6; i++)
            {
                strategy.FixedTick(Step);
            }

            AssertVector(motor.Velocity, reboundDirection * 3);
        }

        [Test, Description("Разворот перенаправляет отскок внутрь без потери скорости")]
        public void LinearSteeringRedirectsReboundInwardWithoutErasingItsSpeed()
        {
            var motor = new Motor();
            using IMovementStrategy strategy = Profile<LinearMovementProfile>().CreateStrategy(Context(motor, Vector3.right));
            strategy.FixedTick(Step);
            motor.Velocity = new Vector3(2, 0, 0.5f);
            strategy.FixedTick(Step);
            strategy.SteerInward(Vector3.left);
            Vector3 planar = new Vector3(motor.Velocity.x, 0, motor.Velocity.z);
            Assert.That(Vector3.Distance(planar.normalized, Vector3.left), Is.LessThan(Tolerance));
            Assert.That(planar.magnitude, Is.EqualTo(new Vector3(2, 0, 0.5f).magnitude).Within(Tolerance));
        }

        [Test, Description("Разворот при движении внутрь не трогает физическую скорость")]
        public void LinearSteeringWhileMovingInwardLeavesPhysicalVelocityUntouched()
        {
            var motor = new Motor();
            using IMovementStrategy strategy = Profile<LinearMovementProfile>().CreateStrategy(Context(motor, Vector3.right));
            strategy.FixedTick(Step);
            strategy.SteerInward(Vector3.right);
            AssertVector(motor.Velocity, new Vector3(3, 0, 0));
            strategy.SteerInward(Vector3.right + Vector3.forward * 0.2f);
            Assert.That(motor.Velocity.x, Is.GreaterThan(0));
        }

        [Test, Description("Общий линейный профиль даёт независимые направление и старт")]
        public void SharedLinearProfileCreatesIndependentDirectionAndStartState()
        {
            var profile = Profile<LinearMovementProfile>();
            string serializedBefore = EditorJsonUtility.ToJson(profile);
            var first = new Motor();
            var second = new Motor();
            using IMovementStrategy a = profile.CreateStrategy(Context(first, Vector3.right));
            using IMovementStrategy b = profile.CreateStrategy(Context(second, Vector3.forward));
            Assert.That(a, Is.Not.SameAs(b));
            a.FixedTick(Step);
            b.FixedTick(Step);
            a.SteerInward(Vector3.left);
            AssertVector(first.Velocity, Vector3.left * 3);
            AssertVector(second.Velocity, Vector3.forward * 3);
            Assert.That(EditorJsonUtility.ToJson(profile), Is.EqualTo(serializedBefore));
        }

        [Test, Description("Прыжок ждёт, затем стартует с независимо вычисленной баллистикой")]
        public void JumpWaitsThenLaunchesWithIndependentlyCalculatedBallisticVelocity()
        {
            var motor = new Motor();
            using IMovementStrategy strategy = Profile<JumpMovementProfile>().CreateStrategy(Context(motor, Vector3.right));
            for (var i = 0; i < 3; i++)
            {
                strategy.FixedTick(Step);
            }

            AssertVector(motor.Velocity, Vector3.zero);
            strategy.FixedTick(Step);
            AssertVector(motor.Velocity, new Vector3(2, 4, 0));
        }

        [Test, Description("Общий профиль прыжка даёт независимые таймеры, направления и фазу")]
        public void SharedJumpProfileHasIndependentTimersDirectionsAndPhase()
        {
            var profile = Profile<JumpMovementProfile>();
            string serializedBefore = EditorJsonUtility.ToJson(profile);
            var first = new Motor();
            var second = new Motor();
            using IMovementStrategy a = profile.CreateStrategy(Context(first, Vector3.right));
            using IMovementStrategy b = profile.CreateStrategy(Context(second, Vector3.forward));
            Assert.That(a, Is.Not.SameAs(b));
            for (var i = 0; i < 3; i++)
            {
                a.FixedTick(Step);
            }

            b.FixedTick(Step);
            a.FixedTick(Step);
            AssertVector(first.Velocity, new Vector3(2, 4, 0));
            AssertVector(second.Velocity, Vector3.zero);
            first.IsGrounded = false;
            first.Velocity = new Vector3(-1, 2, 0.5f);
            a.FixedTick(Step);
            AssertVector(first.Velocity, new Vector3(-1, 2, 0.5f));
            a.SteerInward(Vector3.back);
            for (var i = 0; i < 3; i++)
            {
                b.FixedTick(Step);
            }

            AssertVector(first.Velocity, new Vector3(0, 2, -new Vector3(-1, 0, 0.5f).magnitude));
            AssertVector(second.Velocity, new Vector3(0, 4, 2));
            Assert.That(EditorJsonUtility.ToJson(profile), Is.EqualTo(serializedBefore));
        }

        [Test, Description("Прыжок не стартует в воздухе, пауза перезапускается после приземления")]
        public void JumpDoesNotLaunchInAirAndWaitRestartsAfterObservedLanding()
        {
            var motor = new Motor { IsGrounded = false };
            using IMovementStrategy strategy = Profile<JumpMovementProfile>().CreateStrategy(Context(motor, Vector3.right));
            strategy.FixedTick(10);
            AssertVector(motor.Velocity, Vector3.zero);
            motor.IsGrounded = true;
            for (var i = 0; i < 4; i++)
            {
                strategy.FixedTick(Step);
            }

            AssertVector(motor.Velocity, new Vector3(2, 4, 0));
            motor.IsGrounded = false;
            motor.Velocity = new Vector3(-1, -2, 0);
            strategy.FixedTick(10);
            AssertVector(motor.Velocity, new Vector3(-1, -2, 0));
            motor.IsGrounded = true;
            motor.Velocity = Vector3.zero;
            strategy.FixedTick(Step);
            for (var i = 0; i < 3; i++)
            {
                strategy.FixedTick(Step);
            }

            AssertVector(motor.Velocity, Vector3.zero);
            strategy.FixedTick(Step);
            AssertVector(motor.Velocity, new Vector3(2, 4, 0));
        }

        [Test, Description("Разворот до прыжка меняет направление только этой активации")]
        public void SteeringBeforeJumpChangesOnlyThisActivationsLaunchDirection()
        {
            var motor = new Motor();
            using IMovementStrategy strategy = Profile<JumpMovementProfile>().CreateStrategy(Context(motor, Vector3.right));
            strategy.SteerInward(Vector3.back);
            AssertVector(motor.Velocity, Vector3.zero);
            for (var i = 0; i < 4; i++)
            {
                strategy.FixedTick(Step);
            }

            AssertVector(motor.Velocity, new Vector3(0, 4, -2));
        }

        [Test, Description("Новая стратегия на моторе не наследует старые таймеры и состояние")]
        public void FreshStrategyOnSameMotorDoesNotInheritOldTimerDirectionOrStoppedState()
        {
            var motor = new Motor();
            var profile = Profile<JumpMovementProfile>();
            IMovementStrategy old = profile.CreateStrategy(Context(motor, Vector3.right));
            for (var i = 0; i < 3; i++)
            {
                old.FixedTick(Step);
            }

            old.Dispose();
            using IMovementStrategy fresh = profile.CreateStrategy(Context(motor, Vector3.forward));
            fresh.FixedTick(Step);
            AssertVector(motor.Velocity, Vector3.zero);
            for (var i = 0; i < 3; i++)
            {
                fresh.FixedTick(Step);
            }

            AssertVector(motor.Velocity, new Vector3(0, 4, 2));
            old.FixedTick(10);
            old.SteerInward(Vector3.left);
            old.Dispose();
            AssertVector(motor.Velocity, new Vector3(0, 4, 2));
            fresh.Dispose();
            using IMovementStrategy linear = Profile<LinearMovementProfile>().CreateStrategy(Context(motor, Vector3.left));
            linear.FixedTick(Step);
            AssertVector(motor.Velocity, new Vector3(-3, 0, 0));
        }

        [TestCase(false), TestCase(true)]
        [Description("Disposed-стратегии останавливают движение и игнорируют вызовы")]
        public void DisposedStrategiesStopMotionAndIgnoreFurtherCalls(bool jumping)
        {
            var motor = new Motor();
            MovementProfile profile = jumping ? Profile<JumpMovementProfile>() : Profile<LinearMovementProfile>();
            IMovementStrategy strategy = profile.CreateStrategy(Context(motor, Vector3.right));
            for (var i = 0; i < 4; i++)
            {
                strategy.FixedTick(Step);
            }

            Assert.That(motor.Velocity.sqrMagnitude, Is.GreaterThan(0));
            strategy.Dispose();
            AssertVector(motor.Velocity, Vector3.zero);
            motor.Velocity = new Vector3(7, 0, 0);
            strategy.FixedTick(10);
            strategy.SteerInward(Vector3.left);
            strategy.Dispose();
            AssertVector(motor.Velocity, new Vector3(7, 0, 0));
        }

        [TestCase("_speed", 0f), TestCase("_speed", -1f), TestCase("_speed", float.NaN)]
        [TestCase("_speed", float.PositiveInfinity)]
        [Description("Линейная стратегия отклоняет невалидные параметры")]
        public void LinearRejectsInvalidParameters(string field, float value)
        {
            var profile = Profile<LinearMovementProfile>();
            Set(profile, field, value);
            Assert.Throws<InvalidOperationException>(() => profile.CreateStrategy(Context(new Motor(), Vector3.right)));
        }

        [TestCase("_distance", 0f), TestCase("_height", -1f), TestCase("_waitInterval", 0f)]
        [TestCase("_distance", float.NaN), TestCase("_height", float.PositiveInfinity)]
        [TestCase("_waitInterval", float.NaN)]
        [Description("Стратегия прыжка отклоняет невалидные параметры")]
        public void JumpRejectsInvalidParameters(string field, float value)
        {
            var profile = Profile<JumpMovementProfile>();
            Set(profile, field, value);
            var error = Assert.Throws<InvalidOperationException>(() => profile.CreateStrategy(Context(new Motor(), Vector3.right)));
            Assert.That(error.Message, Does.Contain(field));
        }

        [Test, Description("Контекст отклоняет нулевой мотор, гравитацию и непланарное направление")]
        public void ContextRejectsMissingMotorInvalidGravityAndNonPlanarDirection()
        {
            Assert.Throws<ArgumentNullException>(() => new MovementContext(null, Vector3.right, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MovementContext(new Motor(), Vector3.right, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MovementContext(new Motor(), Vector3.right, float.NaN));
            Assert.Throws<ArgumentException>(() => Context(new Motor(), Vector3.up));
            Assert.Throws<ArgumentException>(() => Context(new Motor(), new Vector3(float.NaN, 0, 0)));
        }

        private T Profile<T>() where T : MovementProfile
        {
            var profile = ScriptableObject.CreateInstance<T>();
            _profiles.Add(profile);
            return profile;
        }


        private static MovementContext Context(Motor motor, Vector3 direction) => new MovementContext(motor, direction, 8);

        private static void AssertVector(Vector3 actual, Vector3 expected)
        {
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(Tolerance), $"Expected {expected}, actual {actual}");
        }

        private sealed class Motor : IAnimalMotor
        {
            public Vector3 Position => Vector3.zero;
            public Vector3 Velocity { get; set; }
            public bool IsGrounded { get; set; } = true;
        }
    }
}
