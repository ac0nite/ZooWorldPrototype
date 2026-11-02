using UnityEngine;
using ZooWorld.Core;
using ZooWorld.Physics;

namespace ZooWorld.Movement
{
    public sealed class JumpMovementStrategy : IMovementStrategy
    {
        readonly IAnimalMotor motor;
        readonly float verticalSpeed;
        readonly float horizontalSpeed;
        readonly float waitInterval;
        Vector3 direction;
        float remainingWait;
        bool launched;
        bool leftGround;
        bool disposed;

        internal JumpMovementStrategy(MovementContext context, float distance, float height, float waitInterval)
        {
            motor = context.Motor;
            direction = context.InitialDirection;
            verticalSpeed = Mathf.Sqrt(2 * context.Gravity * height);
            horizontalSpeed = distance / (2 * verticalSpeed / context.Gravity);
            this.waitInterval = waitInterval;
            remainingWait = waitInterval;
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (disposed) return;
            if (launched)
            {
                if (!motor.IsGrounded) leftGround = true;
                else if (leftGround)
                {
                    launched = false;
                    leftGround = false;
                    remainingWait = waitInterval;
                    motor.Velocity = new Vector3(0, motor.Velocity.y, 0);
                }
                return;
            }
            if (!motor.IsGrounded) return;
            remainingWait -= fixedDeltaTime;
            if (remainingWait > 0) return;
            var velocity = direction * horizontalSpeed;
            velocity.y = verticalSpeed;
            motor.Velocity = velocity;
            launched = true;
        }

        public void SteerInward(Vector3 direction)
        {
            if (disposed) return;
            this.direction = MovementContext.NormalizeDirection(direction);
            if (!launched) return;
            var velocity = this.direction * horizontalSpeed;
            velocity.y = motor.Velocity.y;
            motor.Velocity = velocity;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            motor.Velocity = Vector3.zero;
        }
    }
}
